﻿using UnityEditor;

namespace Insight.Examples
{
    public class BuildScript
    {
        public static string ScenesRoot = "Assets/InsightExamples/4. MasterServer/Scenes/";
        public static BuildOptions BuildOptions = BuildOptions.Development | BuildOptions.EnableHeadlessMode;
        public static string PrevPath = null;

        [MenuItem("Build Insight/Build All", false, 0)]
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
            BuildPipeline.BuildPlayer(scenes, path + "/MasterServer.exe", GetBuildTarget(), BuildOptions);
        }

        public static void BuildRemoteSpawner(string path)
        {
            string[] gameServerScenes = new[]
            {
        ScenesRoot+"RemoteSpawner.unity"
        };
            PlayerSettings.productName = "RemoteSpawner";
            BuildPipeline.BuildPlayer(gameServerScenes, path + "/RemoteSpawner.exe", GetBuildTarget(), BuildOptions);
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
            BuildPipeline.BuildPlayer(gameServerScenes, path + "/GameServer.exe", GetBuildTarget(), BuildOptions);
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
            BuildPipeline.BuildPlayer(scenes, path + "/PlayerClient.exe", GetBuildTarget(), BuildOptions);
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
