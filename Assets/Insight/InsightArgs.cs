﻿using System;
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
            SceneID = ExtractValueInt(Names.SceneID, 0);
            FrameRate = ExtractValueInt(Names.FrameRate, 30);
            ProcessName = ExtractValue(Names.ProcessName, "GameServer.exe");
            ProcessesMax = ExtractValueInt(Names.ProcessesMax, 5);
            ProcessIdleExit = ExtractValueInt(Names.ProcessIdleExit, 60);
            ProcessSpawnerIP = ExtractValue(Names.ProcessSpawnerIP, "localhost");
            ProcessSpawnerPort = ExtractValueInt(Names.ProcessSpawnerPort, 7777);
            PlayersMax = ExtractValueInt(Names.PlayersMax, 100);
            NoisyLogs = ExtractValue(Names.NoisyLogs, "");
            ServerID = ExtractValue(Names.ServerID, "");
            JoinAnyTime = ExtractValue(Names.JoinAnyTime, "");
            GameName = ExtractValue(Names.GameName, "");
            GameType = ExtractValueInt(Names.GameType, 0);
            ServerRegion = ExtractValueInt(Names.ServerRegion, 0);
            AutoAuthClients = ExtractValue(Names.AutoAuthClients, "");
            PlayerStayConnected = ExtractValue(Names.PlayerStayConnected, "");
            GameServerIP = ExtractValue(Names.GameServerIP, "localhost");
            GameServerPort = ExtractValueInt(Names.GameServerPort, 7777);
            MasterServerIP = ExtractValue(Names.MasterServerIP, "localhost");
            MasterServerPort = ExtractValueInt(Names.MasterServerPort, 7777);
        }

        #region Arguments
        public string NetworkAddress { get; private set; } // example, 123.1.1.1
        public int NetworkPort { get; private set; } // example, 7777
        public string UniqueID { get; private set; } // ignore, auto set for spawned GameServers
        public int SceneID { get; private set; } // example, Map2
        public int FrameRate { get; private set; } // example, 30
        public string ProcessName { get; private set; } // example. GameServer.exe
        public int ProcessesMax { get; private set; } // amount of GameServers per VPS Spawner, example, 5
        public int ProcessIdleExit { get; private set; } // seconds to close GameServer if no players, example, 60
        public string ProcessSpawnerIP { get; private set; } // start port, increments per spawned process
        public int ProcessSpawnerPort { get; private set; } // start port, increments per spawned process
        public int PlayersMax { get; private set; } // max connections per spawned GameServer, example, 50
        public string NoisyLogs { get; private set; } // for debugging, on/off, example, on
        public string ServerID { get; private set; } // future use for api or invite codes
        public string JoinAnyTime { get; private set; } // allow joining of gamese in progress
        public string GameName { get; private set; } // name of your game
        public int GameType { get; private set; } // example, 1 = free for all, 2 deathmatch
        public int ServerRegion { get; private set; } // example, 1 = US
        public string AutoAuthClients { get; private set; }
        public string PlayerStayConnected { get; private set; }
        public string GameServerIP { get; private set; }
        public int GameServerPort { get; private set; }
        public string MasterServerIP { get; private set; } // example, 123.1.1.1
        public int MasterServerPort { get; private set; } // example, 7777
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
            public string SceneID { get { return "-SceneID"; } }
            public string FrameRate { get { return "-FrameRate"; } }
            public string ProcessName { get { return "-ProcessName"; } }
            public string ProcessesMax { get { return "-ProcessesMax"; } }
            public string ProcessIdleExit { get { return "-ProcessIdleExit"; } }
            public string ProcessSpawnerIP { get { return "-ProcessSpawnerIP"; } }
            public string ProcessSpawnerPort { get { return "-ProcessSpawnerPort"; } }
            public string PlayersMax { get { return "-PlayersMax"; } }
            public string NoisyLogs { get { return "-NoisyLogs"; } }
            public string ServerID { get { return "-ServerID"; } }
            public string JoinAnyTime { get { return "-JoinAnyTime"; } }
            public string GameName { get { return "-GameName"; } }
            public string GameType { get { return "-GameType"; } }
            public string ServerRegion { get { return "-ServerRegion"; } }
            public string AutoAuthClients { get { return "-AutoAuthClients"; } }
            public string PlayerStayConnected { get { return "-PlayerStayConnected"; } }
            public string GameServerIP { get { return "-GameServerIP"; } }
            public string GameServerPort { get { return "-GameServerPort"; } }
            public string MasterServerIP { get { return "-MasterServerIP"; } }
            public string MasterServerPort { get { return "-MasterServerPort"; } }
        }
    }
}