using Ini;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MSNSQL
{
    class UserSettings
    {
        public struct SETTINGS
        {
            public static string MISSION_LIVE_DIRECTORY = @"C:\UO\ArmA3\SRVBBB\MPMissions";
            public static string MISSION_BROKEN_DIRECTORY = @"C:\UO\ArmA3\_BROKEN";

            public static string DATABASE_ADDRESS = "localhost";
            public static string DATABASE_PORT = "";
            public static string DATABASE_USER = "";
            public static string DATABASE_PASS = "";

            public static string DATABASE_DB = "impulse9_invision";
            public static string DATABASE_TABLE = "ccs_custom_database_7";
            public static string DATABASE_FIELD = "field_48";
            public static string DATABASE_CONDITION = ""; // Implement
            public static string DATABASE_QUERY_OVERRIDE = null;

            public static Behavior PROGRAM_BEHAVIOR = Behavior.BLACKLIST;
            public static string PROGRAM_LOGLEVEL = "INFORMATION";
        }

        enum FILESTATE
        {
            FILE_OK,
            FILE_MISSING,
            FILE_IN_USE,
            FILE_READ_ONLY,
        }

        internal enum Behavior
        {
            BLACKLIST,
            WHITELIST,
            BOTH
        }

        const string DATABASE_SECTION = "DATABASE";
        const string GAMESERVER_SECTION = "GAMESERVER";
        const string PROGRAM_SECTION = "PROGRAM";


        IniFile ini;

        internal UserSettings()
        {
            FILESTATE returnstate = getIniFile();
            if (returnstate == FILESTATE.FILE_MISSING) { Console.Read(); return; }

            GetIniValue(DATABASE_SECTION, "ADDRESS", ref SETTINGS.DATABASE_ADDRESS);
            GetIniValue(DATABASE_SECTION, "PORT", ref SETTINGS.DATABASE_PORT);
            GetIniValue(DATABASE_SECTION, "USER", ref SETTINGS.DATABASE_USER);
            GetIniValue(DATABASE_SECTION, "PASS", ref SETTINGS.DATABASE_PASS);

            GetIniValue(DATABASE_SECTION, "DB", ref SETTINGS.DATABASE_DB);
            GetIniValue(DATABASE_SECTION, "TABLE", ref SETTINGS.DATABASE_TABLE);
            GetIniValue(DATABASE_SECTION, "FIELD", ref SETTINGS.DATABASE_FIELD);
            GetIniValue(DATABASE_SECTION, "CONDITION", ref SETTINGS.DATABASE_CONDITION);
            GetIniValue(DATABASE_SECTION, "QUERY_OVERRIDE", ref SETTINGS.DATABASE_QUERY_OVERRIDE);

            GetIniValue(GAMESERVER_SECTION, "LIVE_MISSION_DIRECTORY", ref SETTINGS.MISSION_LIVE_DIRECTORY);
            GetIniValue(GAMESERVER_SECTION, "BROKEN_MISSION_DIRECTORY", ref SETTINGS.MISSION_BROKEN_DIRECTORY);

            string temp_behavior = "BLACKLIST";
            GetIniValue(PROGRAM_SECTION, "BEHAVIOR", ref temp_behavior);
            ParseBehavior(temp_behavior);
            GetIniValue(PROGRAM_SECTION, "LOGLEVEL", ref SETTINGS.PROGRAM_LOGLEVEL);


        }

        private void ParseBehavior(string inBeh)
        {
            Behavior parsedLevel = Behavior.BLACKLIST;

            try
            {
                parsedLevel = (Behavior)Behavior.Parse(typeof(Behavior), inBeh, true);
            }
            catch
            {
                Program.logsys.LogMessage(Logger.LogLevel.ERROR, "An exception occured parsing " + inBeh + " to a valid behavior. Please review the documentation for valid program operation modes.");
                return;
            }

            // Set the log level in the other function.
            SETTINGS.PROGRAM_BEHAVIOR = parsedLevel;
        }

        internal void ValidateDirectories()
        {
            if (!Directory.Exists(SETTINGS.MISSION_BROKEN_DIRECTORY))
            {
                Program.logsys.LogMessage(Logger.LogLevel.CRITICAL, "Configured BROKEN_MISSION_DIRECTORY does not exist or is not accessable.");
                Program.logsys.LogMessage(Logger.LogLevel.INFORMATION, "BROKEN_MISSION_DIRECTORY is: " + SETTINGS.MISSION_BROKEN_DIRECTORY);
                Program.logsys.LogMessage(Logger.LogLevel.INFORMATION, "The program will continue but will not be able to move missions.");
            }
            if (!Directory.Exists(SETTINGS.MISSION_LIVE_DIRECTORY))
            {
                Program.logsys.LogMessage(Logger.LogLevel.CRITICAL, "Configured LIVE_MISSION_DIRECTORY does not exist or is not accessable.");
                Program.logsys.LogMessage(Logger.LogLevel.INFORMATION, "LIVE_MISSION_DIRECTORY is: " + SETTINGS.MISSION_LIVE_DIRECTORY);
                Program.logsys.LogMessage(Logger.LogLevel.INFORMATION, "The program will continue but will not be able to move missions.");
            }
        }

        internal void OverrideMissionDirectories(string BrokenMissionDir, string LiveMissionDir = null)
        {
            Program.logsys.LogMessage(Logger.LogLevel.DEBUG, "Configured LIVE_MISSION_DIRECTORY does not exist or is not accessable.");
            if (LiveMissionDir == null)
            {
                if (!GetIniValue(GAMESERVER_SECTION, "LIVE_MISSION_DIRECTORY", ref SETTINGS.MISSION_LIVE_DIRECTORY))
                {
                    SETTINGS.MISSION_LIVE_DIRECTORY = Directory.GetCurrentDirectory();
                }
            }
            else
            {
                SETTINGS.MISSION_LIVE_DIRECTORY = LiveMissionDir;
            }

            SETTINGS.MISSION_BROKEN_DIRECTORY = BrokenMissionDir;

        }

        private FILESTATE getIniFile()
        {
            if (!File.Exists(Program.Settings.INIPath))
            {
                ini = new IniFile(Program.Settings.INIPath);
                PopulateINIFile();
                ShowDocumentation();
                Console.Read();
                Program.logsys.LogMessage(Logger.LogLevel.WARNING, "Missing settings file! Creating new settings file.");
                return FILESTATE.FILE_MISSING;
            }

            ini = new IniFile(Program.Settings.INIPath);

            return FILESTATE.FILE_OK;

        }

        private void ShowDocumentation()
        {
            Console.WriteLine("Please review settings file before using this program");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Start parameters:");
            Console.WriteLine("-f <path> - Overrides settings .ini location with fully qualified path '<path>' (e.g. C:\\Test\\MSNSQL_SRV1_SETTINGS.ini)");
            Console.WriteLine();
            Console.WriteLine("");
            Console.WriteLine();
            Console.WriteLine("");
            Console.WriteLine();

            Console.Read();
        }

        private bool GetIniValue(string section, string key, ref string value)
        {
            string readValue = ini.IniReadValue(section, key);
            if (string.IsNullOrWhiteSpace(readValue))
            {
                Program.logsys.LogMessage(Logger.LogLevel.DEBUG, "INI: VALUE FOR KEY " + key + " WAS BLANK! USING DEFAULT VALUE: " + value);
                return false;
            }
            else
            {
                value = readValue;
                return true;
            }
        }

        private void PopulateINIFile()
        {
            ini.IniWriteValue("DATABASE", "ADDRESS", "162.144.14.12");
            ini.IniWriteValue("DATABASE", "PORT", "");
            ini.IniWriteValue("DATABASE", "USER", "");
            ini.IniWriteValue("DATABASE", "PASS", "");
            ini.IniWriteValue("DATABASE", "DB", "impulse9_invision");
            ini.IniWriteValue("DATABASE", "TABLE", "ccs_custom_database_7");
            ini.IniWriteValue("DATABASE", "FIELD", "field_48");
            ini.IniWriteValue("DATABASE", "CONDITION", "");
            ini.IniWriteValue("GAMESERVER", "LIVE_MISSION_DIRECTORY", @"C:\UO\ArmA3\SRVX\MPMissions");
            ini.IniWriteValue("GAMESERVER", "BROKEN_MISSION_DIRECTORY", @"C:\UO\ArmA3\_BROKEN");
            ini.IniWriteValue("PROGRAM", "LOGLEVEL", "INFORMATION");
            ini.IniWriteValue("PROGRAM", "BEHAVIOR", "BLACKLIST"); // Implement tis behevior
        }
    }
}
