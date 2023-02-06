using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Insight
{
    public class ClientGameManager : InsightModule
    {
        InsightClient client;
        Transport networkManagerTransport;
        public int SceneID;
        public string GameName;
        public int GameType;
        public int ServerRegion;

        public List<GameContainer> gamesList = new List<GameContainer>();

        public override void Initialize(InsightClient client, ModuleManager manager)
        {
            this.client = client;

#if MIRROR_71_0_OR_NEWER
             networkManagerTransport = Transport.active;
#else
            networkManagerTransport = Transport.activeTransport;

#endif

            RegisterHandlers();
        }

        void RegisterHandlers()
        {
            client.RegisterHandler<ChangeServerMsg>(HandleChangeServersMsg);
            client.RegisterHandler<GameListMsg>(HandleGameListMsg);
        }

        void HandleChangeServersMsg(InsightNetworkMessage netMsg)
        {
            ChangeServerMsg message = netMsg.ReadMessage<ChangeServerMsg>();

            Debug.Log("[InsightClient] - Connecting to GameServer: " + message.GameServerIP + ":" + message.GameServerPort + "/" + message.SceneID);

            if(networkManagerTransport is MultiplexTransport) {
                ushort startPort = message.GameServerPort;
                foreach(Transport transport in (networkManagerTransport as MultiplexTransport).transports) {
                    SetPort(transport, startPort++);
                }
            } else {
                SetPort(networkManagerTransport, message.GameServerPort);
            }

            NetworkManager.singleton.networkAddress = message.GameServerIP;

            NetworkManager.singleton.StartClient();
            //SceneManager.LoadScene(message.SceneName);
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

        void HandleGameListMsg(InsightNetworkMessage netMsg)
        {
            GameListMsg message = netMsg.ReadMessage<GameListMsg>();

            Debug.Log("[InsightClient] - Received Games List");

            gamesList.Clear();

            foreach (GameContainer game in message.gamesArray)
            {
                //Debug.Log(game.SceneID);

                gamesList.Add(new GameContainer()
                {
                    //GameServerIP = game.GameServerIP,
                    //GameServerPort = game.GameServerPort,
                    UniqueId = game.UniqueId,
                    SceneID = game.SceneID,
                    CurrentPlayers = game.CurrentPlayers,
                    MaxPlayers = game.MaxPlayers,
                    MinPlayers = game.MinPlayers,
                    JoinAnyTime = game.JoinAnyTime,
                    GameName = game.GameName,
                    GameType = game.GameType,
                    ServerRegion = game.ServerRegion
                });
            }
        }

        #region Message Senders
        public void SendRequestSpawnStart(RequestSpawnStartMsg requestSpawnStartMsg)
        {
            client.Send(requestSpawnStartMsg);
        }

        public void SendJoinGameMsg(string UniqueID)
        {
            client.Send(new JoinGameMsg() { UniqueID = UniqueID });
        }

        public void SendGetGameListMsg()
        {
            client.Send(new GameListMsg());
        }
        #endregion
    }
}
