using Mirror;
using UnityEngine;
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

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            client = insight;
            client.transport.OnClientConnected += SendGameRegistrationToGameManager;

            networkManagerTransport = Transport.activeTransport;

            RegisterHandlers();
            GatherCmdArgs();

            InvokeRepeating("SendGameStatusToGameManager", 30f, 30f);
        }

        void RegisterHandlers() { }

        void GatherCmdArgs()
        {
            InsightArgs args = new InsightArgs();
            if (args.IsProvided("-NetworkAddress"))
            {
                Debug.Log("[Args] - NetworkAddress: " + args.NetworkAddress);
                NetworkAddress = args.NetworkAddress;

                NetworkManager.singleton.networkAddress = NetworkAddress;
            }

            if (args.IsProvided("-NetworkPort"))
            {
                Debug.Log("[Args] - NetworkPort: " + args.NetworkPort);
                NetworkPort = (ushort)args.NetworkPort;

                if(networkManagerTransport is MultiplexTransport) {
                    ushort startPort = NetworkPort;
                    foreach(Transport transport in (networkManagerTransport as MultiplexTransport).transports) {
                        SetPort(transport, startPort++);
                    }
                } else if(networkManagerTransport is FallbackTransport) {
                    foreach(Transport transport in (networkManagerTransport as FallbackTransport).transports) {
                        SetPort(transport, NetworkPort);
                    }
                } else {
                    SetPort(networkManagerTransport, NetworkPort);
                }
            }

            if (args.IsProvided("-SceneName"))
            {
                Debug.Log("[Args] - SceneName: " + args.SceneName);
                GameScene = args.SceneName;
                SceneManager.LoadScene(args.SceneName);
            }

            if (args.IsProvided("-UniqueID"))
            {
                Debug.Log("[Args] - UniqueID: " + args.UniqueID);
                UniqueID = args.UniqueID;
            }

            MaxPlayers = NetworkManager.singleton.maxConnections;

            //Start NetworkManager
            NetworkManager.singleton.StartServer();
        }

        void SetPort(Transport transport, ushort port) {
            if(transport.GetType().GetField("port") != null) {
                transport.GetType().GetField("port").SetValue(transport, port);
            }else if(transport.GetType().GetField("Port") != null) {
                transport.GetType().GetField("Port").SetValue(transport, port);
            }else if(transport.GetType().GetField("CommunicationPort") != null) {//For Ignorance
                transport.GetType().GetField("CommunicationPort").SetValue(transport, port);
            }
        }

        void SendGameRegistrationToGameManager()
        {
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
            CurrentPlayers = NetworkManager.singleton.numPlayers;

            Debug.Log("[GameRegistration] - status update");
            client.Send(new GameStatusMsg()
            {
                UniqueID = UniqueID,
                CurrentPlayers = CurrentPlayers
            });
        }
    }
}
