using System;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Implements Logging facade. An external logger must be supplied to perform the actual logging.
    /// </summary>
    public static class Logger
    {
        /// <summary> Enumeration for Log levels </summary>
        public enum LogLevel
        {
            Info,
            Error
        };

        /// <summary> Externally supplied Logger object implementing ILog interface </summary>
        public static ILog ExternalLogger { get; set; }

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
            if (ExternalLogger == null) return;

            // Always log errors regardless of the logging enabled flag
            if (IsLoggingEnabled || logLevel == LogLevel.Error)
                ExternalLogger.Log(TimeStamp, logLevel.ToString(), message);
        }
    }
}
