using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExtensibleCommands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Summary description for WhileCommandTest
    /// </summary>
    [TestClass]
    public class WhileCommandTest
    {
        public WhileCommandTest()
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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Setup.InitLog();
        }

        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            Logger.Log(Logger.LogLevel.Info,
                "----------------------------------------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info,
                string.Format("Starting Test {0}:{1}", GetType().Name, testContextInstance.TestName));
        }

        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod()]
        public void ConstructionTest()
        {
            var command = new WhileCommand(() => false, SimpleCommand.NullCommand);
            Assert.IsNull(command.InitCommand);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "While");

            command = new WhileCommand(() => false, SimpleCommand.NullCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.InitCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "While");

            command = new WhileCommand(() => false, SimpleCommand.NullCommand, 
                SimpleCommand.NullCommand, "MyCommand");
            Assert.AreEqual(command.InitCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.CoreCommand, SimpleCommand.NullCommand);
            Assert.AreEqual(command.Name, "MyCommand");

            // Malformed cases
            bool exceptionCaught = false;
            try
            {
                new WhileCommand(null, null, null, "");
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
                new WhileCommand(() => true, null);
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
            // With Init command
            var command = CreateWhileCommand(true);
            command.Run();

            Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(5, command.CurrentCycle);

            // Without the Init command
            command = CreateWhileCommand(false);
            _counter = 0;
            command.Run();

            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(5, command.CurrentCycle);
        }

        [TestMethod()]
        public void RunErrorTest()
        {
            // Error inside the Core command
            var whileCommand = CreateWhileCommandError(true);
            whileCommand.Run();

            Assert.AreEqual(State.Completed, whileCommand.InitCommand.CurrentState);
            Assert.AreEqual(State.Failed, whileCommand.CoreCommand.CurrentState);
            Assert.AreEqual(State.Failed, whileCommand.CurrentState);
            Assert.AreEqual(1, whileCommand.CurrentCycle);

            // Error inside the Init command
            whileCommand = CreateWhileCommandError(false);
            _counter = 0;
            whileCommand.Run();

            Assert.AreEqual(State.Failed, whileCommand.InitCommand.CurrentState);
            Assert.AreEqual(State.Idle, whileCommand.CoreCommand.CurrentState);
            Assert.AreEqual(State.Failed, whileCommand.CurrentState);
            Assert.AreEqual(0, whileCommand.CurrentCycle);

            Assert.AreEqual(Setup.TestErrorCode, whileCommand.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, whileCommand.Exception.Text);
        }

        [TestMethod()]
        public void AbortCoreTest()
        {
            var command = CreateCorePauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.CoreCommand.CurrentState);
            Assert.AreEqual(2, command.CurrentCycle);
        }

        [TestMethod()]
        public void PauseResumeCoreTest()
        {
            var command = CreateCorePauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.CoreCommand.CurrentState);
                Assert.AreEqual(2, command.CurrentCycle);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.AreEqual(5, command.CurrentCycle);
        }

        [TestMethod()]
        public void PauseAbortCoreTest()
        {
            var command = CreateCorePauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
                Assert.AreEqual(State.Executing, command.CoreCommand.CurrentState);
                Assert.AreEqual(2, command.CurrentCycle);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Aborted, command.CoreCommand.CurrentState);
            Assert.AreEqual(2, command.CurrentCycle);
        }

        [TestMethod()]
        public void AbortInitTest()
        {
            var command = CreateInitPauseAbortCommand(false);

            Setup.RunAndWaitForAbort(command);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Aborted, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Idle, command.CoreCommand.CurrentState);
            Assert.AreEqual(0, command.CurrentCycle);
        }

        [TestMethod()]
        public void PauseResumeInitTest()
        {
            var command = CreateInitPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Executing, command.InitCommand.CurrentState);
                Assert.AreEqual(State.Idle, command.CoreCommand.CurrentState);
                Assert.AreEqual(0, command.CurrentCycle);
            });

            Setup.PauseAndResume(command, assertAfterPause);

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(State.Completed, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Completed, command.CoreCommand.CurrentState);
            Assert.AreEqual(5, command.CurrentCycle);
        }

        [TestMethod()]
        public void PauseAbortInitTest()
        {
            var command = CreateInitPauseAbortCommand(true);

            var assertAfterPause = new Action(() =>
            {
                Assert.AreEqual(State.Executing, command.CurrentState);
                Assert.AreEqual(State.Executing, command.InitCommand.CurrentState);
                Assert.AreEqual(State.Idle, command.CoreCommand.CurrentState);
                Assert.AreEqual(0, command.CurrentCycle);
            });

            Setup.PauseAndAbort(command, assertAfterPause);

            Assert.AreEqual(State.Aborted, command.CurrentState);
            Assert.AreEqual(State.Aborted, command.InitCommand.CurrentState);
            Assert.AreEqual(State.Idle, command.CoreCommand.CurrentState);
            Assert.AreEqual(0, command.CurrentCycle);
        }

        [TestMethod()]
        public void ExternalAbortTest()
        {
            var coreCommand = new SimpleCommand(() =>
                                                  {
                                                      _counter++;
                                                      Thread.Sleep((int)(0.5*Setup.ThreadLatencyDelayMsec));
                                                  });

            var initCommand = new SimpleCommand(() => _counter = 0);
            var whileCommand = new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");

            new Task(whileCommand.Run).Start();

            Thread.Sleep(Setup.ThreadLatencyDelayMsec);
            whileCommand.Abort();
            Thread.Sleep(Setup.ThreadLatencyDelayMsec);

            Assert.AreEqual(State.Aborted, whileCommand.CurrentState);
            Assert.AreEqual(State.Completed, whileCommand.InitCommand.CurrentState);
            Assert.AreEqual(State.Completed, whileCommand.CoreCommand.CurrentState);
        }

        [TestMethod()]
        public void RetrieveSubCommandsTest()
        {
            var initCommand = new SequentialCommand("Branch 1")
                .Add(new SimpleCommand("B1-1"))
                .Add(new SimpleCommand("B1-2"))
                .Add(new SimpleCommand("B1-3"));
            
            var coreCommand = new SequentialCommand("Branch 2")
                .Add(new SimpleCommand("B2-1"))
                .Add(new SimpleCommand("B2-2"))
                .Add(new SimpleCommand("B2-3"))
                .Add(new SimpleCommand("B2-4"))
                .Add(new SimpleCommand("B2-5"));

            var command = new WhileCommand(() => { return true; }, initCommand, coreCommand, "While");

            Assert.AreEqual(3, initCommand.Descendants.Count());
            Assert.AreEqual(5, coreCommand.Descendants.Count());
            Assert.AreEqual(10, command.Descendants.Count());
            Assert.AreEqual(3, initCommand.Children.Count());
            Assert.AreEqual(5, coreCommand.Children.Count());
            Assert.AreEqual(2, command.Children.Count());
        }

        [TestMethod()]
        public void RunInputOutputTest()
        {
            var coreCommand = new SimpleCommand(() => _counter++);
            var initCommand = new SimpleCommandIO<string, int>(input => { _counter = 0; return input.Length; }, "Test");
            var whileCommand = new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");

            initCommand.Input = "input";
            whileCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private int _counter;

        private WhileCommand CreateWhileCommand(bool init)
        {
            var coreCommand = new SimpleCommand(() => _counter++);

            if (init)
            {
                var initCommand = new SimpleCommand(() => _counter = 0);
                return new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");
            }
            else
            {
                return new WhileCommand(() => _counter < 5, coreCommand);
            }
        }

        private WhileCommand CreateWhileCommandError(bool inCore)
        {
            ICommand initCommand, coreCommand;
            if (inCore)
            {
                initCommand = new SimpleCommand(() => _counter = 0);
                coreCommand = new SimpleCommand(() =>
                    {
                        _counter++; 
                        throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
                    });
            }
            else
            {
                initCommand = new SimpleCommand(() =>
                    {
                        _counter = 0;
                        throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
                    });
                coreCommand = new SimpleCommand(() => _counter++);
            }
            return new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");
        }

        private WhileCommand CreateCorePauseAbortCommand(bool pause)
        {
            var initCommand = new SimpleCommand(() => _counter = 0);
            var coreCommand = new SequentialCommand();
            coreCommand.Add(new SimpleCommand(() => _counter++ ));
            var whileCommand = new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");

            if (pause)
                coreCommand.Add(new SimpleCommand(() =>
                {
                    if (whileCommand.CurrentCycle == 2)
                    whileCommand.Pause();
                }));
            else
                coreCommand.Add(new SimpleCommand(() =>
                {
                    if (whileCommand.CurrentCycle == 2)
                        whileCommand.Abort();
                }));

            return whileCommand;
        }

        private WhileCommand CreateInitPauseAbortCommand(bool pause)
        {
            var initCommand = new SequentialCommand(); ;
            initCommand.Add(new SimpleCommand(() => _counter = 0));
            var coreCommand = new SimpleCommand(() => _counter++);
            var whileCommand = new WhileCommand(() => _counter < 5, initCommand, coreCommand, "While");

            if (pause)
                initCommand.Add(new SimpleCommand(() => whileCommand.Pause()));
            else
                initCommand.Add(new SimpleCommand(() => whileCommand.Abort()));
            initCommand.Add(new SimpleCommand(() => Thread.Sleep(Setup.ThreadLatencyDelayMsec)));

            return whileCommand;
        }
    }
}
