using System;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Basic type for all operational exceptions thrown from within command execution path 
    /// </summary>
    public class ExtensibleCommandsException : Exception
    {
        /// <summary> Error ID </summary>
        public int ID { get; }

        /// <summary> Error description </summary>
        public string Text { get; }

        /// <summary> Underlying exception </summary>
        public Exception Exception { get; }

        /// <summary> Timestamp of when the failure occurred </summary>
        public string Timestamp { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        public ExtensibleCommandsException(int id, string text)
            : base(text)
        {
            ID              = id;
            Text            = text;
            Timestamp       = Logger.TimeStamp;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        /// <param name="e">Underlying exception</param>
        public ExtensibleCommandsException(int id, string text, Exception e)
            : this(id, text)
        {
            Exception = e;
        }
    }
}
