//using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Insight
{
    public class InsightGameSettings : InsightModule
    {
        InsightClient client;

        public bool JoinAnyTime;
        [Tooltip("True strips out IP and Port from Games List sent to clients, if that server is not connectable. Full/Private/JoinAnyTime(false).")]
        public bool StripConnectionInfo;

        //[Scene] public string[] verifiedScenes;
        public string[] verifiedScenes;
        public string[] verifiedGameTypes;
        public string[] verifiedServerRegions;

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            this.client = insight;

            client.gameSettingsModule = this;
        }
    }
}