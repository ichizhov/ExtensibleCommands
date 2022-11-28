using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Summary description for GenericCyclicCommandTest
    /// </summary>
    [TestClass]
    public class GenericCyclicCommandTest
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
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [TestInitialize]
        public void MyTestInitialize()
        {
            Logger.Log(Logger.LogLevel.Info,
                "----------------------------------------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info,
                string.Format("Starting Test {0}:{1}", GetType().Name, testContextInstance.TestName));
        }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod()]
        public void ConstructionTest()
        {
            var command = new GenericCyclicCommand<int>(SimpleCommand.NullCommand, new List<int> { 2 });
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "Generic Cyclic");

            command = new GenericCyclicCommand<int>(SimpleCommand.NullCommand, new List<int> { 2 }, "MyCommand");
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new GenericCyclicCommand<int>(null, null);
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
                new GenericCyclicCommand<int>(SimpleCommand.NullCommand, null, "");
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
            Assert.AreEqual(50, command.CurrentElement);  // Enumerator should iterate until the end of the collection and be equal to last element
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            var command = CreateErrorCyclicCommand();

            Setup.RunAndWaitForFailure(command);
            Assert.AreEqual(command.CoreCommand.CurrentState, State.Failed);
            Assert.AreEqual(1, command.CurrentCycle);
            Assert.AreEqual(10, command.CurrentElement);  // Enumerator should be at 1st element

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
            Assert.AreEqual(3, cyclicCommand.CurrentCycle);
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
            Assert.AreEqual(State.Aborted, command.CoreCommand.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var command = CreateCyclicCommand();

            Assert.AreEqual(2, command.CoreCommand.Descendants.Count());
            Assert.AreEqual(3, command.Descendants.Count());
            Assert.AreEqual(2, command.CoreCommand.Children.Count());
            Assert.AreEqual(1, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");

            var list = new List<int> { 10, 20, 30 };
            var coreCommand = new SequentialCommand();
            var cyclicCommand = new GenericCyclicCommand<int>(coreCommand, list);

            coreCommand.Add(new SimpleCommand(() => { int k = cyclicCommand.CurrentElement; }))
                .Add(new SimpleCommand(() => Thread.Sleep((int)(0.6 * Setup.ThreadLatencyDelayMsec)), "Sleep"))
                .Add(command);

            command.Input = "input";
            cyclicCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private GenericCyclicCommand<int> CreateCyclicCommand()
        {
            var list = new List<int> {10, 20, 30, 40, 50};
            var coreCommand = new SequentialCommand();
            var command = new GenericCyclicCommand<int>(coreCommand, list);

            coreCommand.Add(new SimpleCommand(() => { int k = command.CurrentElement; }))
                .Add(new SimpleCommand(() => Thread.Sleep(Setup.ThreadLatencyDelayMsec), "Sleep"));
            return command;
        }

        private GenericCyclicCommand<int> CreateErrorCyclicCommand()
        {
            var list = new List<int> { 10, 20, 30 };
            var coreCommand = new SequentialCommand();
            var command = new GenericCyclicCommand<int>(coreCommand, list);

            coreCommand.Add(new SimpleCommand(() => { int k = command.CurrentElement; }))
                .Add(new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Sleep"));
            return command;
        }

        private GenericCyclicCommand<int> CreatePauseAbortCyclicCommand(bool pause)
        {
            var list = new List<int> { 10, 20, 30 };
            GenericCyclicCommand<int> cyclicCommand = null;
            var coreCommand = new SequentialCommand("Core 2-step sequential command");
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
            cyclicCommand = new GenericCyclicCommand<int>(coreCommand, list, "Cyclic test command");
            return cyclicCommand;
        }
    }
}
