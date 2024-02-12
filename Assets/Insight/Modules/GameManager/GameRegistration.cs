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
        public int GameSceneID;
        public string GameServerIP;
        public ushort GameServerPort;
        public string UniqueID;

        //These should probably be synced from the NetworkManager
        public int MaxPlayers;
        public int CurrentPlayers;

        public bool JoinAnyTime;

        public string GameName;
        public int GameType;
        public int ServerRegion;

        private bool AbortRun = false;

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            client = insight;
            client.transport.OnClientConnected += SendGameRegistrationToGameManager;

            //NetworkManager.singleton.OnServerDisconnect = OnServerDisconnect;
            //NetworkManager.singleton.OnServerAddPlayer = OnServerAddPlayer;

             networkManagerTransport = Transport.active;
            //old networkManagerTransport = Transport.activeTransport;

            // use Game Settings master bool, can be overrided by args
            JoinAnyTime = client.gameSettingsModule.JoinAnyTime;
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
            if (NetworkManager.singleton == null)
                Debug.Log("NULL NM");
            if (InsightClient.instance == null)
                Debug.Log("NULL IC");
            if (InsightClient.instance.gameSettingsModule == null)
                Debug.Log("NULL GM");
            if (InsightClient.instance.gameSettingsModule.verifiedScenes == null || InsightClient.instance.gameSettingsModule.verifiedScenes.Length <= 0)
                Debug.Log("NULL VS");

            InsightArgs args = new InsightArgs();
            if (args.IsProvided("-GameServerIP"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - GameServerIP: " + args.GameServerIP);
                GameServerIP = args.GameServerIP;

                NetworkManager.singleton.networkAddress = GameServerIP;
            }

            if (args.IsProvided("-GameServerPort"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - GameServerPort: " + args.GameServerPort);
                GameServerPort = (ushort)args.NetworkPort;

                if (networkManagerTransport is MultiplexTransport)
                {
                    ushort startPort = GameServerPort;
                    foreach (Transport transport in (networkManagerTransport as MultiplexTransport).transports)
                    {
                        SetPort(transport, startPort++);
                    }
                }
                else
                {
                    SetPort(networkManagerTransport, GameServerPort);
                }
            }
            
            if (args.IsProvided("-SceneID"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - SceneID: " + args.SceneID);

                int buildIndex = SceneUtility.GetBuildIndexByScenePath(client.gameSettingsModule.verifiedScenes[GameSceneID]);

                // check server has a scene for requested id
                if (buildIndex >= 0 && client.gameSettingsModule.verifiedScenes.Length > 0 && args.SceneID < client.gameSettingsModule.verifiedScenes.Length)
                {

                    if (InsightClient.instance.NoisyLogs)
                        Debug.Log("[Args] - Scene found/verified.");
                    GameSceneID = args.SceneID;
                }
                else
                {
                    Debug.Log("[Args] - Scene not found/verified or any Scene is desired.");
                    // Presuming "Any" or "0" has been selected, chose a random one from list?
                    GameSceneID = UnityEngine.Random.Range(1, client.gameSettingsModule.verifiedScenes.Length);
                }
                NetworkManager.singleton.onlineScene = client.gameSettingsModule.verifiedScenes[GameSceneID];
            }
            
           
           // NetworkManager.singleton.onlineScene = InsightClient.instance.gameSettingsModule.verifiedScenes[GameSceneID];
            if (args.IsProvided("-UniqueID"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - UniqueID: " + args.UniqueID);
                UniqueID = args.UniqueID;
            }

            if (args.IsProvided("-JoinAnyTime"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - JoinAnyTime: " + args.JoinAnyTime);
                if (args.JoinAnyTime == "true" || args.JoinAnyTime == "True")
                {
                    JoinAnyTime = true;
                }
            }
            
            if (args.IsProvided("-GameName"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - GameName: " + args.GameName);
                GameName = args.GameName;
            }
            if (args.IsProvided("-GameType"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - GameType: " + args.GameType);
                GameType = args.GameType;
            }
            if (args.IsProvided("-ServerRegion"))
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[Args] - ServerRegion: " + args.ServerRegion);
                ServerRegion = args.ServerRegion;
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
                GameServerIP = GameServerIP,
                GameServerPort = GameServerPort,
                UniqueID = UniqueID,
                SceneID = GameSceneID,
                MaxPlayers = MaxPlayers,
                CurrentPlayers = CurrentPlayers,
                JoinAnyTime = JoinAnyTime,
                GameName = GameName,
                GameType = GameType,
                ServerRegion = ServerRegion
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
