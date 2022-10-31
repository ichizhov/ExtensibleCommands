package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

public class GenericExtensibleCommandsTest {
    @Rule
    public TestName name = new TestName();

    @BeforeClass
    public static void setUpClass() {
        Setup.InitLog();
    }

    @Before
    public void setUpTest() {
        Logger.log(Logger.LogLevel.Info,
                "----------------------------------------------------------------------------------------------------------");
        Logger.log(Logger.LogLevel.Info,
                String.format("Starting Test %s:%s", this.getClass().getName(), name.getMethodName()));
    }

    private class Coord {
        public double X;
        public double Y;
        public double Z;

        public Coord(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }
    }

    private class VisionSearchInput {
        public Coord TargetPosition;
        public String PatternID;

        public VisionSearchInput(Coord position, String patternID) {
            TargetPosition = position;
            PatternID = patternID;
        }
    }

    private class MotionController {
        public void home(String axis) { }
        public Double move(String axis, Double position) { return position + 0.001; }
        public void abort() { }
    }

    private class VisionProcessor {
        public Double search(String patternID) {
            Double score = 95.3;
            return score;
        }
    }

    /**
     * Simulation class for Motion System
     */
    private class MotionSystem {
        public SimpleCommandIO<Double, Double> getMoveXAxisCommand() { return moveXAxisCommand; }
        public SimpleCommandIO<Double, Double> getMoveYAxisCommand() { return moveYAxisCommand; }
        public SimpleCommandIO<Double, Double> getMoveZAxisCommand() { return moveZAxisCommand; }

        private final SimpleCommandIO<Double, Double> moveXAxisCommand;
        private final SimpleCommandIO<Double, Double> moveYAxisCommand;
        private final SimpleCommandIO<Double, Double> moveZAxisCommand;

        public MotionSystem(MotionController motionController) {
            moveXAxisCommand = new SimpleCommandIO<>(position -> motionController.move("X", position), "Move X");
            moveYAxisCommand = new SimpleCommandIO<>(position -> motionController.move("Y", position), "Move Y");
            moveZAxisCommand = new SimpleCommandIO<>(position -> motionController.move("Z", position), "Move Z");
        }
    }

    /**
     * Simulation class for Vision System
     */
    private class VisionSystem {
        public SimpleCommandIO<VisionSearchInput, Double> getVisionSearchCommand() { return visionSearchCommand; }

        private final SimpleCommandIO<VisionSearchInput, Double> visionSearchCommand;

        public VisionSystem(VisionProcessor visionProcessor) {
            visionSearchCommand = new SimpleCommandIO<VisionSearchInput, Double>(input -> visionProcessor.search(input.PatternID), "OutScore");
        }
    }

    /**
     * Container for Pattern Search command
     */
    private class PatternSearchCoordinator {
        public PatternSearchCommand getPatternSearchCommand() { return patternSearchCommand; }

        private final PatternSearchCommand patternSearchCommand;

        public PatternSearchCoordinator(MotionSystem motionSystem, VisionSystem visionSystem) {
            patternSearchCommand = new PatternSearchCommand(motionSystem, visionSystem);
        }
    }

    /**
     * Custom command class implementing Pattern Search sequence
     */
    private class PatternSearchCommand extends SequentialCommand {
        public void setTargetPosition(Coord position) { targetPosition = position; }
        public double getVisionSearchScore() { return visionSearchScore; }
        public Coord getActualPosition() { return actualPosition; }

        public PatternSearchCommand(MotionSystem motionSystem, VisionSystem visionSystem) {
            name = "Alignment";
            // Create the main alignment command
            add(new SimpleCommand(() -> motionSystem.getMoveXAxisCommand().setInput(this.targetPosition.X)))
            .add(new SimpleCommand(() -> motionSystem.getMoveYAxisCommand().setInput(this.targetPosition.Y)))
            .add(new SimpleCommand(() -> motionSystem.getMoveZAxisCommand().setInput(this.targetPosition.Z)))
            .add(new ParallelCommand("Stage move")
                .add(motionSystem.getMoveXAxisCommand())
                .add(motionSystem.getMoveYAxisCommand())
                .add(motionSystem.getMoveZAxisCommand()))                  // Do stage move
            .add(new SimpleCommand(() -> visionSystem.getVisionSearchCommand().setInput(
                new VisionSearchInput(
                    new Coord(motionSystem.getMoveXAxisCommand().getOutput(),
                        motionSystem.getMoveYAxisCommand().getOutput(),
                        motionSystem.getMoveZAxisCommand().getOutput()),
                        "PATTERN_ID")
                )))      // Do vision search
            .add(visionSystem.getVisionSearchCommand())
            .add(new SimpleCommand(() -> {
                visionSearchScore = visionSystem.getVisionSearchCommand().getOutput();
                actualPosition = new Coord(motionSystem.getMoveXAxisCommand().getOutput(),
                    motionSystem.getMoveYAxisCommand().getOutput(),
                    motionSystem.getMoveZAxisCommand().getOutput());
                    }));
        }

