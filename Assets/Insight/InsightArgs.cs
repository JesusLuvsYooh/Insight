using System;
using System.Linq;

// Add any new args in this script

namespace Insight
{
    public class InsightArgs
    {
        private readonly string[] _args;

        public ArgNames Names;

        public InsightArgs()
        {
            _args = Environment.GetCommandLineArgs();

            Names = new ArgNames();

            NetworkAddress = ExtractValue(Names.NetworkAddress, "localhost");
            NetworkPort = ExtractValueInt(Names.NetworkPort, 7777);
            UniqueID = ExtractValue(Names.UniqueID, "");
            SceneName = ExtractValue(Names.SceneName, "");
            FrameRate = ExtractValueInt(Names.FrameRate, 30);
            GameMode = ExtractValueInt(Names.GameMode, 7777);
            ProcessName = ExtractValue(Names.ProcessName, "GameServer.exe");
            ProcessesMax = ExtractValueInt(Names.ProcessesMax, 5);
            ProcessIdleExit = ExtractValueInt(Names.ProcessIdleExit, 60);
            PlayersMax = ExtractValueInt(Names.PlayersMax, 100);
            NoisyLogs = ExtractValueBool(Names.NoisyLogs, true);
            ServerID = ExtractValue(Names.ServerID, "");
            JoinAnyTime = ExtractValueBool(Names.JoinAnyTime, false);
        }

        #region Arguments
        public string NetworkAddress { get; private set; } // example, 123.1.1.1
        public int NetworkPort { get; private set; } // example, 7777
        public string UniqueID { get; private set; } // ignore, auto set for spawned GameServers
        public string SceneName { get; private set; } // example, Map2
        public int FrameRate { get; private set; } // example, 30
        public int GameMode { get; private set; } // example, 1 = free for all, 2 deathmatch
        public string ProcessName { get; private set; } // example. GameServer.exe
        public int ProcessesMax { get; private set; } // amount of GameServers per VPS Spawner, example, 5
        public int ProcessIdleExit { get; private set; } // seconds to close GameServer if no players, example, 60
        public int PlayersMax { get; private set; } // max connections per spawned GameServer, example, 50
        public bool NoisyLogs { get; private set; } // for debugging, on/off, example, on
        public string ServerID { get; private set; } // future use for api or invite codes
        public bool JoinAnyTime { get; private set; } // allow joining of gamese in progress
        #endregion

        #region Helper methods
        public string ExtractValue(string argName, string defaultValue = null)
        {
            if (!_args.Contains(argName))
                return defaultValue;

            int index = _args.ToList().FindIndex(0, a => a.Equals(argName));
            return _args[index + 1];
        }

        public int ExtractValueInt(string argName, int defaultValue = -1)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToInt32(number);
        }

        public bool ExtractValueBool(string argName, bool defaultValue = false)
        {
            var number = ExtractValue(argName, defaultValue.ToString());
            return Convert.ToBoolean(number);
        }

        public bool IsProvided(string argName)
        {
            return _args.Contains(argName);
        }

        #endregion

        public class ArgNames
        {
            public string NetworkAddress { get { return "-NetworkAddress"; } }
            public string NetworkPort { get { return "-NetworkPort"; } }
            public string UniqueID { get { return "-UniqueID"; } }
            public string SceneName { get { return "-SceneName"; } }
            public string FrameRate { get { return "-FrameRate"; } }
            public string GameMode { get { return "-GameMode"; } }
            public string ProcessName { get { return "-ProcessName"; } }
            public string ProcessesMax { get { return "-ProcessesMax"; } }
            public string ProcessIdleExit { get { return "-ProcessIdleExit"; } }
            public string PlayersMax { get { return "-PlayersMax"; } }
            public string NoisyLogs { get { return "-NoisyLogs"; } }
            public string ServerID { get { return "-ServerID"; } }
            public string JoinAnyTime { get { return "-JoinAnyTime"; } }
        }
    }
}