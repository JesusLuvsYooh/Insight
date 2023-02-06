using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TODO: Remove the example specific code from module

namespace Insight
{
    public enum MatchMakingResponseType
    {
        Search,
        Wait, //waiting for players
        Full, //full spwaners
        Join, //gameserver starts, moving player to server
        Timeout, //players not found
        Failed, //gameserver timeout
        NoMatch //no matching filter servers
    }

    public class ServerMatchMaking : InsightModule
    {
        internal InsightServer server;
        ServerAuthentication authModule;
        internal ServerGameManager gameManager;
        MasterSpawner masterSpawner;

        public int MinimumPlayersForGame = 1;
        public float MatchMakingPollRate = 10f;


        public List<UserContainer> playerQueue = new List<UserContainer>();
        public List<MatchContainer> matchList = new List<MatchContainer>();

        public List<GameContainer> filteredGameServers = new List<GameContainer>();
        public List<GameContainer> sortedGameServers = new List<GameContainer>();

        bool _spawnInProgress;

        private int sceneID = 0;
        private string joinAnyTime = "false";
        private string gameName = "InsightExample"; // or use this as an ID, if you want multiple build splits per master server
        private int gameType = 0;
        private int serverRegion = 0;

        public void Awake()
        {
            AddDependency<MasterSpawner>();
            AddDependency<ServerAuthentication>(); //Used to track logged in players
            AddDependency<ServerGameManager>(); //Used to track available games
        }

        public override void Initialize(InsightServer insight, ModuleManager manager)
        {
            server = insight;
            authModule = manager.GetModule<ServerAuthentication>();
            gameManager = manager.GetModule<ServerGameManager>();
            masterSpawner = manager.GetModule<MasterSpawner>();

            RegisterHandlers();

            server.transport.OnServerDisconnected += HandleDisconnect;

            InvokeRepeating("InvokedUpdate", MatchMakingPollRate, MatchMakingPollRate);
        }

        void RegisterHandlers()
        {
            server.RegisterHandler<StartMatchMakingMsg>(HandleStartMatchSearchMsg);
            server.RegisterHandler<StopMatchMakingMsg>(HandleStopMatchSearchMsg);
        }

        void InvokedUpdate()
        {
            UpdateQueue();
            UpdateMatches();
        }

