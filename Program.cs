using System;
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
            logsys.LogMessage(Logger.LogLevel.INFORMATION, "MSNSQL Tool - Version 1.1 - Created by Verox");

            Dictionary<string, string> ArgumentDictionary = ProcessArgs(args);

            if (ArgumentDictionary.ContainsKey("f"))
            {
                Program.Settings.INIPath = ArgumentDictionary["f"];
                logsys.LogMessage(Logger.LogLevel.INFORMATION, "Parameter -f set to: " + Program.Settings.INIPath);
            }

            // Set up the user's settings
            UserSettings settings = new UserSettings();

            // Set the requested log level
            logsys.SetLogLevel(UserSettings.SETTINGS.PROGRAM_LOGLEVEL);

            DatabaseInteraction db = new DatabaseInteraction(logsys);

            try
            {
                db.SetConnectionString(UserSettings.SETTINGS.DATABASE_USER, UserSettings.SETTINGS.DATABASE_PASS, UserSettings.SETTINGS.DATABASE_ADDRESS, UserSettings.SETTINGS.DATABASE_PORT, UserSettings.SETTINGS.DATABASE_DB);              
            }
            catch
            {
                //Console.Read();
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
                logsys.LogMessage(Logger.LogLevel.FATAL, "An unknown exception occurred. The program cannot continue and will shut down.");
                //Console.Read();
                return;
            }

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Operation finished. Closing handles and shutting down.");

            logsys.LogMessage(Logger.LogLevel.INFORMATION, "Shut down complete. Press any key to close.");
            //Console.Read();
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
}
