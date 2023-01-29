using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Insight.Examples
{
    public class GUIGamesListEntry : MonoBehaviour
    {
        public PlayerClientGUI clientComp;

        public Text SceneNameText;
        public Text GameTypeText;
        public Text RegionText;
        public Text PlayerCountText;
        public Button SelectButton;

        public string UniqueID;
        public int SceneID;
        public int CurrentPlayers;
        public int MaxPlayers;

        public bool JoinAnyTime;
        public string GameName;
        public int GameType;
        public int Region;

        private bool Init;

        private void LateUpdate()
        {
            if (!Init)
            {
                Init = true;

                // SceneNameText.text = SceneID.ToString();
                //SceneNameText.text = Path.GetFileNameWithoutExtension(clientComp.gameSettingsModule.verifiedScenes[SceneID]);
                SceneNameText.text = clientComp.gameSettingsModule.verifiedScenes[SceneID];
                GameTypeText.text = clientComp.gameSettingsModule.verifiedGameTypes[GameType];
                RegionText.text = clientComp.gameSettingsModule.verifiedServerRegions[Region];
                PlayerCountText.text = CurrentPlayers + "/" + MaxPlayers;

                if (JoinAnyTime && CurrentPlayers < MaxPlayers)
                { SelectButton.interactable = true; }
            }
        }

        public void HandleSelectButton()
        {
            if (JoinAnyTime)
            {
                clientComp.HandleJoinGameButton(UniqueID);
            }
            else
            {
                if (InsightClient.instance.NoisyLogs)
                    Debug.Log("[GUIGamesListEntry] - Game does not allow joining whilst in progress.");
            }
        }
    }
}
