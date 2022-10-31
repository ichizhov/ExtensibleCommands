using System.Threading;

namespace ExtensibleCommands
{
    /// <summary>
    /// Retries core command N times if it fails
    /// </summary>
    public class RetryCommand : DecoratorCommand
    {
        /// <summary> Number of command retries </summary>
        public int NumberOfRetries { get; }

        /// <summary> Retry delay (in msec) </summary>
        public int RetryDelayMsec { get; }

        /// <summary> Current retry index </summary>
        public int CurrentRetryIndex { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="numberOfRetries">Number of command retries</param>
        public RetryCommand(ICommand coreCommand, int numberOfRetries)
            : this(coreCommand, numberOfRetries, 0, "Retry") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="numberOfRetries">Number of command retries</param>
        /// <param name="retryDelayMsec">Retry delay (in msec)</param>
        public RetryCommand(ICommand coreCommand, int numberOfRetries, int retryDelayMsec)
            : this(coreCommand, numberOfRetries, retryDelayMsec, "Retry") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="numberOfRetries">Number of command retries</param>
        /// <param name="retryDelayMsec">Retry delay (in msec)</param>
        /// <param name="name">Command name</param>
        public RetryCommand(ICommand coreCommand, int numberOfRetries, int retryDelayMsec, string name)
            : base(coreCommand, name)
        {
            NumberOfRetries = numberOfRetries;
            RetryDelayMsec = retryDelayMsec;
        }

        protected override void Execute()
        {
            CurrentRetryIndex = 0;
            for (int i = 0; i < NumberOfRetries; i++)
            {
                CurrentRetryIndex++;
                CoreCommand.Run();

                ProcessAbortAndPauseEvents();

                if (CoreCommand.CurrentState == State.Completed || CoreCommand.CurrentState == State.Aborted || CurrentState == State.Aborted)
                    break;
                if (CoreCommand.CurrentState == State.Failed && !(CoreCommand.Exception is ExtensibleCommandsAllowRetryException))
                    break;

                if (CoreCommand.Exception is ExtensibleCommandsAllowRetryException)
                    Logger.Log(Logger.LogLevel.Error,
                        string.Format("ERROR (RECOVERED)[{0}] - {1}", CoreCommand.Exception.ID, CoreCommand.Exception.Text));
            }

            // If after all retries there is still error, we need to report it.
            if (CoreCommand.CurrentState == State.Failed)
            {
                CurrentState = State.Failed;
                Exception = CoreCommand.Exception;
            }

            Thread.Sleep(RetryDelayMsec);
        }
    }
}
