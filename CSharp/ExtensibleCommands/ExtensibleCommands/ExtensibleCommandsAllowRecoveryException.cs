using System;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Exception that allows a Recovery Command to perform recovery, 
    /// i.e. it does not result in immediate termination of the operation.
    /// All recoverable failures should be of this type.
    /// </summary>
    public class ExtensibleCommandsAllowRecoveryException : ExtensibleCommandsException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        public ExtensibleCommandsAllowRecoveryException(int id, string text)
            : base(id, text)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        /// <param name="e">Underlying exception</param>
        public ExtensibleCommandsAllowRecoveryException(int id, string text, Exception e)
            : base(id, text, e)
        {
        }
    }
}
