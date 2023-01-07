using Mirror;
using UnityEngine;

namespace Insight
{
    public class ServerIdler : InsightModule
    {
        public int MaxSecondsOfIdle = 60;

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            if (MaxSecondsOfIdle > 0)
            {
                CancelInvoke();
                InvokeRepeating("UpdateIdleState", MaxSecondsOfIdle, MaxSecondsOfIdle);
            }
        }

        void UpdateIdleState()
        {
            print("UpdateIdleState");
            //Cancel if players connect to the game.
            if (NetworkManager.singleton.numPlayers == 0)
            {
                Debug.LogWarning("[ServerIdler] - No players connected within the allowed time. Shutting down server");

                NetworkManager.singleton.StopServer();

                Application.Quit();
            }

            CancelInvoke();
        }

        private void Start()
        {
            // Rare cases "Initialize" is not being called, or was cancelled, resulting in GameServers not shutting down if 0 players
            // Dirty fix to check for that rare case
            if (MaxSecondsOfIdle > 0)
            {
                CancelInvoke();
                InvokeRepeating("UpdateIdleState", MaxSecondsOfIdle, MaxSecondsOfIdle);
            }
        }
    }
}
