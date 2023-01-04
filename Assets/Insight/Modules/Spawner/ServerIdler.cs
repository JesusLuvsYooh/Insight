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
                InvokeRepeating("UpdateIdleState", MaxSecondsOfIdle, MaxSecondsOfIdle);
            }
        }

        void UpdateIdleState()
        {
            //Cancel if players connect to the game.
            if(NetworkManager.singleton.numPlayers == 0)
            {
                Debug.LogWarning("[ServerIdler] - No players connected within the allowed time. Shutting down server");

                NetworkManager.singleton.StopServer();

                Application.Quit();
            }

            CancelInvoke();
        }
    }
}
