using Ini;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MSNSQL
{
    class Program
    {
        static internal Logger logsys = new Logger();

        static internal class Settings
        {
            const string INI_FILENAME = "msnsql_settings.ini";

            static internal string INIPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\" + INI_FILENAME;
        }

        

        static void Main(string[] args)
        {
            Console.WriteLine("[INFORMATION] MSNSQL Tool - Version 1.1.0003 - Created by Verox");

            Dictionary<string, string> ArgumentDictionary = ProcessArgs(args);

            if (ArgumentDictionary.ContainsKey("f"))
            {
                Program.Settings.INIPath = ArgumentDictionary["f"];
                logsys.LogMessage(Logger.LogLevel.INFORMATION, "Parameter -f set to: " + Program.Settings.INIPath);
            }

            
            UserSettings settings = new UserSettings();
            DatabaseInteraction db = new DatabaseInteraction(logsys);

            try
            {
                db.SetConnectionString(UserSettings.SETTINGS.DATABASE_USER, UserSettings.SETTINGS.DATABASE_PASS, UserSettings.SETTINGS.DATABASE_ADDRESS, UserSettings.SETTINGS.DATABASE_PORT, UserSettings.SETTINGS.DATABASE_DB);              
            }
            catch
            {
                Console.Read();
                return;
            }

            /*if (args.Length >= 0)
            {
                if (args.Length == 1)
                {
                    settings.OverrideMissionDirectories(args[0]);
                }
                else if (args.Length == 2)
                {
                    settings.OverrideMissionDirectories(args[0], args[1]);
                }
            }*/

            try
            {
            db.GetMissionList(UserSettings.SETTINGS.DATABASE_TABLE, UserSettings.SETTINGS.DATABASE_FIELD);
            }
            catch
            {
                Console.Read();
                return;
            }


            Console.Read();
        }

        static Dictionary<string, string> ProcessArgs(string[] args)
        { 
            Dictionary<string, string> returnDict = new Dictionary<string,string>();

            for (int i = 0; i <= args.Length - 1; i++) {
                if (i == args.Length - 1)
                {
                    returnDict.Add(args[i].Substring(1), "true");
                    continue;
                }

                if (args[i][0] == '-') {
                    if (args[i + 1][0] != '-')
                    {
                        returnDict.Add(args[i].Substring(1), args[i + 1]);
                    }
                    else
                    {
                        returnDict.Add(args[i].Substring(1), "true");
                    }
                }
            }

            return returnDict;

        }
    }

    internal class DatabaseInteraction
    {
        Logger logsys;
        MySqlConnection _con;
        MySqlDataReader _reader;
        MySqlCommand _command;

        internal DatabaseInteraction(Logger logs) {
            logsys = logs;
        }

        internal void SetConnectionString(string username, string password, string address, string port, string database)
        {
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Attempting to connect to the MySQL server at " + address);

            string connectionString = "";
            connectionString += "Server=" + address + ";";
            if (!string.IsNullOrWhiteSpace(port)) { connectionString += "Port=" + port + ";"; }
            connectionString += "Database=" + database + ";";
            connectionString += "Uid=" + username + ";";
            connectionString += "Pwd=" + password + "";

            _con = new MySqlConnection(connectionString);

            try
            {
                _con.Open();
                logsys.LogMessage(Logger.LogLevel.INFORMATION, "Successfully connected to " + address + ". Server version is " + _con.ServerVersion);
            }
            catch (Exception ex)
            {
                logsys.LogMessage(Logger.LogLevel.FATAL, "Error connecting to the database! Exception: " + ex.Message);
                throw new Exception("FATAL ERROR");
            }
            finally
            {
                try
                {
                    _con.Close();
                }
                catch (Exception ex)
                {
                    logsys.LogMessage(Logger.LogLevel.FATAL, "Error closing database connection! Exception: " + ex.Message);
                }
            }

            
        }

        internal void GetMissionList(string table, string field)
        {
            string sqlstring = "SELECT " + field + " FROM " + table;

            if (UserSettings.SETTINGS.DATABASE_QUERY_OVERRIDE != null)
            {
                sqlstring = UserSettings.SETTINGS.DATABASE_QUERY_OVERRIDE;
            }

            MySqlDataReader _reader = null;
            try
            {
                logsys.LogMessage(Logger.LogLevel.INFORMATION, "Querying database...");
                _con.Open();
                _command = new MySqlCommand(sqlstring, _con);
                MySqlDataReader _rdr = _command.ExecuteReader();
                _reader = _rdr;

            }
            catch (Exception ex)
            {
                logsys.LogMessage(Logger.LogLevel.FATAL, "Error running the database query! Exception: " + ex.Message);
                _con.Close();
                throw new Exception("FATAL ERROR");
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Database query successful." + _reader.RecordsAffected);
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Getting file list (all ending in .pbo, non-recursive) in live mission directory...");

            string[] files = null;

            try
            {
                 files = Directory.GetFiles(UserSettings.SETTINGS.MISSION_LIVE_DIRECTORY, "*.pbo");
            }
            catch
            {
                logsys.LogMessage(Logger.LogLevel.FATAL, "Error accessing live mission directory.");
                throw new Exception("FATAL ERROR");
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Success accessing file list.");
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Creating file lookup table...");
            Hashtable filelookup = new Hashtable();

            foreach (string file in files) 
            {
                filelookup.Add(Path.GetFileName(file), true);
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "File look up table created. File count: " + filelookup.Count );
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Checking for and moving valid matches...");
            try
            {
                while (_reader.Read())
                {
                    try
                    {
                        if (filelookup.ContainsKey(_reader[0]))
                        {
                            logsys.LogMessage(Logger.LogLevel.INFORMATION, "MATCHED " + _reader[0] + ". Moving.");
                            if (!Directory.Exists(UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY))
                            {
                                logsys.LogMessage(Logger.LogLevel.ERROR, "The move destination directory does not exist or is not accessable.");
                            }
                            File.Move(UserSettings.SETTINGS.MISSION_LIVE_DIRECTORY + _reader[0], UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY + _reader[0] + ".broken");
                        }
                        else
                        {
                            logsys.LogMessage(Logger.LogLevel.DEBUG, "No match for mission: " + _reader[0]);
                        }

                    }
                    catch (Exception ex) {
                        logsys.LogMessage(Logger.LogLevel.ERROR, "An exception occured evaluating or moving mission " + _reader[0] + ". No operation was performed on this mission. The exception was: " + ex.Message);
                    }
                }
            }
            catch { }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Operation finished. Closing handles and shutting down.");

            _reader.Dispose();
            _con.Close();
        }
    }

    internal class UserSettings
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
            public static string DATABASE_QUERY_OVERRIDE = null;
        }

        enum FILESTATE
        {
            FILE_OK,
            FILE_MISSING,
            FILE_IN_USE,
            FILE_READ_ONLY,
        }

        const string DATABASE_SECTION = "DATABASE";
        const string GAMESERVER_SECTION = "GAMESERVER";


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
            GetIniValue(DATABASE_SECTION, "QUERY_OVERRIDE", ref SETTINGS.DATABASE_QUERY_OVERRIDE);

            GetIniValue(GAMESERVER_SECTION, "LIVE_MISSION_DIRECTORY", ref SETTINGS.MISSION_LIVE_DIRECTORY);
            GetIniValue(GAMESERVER_SECTION, "BROKEN_MISSION_DIRECTORY", ref SETTINGS.MISSION_BROKEN_DIRECTORY);


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
            } else {
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
            ini.IniWriteValue("GAMESERVER", "LIVE_MISSION_DIRECTORY", @"C:\UO\ArmA3\SRVX\MPMissions");
            ini.IniWriteValue("GAMESERVER", "BROKEN_MISSION_DIRECTORY", @"C:\UO\ArmA3\_BROKEN");
            ini.IniWriteValue("PROGRAM", "LOGLEVEL", "INFORMATION");
            ini.IniWriteValue("PROGRAM", "ACTION_ON_MATCH", "MOVE_ON_MATCH or MOVE_ON_NOMATCH"); // Implement tis behevior

        }
    }

    internal class Logger
    {
        public enum LogLevel
        {
            FATAL,
            CRITICAL,
            ERROR,
            WARNING,
            INFORMATION,
            DEBUG,
            NONE
        }

        LogLevel CurrentLogLevel = LogLevel.DEBUG;
        FileStream LogFileStream;
        StreamWriter fsWriter;
        

        internal Logger(LogLevel dLogLevel = LogLevel.DEBUG)
        {
            CurrentLogLevel = dLogLevel;

            if (CurrentLogLevel == LogLevel.NONE) { return; }

            try
            {
                //LogFileStream = File.OpenWrite("msnsql.log");
                //fsWriter = new StreamWriter(LogFileStream);
            }

            catch { CurrentLogLevel = LogLevel.NONE; LogMessage(LogLevel.ERROR, "An exception occured opening the log file for writing!"); }
        }

        internal void LogMessage(LogLevel loglevel, string logmessage, bool newline = true)
        {
            string writeString = "[" + loglevel + "] " + logmessage;
            //if (loglevel <= CurrentLogLevel) {
                //fsWriter.Write(DateTime.Now.ToShortDateString() + " - " + writeString);
                    
            //if (newline) {
                Console.WriteLine(writeString);
            //} else {
            //    Console.Write(logmessage);
            //}
            //}

        }
    }
}
