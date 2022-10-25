﻿using Mirror;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Insight
{
    public class ProcessSpawner : InsightModule
    {
        InsightServer server;
        InsightClient client;

        public bool LogAll = true;

        [Header("Network")]
        [Tooltip("NetworkAddress that spawned processes will use")]
        public string SpawnerNetworkAddress = "localhost";
        [Tooltip("Port that will be used by the NetworkManager in the spawned game")]
        public int StartingNetworkPort = 7777; //Default port of the NetworkManager.
        [Tooltip("Number of ports to allocate for MultiplexTransport")]
        public int allocatedPorts = 1; //How many transports do you use in MultiplexTransport.

        [Header("Paths")]
        [Tooltip("Example Mac filepath: /Users/yourName/Builds/")]
        public string EditorPath;
        [Tooltip("Overwrite if builds are not all in same directory.")]
        public string ProcessPath;
        public string ProcessName;
        private string PathResult;

        [Header("Threads")]
        public int MaximumProcesses = 5;
        public int HealthCheckPollRate = 5; //In Seconds
        bool registrationComplete;

        public RunningProcessContainer[] spawnerProcesses;
        private bool AbortRun = false;

        public override void Initialize(InsightServer server, ModuleManager manager)
        {
            if (AbortRun)
                return;
            this.server = server;
            RegisterHandlers();
        }

        public override void Initialize(InsightClient client, ModuleManager manager)
        {
            if (AbortRun)
                return;
            this.client = client;
            RegisterHandlers();
        }

        void Awake()
        {
            // Mac adjustments to make life easier
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (ProcessName.EndsWith(".exe"))
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - Switched file extension.");
                ProcessName = ProcessName.Replace(".exe", ".app");
            }

            if (!ProcessName.Contains("/Contents/MacOS/"))
            {
                ProcessName = ProcessName + "/Contents/MacOS/" + ProcessName;

                if (ProcessName.EndsWith(".app"))
                {
                    if (LogAll)
                        Debug.Log("[ProcessSpawner] - Auto adjust path for OSX");
                    ProcessName = ProcessName.Remove(ProcessName.Length - 4);
                }
            }

            // Mac prefers not to use dot slash for same directory filepath
            if (ProcessPath == "./")
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - ProcessPath changed");
                ProcessPath = "";
            }
#endif

#if UNITY_EDITOR
            ProcessPath = EditorPath;
#endif

            PathResult = System.IO.Path.Combine(ProcessPath, ProcessName);

            if (System.IO.File.Exists(PathResult))
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - Path exists! " + PathResult);
            }
            else
            {
                Debug.LogError("[ProcessSpawner] - Path does not exist. " + PathResult);
                AbortRun = true;
                return;
            }

            spawnerProcesses = new RunningProcessContainer[MaximumProcesses];
            for (int i = 0; i < spawnerProcesses.Length; i++)
            {
                spawnerProcesses[i] = new RunningProcessContainer();
            }

            InvokeRepeating("CheckSpawnedProcessHealth", HealthCheckPollRate, HealthCheckPollRate);
        }

        void FixedUpdate()
        {
            if (AbortRun)
                return;
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
            if (AbortRun)
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - Abort HandleRequestSpawnStart");
                return;
            }
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
            if (AbortRun)
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - Abort RegisterToMaster");
                return;
            }

            //Used only if acting as a ChildSpawner under a MasterServer
            if (client && !registrationComplete)
            {
                if (client.isConnected)
                {
                    if (LogAll)
                        Debug.Log("[ProcessSpawner] - Registering to Master");
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
            for (int i = 0; i < spawnerProcesses.Length; i++)
            {
                if (spawnerProcesses[i].process == null)
                {
                    continue;
                }

                if (spawnerProcesses[i].process.HasExited)
                {
                    if (LogAll)
                        Debug.Log("Removing process that has exited");
                    spawnerProcesses[i].process = null;
                    spawnerProcesses[i].pid = 0;
                    spawnerProcesses[i].uniqueID = "";
                    return;
                }
            }

            //If running as a remote spawner report the current running process count back to the MasterSpawner
            if (client != null)
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

            for (int i = 0; i < spawnerProcesses.Length; i++)
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

                if (LogAll)
                    Debug.Log("[ProcessSpawner] - UniqueID was not provided for spawn. Generating: " + spawnProperties.UniqueID);
            }

            Process p = new Process();
            // Put the process path and the process name together. We use
            // Path.Combine for this, which will include correct directory
            // seperators on the OS we're running on (ie. C:\Game\ or /Game/ )
            p.StartInfo.FileName = System.IO.Path.Combine(ProcessPath, ProcessName);
            //Args to pass: Port, Scene, UniqueID...
            p.StartInfo.Arguments = ArgsString() +
                " -NetworkAddress " + SpawnerNetworkAddress +
                " -NetworkPort " + (StartingNetworkPort + thisPort * allocatedPorts) +
                " -SceneName " + spawnProperties.SceneName +
                " -UniqueID " + spawnProperties.UniqueID; //What to do if the UniqueID or any other value is null??

            if (System.IO.File.Exists(p.StartInfo.FileName))
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner] - Path exists!" + p.StartInfo.FileName);
            }
            else
            {
                Debug.LogError("[ProcessSpawner] - Path does not exist. " + p.StartInfo.FileName);
                return false;
            }

            if (p.Start())
            {
                if (LogAll)
                    Debug.Log("[ProcessSpawner]: spawning: " + p.StartInfo.FileName + "; args=" + p.StartInfo.Arguments);
            }
            else
            {
                Debug.LogError("[ProcessSpawner] - Process Creation Failed.");
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
            for (int i = 0; i < spawnerProcesses.Length; i++)
            {
                if (spawnerProcesses[i].process == null)
                {
                    return i;
                }
            }

            //if (LogAll) important to display, but do not flag as error
            Debug.LogWarning("[ProcessSpawner] - Maximum Process Count Reached");
            return -1;
        }

        int GetRunningProcessCount()
        {
            int counter = 0;
            for (int i = 0; i < spawnerProcesses.Length; i++)
            {
                if (spawnerProcesses[i].process != null)
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
