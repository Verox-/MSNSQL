using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MSNSQL
{
    internal class DatabaseInteraction
    {
        private delegate void CheckMissions(MySqlDataReader _reader);

        Logger logsys;
        MySqlConnection _con;
        MySqlDataReader _reader;
        MySqlCommand _command;
        //Hashtable filelookup = new Hashtable();

        // Statistics
        int numErrors = 0;
        int numWarnings = 0;
        int numMoves = 0;
        int numFailedMoves = 0;
        int numDBMissions = 0;
        int numFileMissions = 0;

        string[] files = null;

        internal DatabaseInteraction(Logger logs)
        {
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

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Database query successful." + _reader.VisibleFieldCount);
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Getting file list (all ending in .pbo, non-recursive) in live mission directory...");

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
            
            //try
            //{
                CheckMissions checker;

                // 
                if (UserSettings.SETTINGS.PROGRAM_BEHAVIOR == UserSettings.Behavior.BLACKLIST)
                {
                    logsys.LogMessage(Logger.LogLevel.DEBUG, "Program in BLACKLIST mode.");
                    checker = new CheckMissions(CheckBlacklist);
                }
                else
                {
                    logsys.LogMessage(Logger.LogLevel.DEBUG, "Program in WHITELIST mode.");
                    checker = new CheckMissions(CheckWhitelist);
                }

                checker(_reader);


            //}
            //catch (Exception ex)
            //{
            //    logsys.LogMessage(Logger.LogLevel.CRITICAL, "An unknown error occured while checking missions. The exception was: " + ex.Message);
            //    logsys.LogMessage(Logger.LogLevel.CRITICAL, "The program cannot continue and will shut down.");
            //}

            

            _reader.Dispose();
            _con.Close();
        }

        private void MoveFile(string missionName)
        {
            try
            {
                if (!Directory.Exists(UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY))
                {
                    logsys.LogMessage(Logger.LogLevel.ERROR, "The move destination directory does not exist or is not accessable. Directory: " + UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY);
                    numFailedMoves++;
                    numErrors++;
                    return;
                }

                string source = Path.Combine(UserSettings.SETTINGS.MISSION_LIVE_DIRECTORY, missionName);
                string destination = Path.Combine(UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY, missionName + ".broken");

                File.Move(source, destination);
                numMoves++;
            }
            catch (Exception ex)
            {
                logsys.LogMessage(Logger.LogLevel.ERROR, "An exception occured moving mission " + missionName + ". No operation was performed on this mission. The exception was: " + ex.Message);
                numFailedMoves++;
                numErrors++;
            }
        }

        private void CreateHashtable()
        {

        }

        private void CheckWhitelist(MySqlDataReader _reader)
        {
            // Initialize a hashtable of all meeshuns in the database
            Hashtable _databaseMissions = new Hashtable();

            // We need to create a lookup table of all missions in the database. 
            while (_reader.Read())
            {
                try
                {
                    _databaseMissions.Add(_reader[0], true);
                }
                catch (Exception ex)
                {
                    logsys.LogMessage(Logger.LogLevel.ERROR, "Error adding to lookup table, skipping entry. The error was: " + ex.Message);
                }
            }
            

            // Loop through every mission inside the live mission directory
            // If it's found in the database, it's safe
            // If it's not found in the database, move it.
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Checking for and moving valid matches...");
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);

                if (_databaseMissions.ContainsKey(filename))
                {
                    logsys.LogMessage(Logger.LogLevel.DEBUG, "Matched: " + filename);
                }
                else
                {
                    logsys.LogMessage(Logger.LogLevel.INFORMATION, "NO MATCH IN DB FOR " + filename + ". Moving.");
                    MoveFile(filename);
                }
            }
        }

        private void CheckBlacklist(MySqlDataReader _reader)
        {
            Hashtable filelookup = new Hashtable();

            // Create a lookup table of missions in the live directory.
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Creating file lookup table...");

            // Loop through each file in the directory and add it to the table.
            foreach (string file in files)
            {
                filelookup.Add(Path.GetFileName(file), true);
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "File look up table created. File count: " + filelookup.Count);

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Checking for and moving valid matches...");
            while (_reader.Read())
            {
                if (filelookup.ContainsKey(_reader[0]))
                {
                    logsys.LogMessage(Logger.LogLevel.INFORMATION, "MATCHED " + _reader[0] + ". Moving.");
                    MoveFile(_reader[0].ToString());
                }
                else
                {
                    logsys.LogMessage(Logger.LogLevel.DEBUG, "No match for mission: " + _reader[0]);
                }
            }
        }
    }
}
