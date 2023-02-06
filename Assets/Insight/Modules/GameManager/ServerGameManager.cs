using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight
{
    public class ServerGameManager : InsightModule
    {
        InsightServer server;
        MasterSpawner masterSpawner;

        public List<GameContainer> registeredGameServers = new List<GameContainer>();

        public void Awake()
        {
            AddDependency<MasterSpawner>();
        }

        public override void Initialize(InsightServer insight, ModuleManager manager)
        {
            server = insight;
            masterSpawner = manager.GetModule<MasterSpawner>();
            RegisterHandlers();

            server.transport.OnServerDisconnected += HandleDisconnect;
        }

        void RegisterHandlers()
        {
            server.RegisterHandler<RegisterGameMsg>(HandleRegisterGameMsg);
            server.RegisterHandler<GameStatusMsg>(HandleGameStatusMsg);
            server.RegisterHandler<JoinGameMsg>(HandleJoinGameMsg);
            server.RegisterHandler<GameListMsg>(HandleGameListMsg);
        }

        void HandleRegisterGameMsg(InsightNetworkMessage netMsg)
        {
            RegisterGameMsg message = netMsg.ReadMessage<RegisterGameMsg>();

            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[GameManager] - Received GameRegistration request");

            registeredGameServers.Add(new GameContainer()
            {
                GameServerIP = message.GameServerIP,
                GameServerPort = message.GameServerPort,
                UniqueId = message.UniqueID,
                SceneID = message.SceneID,
                MaxPlayers = message.MaxPlayers,
                CurrentPlayers = message.CurrentPlayers,
                JoinAnyTime = message.JoinAnyTime,
                GameName = message.GameName,
                GameType = message.GameType,
                ServerRegion = message.ServerRegion,

                connectionId = netMsg.connectionId
            });

            if (server.autoAuthClients)
            {
                InsightNetworkConnection conn;
                if (server.connections.TryGetValue(netMsg.connectionId, out conn))
                {
                    server.serverAuthentication.registeredUsers.Remove(server.serverAuthentication.GetUserByConnection(netMsg.connectionId));
                    Debug.Log("[GameManager] - Removed GameServer from player list.");
                }
            }
        }

        void HandleGameStatusMsg(InsightNetworkMessage netMsg)
        {
            GameStatusMsg message = netMsg.ReadMessage<GameStatusMsg>();

            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[GameManager] - Received Game status update");

            foreach (GameContainer game in registeredGameServers)
            {
                if (game.UniqueId == message.UniqueID)
                {
                    game.CurrentPlayers = message.CurrentPlayers;
                    return;
                }
            };
        }

        //Checks if the connection that dropped is actually a GameServer
        void HandleDisconnect(int connectionId)
        {
            foreach (GameContainer game in registeredGameServers)
            {
                if (game.connectionId == connectionId)
                {
                    registeredGameServers.Remove(game);
                    return;
                }
            }
        }

        void HandleGameListMsg(InsightNetworkMessage netMsg)
        {
            Debug.Log("[MatchMaking] - Player Requesting Match list");

            GameListMsg gamesListMsg = new GameListMsg();
            gamesListMsg.Load(registeredGameServers);

            if (server.gameSettingsModule.StripConnectionInfo)
            {
                foreach (GameContainer game in gamesListMsg.gamesArray)
                {
                    if (game.JoinAnyTime == false || game.CurrentPlayers >= game.MaxPlayers)
                    {
                        game.GameServerIP = "";
                        game.GameServerPort = 0;
                    }
                }
            }

            netMsg.Reply(gamesListMsg);
        }

        void HandleJoinGameMsg(InsightNetworkMessage netMsg)
        {
            JoinGameMsg message = netMsg.ReadMessage<JoinGameMsg>();

            Debug.Log("[MatchMaking] - Player joining Match.");

            GameContainer game = GetGameByUniqueID(message.UniqueID);

            if (game == null)
            {
                //Something went wrong
                //netMsg.Reply((short)MsgId.ChangeServers, new ChangeServerMsg());
            }
            else
            {
                netMsg.Reply(new ChangeServerMsg()
                {
                    GameServerIP = game.GameServerIP,
                    GameServerPort = game.GameServerPort,
                    SceneID = game.SceneID
                });
            }
        }

        //Used by MatchMaker to request a GameServer for a new Match
        public void RequestGameSpawnStart(RequestSpawnStartMsg requestSpawn)
        {
            //Debug.LogWarning("RequestGameSpawnStart: " + requestSpawn.ServerRegion);
            masterSpawner.InternalSpawnRequest(requestSpawn);
        }

        public GameContainer GetGameByUniqueID(string uniqueID)
        {
            foreach (GameContainer game in registeredGameServers)
            {
                if (game.UniqueId.Equals(uniqueID))
                {
                    return game;
                }
            }
            return null;
        }
    }

    [Serializable]
    public class GameContainer
    {
        public string GameServerIP;
        public ushort GameServerPort;
        public string UniqueId;
        public int connectionId;

        public int SceneID;
        public int MaxPlayers;
        public int MinPlayers;
        public int CurrentPlayers;

        public bool JoinAnyTime;
        public string GameName;
        public int GameType;
        public int ServerRegion;
    }
}
