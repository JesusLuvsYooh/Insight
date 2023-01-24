using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Insight
{
    public class InsightGameSettings : InsightModule
    {
        InsightClient client;

        [Scene] public string[] verifiedScenes;
        public string[] verifiedGameTypes;
        public string[] verifiedServerRegions;

        public override void Initialize(InsightClient insight, ModuleManager manager)
        {
            this.client = insight;

            client.gameSettingsModule = this;
        }
    }
}