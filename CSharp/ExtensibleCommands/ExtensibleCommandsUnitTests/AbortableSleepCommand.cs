using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary> 
    /// Implements sleep command that can be aborted at any time.
    /// </summary>
    public class AbortableSleepCommand : SimpleCommand
    {
        /// <summary> Sleep time (in msec). When it expires, command terminates. </summary>
        public int SleepTimeMsec { get; }

        /// <summary> Internal event to be used to detect aborts </summary>
        private readonly ManualResetEventSlim _eventAborted = new ManualResetEventSlim(false);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sleepTimeMsec">Sleep time (msec)</param>
        public AbortableSleepCommand(int sleepTimeMsec) : this(sleepTimeMsec, "Abortable Sleep") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sleepTimeMsec">Sleep time (msec)</param>
        /// <param name="name">Command name</param>
        public AbortableSleepCommand(int sleepTimeMsec, string name)
        {
            Name = name;
            SleepTimeMsec = sleepTimeMsec;
        }

        /// <summary> Aborts execution </summary>
        public override void Abort()
        {
            _aborted = true;
            _eventAborted.Set();
        }

        /// <summary> 
        /// The body of the command execution. 
        /// Sleep for a specified period of time unless interrupted by Abort() earlier.
        /// </summary>
        protected override void Execute()
        {
            _eventAborted.Reset();
            _eventAborted.Wait(SleepTimeMsec);

            ProcessAbortAndPauseEvents();
        }
    }
}
