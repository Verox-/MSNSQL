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
        Hashtable filelookup = new Hashtable();

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
            

            foreach (string file in files)
            {
                filelookup.Add(Path.GetFileName(file), true);
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "File look up table created. File count: " + filelookup.Count);
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Checking for and moving valid matches...");
            //try
            //{
                CheckMissions checker;

                // 
                if (UserSettings.SETTINGS.PROGRAM_BEHAVIOR == UserSettings.Behavior.BLACKLIST)
                {
                    checker = new CheckMissions(CheckBlacklist);
                }
                else
                {
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
                    logsys.LogMessage(Logger.LogLevel.ERROR, "The move destination directory does not exist or is not accessable.");
                }
                File.Move(UserSettings.SETTINGS.MISSION_LIVE_DIRECTORY + missionName, UserSettings.SETTINGS.MISSION_BROKEN_DIRECTORY + missionName + ".broken");
            }
            catch (Exception ex)
            {
                logsys.LogMessage(Logger.LogLevel.ERROR, "An exception occured moving mission " + missionName + ". No operation was performed on this mission. The exception was: " + ex.Message);

            }
        }

        private void CheckWhitelist(MySqlDataReader _reader)
        {
            while (_reader.Read())
            {
                if (filelookup.ContainsKey(_reader[0]))
                {
                    logsys.LogMessage(Logger.LogLevel.DEBUG, "Matched: " + _reader[0]);
                }
                else
                {
                    logsys.LogMessage(Logger.LogLevel.INFORMATION, "NO MATCH FOR " + _reader[0] + ". Moving.");
                    MoveFile(_reader[0].ToString());
                }
            }
        }

        private void CheckBlacklist(MySqlDataReader _reader)
        {
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
