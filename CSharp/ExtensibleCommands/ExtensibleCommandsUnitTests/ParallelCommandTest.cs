using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;
using System.Linq;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for ParallelCommandTest and is intended
    ///to contain all ParallelCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ParallelCommandTest
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
        public void ModifyWhileExecutingTest()
        {
            bool exceptionCaught = false;
            var command = new ParallelCommand("P")
                .Add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec))
                .Add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec));

            new Thread(command.Run).Start();
            Thread.Sleep((int)(0.5 * Setup.ThreadLatencyDelayMsec));

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
            var command = new ParallelCommand("Parallel")
                .Add(new SimpleCommand(() => Thread.Sleep(200), "P1"))
                .Add(new SimpleCommand(() => Thread.Sleep(300), "P2"))
                .Add(new SimpleCommand(() => Thread.Sleep(400), "P3"))
                .Add(new SimpleCommand(() => Thread.Sleep(500), "P4"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P5"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P6"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P7"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P8"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P9"));

            Setup.RunAndWaitForNormalCompletion(command);

            Assert.IsTrue(command.ElapsedTimeMsec < 1500);
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            var command = new ParallelCommand("Parallel")
                .Add(new SimpleCommand(() => Thread.Sleep(200), "P1"))
                .Add(new SimpleCommand(() => Thread.Sleep(300), "P2"))
                .Add(new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "P3-Error"))
                .Add(new SimpleCommand(() => Thread.Sleep(500), "P4"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P5"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P6"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P7"));

            Setup.RunAndWaitForFailure(command);

            Assert.IsTrue(command.ElapsedTimeMsec < 1500);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Failed, command.GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(3).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(4).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(5).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(6).CurrentState);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void RunFatalErrorTest()
        {
            var command = new ParallelCommand("Parallel")
                .Add(new SimpleCommand(() => Thread.Sleep(200), "P1"))
                .Add(new SimpleCommand(() => Thread.Sleep(300), "P2"))
                .Add(new SimpleCommand(() => { throw new Exception(Setup.TestErrorDescription); }, "P3-Error"))
                .Add(new SimpleCommand(() => Thread.Sleep(500), "P4"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P5"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P6"))
                .Add(new SimpleCommand(() => Thread.Sleep(600), "P7"));

            try
            {
                command.Run();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Fatal error"));
                Assert.AreEqual(ex.InnerException.Message, Setup.TestErrorDescription);
            }

            Assert.IsTrue(command.ElapsedTimeMsec < 1500);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Failed, command.GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(3).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(4).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(5).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(6).CurrentState);
            Assert.IsNull(command.Exception);
        }

        [TestMethod()]
        public void AbortTest()
        {
            var command = new ParallelCommand();
            command.Add(new SimpleCommand(() => Thread.Sleep(100), "P1"))
                .Add(new SimpleCommand(() => Thread.Sleep(100), "P2"))
                .Add(new SimpleCommand(() => command.Abort(), "P3-Abort"))
                .Add(new SimpleCommand(() => Thread.Sleep(500), "P4"))
                .Add(new SimpleCommand(() => Thread.Sleep(400), "P5"));

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(3).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(4).CurrentState);
        }

        [TestMethod()]
        public void AbortTestWithAbortableSubCommands()
        {
            var command = new ParallelCommand();
            command.Add(new AbortableSleepCommand(2600, "P1"))
                .Add(new AbortableSleepCommand(2700, "P2"))
                .Add(new SimpleCommand(() => { Thread.Sleep(3*Setup.ThreadLatencyDelayMsec); command.Abort(); }, "P3-Abort"))
                .Add(new AbortableSleepCommand(3700, "P4"));

            Setup.RunAndWaitForAbort(command);
            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Aborted, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Aborted, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Aborted, command.GetSubCommand(3).CurrentState);
        }

        [TestMethod()]
        public void PauseResumeTest()
        {
            var command = CreatePauseAbortParallelCommand();

            var assertAfterPause = new Action(() =>
            {
                Assert.IsTrue(command.CurrentState == State.Executing);
                Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(2).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(3).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(4).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(5).CurrentState);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(2).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(3).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(4).CurrentState);
            Assert.AreEqual(State.Completed, command.GetSubCommand(5).CurrentState);
        }

        [TestMethod()]
        public void PauseAbortTest()
        {
            var command = CreatePauseAbortParallelCommand();

            var assertAfterPause = new Action(() =>
            {
                Assert.IsTrue(command.CurrentState == State.Executing);
                Assert.AreEqual(State.Completed, command.GetSubCommand(0).CurrentState);
                Assert.AreEqual(State.Completed, command.GetSubCommand(1).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(2).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(3).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(4).CurrentState);
                Assert.AreEqual(State.Executing, command.GetSubCommand(5).CurrentState);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Aborted, command.GetSubCommand(2).CurrentState);
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            SimpleCommand p1, p2, p3, p4, p5, p6, p7;
            var command = new ParallelCommand()
                .Add(p1 = new SimpleCommand(() => Thread.Sleep(100), "P1"))
                .Add(p2 = new SimpleCommand(() => Thread.Sleep(100), "P2"))
                .Add(p3 = new SimpleCommand(() => Thread.Sleep(500), "P3"))
                .Add(p4 = new SimpleCommand(() => Thread.Sleep(400), "P4"))
                .Add(p5 = new SimpleCommand(() => Thread.Sleep(600), "P5"))
                .Add(p6 = new SimpleCommand(() => Thread.Sleep(200), "P6"))
                .Add(p7 = new SimpleCommand(() => Thread.Sleep(600), "P7"));

            Setup.RunAndAbort(command);

            // Sub-command are not aborted, they all should be completed by the time abort has taken effect
            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, p1.CurrentState);
            Assert.AreEqual(State.Completed, p2.CurrentState);
            Assert.AreEqual(State.Completed, p3.CurrentState);
            Assert.AreEqual(State.Completed, p4.CurrentState);
            Assert.AreEqual(State.Completed, p5.CurrentState);
            Assert.AreEqual(State.Completed, p6.CurrentState);
            Assert.AreEqual(State.Completed, p7.CurrentState);

            Setup.RunAndAbort(command);

            // Sub-command are not aborted, they all should be completed by the time abort has taken effect
            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, p1.CurrentState);
            Assert.AreEqual(State.Completed, p2.CurrentState);
            Assert.AreEqual(State.Completed, p3.CurrentState);
            Assert.AreEqual(State.Completed, p4.CurrentState);
            Assert.AreEqual(State.Completed, p5.CurrentState);
            Assert.AreEqual(State.Completed, p6.CurrentState);
            Assert.AreEqual(State.Completed, p7.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command1 = new ParallelCommand("Branch 1")
                .Add(new SimpleCommand("B1-1"))
                .Add(new SimpleCommand("B1-2"))
                .Add(new SimpleCommand("B1-3"));

            var command2 = new ParallelCommand("Branch 2")
                .Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))
                .Add(new SimpleCommand("B2-3"))
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            var command = new ParallelCommand("Main")
                .Add(new SimpleCommand("A1"))
                .Add(new SimpleCommand("A2"))
                .Add(command1)
                .Add(command2);

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(5, command2.Descendants.Count());
            Assert.AreEqual(12, command.Descendants.Count());
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(5, command2.Children.Count());
            Assert.AreEqual(4, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var parallelCommand = new ParallelCommand();

            parallelCommand.Add(command);

            command.Input = "input";
            parallelCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private ParallelCommand CreatePauseAbortParallelCommand()
        {
            var command = new ParallelCommand("Parallel test command");
            var command1 = new SequentialCommand("P3")
                .Add(new SimpleCommand(() => Thread.Sleep(Setup.ThreadLatencyDelayMsec), "P3-S1"))
                .Add(new SimpleCommand(() => Thread.Sleep(2 * Setup.ThreadLatencyDelayMsec), "P3-S2"))
                .Add(new SimpleCommand(() => Thread.Sleep(4 * Setup.ThreadLatencyDelayMsec), "P3-S3"));

            var command2 = new SequentialCommand("P6");
            command2.Add(new SimpleCommand(() => Thread.Sleep((int)(3 * Setup.ThreadLatencyDelayMsec)), "P6-S1"));
            command2.Add(new SimpleCommand(() => command.Pause(), "P6-S2"));
            command2.Add(new SimpleCommand(() => Thread.Sleep(Setup.ThreadLatencyDelayMsec), "P6-S3"));

            command.Add(new SimpleCommand(() => Thread.Sleep((int)(0.1 * Setup.ThreadLatencyDelayMsec)), "P1"))
                .Add(new SimpleCommand(() => Thread.Sleep((int)(0.1 * Setup.ThreadLatencyDelayMsec)), "P2"))
                .Add(command1)
                .Add(new SimpleCommand(() => Thread.Sleep(7 * Setup.ThreadLatencyDelayMsec), "P4"))
                .Add(new SimpleCommand(() => Thread.Sleep(9 * Setup.ThreadLatencyDelayMsec), "P5"))
                .Add(command2);
            return command;
        }
    }
}
