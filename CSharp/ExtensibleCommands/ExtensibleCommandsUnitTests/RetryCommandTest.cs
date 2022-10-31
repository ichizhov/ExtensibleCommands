using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for RetryCommandTest and is intended
    ///to contain all RetryCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RetryCommandTest
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

        private int _counter;

        [TestMethod()]
        public void ConstructionTest()
        {
            var command = new RetryCommand(SimpleCommand.NullCommand, 3);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.NumberOfRetries, 3);
            Assert.AreEqual(command.RetryDelayMsec, 0);
            Assert.AreEqual(command.Name, "Retry");

            command = new RetryCommand(SimpleCommand.NullCommand, 5, 100);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.NumberOfRetries, 5);
            Assert.AreEqual(command.RetryDelayMsec, 100);
            Assert.AreEqual(command.Name, "Retry");

            command = new RetryCommand(SimpleCommand.NullCommand, 7, 200, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.NumberOfRetries, 7);
            Assert.AreEqual(command.RetryDelayMsec, 200);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new RetryCommand(null, 5);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("is NULL"))
                    exceptionCaught = true;
            }
            Assert.IsTrue(exceptionCaught);
        }

        [TestMethod()]
        public void RunOKTest()
        {
            _counter = 0;
            var coreCommand = new SimpleCommand(() => { _counter++; }, "Core");
            var command = new RetryCommand(coreCommand, 10, 0, "Retry");

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, coreCommand.CurrentState);
            Assert.AreEqual(1, _counter);
        }

        [TestMethod()]
        public void RunCriticalErrorTest()
        {
            _counter = 0;
            // If normal exception is thrown, the command should fail without excercising recovery command
            var coreCommand = new SimpleCommand(() => { _counter++; throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var command = new RetryCommand(coreCommand, 10, 0, "Retry");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(1, _counter);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunNonCriticalErrorTest1()
        {
            _counter = 0;
            // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should fail immediately, because recovery exception is higher in hierarchy
            var coreCommand = new SimpleCommand(() => { _counter++; throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var command = new RetryCommand(coreCommand, 10, 0, "Retry");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(1, _counter);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunNonCriticalErrorTest2()
        {
            _counter = 0;
            // If ExtensibleCommandsAllowRetryException exception is thrown, the command should succeed after excercising recovery command
            var coreCommand = new SimpleCommand(() => { _counter++; throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var command = new RetryCommand(coreCommand, 10, 0, "Retry");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(10, _counter);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void AbortTest()
        {
            var retryCommand = CreatePauseAbortRetryCommand(false);

            Setup.RunAndWaitForAbort(retryCommand);

            Assert.AreEqual(1, retryCommand.CurrentRetryIndex);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeTest()
        {
            var retryCommand = CreatePauseAbortRetryCommand(true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });
            
            Setup.PauseAndResume(retryCommand, assertAfterPause);

            Assert.AreEqual(State.Completed, retryCommand.CurrentState);
            Assert.AreEqual(1, retryCommand.CurrentRetryIndex);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        /// <summary>
        ///   Test Pause and Abort
        ///</summary>
        [TestMethod()]
        public void PauseAbortTest()
        {
            var retryCommand = CreatePauseAbortRetryCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(retryCommand, assertAfterPause);

            Assert.AreEqual(State.Aborted, retryCommand.CurrentState);
            Assert.AreEqual(1, retryCommand.CurrentRetryIndex);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (retryCommand.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            // This is the case when we do local abort on an command that failed.
            // Abort should be superseded by failure, i.e. no abort event will be issued.
            var coreCommand = new SimpleCommand(() =>
            {
                Thread.Sleep(Setup.ThreadLatencyDelayMsec);
                throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription);
            }, "Core");
            var command = new RetryCommand(coreCommand, 10, 0, "Retry");

            command.ResetFinished();
            new Thread(command.Run).Start();
            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            command.Abort();
            command.WaitUntilFinished(Setup.WaitTimeoutMsec);

            // Retry command must be in Failed state
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Failed, command.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new SequentialCommand("Core")
                .Add(new SimpleCommand("1"))
                .Add(new SimpleCommand("2"))
                .Add(new SimpleCommand("3"));

            var command = new RetryCommand(command1, 3);

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(4, command.Descendants.Count());
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(1, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var retryCommand = new RetryCommand(command, 3);

            command.Input = "input";
            retryCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private RetryCommand CreatePauseAbortRetryCommand(bool pause)
        {
            RetryCommand retryCommand = null;
            var seqCommand = new SequentialCommand("Core");
            seqCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (pause)
                seqCommand.Add(new SimpleCommand(() => retryCommand.Pause(), "S2-Stop"));
            else
                seqCommand.Add(new SimpleCommand(() => retryCommand.Abort(), "S2-Abort"));

            seqCommand.Add(new SimpleCommand(() => { }, "S3"));
            retryCommand = new RetryCommand(seqCommand, 5, 0, "Retry");
            return (retryCommand);
        }
    }
}