        private Coord targetPosition;
        private double visionSearchScore;
        private Coord actualPosition;
    }

    @Test
    public void stageInitializationTest() throws Exception {
        // Simulate HW resources
        var mc = new MotionController();

        // Define a command to initialize stage parameters
        var initParametersCommand = new SimpleCommand(() -> { /* Add parameter initialization! */}, "Init parameters");

        // Define abortable X/Y/Z home commands
        var homeXAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.home("X")),
                () -> mc.abort(), "home X");

        var homeYAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.home("Y")),
                () -> mc.abort(), "home Y");

        var homeZAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.home("Z")),
                () -> mc.abort(), "home Z");

        // Define X/Y/Z retry home commands
        var homeXAxisRetryCommand = new RetryCommand(homeXAxisCommand, 3);
        var homeYAxisRetryCommand = new RetryCommand(homeYAxisCommand, 3);
        var homeZAxisRetryCommand = new RetryCommand(homeZAxisCommand, 3);

        // Define abortable X/Y/Z Initial move commands
        var initMoveXAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.move("X", 0.0)),
        () -> mc.abort(), "Init move X");

        var initMoveYAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.move("Y", 0.0)),
        () -> mc.abort(), "Init move Y");

        var initMoveZAxisCommand = new AbortableCommand(new SimpleCommand(() -> mc.move("Z", 0.0)),
        () -> mc.abort(), "Init move Z");

        // Define an command to do a parallel home and intial move of all 3 axes
        var homeAndMoveCommand = new ParallelCommand("Parallel home and move")
                .add(new SequentialCommand("home and move X")
                        .add(homeXAxisRetryCommand)
                        .add(initMoveXAxisCommand))
                .add(new SequentialCommand("home and move Y")
                        .add(homeYAxisRetryCommand)
                        .add(initMoveYAxisCommand))
                .add(new SequentialCommand("home and move Z")
                        .add(homeZAxisRetryCommand)
                        .add(initMoveZAxisCommand));

        // Define an command to log stage initialization record
        var logRecordCommand = new SimpleCommand(() -> { /* Add logging! */}, "Log record");

        // Define a complete Stage Initialization command
        var stageInitializationCommand = new TryCatchFinallyCommand(new SequentialCommand("Initialize")
                .add(initParametersCommand).add(homeAndMoveCommand), logRecordCommand, "Stage Initialization");

        stageInitializationCommand.run();
    }

    @Test
    public void ParameterInjectionTest() throws Exception {
        // Simulate HW resources
        var patternSearchCoordinator = new PatternSearchCoordinator(new MotionSystem(new MotionController()),
                new VisionSystem(new VisionProcessor()));

        // Create main alignment command
        var cmd = patternSearchCoordinator.getPatternSearchCommand();
        cmd.setTargetPosition(new Coord(100.0, 20.0, -30.0));
        cmd.run();

        Assert.assertEquals(95.3, cmd.getVisionSearchScore(), 0.001);
        Assert.assertEquals(100.001, cmd.getActualPosition().X, 0.001);
        Assert.assertEquals(20.001, cmd.getActualPosition().Y, 0.001);
        Assert.assertEquals(-29.999, cmd.getActualPosition().Z, 0.001);

        cmd.setTargetPosition(new Coord(-10.0, 23.0, -3.0));
        cmd.run();

        Assert.assertEquals(95.3, cmd.getVisionSearchScore(), 0.001);
        Assert.assertEquals(-9.999, cmd.getActualPosition().X, 0.001);
        Assert.assertEquals(23.001, cmd.getActualPosition().Y, 0.001);
        Assert.assertEquals(-2.999, cmd.getActualPosition().Z, 0.001);
    }

    public void HelloWorldTest() throws Exception {
        // Output string to console
        var helloWordlCmd = new SimpleCommandI<String>(input -> System.out.println(input), "Hello World\n");

        // Wait for user input
        var waitForConsoleInputCmd = new SimpleCommand(() -> System.in.read());

        // Create sequence of the above 2 steps
        var sequentialCommand = new SequentialCommand();
        sequentialCommand.add(helloWordlCmd).add(waitForConsoleInputCmd);

        // Supply input and run sequence
        helloWordlCmd.setInput("Hello World!");
        sequentialCommand.run();
    }
}
