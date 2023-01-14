using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Insight
{
    public class GameRegistration : InsightModule
    {
        InsightClient client;
        Transport networkManagerTransport;

        //Pulled from command line arguments
        public string GameScene;
        public string NetworkAddress;
        public ushort NetworkPort;
        public string UniqueID;

        //These should probably be synced from the NetworkManager
        public int MaxPlayers;
        public int CurrentPlayers;

        //Current insight flow, is allowing players to send info through master server to game server. (rare use case)
        //You either want to disable that, or add vigorous checks, here we will do a verified scenes string name check as an example
        [Scene] public string[] verifiedScenes;
        private bool AbortRun = false;

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            client = insight;
            client.transport.OnClientConnected += SendGameRegistrationToGameManager;

            //NetworkManager.singleton.OnServerDisconnect = OnServerDisconnect;
            //NetworkManager.singleton.OnServerAddPlayer = OnServerAddPlayer;

#if MIRROR_71_0_OR_NEWER
             networkManagerTransport = Transport.active;
#else
            networkManagerTransport = Transport.activeTransport;

#endif

            //networkManagerTransport.OnServerDisconnected = OnServerDisconnect;
            //networkManagerTransport.OnServerConnected = OnServerCconnected;

            RegisterHandlers();
            GatherCmdArgs();

            if (AbortRun)
            {
                Debug.LogError("Registration aborted.");
                return;
            }
            //Changed to use a Number of Players changed check, rather than timed loop.
            //InvokeRepeating("SendGameStatusToGameManager", 30f, 30f);
        }

        //void OnServerDisconnect(int value)
        //{
        //    Debug.Log("[Game Registration] OnServerDisconnect." + value);
        //    SendGameStatusToGameManager();
        //}

        //void OnServerCconnected(int value)
        //{
        //    Debug.Log("[Game Registration] OnServerConnected." + value);
        //    SendGameStatusToGameManager();
        //}

        int _theVariable;

        public int TheVariable
        {
            get { return _theVariable; }
            set
            {
                _theVariable = value;
                if (_theVariable == 1)
                {
                    //Do stuff here.
                }
            }
        }

        void RegisterHandlers() { }

        void GatherCmdArgs()
        {
            InsightArgs args = new InsightArgs();
            if (args.IsProvided("-NetworkAddress"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - NetworkAddress: " + args.NetworkAddress);
                NetworkAddress = args.NetworkAddress;

                NetworkManager.singleton.networkAddress = NetworkAddress;
            }

            if (args.IsProvided("-NetworkPort"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - NetworkPort: " + args.NetworkPort);
                NetworkPort = (ushort)args.NetworkPort;

                if (networkManagerTransport is MultiplexTransport)
                {
                    ushort startPort = NetworkPort;
                    foreach (Transport transport in (networkManagerTransport as MultiplexTransport).transports)
                    {
                        SetPort(transport, startPort++);
                    }
                }
                else
                {
                    SetPort(networkManagerTransport, NetworkPort);
                }
            }

            if (args.IsProvided("-SceneName"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - SceneName: " + args.SceneName);

                // if no scenes registered for client control verification, presume no scene switch
                if (verifiedScenes.Length > 0)
                {
                    foreach (string _sceneName in verifiedScenes)
                    {
                        if (_sceneName == args.SceneName)
                        {
                            if (InsightClient.instance.NoisyLogs)
                                Debug.Log("[Args] - Scene found/verified.");
                            GameScene = args.SceneName;
                            SceneManager.LoadScene(GameScene);
                        }
                        else
                        {
                            Debug.LogWarning("[Args] - Scene not found/verified.");
                            // if no matches, you could go to a verified scene, or stay on current scene
                            //GameScene = verifiedScenes[0];
                        }
                    }
                }
                else
                {
                    //What to do if no verified scenes added in inspector, carry on or..Debug.LogWarning("[Args] - No scenes in verified array.");
                    //AbortRun = true;

                    // or do default behaviour of older insight, accept client sent scene string and try to load
                    GameScene = args.SceneName;
                    //SceneManager.LoadScene(GameScene);
                }
                NetworkManager.singleton.onlineScene = GameScene;
            }

            if (args.IsProvided("-UniqueID"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - UniqueID: " + args.UniqueID);
                UniqueID = args.UniqueID;
            }

            MaxPlayers = NetworkManager.singleton.maxConnections;

            if (AbortRun == true)
            {
                Debug.LogWarning("[Args] - Aborting Setup.");
                NetworkManager.singleton.StopServer();
                //Application.Quit();
            }
            else
            {
                //Start NetworkManager
                NetworkManager.singleton.StartServer();
            }
        }

        void SetPort(Transport transport, ushort port)
        {
            if (transport.GetType().GetField("port") != null)
            {
                transport.GetType().GetField("port").SetValue(transport, port);
            }
            else if (transport.GetType().GetField("Port") != null)
            {
                transport.GetType().GetField("Port").SetValue(transport, port);
            }
            else if (transport.GetType().GetField("CommunicationPort") != null)
            {//For Ignorance
                transport.GetType().GetField("CommunicationPort").SetValue(transport, port);
            }
        }

        void SendGameRegistrationToGameManager()
        {
            if (InsightClient.instance.NoisyLogs)
                Debug.Log("[GameRegistration] - registering with master");
            client.Send(new RegisterGameMsg()
            {
                NetworkAddress = NetworkAddress,
                NetworkPort = NetworkPort,
                UniqueID = UniqueID,
                SceneName = GameScene,
                MaxPlayers = MaxPlayers,
                CurrentPlayers = CurrentPlayers
            });
        }

        void SendGameStatusToGameManager()
        {
            //Update with current values from NetworkManager:
            //CurrentPlayers = NetworkManager.singleton.numPlayers;
            // Chosing to use connections count, as it includes any connections joining, leaving, or stuck in limbo.
            CurrentPlayers = NetworkServer.connections.Count;

            if (InsightClient.instance.NoisyLogs)
                Debug.Log("[GameRegistration] - status update");
            client.Send(new GameStatusMsg()
            {
                UniqueID = UniqueID,
                CurrentPlayers = CurrentPlayers
            });
        }

        private int tempNumPlayers = 0;

        private void Update()
        {
            if (tempNumPlayers != NetworkServer.connections.Count)
            {
                tempNumPlayers = NetworkServer.connections.Count;
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Game Registration] NumPlayers changed: " + tempNumPlayers);
                SendGameStatusToGameManager();
            }
        }
    }
}
