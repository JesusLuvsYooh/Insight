using Mirror;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Insight.Examples
{
    public enum PlayerClientGUIState { Login, Main, Game };

    public class PlayerClientGUI : MonoBehaviour
    {
        [Header("Root UI Panels")]
        public GameObject RootLoginPanel;
        public GameObject RootMainPanel;
        public GameObject RootGamePanel;

        PlayerClientGUIState playerGuiState;

        [Header("Insight Modules")]
        public ClientAuthentication authComp;
        public ChatClient chatComp;
        public ClientGameManager gameComp;
        public ClientMatchMaking matchComp;

        [Header("UI Buttons")]
        public GameObject StartMatchMakingButton;
        public GameObject StopMatchMakingButton;
        public GameObject GetGameListButton;
        public GameObject CreateGameButton;

        [Header("Game List UI Panels")]
        public GameObject GameListArea;
        public GameObject GameListPanel;

        public GameObject GameListItemPrefab;

        public Text chatTextField;
        public InputField chatInputField;

        public List<GameContainer> gamesList = new List<GameContainer>();

        [Header("Playlist/Game Name")]
        public int sceneID = 0;
        public string gameName = "InsightExample"; // no spaces, think of it as an ID 
        public int gameType = 0;
        public int serverRegion = 0;

        [Header("Matchmaking Options")]
        public Dropdown dropdownSceneID;
        public Dropdown dropdownGameType;
        public Dropdown dropdownServerRegion;


        private void Start()
        {
            SwitchToLogin();

            //SetupUI();
        }

        void Update()
        {
            switch (playerGuiState)
            {
                case PlayerClientGUIState.Login:
                    SwitchToLogin();

                    if (authComp.loginSucessful)
                    {
                        playerGuiState = PlayerClientGUIState.Main;
                        return;
                    }
                    break;
                case PlayerClientGUIState.Main:
                    SwitchToMain();
                    CheckGamesList();
                    break;
                case PlayerClientGUIState.Game:
                    SwitchToGame();
                    break;
            }

            if (NetworkManager.singleton.isNetworkActive)
            {
                playerGuiState = PlayerClientGUIState.Game;
            }
            else if (authComp.loginSucessful)
            {
                playerGuiState = PlayerClientGUIState.Main;
            }
        }

        public void FixedUpdate()
        {
            if (chatComp == null)
                return;
            //This is gross. Needs a better design that does not introduce coupling.
            chatTextField.text = chatComp.chatLog;
        }

        private void SwitchToLogin()
        {
            RootLoginPanel.SetActive(true);
            RootMainPanel.SetActive(false);
            RootGamePanel.SetActive(false);
        }

        private void SwitchToMain()
        {
            RootLoginPanel.SetActive(false);
            RootMainPanel.SetActive(true);
            RootGamePanel.SetActive(false);

            SetupMatchMakingSettings();
        }

        private void SwitchToGamesList()
        {
            RootLoginPanel.SetActive(false);
            RootMainPanel.SetActive(false);
            RootGamePanel.SetActive(false);
        }

        private void SwitchToGame()
        {
            RootLoginPanel.SetActive(false);
            RootMainPanel.SetActive(false);
            RootGamePanel.SetActive(true);
        }

        public void HandleStartMatchMakingButton()
        {
            StartMatchMakingButton.SetActive(false);
            StopMatchMakingButton.SetActive(true);

            matchComp.SendStartMatchMaking(new StartMatchMakingMsg() { SceneID = sceneID, GameName = gameName, GameType = gameType, ServerRegion = serverRegion });
        }

        public void HandleStopMatchMakingButton()
        {
            StartMatchMakingButton.SetActive(true);
            StopMatchMakingButton.SetActive(false);

            matchComp.SendStopMatchMaking();
        }

        public void HandleGetGameListButton()
        {
            gameComp.SendGetGameListMsg();

            GetGameListButton.SetActive(false);
            StartMatchMakingButton.SetActive(false);
            StopMatchMakingButton.SetActive(false);
            CreateGameButton.SetActive(false);

            GameListArea.SetActive(true);
        }

        public void HandleJoinGameButton(string UniqueID)
        {
            HandleCancelButton();

            gameComp.SendJoinGameMsg(UniqueID);

            playerGuiState = PlayerClientGUIState.Game;
        }

        public void HandleCancelButton()
        {
            foreach (Transform child in GameListPanel.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            GameListArea.SetActive(false);
            GetGameListButton.SetActive(true);
            StartMatchMakingButton.SetActive(true);
            StopMatchMakingButton.SetActive(false);
            CreateGameButton.SetActive(true);
        }

        public void HandleCreateGameButton()
        {
            gameComp.SendRequestSpawnStart(new RequestSpawnStartMsg() { SceneID = sceneID, GameName = gameName, GameType = gameType, ServerRegion = serverRegion });
        }

        public void HandleSendChatButton()
        {
            chatComp.SendChatMsg(chatInputField.text);
            chatInputField.text = "";
        }

        public void HandleExitButton()
        {
            NetworkManager.singleton.StopClient();
        }

        private void CheckGamesList()
        {
            gamesList.Clear();

            if (gameComp.gamesList.Count > 0)
            {
                gamesList.AddRange(gameComp.gamesList);
                gameComp.gamesList.Clear();
                UpdateGameListUI();
            }
        }

        public void UpdateGameListUI()
        {
            foreach (GameContainer game in gamesList)
            {
                GameObject instance = Instantiate(GameListItemPrefab);
                instance.transform.parent = GameListPanel.transform;
                GUIGamesListEntry comp = instance.GetComponent<GUIGamesListEntry>();
                comp.clientComp = this;
                comp.UniqueID = game.UniqueId;
                comp.CurrentPlayers = game.CurrentPlayers;
                comp.MaxPlayers = game.MaxPlayers;
                comp.SceneID = game.SceneID;
                comp.JoinAnyTime = game.JoinAnyTime;
                comp.GameName = game.GameName;
                comp.GameType = game.GameType;
            }
        }

        void SetupMatchMakingSettings()
        {
            dropdownSceneID.options.Clear();
            List<string> listOfSceneID = new List<string>();
            foreach (var value in InsightClient.instance.gameSettingsModule.verifiedScenes)
            {
                listOfSceneID.Add(Path.GetFileNameWithoutExtension(value));
            }
            foreach (var _sceneID in listOfSceneID)
            {
                dropdownSceneID.options.Add(new Dropdown.OptionData() { text = _sceneID });
            }
            DropdownSelectedSceneID(dropdownSceneID);
            dropdownSceneID.onValueChanged.AddListener(delegate { DropdownSelectedSceneID(dropdownSceneID); });


            dropdownGameType.options.Clear();
            List<string> listOfGameType = new List<string>();
            foreach (var value in InsightClient.instance.gameSettingsModule.verifiedGameTypes)
            {
                listOfGameType.Add(value);
            }
            foreach (var _gameType in listOfGameType)
            {
                dropdownGameType.options.Add(new Dropdown.OptionData() { text = _gameType });
            }
            DropdownSelectedGameType(dropdownGameType);
            dropdownGameType.onValueChanged.AddListener(delegate { DropdownSelectedGameType(dropdownGameType); });
           

            dropdownServerRegion.options.Clear();
            List<string> listOfServerRegion = new List<string>();
            foreach (var value in InsightClient.instance.gameSettingsModule.verifiedServerRegions)
            {
                listOfServerRegion.Add(value);
            }
            foreach (var _serverRegion in listOfServerRegion)
            {
                dropdownServerRegion.options.Add(new Dropdown.OptionData() { text = _serverRegion });
            }
            DropdownSelectedServerRegion(dropdownServerRegion);
            dropdownServerRegion.onValueChanged.AddListener(delegate { DropdownSelectedServerRegion(dropdownServerRegion); });
        }

        void DropdownSelectedSceneID(Dropdown _dropdown)
        {
            int index = _dropdown.value;
            sceneID = _dropdown.value;
        }

        void DropdownSelectedGameType(Dropdown _dropdown)
        {
            int index = _dropdown.value;
            gameType = _dropdown.value;
        }

        void DropdownSelectedServerRegion(Dropdown _dropdown)
        {
            int index = _dropdown.value;
            serverRegion = _dropdown.value;
        }

    }
}
