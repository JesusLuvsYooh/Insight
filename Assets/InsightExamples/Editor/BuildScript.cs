using UnityEditor;

namespace Insight.Examples
{
    public class BuildScript
    {
        public static string ScenesRoot = "Assets/InsightExamples/4. MasterServer/Scenes/";
        public static BuildOptions BuildOptions = BuildOptions.Development | BuildOptions.EnableHeadlessMode;
        public static string PrevPath = null;
        public static string extension = ".exe";
        public static bool individualFolders = true;

        [MenuItem("Build Insight/Build All Win", false, 0)]
        public static void BuildAllMenu()
        {
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildMasterServer(path);
                BuildRemoteSpawner(path);
                BuildGameServer(path);
                BuildPlayerClient(path);
            }
        }
        [MenuItem("Build Insight/Build All Mac", false, 0)]
        public static void BuildAllMenuMac()
        {
#if UNITY_STANDALONE_OSX
            extension = ".app";
            BuildOptions = BuildOptions.Development;
#endif
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildMasterServer(path);
                BuildRemoteSpawner(path);
                BuildGameServer(path);
                BuildPlayerClient(path);
            }
        }

        [MenuItem("Build Insight/MasterServer", false, 100)]
        public static void BuildMasterServerMenu()
        {
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildMasterServer(path);
            }
        }

        [MenuItem("Build Insight/RemoteSpawner", false, 101)]
        public static void BuildRemoteSpawnerMenu()
        {
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildRemoteSpawner(path);
            }
        }

        [MenuItem("Build Insight/GameServer", false, 102)]
        public static void BuildGameServerMenu()
        {
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildGameServer(path);
            }
        }

        [MenuItem("Build Insight/PlayerClient", false, 103)]
        public static void BuildPlayerClientMenu()
        {
            string path = GetPath();
            if (!string.IsNullOrEmpty(path))
            {
                BuildPlayerClient(path);
            }
        }

        public static void BuildMasterServer(string path)
        {
            string[] scenes = new[]
            {
        ScenesRoot+"MasterServer.unity"
        };
            PlayerSettings.productName = "MasterServer";
            if (individualFolders)
            {
                path += "/" + PlayerSettings.productName;
            }
            BuildPipeline.BuildPlayer(scenes, path + "/MasterServer" + extension, GetBuildTarget(), BuildOptions);
        }

        public static void BuildRemoteSpawner(string path)
        {
            string[] gameServerScenes = new[]
            {
        ScenesRoot+"RemoteSpawner.unity"
        };
            PlayerSettings.productName = "RemoteSpawner";
            if (individualFolders)
            {
                path += "/" + PlayerSettings.productName;
            }
            BuildPipeline.BuildPlayer(gameServerScenes, path + "/RemoteSpawner" + extension, GetBuildTarget(), BuildOptions);
        }

        public static void BuildGameServer(string path)
        {
            string[] gameServerScenes = new[]
            {
        ScenesRoot+"GameServer.unity",
        //Scene used for MasterServer Demo
        ScenesRoot+"SuperAwesomeGame.unity",
        ScenesRoot+"GreatGoodMap.unity"
        };
            PlayerSettings.productName = "GameServer";
            if (individualFolders)
            {
                path += "/"+ PlayerSettings.productName;
            }
            BuildPipeline.BuildPlayer(gameServerScenes, path + "/GameServer" + extension, GetBuildTarget(), BuildOptions);
        }

        public static void BuildPlayerClient(string path)
        {
            string[] scenes = new[]
            {
        ScenesRoot+"PlayerClient.unity",
        //Scene used for MasterServer Demo
        ScenesRoot+"SuperAwesomeGame.unity",
        ScenesRoot+"GreatGoodMap.unity"
        };
            PlayerSettings.productName = "PlayerClient";
            if (individualFolders)
            {
                path += "/" + PlayerSettings.productName;
            }
            BuildPipeline.BuildPlayer(scenes, path + "/PlayerClient" + extension, GetBuildTarget(), BuildOptions);
        }

        #region Helpers
        public static string GetPath()
        {
            string prevPath = EditorPrefs.GetString("msf.buildPath", "");
            string path = EditorUtility.SaveFolderPanel("Choose Location for binaries", prevPath, "");

            if (!string.IsNullOrEmpty(path))
            {
                EditorPrefs.SetString("msf.buildPath", path);
            }
            return path;
        }

        public static BuildTarget GetBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }
        #endregion
    }
}
