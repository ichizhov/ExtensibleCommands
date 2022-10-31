using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;
using System.Reactive.Linq;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for SequentialCommandTest and is intended
    ///to contain all SequentialCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SequentialCommandTest
    {
        private TestContext testContextInstance;

        private int _percentComplete;
        private int _numberOfUpdates;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Setup.InitLog();
        }

        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            Logger.Log(Logger.LogLevel.Info,
                "----------------------------------------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info,
                string.Format("Starting Test {0}:{1}", GetType().Name, testContextInstance.TestName));
        }
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        public void ModifyWhileExecutingTest()
        {
            bool exceptionCaught = false;
            var command = new SequentialCommand("Seq")
                .Add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec))
                .Add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec));

            new Thread(command.Run).Start();
            Thread.Sleep((int)(0.2 * Setup.ThreadLatencyDelayMsec));

            try
            {
                command.Add(SimpleCommand.NullCommand);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Attempt to add"))
                    exceptionCaught = true;
            }
            Assert.IsTrue(exceptionCaught);
        }

        [TestMethod()]
        public void RunOKTest()
        {
            var command = new SequentialCommand("Seq")
                .Add(new AbortableSleepCommand(1))
                .Add(new AbortableSleepCommand(1))
                .Add(new AbortableSleepCommand(1));

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            var command = new SequentialCommand("Seq")
                .Add(new AbortableSleepCommand(1))
                .Add(new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);}))
                .Add(new AbortableSleepCommand(1));

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Failed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, command.GetSubCommand(2).CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void AbortTest()
        {
            var command = new SequentialCommand();
            command.Add(new SimpleCommand(() => { }))
                .Add(new SimpleCommand(() => command.Abort()))
                .Add(new SimpleCommand(() => { }));

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, command.GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeTest()
        {
            var command = new SequentialCommand();
            command.Add(new SimpleCommand(() => {}, "S1"))
                .Add(new SimpleCommand(() => command.Pause(), "S2-Pause"))
                .Add(new SimpleCommand(() => { }, "S3"));

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, command.GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortTest()
        {
            var command = new SequentialCommand();
            command.Add(new SimpleCommand(() => { }, "S1"))
                .Add(new SimpleCommand(() => command.Pause(), "S2-Pause"))
                .Add(new SimpleCommand(() => { }, "S3"));

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, command.GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, command.GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new SequentialCommand("Branch 1")
                .Add(new SimpleCommand("B1-1"))
                .Add(new SimpleCommand("B1-2"))
                .Add(new SimpleCommand("B1-3"));

            var command2 = new SequentialCommand("Branch 2")
                .Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))
                .Add(new SimpleCommand("B2-3"))
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            var command = new SequentialCommand("Main")
                .Add(new SimpleCommand("A1"))
                .Add(new SimpleCommand("A2"))
                .Add(command1)
                .Add(command2);

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(5, command2.Descendants.Count());
            Assert.AreEqual(12, command.Descendants.Count());
            Assert.AreEqual(12, command.Descendants.Count());    // Make sure multiple calls produce the same result
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(5, command2.Children.Count());
            Assert.AreEqual(4, command.Children.Count());
            Assert.AreEqual(4, command.Children.Count());        // Make sure multiple calls produce the same result
        }

        [TestMethod()]
        public void ProgressUpdateTest()
        {
            //--------------------- Normal completion ------------
         
            var command1 = new SequentialCommand("Branch 1")
                .Add(new SimpleCommand("B1-1"))
                .Add(new SimpleCommand("B1-2"))
                .Add(new SimpleCommand("B1-3"));

            var command2 = new SequentialCommand("Branch 2")
                .Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))
                .Add(new SimpleCommand("B2-3"))
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            var command = new SequentialCommand("Main")
                .Add(new SimpleCommand("A1"))
                .Add(new SimpleCommand("A2"))
                .Add(command1)
                .Add(command2);

            var updated = command.ProgressUpdated.Subscribe(OnProgressUpdate);

            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(100, command.PercentCompleted);
            Assert.AreEqual(1.0, command.FractionCompleted);
            Assert.AreEqual(100, _percentComplete);
            Assert.AreEqual(10, _numberOfUpdates);

            // Run again to check if we are unsubscribing correctly
            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(100, command.PercentCompleted);
            Assert.AreEqual(1.0, command.FractionCompleted);
            Assert.AreEqual(100, _percentComplete);
            Assert.AreEqual(10, _numberOfUpdates);

            //--------------------- Failure ------------
            // Reformat command to produce failure in the middle
            command2 = new SequentialCommand("Branch 2")
                .Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))     // This is the last sub-command successfully completed
                .Add(new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }))
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            command = new SequentialCommand("Main")
                .Add(new SimpleCommand("A1"))
                .Add(new SimpleCommand("A2"))
                .Add(command1)
                .Add(command2);

            updated.Dispose();
            updated = command.ProgressUpdated.Subscribe(OnProgressUpdate);

            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(70, command.PercentCompleted);
            Assert.AreEqual(0.7, command.FractionCompleted);
            Assert.AreEqual(70, _percentComplete);
            Assert.AreEqual(7, _numberOfUpdates);

            // Run again
            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(70, command.PercentCompleted);
            Assert.AreEqual(0.7, command.FractionCompleted);
            Assert.AreEqual(70, _percentComplete);
            Assert.AreEqual(7, _numberOfUpdates);

            //--------------------- Abort ------------
            // Reformat command to abort in the middle
            command2 = new SequentialCommand("Branch 2");
            command2.Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))
                .Add(new SimpleCommand(() => command.Abort()))   // This is the last sub-command successfully completed
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            command = new SequentialCommand("Main")
                .Add(new SimpleCommand("A1"))
                .Add(new SimpleCommand("A2"))
                .Add(command1)
                .Add(command2);

            updated.Dispose();
            updated = command.ProgressUpdated.Subscribe(OnProgressUpdate);

            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(80, command.PercentCompleted);
            Assert.AreEqual(0.8, command.FractionCompleted);
            Assert.AreEqual(80, _percentComplete);
            Assert.AreEqual(8, _numberOfUpdates);

            // Run again
            _numberOfUpdates = 0;
            command.Run();

            Assert.AreEqual(80, command.PercentCompleted);
            Assert.AreEqual(0.8, command.FractionCompleted);
            Assert.AreEqual(80, _percentComplete);
            Assert.AreEqual(8, _numberOfUpdates);

            updated.Dispose();
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            var command1 = new AbortableSleepCommand(8 * Setup.ThreadLatencyDelayMsec, "A1");
            var command2 = new AbortableSleepCommand(5 * Setup.ThreadLatencyDelayMsec, "A2");

            var command = new SequentialCommand()
                .Add(command1)
                .Add(command2);

            Setup.RunAndAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Aborted, command1.CurrentState);
            Assert.AreEqual(State.Idle, command2.CurrentState);
            Assert.AreEqual(State.Aborted, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Idle, command.GetSubCommand(1).CurrentState);
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var sequentialCommand = new SequentialCommand();

            sequentialCommand.Add(command);

            command.Input = "input";
            sequentialCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private void OnProgressUpdate(ProgressUpdate update)
        {
            _percentComplete = update.PercentCompleted;
            _numberOfUpdates++;
        }
    }

}
