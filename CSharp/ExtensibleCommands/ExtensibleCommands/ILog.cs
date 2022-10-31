namespace ExtensibleCommands
{
    /// <summary> 
    /// Interface representing simple log functionality for external dependency injection
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Log message
        /// </summary>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        void Log(string timestamp, string level, string message);
    }
}
