using System;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Implements static properties and methods that are applicable to generic Command behavior.
    /// </summary>
    public static class ExtensibleCommandsCore
    {
        /// <summary> Enumeration for Log levels </summary>
        public enum LogLevel
        {
            Info,
            Error
        };

        /// <summary> Externally supplied Logger object implementing ILog interface </summary>
        public static ILog Logger { get; set; }

        /// <summary> Timestamp string </summary>
        public static string TimeStamp
        {
            get { return string.Format("{0:yyyy/MM/dd HH:mm:ss:fff}", DateTime.Now); }
        }

        /// <summary> Is logging of command events enabled? </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Log informational or error message
        /// </summary>
        /// <param name="logLevel">Log level</param>
        /// <param name="message">Message to be logged</param>
        public static void Log(LogLevel logLevel, string message)
        {
            if (Logger == null) return;

            // Always log errors regardless of the logging enabled flag
            if (IsLoggingEnabled || logLevel == LogLevel.Error)
                Logger.Log(TimeStamp, logLevel.ToString(), message);
        }
    }
}
