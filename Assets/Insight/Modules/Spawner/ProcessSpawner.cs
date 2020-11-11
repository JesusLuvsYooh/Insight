﻿using Mirror;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Insight
{
    public class ProcessSpawner : InsightModule
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(ProcessSpawner));

        [HideInInspector] public InsightServer server;
        [HideInInspector] public InsightClient client;

        [Header("Network")]
        [Tooltip("NetworkAddress that spawned processes will use")]
        public string SpawnerNetworkAddress = "localhost";
        [Tooltip("Port that will be used by the NetworkManager in the spawned game")]
        public int StartingNetworkPort = 7777; //Default port of the NetworkManager.

        [Header("Paths")]
        public string EditorPath;
        public string ProcessPath;
        public string ProcessName;

        [Header("Threads")]
        public int MaximumProcesses = 5;
        public int HealthCheckPollRate = 5; //In Seconds
        bool registrationComplete;

        public RunningProcessContainer[] spawnerProcesses;

        public override void Initialize(InsightServer server, ModuleManager manager)
        {
            this.server = server;
            RegisterHandlers();
        }

        public override void Initialize(InsightClient client, ModuleManager manager)
        {
            this.client = client;
            RegisterHandlers();
        }

        void Awake()
        {
#if UNITY_EDITOR
            ProcessPath = EditorPath;
#endif

            for (int i = 0; i < spawnerProcesses.Length; i++)
            {
                spawnerProcesses[i] = new RunningProcessContainer();
            }

            InvokeRepeating("CheckSpawnedProcessHealth", HealthCheckPollRate, HealthCheckPollRate);
        }

        void FixedUpdate()
        {
            RegisterToMaster();
        }

        void RegisterHandlers()
        {
            if (client)
            {
                client.RegisterHandler<RequestSpawnStartMsg>(HandleRequestSpawnStart);
                client.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
            }
            if (server)
            {
                server.RegisterHandler<RequestSpawnStartMsg>(HandleRequestSpawnStart);
                server.RegisterHandler<KillSpawnMsg>(HandleKillSpawn);
            }
        }

        void HandleRequestSpawnStart(InsightNetworkMessage netMsg)
        {
            RequestSpawnStartMsg message = netMsg.ReadMessage<RequestSpawnStartMsg>();

            //Try to start the new process
            if (!InternalStartNewProcess(message))
            {
                //Temporary stop replying if spawning fails.
                //netMsg.Reply((short)MsgId.Error, new ErrorMsg() { Text = "[ProcessSpawner] - Spawn failed" });
            }

            if (netMsg.callbackId != 0)
            {
                netMsg.Reply(new RequestSpawnStartMsg()
                {
                    //If the UniqueID is not provided in the MSG it is generated by the spawner
                    //Should that be passed back and used here?

                    SceneName = message.SceneName,
                    NetworkAddress = SpawnerNetworkAddress,
                    UniqueID = message.UniqueID
                });
            }
        }

        void RegisterToMaster()
        {
            //Used only if acting as a ChildSpawner under a MasterServer
            if (client && !registrationComplete)
            {
                if (client.isConnected)
                {
                    logger.LogWarning("[ProcessSpawner] - Registering to Master");
                    client.Send(new RegisterSpawnerMsg()
                    {
                        UniqueID = "", //Can provide a password to authenticate to the master as a trusted spawner
                        MaxThreads = MaximumProcesses
                    });
                    registrationComplete = true;
                }
            }
        }

        void CheckSpawnedProcessHealth()
        {
            //Check to see if a previously running process exited without warning
            for(int i = 0; i < spawnerProcesses.Length; i++)
            {
                if(spawnerProcesses[i].process == null)
                {
                    continue;
                }

                if (spawnerProcesses[i].process.HasExited)
                {
                    logger.Log("Removing process that has exited");
                    spawnerProcesses[i].process = null;
                    spawnerProcesses[i].pid = 0;
                    spawnerProcesses[i].uniqueID = "";
                    return;
                }
            }

            //If running as a remote spawner report the current running process count back to the MasterSpawner
            if(client != null)
            {
                client.Send(new SpawnerStatusMsg()
                {
                    CurrentThreads = GetRunningProcessCount(),
                });
            }
        }

        void HandleKillSpawn(InsightNetworkMessage netMsg)
        {
            KillSpawnMsg message = netMsg.ReadMessage<KillSpawnMsg>();

            for(int i = 0; i < spawnerProcesses.Length; i++)
            {
                if (spawnerProcesses[i].uniqueID.Equals(message.UniqueID))
                {
                    spawnerProcesses[i].process.Kill();
                    spawnerProcesses[i].process = null;
                    spawnerProcesses[i].pid = 0;
                    spawnerProcesses[i].uniqueID = "";
                    break;
                }
            }
        }

        bool InternalStartNewProcess(RequestSpawnStartMsg spawnProperties)
        {
            int thisPort = GetPort();
            if (thisPort == -1)
            {
                return false;
            }

            //If a UniqueID was not provided add one for GameResitration
            if (string.IsNullOrEmpty(spawnProperties.UniqueID))
            {
                spawnProperties.UniqueID = Guid.NewGuid().ToString();

                logger.LogWarning("[ProcessSpawner] - UniqueID was not provided for spawn. Generating: " + spawnProperties.UniqueID);
            }

            Process p = new Process();
            // Put the process path and the process name together. We use
            // Path.Combine for this, which will include correct directory
            // seperators on the OS we're running on (ie. C:\Game\ or /Game/ )
            p.StartInfo.FileName = System.IO.Path.Combine(ProcessPath, ProcessName);
            //Args to pass: Port, Scene, UniqueID...
            p.StartInfo.Arguments = ArgsString() +
                " -NetworkAddress " + SpawnerNetworkAddress +
                " -NetworkPort " + (StartingNetworkPort + thisPort) + 
                " -SceneName " + spawnProperties.SceneName +
                " -UniqueID " + spawnProperties.UniqueID; //What to do if the UniqueID or any other value is null??

            if (p.Start())
            {
                logger.Log("[ProcessSpawner]: spawning: " + p.StartInfo.FileName + "; args=" + p.StartInfo.Arguments);
            }
            else
            {
                logger.LogError("[ProcessSpawner] - Process Creation Failed.");
                return false;
            }

            //Update the collection with newly started process
            spawnerProcesses[thisPort] = new RunningProcessContainer() { process = p, pid = p.Id, uniqueID = spawnProperties.UniqueID };

            //If registered to a master. Notify it of the current thread utilization
            if (client != null)
            {
                client.Send(new SpawnerStatusMsg() { CurrentThreads = GetRunningProcessCount() });
            }

            return true;
        }

        static string ArgsString()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            return args != null ? string.Join(" ", args.Skip(1).ToArray()) : "";
        }

        int GetPort()
        {
            for(int i = 0; i < spawnerProcesses.Length; i++)
            {
                if(spawnerProcesses[i].process == null)   
                {
                    return i;
                }
            }

            logger.LogError("[ProcessSpawner] - Maximum Process Count Reached");
            return -1;
        }

        int GetRunningProcessCount()
        {
            int counter = 0;
            for(int i = 0; i < spawnerProcesses.Length; i++)
            {
                if(spawnerProcesses[i].process != null)   
                {
                    counter++;
                }
            }
            return counter;
        }
    }

    [Serializable]
    public class RunningProcessContainer
    {
        public Process process;
        public int pid;
        public string uniqueID;
    }
}
