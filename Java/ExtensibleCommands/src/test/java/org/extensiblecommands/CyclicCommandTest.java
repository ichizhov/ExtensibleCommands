package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class CyclicCommandTest {
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

    @Test
    public void constructionTest() {
        var command = new CyclicCommand(SimpleCommand.NullCommand, 3);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getNumberOfRepeats(), 3);
        Assert.assertEquals(command.getName(), "Cyclic");

        command = new CyclicCommand(SimpleCommand.NullCommand, 5, "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getNumberOfRepeats(), 5);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new CyclicCommand(null, 5);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        var command = createCyclicCommand();
        Setup.runAndWaitForNormalCompletion(command);
        Assert.assertEquals(3, command.getCurrentCycle());
    }

    @Test
    public void runErrorTest() throws Exception {
        // If normal exception is thrown, the command should fail without excercising recovery command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); });
        var command = new CyclicCommand(coreCommand, 3);

        Setup.runAndWaitForFailure(command);
        Assert.assertEquals(coreCommand.getState(), State.Failed);

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void pauseResumeTest() throws Exception {
        var cyclicCommand = createPauseAbortCyclicCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, cyclicCommand.getCoreCommand().getState());
            Assert.assertEquals(1, cyclicCommand.getCurrentCycle());
        };

        Setup.pauseAndResume(cyclicCommand, assertAfterPause);

        Assert.assertEquals(State.Completed, cyclicCommand.getState());
        Assert.assertEquals(State.Completed, cyclicCommand.getCoreCommand().getState());
        Assert.assertEquals(2, cyclicCommand.getCurrentCycle());
    }

    @Test
    public void pauseAbortTest() throws Exception {
        var cyclicCommand = createPauseAbortCyclicCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, cyclicCommand.getCoreCommand().getState());
            Assert.assertEquals(1, cyclicCommand.getCurrentCycle());
        };

        Setup.pauseAndAbort(cyclicCommand, assertAfterPause);

        Assert.assertEquals(State.Aborted, cyclicCommand.getState());
        Assert.assertEquals(State.Aborted, cyclicCommand.getCoreCommand().getState());
        Assert.assertEquals(1, cyclicCommand.getCurrentCycle());
    }

    @Test
    public void abortTest() throws Exception {
        var cyclicCommand = createPauseAbortCyclicCommand(false);

        Setup.runAndWaitForAbort(cyclicCommand);

        Assert.assertEquals(State.Aborted, cyclicCommand.getCoreCommand().getState());
        Assert.assertEquals(1, cyclicCommand.getCurrentCycle());
    }

    @Test
    public void externalAbortTest() throws Exception {
        var command = createCyclicCommand();

        Setup.runAndAbort(command);
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command1 = new SequentialCommand("Core")
            .add(new SimpleCommand("1"))
            .add(new SimpleCommand("2"))
            .add(new SimpleCommand("3"));

        var command = new CyclicCommand(command1, 3);

        Assert.assertEquals(3, command1.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(1, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var cyclicCommand = new CyclicCommand(command, 3, "");

        command.setInput("input");
        cyclicCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private CyclicCommand createCyclicCommand() {
        var coreCommand = new SimpleCommand(() -> sleep((Setup.ThreadLatencyDelayMsec)), "Sleep");
        return new CyclicCommand(coreCommand, 3);
    }

    private CyclicCommand createPauseAbortCyclicCommand(boolean pause) {
        var coreCommand = new SequentialCommand("Core 2-step sequential command");
        var cyclicCommand = new CyclicCommand(coreCommand, 2, "Cyclic test command");
        coreCommand.add(new SimpleCommand(() -> sleep((int)(0.3 * Setup.ThreadLatencyDelayMsec)), "Sleep"));
        if (pause)
            coreCommand.add(new SimpleCommand(() -> {
                // Only pause during the first cycle
                if (cyclicCommand.getCurrentCycle() == 1)
                    cyclicCommand.pause();
            }));
        else
            coreCommand.add(new SimpleCommand(() -> {
                // Only abort during the first cycle
                if (cyclicCommand.getCurrentCycle() == 1)
                    cyclicCommand.abort();
            }));
        return cyclicCommand;
    }
}
