using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for AbortableCommandTest and is intended
    ///to contain all AbortableCommandTest Unit Tests
    ///</summary>
    [TestClass]
    public class AbortableCommandTest
    {
        private TestContext testContextInstance;

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
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        [TestInitialize]
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
        public void ConstructionTest()
        {
            var command = new AbortableCommand(SimpleCommand.NullCommand, () => { });
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "Abortable");

            command = new AbortableCommand(SimpleCommand.NullCommand, () => { }, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");
        }
            
        [TestMethod()]
        public void RunOKTest()
        {
            var command = new AbortableCommand(new SimpleCommand(() => Thread.Sleep(100)),
               () => { }, "Test abortable command");

            command.Run();

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.IsTrue(command.ElapsedTimeMsec < 500);
            Assert.IsTrue(command.ElapsedTimeMsec > 80);

            // Run again
            command.Run();

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.IsTrue(command.ElapsedTimeMsec < 500);
            Assert.IsTrue(command.ElapsedTimeMsec > 80);
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            var command = new AbortableCommand(new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }),
               () => { }, "Test abortable command");

            command.Run();

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
        }

        [TestMethod]
        public void PauseAndResumeTest()
        {
            var command = CreateAbortableCommand();
            command.ResetFinished();
            new Thread(command.Run).Start();
            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            // Pause
            command.Pause();

            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            // Verify that commands are still executing - Pause has no effect here
            Assert.AreEqual(State.Executing, command.CurrentState);
            Assert.AreEqual(State.Executing, command.CoreCommand.CurrentState);

            // Resume (has no effect either)
            command.Resume();
            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            // Verify that commands are still in Executing state
            Assert.AreEqual(State.Executing, command.CurrentState);
            Assert.AreEqual(State.Executing, command.CoreCommand.CurrentState);

            // Now abort
            command.Abort();

            command.WaitUntilFinished(Setup.WaitTimeoutMsec);

            // Verify that commands are in Aborted state
            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);    // Core command completes normally
        }

        [TestMethod]
        public void PauseAndAbortTest()
        {
            var command = CreateAbortableCommand();
            command.ResetFinished();

            new Thread(command.Run).Start();
            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            // Pause
            command.Pause();

            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            // Verify that commands are still executing - Pause has no effect here
            Assert.AreEqual(State.Executing, command.CurrentState);
            Assert.AreEqual(State.Executing, command.CoreCommand.CurrentState);

            // Now abort
            command.Abort();

            command.WaitUntilFinished(Setup.WaitTimeoutMsec);

            // Verify that commands are in Aborted state
            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
        }

        [TestMethod]
        public void ExternalAbortTest()
        {
            var command = CreateAbortableCommand();

            Setup.RunAndAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new SequentialCommand("Core")
                .Add(new SimpleCommand("1"))
                .Add(new SimpleCommand("2"))
                .Add(new SimpleCommand("3"));

            var command = new AbortableCommand(command1, () => { }, "Main");

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(4, command.Descendants.Count());
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(1, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var abortableCommand = new AbortableCommand(command, () => { }, "");

            command.Input = "input";
            abortableCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private AbortableCommand CreateAbortableCommand()
        {
            var wait = new AutoResetEvent(false);

            // Aborting command by setting an event
            var abort = new AbortableCommand.AbortDelegate(() => wait.Set());

            // Command is to wait for an event
            return new AbortableCommand(new SimpleCommand(() =>
            {
                wait.WaitOne(2000);
            }, "Wait"), abort, "Abortable");
        }
    }
}
