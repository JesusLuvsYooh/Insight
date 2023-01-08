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
            if (NetworkManager.singleton.numPlayers == 0)
            {
                // Moved CancelInvoke here, as we want to close dead GameServers, even after a players joined
                // Previously if players joined, then left, it would no longer be running the invoke and close if 0 players
                CancelInvoke();

                Debug.LogWarning("[ServerIdler] - No players connected within the allowed time. Shutting down server");

                NetworkManager.singleton.StopServer();

                Application.Quit();
            }
        }
    }
}
