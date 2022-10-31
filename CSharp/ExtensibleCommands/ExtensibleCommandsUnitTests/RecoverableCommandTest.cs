using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for RecoverableCommandTest and is intended
    ///to contain all RecoverableCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RecoverableCommandTest
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

        [TestMethod()]
        public void ConstructionTest()
        {
            var command = new RecoverableCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.RecoveryCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "Recoverable");

            command = new RecoverableCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.RecoveryCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new RecoverableCommand(null, null);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("is NULL"))
                    exceptionCaught = true;
            }
            Assert.IsTrue(exceptionCaught);

            exceptionCaught = false;
            try
            {
                new RecoverableCommand(SimpleCommand.NullCommand, null);
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
            var coreCommand = new SimpleCommand(() => { }, "Core");
            var recoveryCommand = new SimpleCommand(() => { }, "Recovery");
            var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Completed, coreCommand.CurrentState);
            Assert.AreEqual(State.Idle, recoveryCommand.CurrentState);
        }

        [TestMethod()]
        public void RunCriticalErrorTest()
        {
            // If normal exception is thrown, the command should fail without excercising recovery command
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var recoveryCommand = new SimpleCommand(() => { }, "Recovery");
            var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Idle, recoveryCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunNonCriticalErrorTest1()
        {
            // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should succeed after excercising recovery command
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var recoveryCommand = new SimpleCommand(() => { }, "Recovery");
            var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, recoveryCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunNonCriticalErrorTest2()
        {
            // If ExtensibleCommandsAllowRetryException exception is thrown, the command should succeed after excercising recovery command
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var recoveryCommand = new SimpleCommand(() => { }, "Recovery");
            var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, recoveryCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void AbortCoreTest()
        {
            var command = CreateCorePauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Idle, command.RecoveryCommand.CurrentState);
        }

        [TestMethod()]
        public void PauseResumeCoreTest()
        {
            var command = CreateCorePauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Idle, command.RecoveryCommand.CurrentState);
        }

        [TestMethod()]
        public void PauseAbortCoreTest()
        {
            var command = CreateCorePauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Idle, command.RecoveryCommand.CurrentState);
        }

        [TestMethod()]
        public void AbortRecoveryTest()
        {
            var command = CreateRecoveryPauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.RecoveryCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.RecoveryCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeRecoveryTest()
        {
            var command = CreateRecoveryPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.RecoveryCommand.CurrentState);
                Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.RecoveryCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.RecoveryCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortRecoveryTest()
        {
            var command = CreateRecoveryPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.RecoveryCommand.CurrentState);
                Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.RecoveryCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.RecoveryCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.RecoveryCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.RecoveryCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new SequentialCommand("Core")
                .Add(new SimpleCommand("1"))
                .Add(new SimpleCommand("2"))
                .Add(new SimpleCommand("3"));

            var command2 = new SequentialCommand("Recovery")
                .Add(new SimpleCommand("R-1"))
                .Add(new SimpleCommand("R-2"))
                .Add(new SimpleCommand("R-3"))
                .Add(new SimpleCommand("R-4"))
                .Add(new SimpleCommand("R-5"));

            var command = new RecoverableCommand(command1, command2, "Main");

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(5, command2.Descendants.Count());
            Assert.AreEqual(10, command.Descendants.Count());
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(5, command2.Children.Count());
            Assert.AreEqual(2, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var recovery = SimpleCommand.NullCommand;
            var recoverableCommand = new RecoverableCommand(command, recovery);

            command.Input = "input";
            recoverableCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private RecoverableCommand CreateCorePauseAbortCommand(bool stop)
        {
            var recoveryCommand = new SimpleCommand(() => { }, "Recovery");
            var coreCommand = new SequentialCommand("Core");
            var recoverableCommand = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            coreCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (stop)
                coreCommand.Add(new SimpleCommand(() => recoverableCommand.Pause(), "S2-Stop"));
            else
                coreCommand.Add(new SimpleCommand(() => recoverableCommand.Abort(), "S2-Abort"));

            coreCommand.Add(new SimpleCommand(() => { }, "S3"));

            return (recoverableCommand);
        }

        private RecoverableCommand CreateRecoveryPauseAbortCommand(bool stop)
        {
            var recoveryCommand = new SequentialCommand("Recovery");
            // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should succeed after excercising recovery command
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var recoverableCommand = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

            recoveryCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (stop)
                recoveryCommand.Add(new SimpleCommand(() => recoverableCommand.Pause(), "S2-Stop"));
            else
                recoveryCommand.Add(new SimpleCommand(() => recoverableCommand.Abort(), "S2-Abort"));

            recoveryCommand.Add(new SimpleCommand(() => { }, "S3"));
            return (recoverableCommand);
        }
    }
}
