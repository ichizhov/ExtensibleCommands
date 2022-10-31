using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExtensibleCommands;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    ///This is a test class for SimpleCommandTest and is intended
    ///to contain all SimpleCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SimpleCommandTest
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

            _commandCompletedCount = 0;
            _commandFailedCount = 0;
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
            var command = new SimpleCommand(() => { });
            Assert.AreEqual(command.Name, "Simple");

            command = new SimpleCommand("MyCommand");
            Assert.AreEqual(command.Name, "MyCommand");

            command = new SimpleCommand(() => { }, "MyCommand");
            Assert.AreEqual(command.Name, "MyCommand");

            var command1 = new SimpleCommandI<int>(a => { });
            Assert.AreEqual(command1.Name, "Simple(Input)");

            command1 = new SimpleCommandI<int>(a => { }, "MyCommand");
            Assert.AreEqual(command1.Name, "MyCommand");

            var command2 = new SimpleCommandIO<int, int>(a => 2);
            Assert.AreEqual(command2.Name, "Simple(Input, Output)");

            command2 = new SimpleCommandIO<int, int>(a => 2, "MyCommand");
            Assert.AreEqual(command2.Name, "MyCommand");
        }

        [TestMethod()]
        public void RunNullCommandTest()
        {
            var command = SimpleCommand.NullCommand;
            var started = command.StateChanged.Where(s => s == State.Executing).Subscribe(s => { _commandStartedCount++; });
            var completed = command.StateChanged.Where(s => s == State.Completed).Subscribe(s => { _commandCompletedCount++; });
            var failed = command.StateChanged.Where(s => s == State.Failed).Subscribe(s => { _commandFailedCount++; });
           
            command.Run();

            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(1, _commandStartedCount);        // Started event is generated ONCE
            Assert.AreEqual(1, _commandCompletedCount);      // Completed event is generated ONCE
            Assert.AreEqual(0, _commandFailedCount);         // Failed event is NOT generated

            started.Dispose();
            completed.Dispose();
            failed.Dispose();
        }

        [TestMethod()]
        public void RunOKTest()
        {
            var command = new SimpleCommand(() => System.Threading.Thread.Sleep(10));

            var started = command.StateChanged.Where(s => s == State.Executing).Subscribe(s => { _commandStartedCount++; });
            var completed = command.StateChanged.Where(s => s == State.Completed).Subscribe(s => { _commandCompletedCount++; });
            var failed = command.StateChanged.Where(s => s == State.Failed).Subscribe(s => { _commandFailedCount++; });

            command.Run();
            Assert.AreEqual(State.Completed, command.CurrentState);
            Assert.AreEqual(1, _commandStartedCount);        // Started event is generated ONCE
            Assert.AreEqual(1, _commandCompletedCount);      // Completed event is generated ONCE
            Assert.AreEqual(0, _commandFailedCount);         // Failed event is NOT generated

            Assert.IsTrue(command.ElapsedTimeMsec > 9);
            Assert.IsTrue(command.ElapsedTimeMsec < 50);    // Allow some buffer

            Assert.IsTrue(command.ElapsedTime.TotalMilliseconds > 9);
            Assert.IsTrue(command.ElapsedTime.TotalMilliseconds < 50);    // Allow some buffer

            started.Dispose();
            completed.Dispose();
            failed.Dispose();
        }
        
        [TestMethod()]
        public void RunErrorTest()
        {
            var command = new SimpleCommand(() => { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); });

            var started = command.StateChanged.Where(s => s == State.Executing).Subscribe(s => { _commandStartedCount++; });
            var completed = command.StateChanged.Where(s => s == State.Completed).Subscribe(s => { _commandCompletedCount++; });
            var failed = command.StateChanged.Where(s => s == State.Failed).Subscribe(s => { _commandFailedCount++; });

            command.Run();
            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(1, _commandStartedCount);        // Started event is generated ONCE
            Assert.AreEqual(1, _commandFailedCount);         // Failed event is generated ONCE
            Assert.AreEqual(0, _commandCompletedCount);      // Completed event is NOT generated

            Assert.AreEqual(Setup.TestErrorCode, command.Exception.ID);
            Assert.AreEqual(Setup.TestErrorDescription, command.Exception.Text);

            started.Dispose();
            completed.Dispose();
            failed.Dispose();
        }

        [TestMethod()]
        public void RunFatalErrorTest()
        {
            var command = new SimpleCommand(() => { throw new Exception(Setup.TestErrorDescription); });

            var started = command.StateChanged.Where(s => s == State.Executing).Subscribe(s => { _commandStartedCount++; });
            var completed = command.StateChanged.Where(s => s == State.Completed).Subscribe(s => { _commandCompletedCount++; });
            var failed = command.StateChanged.Where(s => s == State.Failed).Subscribe(s => { _commandFailedCount++; });

            try
            {
                command.Run();
                Assert.IsTrue(false);
            }
            catch (Exception e)
            {
                Assert.AreEqual(Setup.TestErrorDescription, e.Message);
            }

            Assert.AreEqual(State.Failed, command.CurrentState);
            Assert.AreEqual(1, _commandStartedCount);        // Started event is generated ONCE
            Assert.AreEqual(1, _commandFailedCount);         // Failed event is generated ONCE
            Assert.AreEqual(0, _commandCompletedCount);      // Completed event is NOT generated

            started.Dispose();
            completed.Dispose();
            failed.Dispose();
        }

        [TestMethod()]
        public void MultiThreadedTest()
        {
            int a = 0;
            var c1 = new SimpleCommand(() => a++);
            var t1 = new Task(() => { for (int i = 0; i < 1000; i++) { c1.Run(); }});
            var t2 = new Task(() => { for (int i = 0; i < 1000; i++) { c1.Run(); }});
            var t3 = new Task(() => { for (int i = 0; i < 1000; i++) { c1.Run(); }});
            var t4 = new Task(() => { for (int i = 0; i < 1000; i++) { c1.Run(); }});
            var t5 = new Task(() => { for (int i = 0; i < 1000; i++) { c1.Run(); } });
            
            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t5.Start();

            t1.Wait();
            t2.Wait();
            t3.Wait();
            t4.Wait();
            t5.Wait();

            Assert.AreEqual(5000, a);
        }
            
        [TestMethod()]
        public void SimpleInputTest()
        {
            var command = new SimpleCommandI<string>(input => { }, "Test");

            // Run #1 : Directly set input
            command.Input = "333";
            command.Run();

            Assert.AreEqual("333", command.Input);
        }

        [TestMethod()]
        public void SimpleInputOutputTest()
        {
            var command = new SimpleCommandIO<string, int>(input => input.Length, "Test");

            // Run #1 : Directly set input
            command.Input = "333";
            command.Run();

            Assert.AreEqual("333", command.Input);
            Assert.AreEqual(3, command.Output);
        }

        //----------------------------------------------------------------------------------------------------------------------

        private int _commandStartedCount;
        private int _commandCompletedCount;
        private int _commandFailedCount;
    }
}
