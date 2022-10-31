using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    /// <summary>
    /// Summary description for CommandExamplesTest
    /// </summary>
    [TestClass]
    public class CommandExamplesTest
    {
        public CommandExamplesTest()
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
        //
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

        [TestMethod]
        public void SimpleCommandExample()
        {
            var simpleCommand = new SimpleCommand(() =>
                Console.WriteLine("Simple Command test"), "Simple Command");

            simpleCommand.Run();
        }

        [TestMethod]
        public void SimpleCommandInputExample()
        {
            var simpleCommand = new SimpleCommandI<int>(i =>
                Console.WriteLine("Simple Command input = {0}", i), "Simple Command with input");

            simpleCommand.Input = 10;
            simpleCommand.Run();
        }

        [TestMethod]
        public void SimpleCommandInputOutputExample()
        {
            var simpleCommand = new SimpleCommandIO<int, int>(i => i * i, "Simple Command with input/output");

            simpleCommand.Input = 2;
            simpleCommand.Run();
            Console.WriteLine("Simple Command output = {0}", simpleCommand.Output);
        }

        [TestMethod]
        public void SequentialCommandExample()
        {
            var sequentialCommand = new SequentialCommand("Sequential Command");
            sequentialCommand.Add(new SimpleCommand(DoSomething1, "Step 1"));
            sequentialCommand.Add(new SimpleCommand(DoSomething2, "Step 2"));

            sequentialCommand.Run();

            var sequentialCommandA = new SequentialCommand("Sequential Command A")
                .Add(new SimpleCommand(DoSomething1, "Step 1"))
                .Add(new SimpleCommand(DoSomething2, "Step 2"));

            sequentialCommandA.Run();
        }

        [TestMethod]
        public void ParallelCommandExample()
        {
            var parallelCommand = new ParallelCommand("Parallel Command");
            parallelCommand.Add(new SimpleCommand(DoSomething1, "Step 1"));
            parallelCommand.Add(new SimpleCommand(DoSomething2, "Step 2"));

            parallelCommand.Run();

            var parallelCommandA = new ParallelCommand("Parallel Command A")
                .Add(new SimpleCommand(DoSomething1, "Step 1"))
                .Add(new SimpleCommand(DoSomething2, "Step 2"));

            parallelCommandA.Run();
        }

        [TestMethod]
        public void ConditionalCommandExample()
        {
            bool flag = true;
            var trueCommand = new SimpleCommand(() => { }, "True");
            var falseCommand = new SimpleCommand(() => { }, "False");
            var conditionalCommand = new ConditionalCommand(() => flag,
                trueCommand, falseCommand, "Conditional");

            conditionalCommand.Run();
        }

        [TestMethod]
        public void RetryCommandExample()
        {
            var coreCommand = new SimpleCommand(() => { throw new ExtensibleCommandsAllowRetryException(0, "ERROR!"); });
            var retryCommand = new RetryCommand(coreCommand, 3, 100, "Retry command with delay");

            retryCommand.Run();
        }

        [TestMethod]
        public void CyclicCommandExample()
        {
            var cyclicCommand = new CyclicCommand(new SimpleCommand(DoSomething), 100,
                "Repeat DoSomething() 100 times");

            cyclicCommand.Run();
        }

        [TestMethod]
        public void GenericCyclicCommandExample()
        {
            var list = new List<int> { 10, 20, 30 };
            var coreCommand = new SimpleCommand(DoSomething);
            var genericCyclicCommand = new GenericCyclicCommand<int>(coreCommand, list,
                "Cycle through elements of the list");

            genericCyclicCommand.Run();
        }

        [TestMethod]
        public void RecoverableCommandExample()
        {
            var coreCommand = new SimpleCommand(() => { throw new
                ExtensibleCommandsAllowRecoveryException(0, "ERROR!"); });
            var recoveryCommand = new SimpleCommand(DoSomething);
            var recoverableCommand = new RecoverableCommand(coreCommand, recoveryCommand,
                "Recoverable Command");

            recoverableCommand.Run();
        }

        [TestMethod]
        public void RetryAndRecoveryExample()
        {
            var coreCommand = new SimpleCommand(() => { throw new
                ExtensibleCommandsAllowRetryException(0, "ERROR!"); });
            var retryCommand = new RetryCommand(coreCommand, 3, 100, "Retry command with delay");
            var recoveryCommand = new SimpleCommand(DoSomething);
            var recoverableCommand = new RecoverableCommand(retryCommand, recoveryCommand,
                "Retry and Recovery");

            recoverableCommand.Run();
        }

        [TestMethod]
        public void TryCatchFinallyCommandExample()
        {
            var coreCommand = new SimpleCommand(DoSomething);
            var finallyCommand = new SimpleCommand(ReturnToSafeState);
            var tryCatchFinallyCommand = new TryCatchFinallyCommand(coreCommand,
                finallyCommand, "Try-Catch-Finally");

            tryCatchFinallyCommand.Run();
        }

        [TestMethod]
        public void WhileCommandExample()
        {
            int counter = 0;
            var coreCommand = new SimpleCommand(() => counter++);
            var initCommand = new SimpleCommand(() => counter = 0);
            var whileCommand = new WhileCommand(() => counter < 5, initCommand, coreCommand,
                "While Command");

            whileCommand.Run();
        }

        [TestMethod]
        public void AbortCommandExample()
        {
            var coreCommand = new SimpleCommand(DoSomething);
            var abortableCommand = new AbortableCommand(coreCommand, Abort, "Abortable command test");

            abortableCommand.Run();
        }

        public void HelloWorldTest()
        {
            // Output string to console
            var helloWordlCmd = new SimpleCommandI<string>(input => Console.WriteLine(input), "Hello World\n");

            // Wait for user input
            var waitForConsoleInputCmd = new SimpleCommand(() => Console.ReadKey());

            // Create sequence of the above 2 steps
            var sequentialCommand = new SequentialCommand();
            sequentialCommand.Add(helloWordlCmd).Add(waitForConsoleInputCmd);

            // Supply input and run sequence
            helloWordlCmd.Input = "Hello World!";
            sequentialCommand.Run();
        }

        //----------------------------------------------------------------------------------------------------------------------

        private void DoSomething()
        {

        }

        private void DoSomething1()
        {

        }

        private void DoSomething2()
        {

        }

        private void ReturnToSafeState()
        {

        }

        private void Abort()
        {

        }

    }
}
