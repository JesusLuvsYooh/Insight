﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Insight.Examples
{
    public class GUIMasterServer : MonoBehaviour
    {
        [Header("Insight")]
        public InsightServer masterServer;
        public ModuleManager moduleManager;
        public ServerAuthentication authModule;
        public ChatServer chatModule;
        public MasterSpawner masterSpawnerModule;
        public ServerGameManager gameModule;
        public ServerMatchMaking matchModule;

        [Header("Labels")]
        public Text spawnerCountText;
        public Text gameCountText;
        public Text userCountText;
        public Text playersInQueueCountText;
        public Text activeGamesText;
        public Text connectionsText;

        bool Init;
        private string previousActiveGamesText = "";
        private string currentActiveGamesText = "";

        void FixedUpdate()
        {
            if (!Init)
            {
                if (masterServer.isConnected)
                {
                    Init = true;
                    moduleManager = masterServer.GetComponent<ModuleManager>();
                    authModule = moduleManager.GetModule<ServerAuthentication>();
                    chatModule = moduleManager.GetModule<ChatServer>();
                    masterSpawnerModule = moduleManager.GetModule<MasterSpawner>();
                    gameModule = moduleManager.GetModule<ServerGameManager>();
                    matchModule = moduleManager.GetModule<ServerMatchMaking>();
                }
                return;
            }

            spawnerCountText.text = masterSpawnerModule.registeredSpawners.Count.ToString();
            gameCountText.text = gameModule.registeredGameServers.Count.ToString();
            userCountText.text = authModule.registeredUsers.Count.ToString();
            playersInQueueCountText.text = matchModule.playerQueue.Count.ToString();
            connectionsText.text = InsightServer.instance.connections.Count.ToString();

            //Clear previous values
            activeGamesText.text = "";
            currentActiveGamesText = "";

            //Game Status
            foreach (GameContainer game in gameModule.registeredGameServers)
            {
                activeGamesText.text += game.UniqueId + " - " + game.NetworkAddress + ":" + game.NetworkPort + " - " + game.SceneID + " - " + game.CurrentPlayers + "/" + game.MaxPlayers + " - " + game.GameName + " - " + game.GameType + " - " + game.JoinAnyTime + Environment.NewLine;
                currentActiveGamesText += game.NetworkAddress + ":" + game.NetworkPort + " - " + game.SceneID + " - " + game.CurrentPlayers + "/" + game.MaxPlayers + " - " + game.GameName + " - " + game.GameType + " - " + game.JoinAnyTime + Environment.NewLine;
            }

            if (previousActiveGamesText != currentActiveGamesText)
            {
                previousActiveGamesText = currentActiveGamesText;
                if (currentActiveGamesText != "")
                { print(currentActiveGamesText); }
            }
        }
    }
}
