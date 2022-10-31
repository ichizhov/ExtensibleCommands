using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExtensibleCommands;

namespace ExtensibleCommandsUnitTest
{
    [TestClass]
    public class GenericExtensibleCommandsTest
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

        // ------------------------------------------------------------------------------------------------
        // Helper classes

        private class Coord
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Coord() { }

            public Coord(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        private class VisionSearchInput
        {
            public Coord TargetPosition { get; set; }
            public string PatternID { get; set; }

            public VisionSearchInput(Coord position, string patternID)
            {
                TargetPosition = position;
                PatternID = patternID;
            }
        }

        private class MotionController
        {
            /// <summary>
            /// Simulate Abort move
            /// </summary>
            public void Abort() { }

            /// <summary>
            /// Simulate Stage homing
            /// </summary>
            /// <param name="axis"></param>
            public void Home(string axis) { }

            /// <summary>
            /// Simulate Stage move
            /// </summary>
            /// <param name="axis"></param>
            /// <param name="position"></param>
            /// <returns></returns>
            public double Move(string axis, double position) { return position + 0.001; }
        }

        private class VisionProcessor
        {
            public double Search(string patternID)
            {
                double score = 95.3;
                return score;
            }
        }

        /// <summary>
        /// Simulation class for Motion System
        /// </summary>
        private class MotionSystem
        {
            public SimpleCommandIO<double, double> MoveXAxisCommand { get; private set; }
            public SimpleCommandIO<double, double> MoveYAxisCommand { get; private set; }
            public SimpleCommandIO<double, double> MoveZAxisCommand { get; private set; }

            public MotionSystem(MotionController motionController)
            {
                MoveXAxisCommand = new SimpleCommandIO<double, double>(position => motionController.Move("X", position), "Move X");
                MoveYAxisCommand = new SimpleCommandIO<double, double>(position => motionController.Move("Y", position), "Move Y");
                MoveZAxisCommand = new SimpleCommandIO<double, double>(position => motionController.Move("Z", position), "Move Z");
            }
        }

        /// <summary>
        /// Simulation class for Vision System
        /// </summary>
        private class VisionSystem
        {
            public SimpleCommandIO<VisionSearchInput, double> VisionSearchCommand { get; private set; }

            public VisionSystem(VisionProcessor visionProcessor)
            {
                VisionSearchCommand = new SimpleCommandIO<VisionSearchInput, double>(input => visionProcessor.Search(input.PatternID), "OutScore");
            }
        }

        /// <summary>
        /// Container for Pattern Search command
        /// </summary>
        private class PatternSearchCoordinator
        {
            public PatternSearchCommand PatternSearchCommand { get; private set; }

            public PatternSearchCoordinator(MotionSystem motionSystem, VisionSystem visionSystem)
            {
                PatternSearchCommand = new PatternSearchCommand(motionSystem, visionSystem);
            }
        }

        /// <summary>
        /// Custom command class implementing Pattern Search sequence
        /// </summary>
        private class PatternSearchCommand : SequentialCommand
        {
            // Input
            public Coord TargetPosition { set; get; }

            // Output
            public double VisionSearchScore { private set; get; }
            public Coord ActualPosition { private set; get; }

            public PatternSearchCommand(MotionSystem motionSystem, VisionSystem visionSystem)
            {
                Name = "Pattern Search";

                Add(new SimpleCommand(() => motionSystem.MoveXAxisCommand.Input = TargetPosition.X))    // Set input for X Move
                .Add(new SimpleCommand(() => motionSystem.MoveYAxisCommand.Input = TargetPosition.Y))   // Set input for Y Move
                .Add(new SimpleCommand(() => motionSystem.MoveZAxisCommand.Input = TargetPosition.Z))   // Set input for Z Move
                .Add(new ParallelCommand("Stage move")
                    .Add(motionSystem.MoveXAxisCommand)
                    .Add(motionSystem.MoveYAxisCommand)
                    .Add(motionSystem.MoveZAxisCommand))           // Do stage move (parallel X, Y, Z)             
                .Add(new SimpleCommand(() =>
                        visionSystem.VisionSearchCommand.Input = new VisionSearchInput(
                            new Coord(motionSystem.MoveXAxisCommand.Output,
                                motionSystem.MoveYAxisCommand.Output,
                                motionSystem.MoveZAxisCommand.Output),
                                "PATTERN_ID")
                    ))                                              // Set input for Vision Search
                .Add(visionSystem.VisionSearchCommand)              // Do Vision Search
                .Add(new SimpleCommand(() => 
                    { 
                        VisionSearchScore = visionSystem.VisionSearchCommand.Output;
                        ActualPosition = new Coord(motionSystem.MoveXAxisCommand.Output,
                            motionSystem.MoveYAxisCommand.Output,
                            motionSystem.MoveZAxisCommand.Output);
                    }));                                            // Set overall command output
            }
        }

        [TestMethod]
        public void StageInitializationTest()
        {
            // Simulate HW resources
            var mc = new MotionController();

            // Define a command to initialize stage parameters
            var initParametersCommand = new SimpleCommand(() => { /* Add parameter initialization! */}, "Init parameters");

            // Define abortable X/Y/Z Home commands
            var homeXAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Home("X")), mc.Abort, "Home X");

