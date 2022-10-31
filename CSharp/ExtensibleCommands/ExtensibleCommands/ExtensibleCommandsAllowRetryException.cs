using System;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Exception that allows a Retry Command to perform retries, 
    /// i.e. it does not result in immediate termination of the operation.
    /// For example, intermittent failures should be of this type to allow retries.
    /// A Retry exception is also a Recovery exception to allow a recovery if all retries fail.
    /// </summary>
    public class ExtensibleCommandsAllowRetryException : ExtensibleCommandsAllowRecoveryException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        public ExtensibleCommandsAllowRetryException(int id, string text)
            : base(id, text)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Error ID</param>
        /// <param name="text">Error description</param>
        /// <param name="e">Underlying exception</param>
        public ExtensibleCommandsAllowRetryException(int id, string text, Exception e)
            : base(id, text, e)
        {
        }
    }
}
