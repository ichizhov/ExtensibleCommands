using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for CyclicCommandTest and is intended
    ///to contain all CyclicCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CyclicCommandTest
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
            var command = new CyclicCommand(SimpleCommand.NullCommand, 3);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.NumberOfRepeats, 3);
            Assert.AreEqual(command.Name, "Cyclic");

            command = new CyclicCommand(SimpleCommand.NullCommand, 5, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.NumberOfRepeats, 5);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new CyclicCommand(null, 5);
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
            var command = CreateCyclicCommand();
            Setup.RunAndWaitForNormalCompletion(command);
            Assert.AreEqual(5, command.CurrentCycle);
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); });
            var command = new CyclicCommand(coreCommand, 3);

            Setup.RunAndWaitForFailure(command);
            Assert.AreEqual(coreCommand.CurrentState, State.Failed);

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);
        }

        [TestMethod()]
        public void PauseResumeTest()
        {
            var cyclicCommand = CreatePauseAbortCyclicCommand(true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, cyclicCommand.CoreCommand.CurrentState);
                Assert.AreEqual(1, cyclicCommand.CurrentCycle);
            });

            Setup.PauseAndResume(cyclicCommand, assertAfterPause);

            Assert.AreEqual(State.Completed, cyclicCommand.CurrentState);
            Assert.AreEqual(State.Completed, cyclicCommand.CoreCommand.CurrentState);
            Assert.AreEqual(2, cyclicCommand.CurrentCycle);
        }

        [TestMethod()]
        public void PauseAbortTest()
        {
            var cyclicCommand = CreatePauseAbortCyclicCommand(true);

            var assertAfterPause = new System.Action(() =>
            {
                Assert.AreEqual(State.Executing, cyclicCommand.CoreCommand.CurrentState);
                Assert.AreEqual(1, cyclicCommand.CurrentCycle);
            });

            Setup.PauseAndAbort(cyclicCommand, assertAfterPause);

            Assert.AreEqual(State.Aborted, cyclicCommand.CurrentState);
            Assert.AreEqual(State.Aborted, cyclicCommand.CoreCommand.CurrentState);
            Assert.AreEqual(1, cyclicCommand.CurrentCycle);
        }

        [TestMethod()]
        public void AbortTest()
        {
            var cyclicCommand = CreatePauseAbortCyclicCommand(false);

            Setup.RunAndWaitForAbort(cyclicCommand);

            Assert.AreEqual(State.Aborted, cyclicCommand.CoreCommand.CurrentState);
            Assert.AreEqual(1, cyclicCommand.CurrentCycle);
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            var command = CreateCyclicCommand();

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

            var command = new CyclicCommand(command1, 3);

            Assert.AreEqual(3, command1.Descendants.Count());
            Assert.AreEqual(4, command.Descendants.Count());
            Assert.AreEqual(3, command1.Children.Count());
            Assert.AreEqual(1, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");
            var cyclicCommand = new CyclicCommand(command, 3, "");

            command.Input = "input";
            cyclicCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private CyclicCommand CreateCyclicCommand()
        {
            var coreCommand = new SimpleCommand(() => Thread.Sleep(Setup.ThreadLatencyDelayMsec), "Sleep");
            return new CyclicCommand(coreCommand, 5);
        }

        private CyclicCommand CreatePauseAbortCyclicCommand(bool pause)
        {
            var coreCommand = new SequentialCommand("Core 2-step sequential command");
            CyclicCommand cyclicCommand = new CyclicCommand(coreCommand, 2, "Cyclic test command");
            coreCommand.Add(new SimpleCommand(() => Thread.Sleep((int)(0.3 * Setup.ThreadLatencyDelayMsec)), "Sleep"));
            if (pause)
                coreCommand.Add(new SimpleCommand(() =>
                {
                    // Only pause during the first cycle
                    if (cyclicCommand.CurrentCycle == 1)
                        cyclicCommand.Pause();
                }));
            else
                coreCommand.Add(new SimpleCommand(() =>
                {
                    // Only abort during the first cycle
                    if (cyclicCommand.CurrentCycle == 1)
                        cyclicCommand.Abort();
                }));

            return cyclicCommand;
        }
    }
}
