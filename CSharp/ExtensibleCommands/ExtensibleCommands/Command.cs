using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace ExtensibleCommands
{
    /// <summary>
    /// Base class for all commands. Implements fundamental command functions such as:
    /// - execution;
    /// - failure handling and notification;
    /// - suspension & recovery
    /// </summary>
    public abstract class Command : ICommand
    {
        /// <summary> Command name (preferably, uniquely identifies the command) </summary>
        public string Name { get; protected set; }

        /// <summary> Current state of the command </summary>
        public State CurrentState
        {
            get { return _state; }
            protected set
            {
                State previous = _state;
                _state = value;
                Logger.Log(Logger.LogLevel.Info,
                        string.Format("Command {0} : {1} -> {2}", Name, previous, _state));
                if (previous != _state)
                {
                    _stateChanged.OnNext(_state);
                }
            }
        }

        /// <summary> Observable signaling command state change </summary>
        public IObservable<State> StateChanged { get { return _stateChanged; } }

        /// <summary> Observable signaling when a progress update is ready </summary>
        public IObservable<ProgressUpdate> ProgressUpdated { get { return _progressUpdated; } }

        /// <summary> Exception generated during command execution (if any) </summary>
        public ExtensibleCommandsException Exception { get; protected set; }

        /// <summary> List of all child command objects (1st level only) </summary>
        public virtual IEnumerable<ICommand> Children { get { return new List<ICommand>(); } }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public virtual IEnumerable<ICommand> Descendants { get { return new List<ICommand>(); } }

        /// <summary> Current elapsed time of command execution. Can be queried before command completion. </summary>
        public TimeSpan ElapsedTime { get { return _stopWatch.Elapsed; } }

        /// <summary> Current elapsed time of command execution (in msec). Can be queried before command completion. </summary>
        public int ElapsedTimeMsec { get { return (int)_stopWatch.Elapsed.TotalMilliseconds; } }

        /// <summary> Fraction of command completed (between 0 and 1) </summary>
        public double FractionCompleted { get; protected set; }

        /// <summary> Fraction of command completed in percents (between 0 and 100) </summary>
        public int PercentCompleted { get { return (int)(100 * FractionCompleted); } }

        /// <summary> Synchronization lock object </summary>
        protected readonly object _lock = new object();

        /// <summary> Event signaling the start of command </summary>
        protected ManualResetEventSlim _eventStarted = new ManualResetEventSlim(true);

        /// <summary> Event signaling the completion of command (with any outcome) </summary>
        protected ManualResetEventSlim _eventFinished = new ManualResetEventSlim(false);

        /// <summary> Local Abort flag (set on every individual command by calling Abort() method </summary>
        protected volatile bool _aborted;

        /// <summary> Local Pause flag (set on every individual command by calling Pause() method </summary>
        protected volatile bool _paused;

        /// <summary> Current state of the command </summary>
        private State _state = State.Idle;

        /// <summary> Observable signaling command state change </summary>
        private Subject<State> _stateChanged = new Subject<State>();

        /// <summary> Observable signaling when a progress update is ready </summary>
        private Subject<ProgressUpdate> _progressUpdated = new Subject<ProgressUpdate>();

        /// <summary> Stop watch object to measure how much time command execution takes </summary>
        private readonly Stopwatch _stopWatch = new Stopwatch();

        /// <summary> Event signaling that the command has been resumed </summary>
        private ManualResetEventSlim _eventResuming = new ManualResetEventSlim(false);

        /// <summary> Descendants that do not have their own descendants (i.e. leaves on the tree) </summary>
        private IEnumerable<ICommand> _leaves { get { return Descendants.Where(each => !each.Children.Any()); } }

        /// <summary> Number of leaf descendant commands </summary>
        private int _numberOfLeaves;

        /// <summary> Number of completed leaf descendant commands (updated during command execution) </summary>
        private int _numberOfLeavesCompleted;

        /// <summary> Lock for leaf descendant command updates </summary>
        private readonly object _leafUpdateLock = new object();

        /// <summary> Subscriptions to progress of leaf descendant commands </summary>
        private List<IDisposable> _leafSubscriptions = new List<IDisposable>();

        /// <summary> Main method to run the command </summary>
        public virtual void Run()
        {
            lock (_lock)
            {
                // Start timer
                _stopWatch.Stop();
                _stopWatch.Reset();
                _stopWatch.Start();

                _paused = false;
                _aborted = false;

                _numberOfLeaves = _leaves.Count();
                _numberOfLeavesCompleted = 0;
                FractionCompleted = 0.0;
                SubscribeForLeafProgressUpdates();

                _eventFinished.Reset();
                _eventResuming.Reset();

                try
                {
                    CurrentState = State.Executing;
                    Exception = null;

                    _eventStarted.Set();

                    Execute();

                    // Still need to check for errors even if there was no exception thrown
                    CheckErrors();
                    SignalCompletion();
                }
                catch (ExtensibleCommandsException e)
                {
                    CurrentState = State.Failed;
                    Exception = e;

                    CheckErrors();
                    SignalCompletion();
                }
                catch (Exception)
                {
                    // If any other exception is thrown, terminate command execution.
                    // This case is not handled by Extensible Commands!
                    CurrentState = State.Failed;
                    throw;
                }
                finally
                {
                    // Even if unhandled exception is thrown, make sure we do this
                    UnsubscribeFromLeafProgressUpdates();

                    // Record elapsed time
                    _stopWatch.Stop();

                    _eventFinished.Set();
                    _eventStarted.Reset();
                }
            }
        }

        /// <summary> Pause command execution </summary>
        public virtual void Pause()
        {
            // Iterate through children objects and pause them first.
            // This works recursively, i.e. each child will pause its children.
            foreach (var syncCommand in Children)
                syncCommand.Pause();

            _paused = true;

            if (CurrentState == State.Executing)
                Logger.Log(Logger.LogLevel.Info,
                    string.Format("Command {0} is PAUSED", Name));
        }

        /// <summary> Resume command execution </summary>
        public virtual void Resume()
        {
            // Iterate through children objects and resume them first.
            // This works recursively, i.e. each child will resume its children.
            foreach (var syncCommand in Children)
                syncCommand.Resume();

            if (_paused)
            {
                _paused = false;
                if (_eventResuming != null)
                    _eventResuming.Set();
            }

            if (CurrentState == State.Executing)
                Logger.Log(Logger.LogLevel.Info,
                    string.Format("Command {0} is RESUMED", Name));
        }

        /// <summary> Abort command execution </summary>
        public virtual void Abort()
        {
            // Iterate through children objects and abort them first.
            // This works recursively, i.e. each child will abort its children.
            foreach (var syncCommand in Children)
                syncCommand.Abort();

            _paused = false;
            _aborted = true;

            if (_eventResuming != null)
                _eventResuming.Set();

            if (CurrentState == State.Executing)
                Logger.Log(Logger.LogLevel.Info,
                    string.Format("Command {0} is ABORTED", Name));
        }

        /// <summary>
        /// Force reset of the Finished event to guarantee that this command
        /// can be reliably waited upon in a different thread using WaitUntilFinished().
        /// </summary>
        public void ResetFinished()
        {
            _eventFinished.Reset();
        }

        /// <summary>
        /// Wait until command is finished (with a timeout)
        /// </summary>
        /// <param name="timeoutMsec">Wait timeout (msec)</param>
        public void WaitUntilFinished(int timeoutMsec)
        {
            if (timeoutMsec == 0)
                _eventFinished.Wait();
            else
                _eventFinished.Wait(timeoutMsec);

            // Ensure that this event is reset, is important for parallel command
            _eventFinished.Reset();
        }

        /// <summary> The body of the command execution </summary>
        abstract protected void Execute();

        /// <summary> Set the main command state based on the child commands states </summary>
        abstract protected void CheckErrors();

        /// <summary> Make sure the state is set correctly in case of Abort or Pause </summary>
        protected void ProcessAbortAndPauseEvents()
        {
            if (_aborted && CurrentState != State.Failed)
            {
                CurrentState = State.Aborted;
                return; // Do not care about Pause if Abort has been issued
            }

            // What if we already failed?
            if (CurrentState == State.Failed)
                return;

            if (_paused)
                _eventResuming.Wait();

            if (_aborted && CurrentState != State.Failed)
                CurrentState = State.Aborted;
        }

        /// <summary> Generate appropriate events on command completion </summary>
        protected void SignalCompletion()
        {
            // Command completed successfully
            if (CurrentState == State.Executing)
                CurrentState = State.Completed;
        }

        /// <summary> Recalculate fraction completed </summary>
        private void UpdateFractionCompleted()
        {
            _numberOfLeavesCompleted++;
            string progressMessage;

            if (_numberOfLeaves > 0)
            {
                FractionCompleted = (double)_numberOfLeavesCompleted / _numberOfLeaves;
                progressMessage = string.Format("{0} percent complete", PercentCompleted);
            }
            else
            {
                FractionCompleted = 1.0;
                progressMessage = "Complete";
            }

            _progressUpdated.OnNext(new ProgressUpdate(PercentCompleted, FractionCompleted, progressMessage));
        }

        /// <summary> Subscribe for progress updates from "leaf" descendants </summary>
        private void SubscribeForLeafProgressUpdates()
        {
            // We are only intertested in updates from "leaf" commands, not complex command aggregating other commands
            foreach (var command in _leaves)
            {
                // Only subscribe to successfully completed "leaf" sub-commands.
                // Consider failed or aborted sub-commands not completed.
                _leafSubscriptions.Add(command.StateChanged.Where(s => s == State.Completed).Subscribe(s =>
                {
                    // Update fraction completed when any of the descendant command completes
                    lock (_leafUpdateLock)
                    {
                        UpdateFractionCompleted();
                    }
                }));
            }
        }

        /// <summary> Unsubscribe from "leaf" progress updates </summary>
        private void UnsubscribeFromLeafProgressUpdates()
        {
            // We are only interested in updates from "leaf" commands, not complex command aggregating other commands
            foreach (var subscription in _leafSubscriptions)
                subscription.Dispose();

            _leafSubscriptions.Clear();
        }
    }
}
