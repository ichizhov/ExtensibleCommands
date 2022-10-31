using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class ConditionalCommandTest
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
            var command = new ConditionalCommand(() => true, SimpleCommand.NullCommand,
                SimpleCommand.NullCommand);
            Assert.AreEqual(command.TrueCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.FalseCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "Conditional");

            command = new ConditionalCommand(() => true, SimpleCommand.NullCommand,
                SimpleCommand.NullCommand, "MyCommand");
            Assert.AreEqual(command.TrueCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.FalseCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new ConditionalCommand(() => true, null, null, "");
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
                new ConditionalCommand(() => true, SimpleCommand.NullCommand, null, "");
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
                command = new ConditionalCommand(() => true, SimpleCommand.NullCommand, SimpleCommand.NullCommand, "");
                command.Run();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("is NULL"))
                    exceptionCaught = true;
            }
            Assert.IsFalse(exceptionCaught);
        }

        [TestMethod()]
        public void TrueCommandTest()
        {
            var conditionalCommand = CreateConditionalCommand(true);

            Setup.RunAndWaitForNormalCompletion(conditionalCommand);

            Assert.AreEqual(State.Completed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Completed, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
        }

        [TestMethod()]
        public void FalseCommandTest()
        {
            var conditionalCommand = CreateConditionalCommand(false);

            Setup.RunAndWaitForNormalCompletion(conditionalCommand);

            Assert.AreEqual(State.Completed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Completed, conditionalCommand.FalseCommand.CurrentState);
        }

        [TestMethod()]
        public void RunTrueCommandErrorTest()
        {
            var trueCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "True");
            var falseCommand = new SimpleCommand(() => { }, "False");
            var conditionalCommand = new ConditionalCommand(() => true, trueCommand, falseCommand, "Conditional");

            Setup.RunAndWaitForFailure(conditionalCommand);

            Assert.AreEqual(State.Failed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Failed, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, conditionalCommand.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, conditionalCommand.Exception.Text);
        }

        [TestMethod()]
        public void RunFalseCommandErrorTest()
        {
            var trueCommand = new SimpleCommand(() => { }, "True");
            var falseCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "False");
            var conditionalCommand = new ConditionalCommand(() => false, trueCommand, falseCommand, "Conditional");

            Setup.RunAndWaitForFailure(conditionalCommand);

            Assert.AreEqual(State.Failed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Failed, conditionalCommand.FalseCommand.CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, conditionalCommand.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, conditionalCommand.Exception.Text);
        }

        [TestMethod()]
        public void AbortTrueTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(true, false);

            Setup.RunAndWaitForAbort(conditionalCommand);

            Assert.AreEqual(State.Aborted, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Aborted, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeTrueTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(true, true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, conditionalCommand.CurrentState);
                Assert.AreEqual(State.Executing, conditionalCommand.TrueCommand.CurrentState);
                Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(conditionalCommand, assertAfterPause);

            Assert.AreEqual(State.Completed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Completed, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortTrueTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(true, true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, conditionalCommand.CurrentState);
                Assert.AreEqual(State.Executing, conditionalCommand.TrueCommand.CurrentState);
                Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(conditionalCommand, assertAfterPause);

            Assert.AreEqual(State.Aborted, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Aborted, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (conditionalCommand.TrueCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void AbortFalseTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(false, false);

            Setup.RunAndWaitForAbort(conditionalCommand);

            Assert.AreEqual(State.Aborted, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Aborted, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeFalseTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(false, true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, conditionalCommand.CurrentState);
                Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
                Assert.AreEqual(State.Executing, conditionalCommand.FalseCommand.CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndResume(conditionalCommand, assertAfterPause);

            Assert.AreEqual(State.Completed, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Completed, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortFalseTest()
        {
            var conditionalCommand = CreateConditionalBranchStopAbortCommand(false, true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, conditionalCommand.CurrentState);
                Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
                Assert.AreEqual(State.Executing, conditionalCommand.FalseCommand.CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Idle, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(2).CurrentState);
            });

            Setup.PauseAndAbort(conditionalCommand, assertAfterPause);

            Assert.AreEqual(State.Aborted, conditionalCommand.CurrentState);
            Assert.AreEqual(State.Idle, conditionalCommand.TrueCommand.CurrentState);
            Assert.AreEqual(State.Aborted, conditionalCommand.FalseCommand.CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Idle, (conditionalCommand.FalseCommand as SequentialCommand).GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new SequentialCommand("Core")
                .Add(new SimpleCommand("1"))
                .Add(new SimpleCommand("2"))
                .Add(new SimpleCommand("3"));

            var command2 = new SequentialCommand("True")
                .Add(new SimpleCommand("T-1"))
                .Add(new SimpleCommand("T-2"))
                .Add(new SimpleCommand("T-3"))
                .Add(new SimpleCommand("T-4"))
                .Add(new SimpleCommand("T-5"));

            var command = new ConditionalCommand(() => true, command1, command2, "Main");

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
            
            var conditionalCommand = new ConditionalCommand(() => true,
                command, SimpleCommand.NullCommand, "");

            command.Input = "input";
            conditionalCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private ConditionalCommand CreateConditionalCommand(bool flag)
        {
            var trueCommand = new SimpleCommand(() => { }, "True");
            var falseCommand = new SimpleCommand(() => { }, "False");
            return new ConditionalCommand(() => flag, trueCommand, falseCommand, "Conditional");
        }

        private ConditionalCommand CreateConditionalBranchStopAbortCommand(bool branch, bool pause)
        {
            ICommand trueCommand, falseCommand;

            SequentialCommand seqCommand;
            if (branch)
            {
                seqCommand = new SequentialCommand("True");
                trueCommand = seqCommand;
                falseCommand = new SimpleCommand(() => { }, "False");
            }
            else
            {
                seqCommand = new SequentialCommand("False");
                trueCommand = new SimpleCommand(() => { }, "True");
                falseCommand = seqCommand;
            }

            var conditionalCommand = new ConditionalCommand(() => branch, trueCommand, falseCommand, "Conditional");

            seqCommand.Add(new SimpleCommand(() => { }, "S1"));

            if (pause)
                seqCommand.Add(new SimpleCommand(() => conditionalCommand.Pause(), "S2-Pause"));
            else
                seqCommand.Add(new SimpleCommand(() => conditionalCommand.Abort(), "S2-Abort"));
            seqCommand.Add(new SimpleCommand(() => { }, "S3"));

            return conditionalCommand;
        }
    }
}
