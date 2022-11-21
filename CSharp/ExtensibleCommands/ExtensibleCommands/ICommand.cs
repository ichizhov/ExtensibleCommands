using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary> Command state enumeration </summary>
    public enum State { Idle, Executing, Failed, Aborted, Completed };

    /// <summary> Interface representing basic command functionality </summary>
    public interface ICommand
    {
        /// <summary> Command name (preferably, uniquely identifies the command) </summary>
        string Name { get; }

        /// <summary> Current state of the command </summary>
        State CurrentState { get; }

        /// <summary> Observable signaling command state change </summary>
        IObservable<State> StateChanged { get; }

        /// <summary> Observable signaling when a progress update is ready </summary>
        IObservable<ProgressUpdate> ProgressUpdated { get; }

        /// <summary> Exception generated during command execution (if any) </summary>
        ExtensibleCommandsException Exception { get; }

        /// <summary> List of all child command objects (1st level only) </summary>
        IEnumerable<ICommand> Children { get; }

        /// <summary> List of all descendant command objects (all levels) </summary>
        IEnumerable<ICommand> Descendants { get; }

        /// <summary> Current elapsed time of command execution. Can be queried before command completion. </summary>
        TimeSpan ElapsedTime { get; }

        /// <summary> Current elapsed time of command execution (in msec). Can be queried before command completion. </summary>
        int ElapsedTimeMsec { get; }

        /// <summary> Fraction of command completed (between 0 and 1) </summary>
        double FractionCompleted { get; }

        /// <summary> Fraction of command completed in percent (between 0 and 100) </summary>
        int PercentCompleted { get; }

        /// <summary> Main method to run the command </summary>
        void Run();

        /// <summary> Pause command execution </summary>
        void Pause();

        /// <summary> Resume command execution </summary>
        void Resume();

        /// <summary> Abort command execution </summary>
        void Abort();

        /// <summary>
        /// Force reset of the Finished event to guarantee that this command
        /// can be reliably waited upon in a different thread using WaitUntilFinished().
        /// </summary>
        void ResetFinished();

        /// <summary>
        /// Wait until command is finished (with a timeout)
        /// </summary>
        /// <param name="timeoutMsec">Wait timeout (msec)</param>
        void WaitUntilFinished(int timeoutMsec);
    }
}
