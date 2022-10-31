using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;
using System;
using System.Reactive.Linq;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Common parameters and behaviors for command unit  tests
    /// </summary>
    public class Setup
    {
        public const int TestErrorCode = 1;
        public const string TestErrorDescription = "Command delegate error";

        /// <summary> Standard timeout to abort waiting for synchronization events in all tests. </summary>
        public const int WaitTimeoutMsec = 5000;

        /// <summary> Standard delay to account for thread latency (to allow time for all threads to complete and update command states). </summary>
        public const int ThreadLatencyDelayMsec = 200;

        /// <summary>
        /// Initialize logger
        /// </summary>
        public static void InitLog()
        {
            Logger.ExternalLogger = new TestLogger();
            Logger.IsLoggingEnabled = true;
        }

        /// <summary>
        /// Execute command and wait for its completion
        /// </summary>
        /// <param name="command">Command to run</param>
        public static void RunAndWaitForNormalCompletion(ICommand command)
        {
            command.ResetFinished();
            new Thread(command.Run).Start();

            // Wait for sync command to be completed
            command.WaitUntilFinished(WaitTimeoutMsec);
            Assert.AreEqual(State.Completed, command.CurrentState);
        }

        /// <summary>
        /// Execute command and wait for its failure
        /// </summary>
        /// <param name="command">Command to run</param>
        public static void RunAndWaitForFailure(ICommand command)
        {
            var failed = command.StateChanged.Where(s => s == State.Failed).Subscribe(s => { _isFailureEventReceived = true; });

            try
            {
                command.ResetFinished();
                new Thread(command.Run).Start();

                // Wait for sync command to be completed
                command.WaitUntilFinished(WaitTimeoutMsec*100);

                // Verify that the Top Command Failed event has been received
                Assert.IsTrue(_isFailureEventReceived);

                Assert.AreEqual(State.Failed, command.CurrentState);
            }
            finally
            {
                failed.Dispose();
            }
        }

        /// <summary>
        /// Runs the supplied command asynchronously, pauses it and then resumes.
        /// Relies on the supplied command to pause itself.
        /// </summary>
        /// <param name="command">Command to run</param>
        /// <param name="assertAfterPause">Assertion to execute after pause</param>
        public static void PauseAndResume(ICommand command, Action assertAfterPause)
        {
            command.ResetFinished();
            new Thread(command.Run).Start();
            Thread.Sleep(5 * ThreadLatencyDelayMsec);

            Assert.AreEqual(State.Executing, command.CurrentState);
            
            if (assertAfterPause != null)
                assertAfterPause();

            command.Resume();

            Thread.Sleep(ThreadLatencyDelayMsec);

            // Wait for sync command to be completed
            command.WaitUntilFinished(2*WaitTimeoutMsec);
        }

        /// <summary>
        /// Runs the supplied command asynchronously, pauses it and then aborts.
        /// Relies on timing of the supplied command to pause itself.
        /// </summary>
        /// <param name="command">Command to run</param>
        /// <param name="assertAfterPause">Assertion to execute after pause</param>
        public static void PauseAndAbort(ICommand command, System.Action assertAfterPause)
        {
            command.ResetFinished();
            new Thread(command.Run).Start();
            Thread.Sleep(2*ThreadLatencyDelayMsec);

            Assert.AreEqual(State.Executing, command.CurrentState);
            
            if (assertAfterPause != null)
                assertAfterPause();

            command.Abort();

            // Wait for sync command to be completed
            command.WaitUntilFinished(WaitTimeoutMsec);
        }

        /// <summary>
        /// Runs the supplied command asynchronously and then aborts it.
        /// The supplied command must have the correct timing consistent with the delays in this method!
        /// </summary>
        /// <param name="command">Command to run</param>
        public static void RunAndAbort(ICommand command)
        {
            command.ResetFinished();
            new Thread(command.Run).Start();
            Thread.Sleep(ThreadLatencyDelayMsec);

            command.Abort();
            command.WaitUntilFinished(WaitTimeoutMsec);
        }

        /// <summary>
        /// Runs the supplied command and waits for the command to be aborted.
        /// Relies on the supplied command to abort itself.
        /// </summary>
        /// <param name="command">Command to run</param>
        public static void RunAndWaitForAbort(ICommand command)
        {
            command.ResetFinished();
            new Thread(command.Run).Start();

            Thread.Sleep(2 * ThreadLatencyDelayMsec);

            // Wait for sync command to be completed
            command.WaitUntilFinished(WaitTimeoutMsec);
        }

        private static bool _isFailureEventReceived = false;
    }
}
