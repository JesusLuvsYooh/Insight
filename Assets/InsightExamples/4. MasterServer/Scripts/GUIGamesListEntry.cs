using UnityEngine;
using UnityEngine.UI;

namespace Insight.Examples
{
    public class GUIGamesListEntry : MonoBehaviour
    {
        public PlayerClientGUI clientComp;

        public Text SceneNameText;
        public Text PlayerCountText;

        public string UniqueID;
        public string SceneName;
        public int CurrentPlayers;
        public int MaxPlayers;

        public bool JoinAnyTime;

        private bool Init;

        private void LateUpdate()
        {
            if (!Init)
            {
                Init = true;

                SceneNameText.text = SceneName;
                PlayerCountText.text = CurrentPlayers + "/" + MaxPlayers;
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