            var homeYAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Home("Y")), mc.Abort, "Home Y");

            var homeZAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Home("Z")), mc.Abort, "Home Z");

            // Define X/Y/Z retry Home commands
            var homeXAxisRetryCommand = new RetryCommand(homeXAxisCommand, 3);
            var homeYAxisRetryCommand = new RetryCommand(homeYAxisCommand, 3);
            var homeZAxisRetryCommand = new RetryCommand(homeZAxisCommand, 3);

            // Define abortable X/Y/Z Initial move commands
            var initMoveXAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Move("X", 0.0)), 
                mc.Abort, "Init Move X");

            var initMoveYAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Move("Y", 0.0)),
            mc.Abort, "Init Move Y");

            var initMoveZAxisCommand = new AbortableCommand(new SimpleCommand(() => mc.Move("Z", 0.0)),
            mc.Abort, "Init Move Z");

            // Define an command to do a parallel home and intial move of all 3 axes
            var homeAndMoveCommand = new ParallelCommand("Parallel Home and Move")
                .Add(new SequentialCommand("Home and Move X")
                        .Add(homeXAxisRetryCommand)
                        .Add(initMoveXAxisCommand))
                .Add(new SequentialCommand("Home and Move Y")
                        .Add(homeYAxisRetryCommand)
                        .Add(initMoveYAxisCommand))
                .Add(new SequentialCommand("Home and Move Z")
                        .Add(homeZAxisRetryCommand)
                        .Add(initMoveZAxisCommand));

            // Define an command to log stage initialization record
            var logRecordCommand = new SimpleCommand(() => { /* Add logging! */}, "Log record");

            // Define a complete Stage Initialization command
            var stageInitializationCommand = new TryCatchFinallyCommand(new SequentialCommand("Initialize")
                .Add(initParametersCommand).Add(homeAndMoveCommand), logRecordCommand, "Stage Initialization");

            stageInitializationCommand.Run();
        }

        [TestMethod()]
        public void ParameterInjectionTest()
        {
            // Simulate HW resources
            var patternSearchCoordinator = new PatternSearchCoordinator(new MotionSystem(new MotionController()),
                new VisionSystem(new VisionProcessor()));

            // Create main alignment command
            var cmd = patternSearchCoordinator.PatternSearchCommand;
            cmd.TargetPosition = new Coord(100.0, 20.0, -30.0);
            cmd.Run();

            Assert.AreEqual(95.3, cmd.VisionSearchScore);
            Assert.AreEqual(100.001, cmd.ActualPosition.X);
            Assert.AreEqual(20.001, cmd.ActualPosition.Y);
            Assert.AreEqual(-29.999, cmd.ActualPosition.Z);

            cmd.TargetPosition = new Coord(-10.0, 23.0, -3.0);
            cmd.Run();

            Assert.AreEqual(95.3, cmd.VisionSearchScore);
            Assert.AreEqual(-9.999, cmd.ActualPosition.X);
            Assert.AreEqual(23.001, cmd.ActualPosition.Y);
            Assert.AreEqual(-2.999, cmd.ActualPosition.Z);
        }
    }
}
