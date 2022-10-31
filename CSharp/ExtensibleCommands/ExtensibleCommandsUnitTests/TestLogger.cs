using System;
using System.IO;
using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Implements logger: print out in Console window
    /// </summary>
    public class TestLogger : ILog
    {
        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        public void Log(string timestamp, string level, string message)
        {
            Console.WriteLine("{0} - {1} - {2}", timestamp, level, message);
        }
    }
}
