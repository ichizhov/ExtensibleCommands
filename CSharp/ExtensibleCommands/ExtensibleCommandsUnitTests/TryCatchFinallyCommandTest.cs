using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Summary description for TryCatchFinallyTest
    /// </summary>
    [TestClass]
    public class TryCatchFinallyCommandTest
    {
        public TryCatchFinallyCommandTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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

        #region Additional test attributes        //
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
            var command = new TryCatchFinallyCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.FinallyCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "Try-Catch-Finally");

            command = new TryCatchFinallyCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.FinallyCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new TryCatchFinallyCommand(null, null);
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
                new TryCatchFinallyCommand(SimpleCommand.NullCommand, null);
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
            var coreCommand = new SimpleCommand(() => {});
            var finallyCommand = new SimpleCommand(() => { });
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, finallyCommand.CurrentState);
        }

        [TestMethod()]
        public void RunCoreCommandErrorTest1()
        {
            // If an ExtensibleCommandsAllowRecoveryException is thrown inside the Core command, the Finally command executes but the command fails
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var finallyCommand = new SimpleCommand(() => { }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, finallyCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunCoreCommandErrorTest2()
        {
            // If an ExtensibleCommandsAllowRecoveryException is thrown inside the Core command, the Finally command executes but the command fails
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var finallyCommand = new SimpleCommand(() => { }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, finallyCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunCoreCommandErrorTest3()
        {
            // If an ExtensibleCommandsException is thrown inside the Core command, the Finally command executes but the command fails
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var finallyCommand = new SimpleCommand(() => { }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Completed, finallyCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunFinallyCommandErrorTest()
        {
            // If the Core command completes successfully and an ExtensibleCommandsException is thrown inside the Finally command, the command fails
            var coreCommand = new SimpleCommand(() => {}, "Core");
            var finallyCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Completed, coreCommand.CurrentState);
            Assert.AreEqual(State.Failed, finallyCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunCoreAndFinallyCommandErrorTest()
        {
            // If an ExtensibleCommandsException is thrown inside both the Core and the Finally commands, the command fails
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
            var finallyCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndWaitForFailure(command);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, coreCommand.CurrentState);
            Assert.AreEqual(State.Failed, finallyCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void AbortTest()
        {
            var command = CreateCorePauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.CoreCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.CoreCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Idle, command.FinallyCommand.CurrentState);
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
            Assert.AreEqual(State.Completed, command.FinallyCommand.CurrentState);
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
            Assert.AreEqual(State.Idle, command.FinallyCommand.CurrentState);
        }

        [TestMethod()]
        public void AbortFinallyTest()
        {
            var command = CreateFinallyPauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            // The command fails because the Core command failed and the Finally command did not have a chance to complete because it was aborted
            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.FinallyCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.FinallyCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeFinallyTest()
        {
            var command = CreateFinallyPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.FinallyCommand.CurrentState);
                Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.FinallyCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.FinallyCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortFinallyTest()
        {
            var command = CreateFinallyPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.FinallyCommand.CurrentState);
                Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (command.FinallyCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.FinallyCommand.CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (command.FinallyCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (command.FinallyCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            // An ExtensibleCommandsException exception is thrown inside the Core command, and the command is aborted
            // before the Finally command gets executed
            var coreCommand = new SimpleCommand(() =>
            {
                Thread.Sleep(3 * Setup.ThreadLatencyDelayMsec);
                throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
            }, "Core");
            var finallyCommand = new SimpleCommand(() => { }, "Finally");
            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

            Setup.RunAndAbort(command);
            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(State.Failed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Idle, command.FinallyCommand.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var coreCommand = new ParallelCommand("P")
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand);

            var finallyCommand = new SequentialCommand("S")
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand)
                .Add(SimpleCommand.NullCommand);

            var command = new TryCatchFinallyCommand(coreCommand, finallyCommand);

            Assert.AreEqual(5, command.CoreCommand.Descendants.Count());
            Assert.AreEqual(5, command.FinallyCommand.Descendants.Count());
            Assert.AreEqual(12, command.Descendants.Count());
            Assert.AreEqual(5, command.CoreCommand.Children.Count());
            Assert.AreEqual(5, command.FinallyCommand.Children.Count());
            Assert.AreEqual(2, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var tryCatchFinallyCommand = new TryCatchFinallyCommand(command, SimpleCommand.NullCommand);

            command.Input = "input";
            tryCatchFinallyCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private TryCatchFinallyCommand CreateCorePauseAbortCommand(bool pause)
        {
            TryCatchFinallyCommand command = null;
            var coreCommand = new SequentialCommand("Core");
            coreCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (pause)
                coreCommand.Add(new SimpleCommand(() => command.Pause(), "S2-Stop"));
            else
                coreCommand.Add(new SimpleCommand(() => command.Abort(), "S2-Abort"));

            coreCommand.Add(new SimpleCommand(() => { }, "S3"));

            var finallyCommand = new SimpleCommand(() => { }, "Finally");
            command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");
            return command;
        }

        private TryCatchFinallyCommand CreateFinallyPauseAbortCommand(bool pause)
        {
            TryCatchFinallyCommand command = null;
            // Throw an ExtensibleCommandsException inside the Core command
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");

            // Pause or Abort inside the Finally command
            var finallyCommand = new SequentialCommand("Recovery");
            finallyCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (pause)
                finallyCommand.Add(new SimpleCommand(() => command.Pause(), "S2-Stop"));
            else
                finallyCommand.Add(new SimpleCommand(() => command.Abort(), "S2-Abort"));

            finallyCommand.Add(new SimpleCommand(() => { }, "S3"));
            command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");
            return command;
        }
    }
}
