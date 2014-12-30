using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSNSQL
{
    class Logger
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

        // The log level we will be logging at.
        LogLevel CurrentLogLevel = LogLevel.INFORMATION;

        // The stream to the log file
        System.IO.StreamWriter file;
        //FileStream LogFileStream;
        //StreamWriter fsWriter;
        

        internal Logger(LogLevel dLogLevel = LogLevel.INFORMATION)
        {
            
            // Set the requested log level
            CurrentLogLevel = dLogLevel;

            // If we don't want to log anything at all, exit now.
            if (CurrentLogLevel == LogLevel.NONE) { return; }

            // Open the stream to the log file
            try
            {
                file = new System.IO.StreamWriter(@"msnsql.log", true);
            }
            catch 
            { 
                CurrentLogLevel = LogLevel.NONE; 
                LogMessage(LogLevel.ERROR, "An exception occured opening the log file for writing!"); 
            }
        }

        /// <summary>
        /// Directly sets the program's log level.
        /// </summary>
        /// <param name="dLogLevel">Requested log level</param>
        internal void SetLogLevel(LogLevel dLogLevel)
        {
            CurrentLogLevel = dLogLevel;
        }

        /// <summary>
        /// Parses a string representation into a log level and sets it.
        /// </summary>
        /// <param name="dLogLevel">Valid string representation of requested log level.</param>
        internal void SetLogLevel(string dLogLevel)
        {
            LogLevel parsedLevel;

            try
            {
                parsedLevel = (LogLevel)LogLevel.Parse(typeof(LogLevel), dLogLevel, true);

            }
            catch
            {
                LogMessage(LogLevel.ERROR, "An exception occured parsing " + dLogLevel + " to a valid log level. Please review the documentation for valid log levels.");
                return;
            }

            // Set the log level in the other function.
            SetLogLevel(parsedLevel);
        }

        /// <summary>
        /// Logs a message to the console and to the log file
        /// </summary>
        /// <param name="loglevel">Severity of the message to be logged</param>
        /// <param name="logmessage">Message to be logged</param>
        /// <param name="newline">Newline overide, for multi-part messages.</param>
        internal void LogMessage(LogLevel loglevel, string logmessage, bool newline = true)
        {
            string writeString = "[" + loglevel + "] " + logmessage;

            // If the severity of this message is lower than the requested log level, ignore it.
            if (loglevel <= CurrentLogLevel) 
            {                 
                // Write the message to the console and log.
                if (newline) {
                    Console.WriteLine(writeString);
                    file.WriteLine(DateTime.Now.ToShortDateString() + " - " + writeString);
                } else {
                    Console.Write(logmessage);
                    file.Write(DateTime.Now.ToShortDateString() + " - " + writeString);
                }
            }

            // Flush the writer
            file.Flush();

        }
    }
}