        void HandleStartMatchSearchMsg(InsightNetworkMessage netMsg)
        {
            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[MatchMaking] - Player joining MatchMaking.");

            StartMatchMakingMsg message = netMsg.ReadMessage<StartMatchMakingMsg>();

            sceneID = message.SceneID;
            gameName = message.GameName;
            gameType = message.GameType;
            serverRegion = message.ServerRegion;

            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[MatchMaking] - Client data received: " + sceneID + " -  " + gameName + " - " + gameType + " - " + serverRegion);

            // should never happen
            if (masterSpawner.registeredSpawners.Count == 0)
            {
                Debug.LogError("[MatchMaking] - No Spawners, cannot continue!");

                netMsg.Reply(new MatchMakingResponseMsg() { ResponseType = MatchMakingResponseType.Failed });
            }
            // no servers to search or filter, queue player which will create new gameserver
            else if (gameManager.registeredGameServers.Count == 0)
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - No GameServers.");
                AddPlayerToGameServerQueue(netMsg);
            }
            else
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - SearchForServers.");
                SearchForServers(netMsg);
            }
        }

        void SearchForServers(InsightNetworkMessage netMsg)
        {
            GameContainer game = null;
            //game = gameManager.registeredGameServers[0];
            

            // clear our list, to apply matchmaking options
            filteredGameServers.Clear();
            sortedGameServers.Clear();

            sortedGameServers = gameManager.registeredGameServers.OrderBy(value => value.CurrentPlayers).ToList();

            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[MatchMaking] - sortedGameServers count- " + sortedGameServers.Count);

            // filter games that do not let players join once they have started
            foreach (GameContainer gameTemp in sortedGameServers)
            {
                if (gameTemp.JoinAnyTime == true)
                {
                    filteredGameServers.Add(gameTemp);
                }
            };
            sortedGameServers = new List<GameContainer>(filteredGameServers);

            FilterForServerSpace(netMsg);

            // you can reorganise these 3 to prioritise order 
            FilterForServerRegion(netMsg);

            FilterForServerGameType(netMsg);

            FilterForServerSceneID(netMsg);

            if (filteredGameServers != null && filteredGameServers.Count > 0)
            {
                game = filteredGameServers[0];

                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - Matching game found.");

                netMsg.Reply(new MatchMakingResponseMsg()
                {
                    ResponseType = MatchMakingResponseType.Join
                });
            }
           


            if (game != null)
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - Match found, sending data to client.");

                netMsg.Reply(new ChangeServerMsg()
                {
                    GameServerIP = game.GameServerIP,
                    GameServerPort = game.GameServerPort,
                    SceneID = game.SceneID
                });

                if (InsightServer.instance.playerStayConnected == false)
                {
                    authModule.registeredUsers.Remove(authModule.GetUserByConnection(netMsg.connectionId));
                    NetworkServer.RemoveConnection(netMsg.connectionId);
                }
            }
            else if (game == null)
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - No Matches from players filtered options.");
                AddPlayerToGameServerQueue(netMsg);
            }
        }

        void AddPlayerToGameServerQueue(InsightNetworkMessage netMsg)
        {
            if (InsightServer.instance.NoisyLogs)
                Debug.Log("[MatchMaking] - Queue player for new GameServer spawn.");
            playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
            netMsg.Reply(new MatchMakingResponseMsg()
            {
                ResponseType = MatchMakingResponseType.Wait
            });
        }

        void FilterForServerSpace(InsightNetworkMessage netMsg)
        {
            // filter regions if client has a prefered one selected
            if (sortedGameServers != null && sortedGameServers.Count > 0)
            {
                filteredGameServers.Clear();
                foreach (GameContainer gameTemp in sortedGameServers)
                {
                    if (gameTemp.CurrentPlayers < gameTemp.MaxPlayers)
                    {
                        filteredGameServers.Add(gameTemp);
                    }
                };

                //sortedGameServers = filteredGameServers;
                sortedGameServers = new List<GameContainer>(filteredGameServers);
                if (filteredGameServers == null || filteredGameServers.Count <= 0)
                {
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - No empty servers.");
                    playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.Full
                    });
                }
            }
        }

        void FilterForServerRegion(InsightNetworkMessage netMsg)
        {
            // filter regions if client has a prefered one selected
            if (sortedGameServers != null && sortedGameServers.Count > 0 && serverRegion > 0)
            {
                filteredGameServers.Clear();
                foreach (GameContainer gameTemp in sortedGameServers)
                {
                    if (gameTemp.ServerRegion == serverRegion)
                    {
                        filteredGameServers.Add(gameTemp);
                    }
                };

                //sortedGameServers = filteredGameServers;
                sortedGameServers = new List<GameContainer>(filteredGameServers);
                if (filteredGameServers == null || filteredGameServers.Count <= 0)
                {
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - No matching region servers.");
                    playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.NoMatch
                    });
                }
            }
        }

        void FilterForServerGameType(InsightNetworkMessage netMsg)
        {
            // filter game type if client has a prefered one selected
            if (sortedGameServers != null && sortedGameServers.Count > 0 && gameType > 0)
            {
                filteredGameServers.Clear();
                foreach (GameContainer gameTemp in sortedGameServers)
                {
                    if (gameTemp.GameType == gameType)
                    {
                        filteredGameServers.Add(gameTemp);
                    }
                };

                //sortedGameServers = filteredGameServers;
                sortedGameServers = new List<GameContainer>(filteredGameServers);
                if (filteredGameServers == null || filteredGameServers.Count <= 0)
                {
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - No matching game type servers.");
                    playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.NoMatch
                    });
                }
            }
        }

        void FilterForServerSceneID(InsightNetworkMessage netMsg)
        {
            // filter scene id if client has a prefered one selected
            if (sortedGameServers != null && sortedGameServers.Count > 0 && sceneID > 0)
            {
                filteredGameServers.Clear();
                foreach (GameContainer gameTemp in sortedGameServers)
                {
                    if (gameTemp.SceneID == sceneID)
                    {
                        filteredGameServers.Add(gameTemp);
                    }
                };

                //sortedGameServers = filteredGameServers;
                sortedGameServers = new List<GameContainer>(filteredGameServers);
                if (filteredGameServers == null || filteredGameServers.Count <= 0)
                {
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - No matching scene map servers.");
                    playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.NoMatch
                    });
                }
            }
        }

        void HandleStopMatchSearchMsg(InsightNetworkMessage netMsg)
        {
            foreach (UserContainer seraching in playerQueue)
            {
                if (seraching.connectionId == netMsg.connectionId)
                {
                    playerQueue.Remove(seraching);
                    return;
                }
            }
        }

        void HandleDisconnect(int connectionId)
        {
            foreach (UserContainer user in playerQueue)
            {
                if (user.connectionId == connectionId)
                {
                    playerQueue.Remove(user);
                    break;
                }
            }
            foreach (MatchContainer match in matchList)
            {
                foreach (UserContainer user in match.matchUsers)
                {
                    if (user.connectionId == connectionId)
                    {
                        match.matchUsers.Remove(user);
                        break;
                    }
                }
            }
        }

        void UpdateQueue()
        {
            if (playerQueue.Count < MinimumPlayersForGame)
            {
                // to stop log spam
                if (MinimumPlayersForGame > 1)
                {
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - Minimum players in queue not reached.");
                }
                return;
            }

            if (masterSpawner.registeredSpawners.Count == 0)
            {
                Debug.LogWarning("[MatchMaking] - No spawners for players in queue.");
                return;
            }

            CreateMatch();
        }

        void CreateMatch()
        {
            //Used to track completion of requested spawn
            string uniqueID = Guid.NewGuid().ToString();

            //Specify the match details
            RequestSpawnStartMsg requestSpawnStart = new RequestSpawnStartMsg()
            {
                SceneID = sceneID,
                UniqueID = uniqueID,
                //JoinAnyTime = joinAnyTime,
                GameName = gameName,
                GameType = gameType,
                ServerRegion = serverRegion
            };

            List<UserContainer> matchUsers = new List<UserContainer>();

            //This should check to make sure that the max players is not higher than the number in queue
            //Add the players from the queue into this match:
            for (int i = playerQueue.Count - 1; i >= 0; i--)
            {
                matchUsers.Add(playerQueue[i]);
                authModule.registeredUsers.Remove(authModule.GetUserByConnection(playerQueue[i].connectionId));
                playerQueue.RemoveAt(i);
            }
            matchList.Add(new MatchContainer(this, requestSpawnStart, matchUsers));
        }

        void UpdateMatches()
        {
            foreach (MatchContainer match in matchList)
            {
                if (match.InitMatch)
                {
                    bool stillActiveGame = false;
                    foreach (GameContainer game in gameManager.registeredGameServers)
                    {
                        if (match.MatchServer.UniqueId == game.UniqueId)
                        {
                            stillActiveGame = true;
                        }
                    }

                    if (!stillActiveGame)
                    {
                        match.MatchComplete = true;
                    }
                }
                match.Update();
            }

            for (int i = matchList.Count - 1; i >= 0; i--)
            {
                if (matchList[i].MatchComplete)
                {
                    matchList.RemoveAt(i);
                }
                else
                {
                    matchList[i].Update();
                }
            }
        }
    }

    [Serializable]
    public class MatchContainer
    {
        public ServerMatchMaking matchModule;
        public GameContainer MatchServer;
        public List<UserContainer> matchUsers;

        //These two are probably redundant
        public string playlistName;
        public RequestSpawnStartMsg matchProperties;

        //How long to wait for the server to start before cancelling the match and returning the players to the queue
        //-1 or 0 will disable timeout
        public float MatchTimeoutInSeconds = 30f;
        public DateTime matchStartTime;

        public bool InitMatch;
        public bool MatchComplete;

        public MatchContainer(ServerMatchMaking MatchModule, RequestSpawnStartMsg MatchProperties, List<UserContainer> MatchUsers)
        {
            matchModule = MatchModule;
            matchProperties = MatchProperties;
            matchModule.gameManager.RequestGameSpawnStart(matchProperties);
            matchUsers = MatchUsers;
            matchStartTime = DateTime.UtcNow;
        }

        public void Update()
        {
            if (!InitMatch)
            {
                if (IsSpawnServerActive())
                {
                    InitMatch = true;
                    MatchServer = matchModule.gameManager.GetGameByUniqueID(matchProperties.UniqueID);

                    MovePlayersToServer();
                }
            }
        }

        bool IsSpawnServerActive()
        {
            if (matchModule.gameManager.GetGameByUniqueID(matchProperties.UniqueID) == null)
            {
                //Server spawn timeout check
                if (MatchTimeoutInSeconds > 0 && matchStartTime.AddSeconds(MatchTimeoutInSeconds) < DateTime.UtcNow)
                {
                    CancelMatch();
                }

                if (InsightServer.instance.NoisyLogs)
                    Debug.LogWarning("Server not active at this time");
                return false;
            }
            return true;
        }

        void MovePlayersToServer()
        {
            foreach (UserContainer user in matchUsers)
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("MovePlayersToServer - ChangeServerMsg");
                matchModule.server.SendToClient(user.connectionId, new ChangeServerMsg()
                {
                    GameServerIP = MatchServer.GameServerIP,
                    GameServerPort = MatchServer.GameServerPort,
                    SceneID = MatchServer.SceneID
                });

                if (InsightServer.instance.playerStayConnected == false)
                {
                    InsightNetworkConnection conn;
                    if (InsightServer.instance.connections.TryGetValue(user.connectionId, out conn))
                    {
                        if (InsightServer.instance.NoisyLogs)
                            Debug.Log("MovePlayersToServer - remove users from MS");
                        conn.Disconnect();
                        InsightServer.instance.RemoveConnection(user.connectionId);
                    }
                }
            }
        }

        void CancelMatch()
        {
            Debug.LogError("Server failed to start within timoue period. Cancelling match.");

            //TODO: Destroy the match process somewhere: MatchServer

            //Put the users back in the queue
            foreach (UserContainer user in matchUsers)
            {
                matchModule.playerQueue.Add(user);
            }
            matchUsers.Clear();

            //Flag to destroy match on next update
            MatchComplete = true;
        }
    }
}
