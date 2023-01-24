using Mirror;
using System;
using System.Collections.Generic;
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
        Failed //gameserver timeout
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

            netMsg.Reply(new MatchMakingResponseMsg()
            {
                ResponseType = (ushort)MatchMakingResponseType.Search
            });

            // temp check, needs to cycle through all spawners and gameservers
            if (joinAnyTime == "false" || joinAnyTime == "False")
            {
                playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                netMsg.Reply(new MatchMakingResponseMsg()
                {
                    ResponseType = MatchMakingResponseType.Wait
                });
            }
            else
            {
                if (masterSpawner.registeredSpawners.Count == 0 || gameManager.registeredGameServers.Count == 0)
                {
                    //if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - No spawners or servers, queuing player.");
                    playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.Failed
                    });
                }
                else
                {
                    SearchForServerSpaces(netMsg);
                }
            }
        }

        void SearchForServerSpaces(InsightNetworkMessage netMsg)
        {
            GameContainer game = null;
            

            foreach (GameContainer gameTemp in gameManager.registeredGameServers)
            {
                if (gameTemp.CurrentPlayers < gameTemp.MaxPlayers)
                {
                    game = gameTemp;
                    if (InsightServer.instance.NoisyLogs)
                        Debug.Log("[MatchMaking] - Game with space found.");
                    netMsg.Reply(new MatchMakingResponseMsg()
                    {
                        ResponseType = MatchMakingResponseType.Join
                    });
                    break;
                }
            };

            if (game == null)
            {
                if (InsightServer.instance.NoisyLogs)
                Debug.Log("[MatchMaking] - No spaces queue player.");
                playerQueue.Add(authModule.GetUserByConnection(netMsg.connectionId));
                netMsg.Reply(new MatchMakingResponseMsg()
                {
                    ResponseType = MatchMakingResponseType.Full
                });
            }
            else
            {
                if (InsightServer.instance.NoisyLogs)
                    Debug.Log("[MatchMaking] - Sending client to server.");

                netMsg.Reply(new ChangeServerMsg()
                {
                    NetworkAddress = game.NetworkAddress,
                    NetworkPort = game.NetworkPort,
                    SceneID = game.SceneID
                });

                if (InsightServer.instance.PlayerStayConnectedToMasterServer == false)
                {
                    authModule.registeredUsers.Remove(authModule.GetUserByConnection(netMsg.connectionId));
                    NetworkServer.RemoveConnection(netMsg.connectionId);
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
                //Currently sent via request of client during matchmaking, completely optional, could be used for rare case
                // weirdly it was setup to send requested scene name, but then was hardcoded to always be "SuperAwesomeGame"
                // args are received in GameRegistration script, add scenes into inspector (verifiedScenes) on GameServer/GameRegistration prefab
                // (if you want clients to send request scene still)
                SceneID = sceneID,
                //This should not be hard coded. Might not be used at all if your GameServer.exe controls scenes.
                //SceneName = "SuperAwesomeGame",
                UniqueID = uniqueID,
                JoinAnyTime = joinAnyTime,
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
                playerQueue.RemoveAt(i);
                if (InsightServer.instance.PlayerStayConnectedToMasterServer == false)
                {
                    authModule.registeredUsers.Remove(authModule.GetUserByConnection(playerQueue[i].connectionId));
                    NetworkServer.RemoveConnection(playerQueue[i].connectionId);
                }
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
                matchModule.server.SendToClient(user.connectionId, new ChangeServerMsg()
                {
                    NetworkAddress = MatchServer.NetworkAddress,
                    NetworkPort = MatchServer.NetworkPort,
                    SceneID = MatchServer.SceneID
                });
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
