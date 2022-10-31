package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static java.lang.Thread.sleep;

public class GenericCyclicCommandTest {
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
        var command = new GenericCyclicCommand<Integer>(SimpleCommand.NullCommand, Arrays.asList(2));
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "Generic Cyclic");

        command = new GenericCyclicCommand<Integer>(SimpleCommand.NullCommand, Arrays.asList(2), "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new GenericCyclicCommand<Integer>(null, null);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }

        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            new GenericCyclicCommand<Integer>(SimpleCommand.NullCommand, null, "");
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
        Assert.assertEquals(30, (int)command.getCurrentElement());  // Enumerator should iterate until the end of the collection and be equal to last element
    }

    @Test
    public void runErrorTest() throws Exception {
        var command = createErrorCyclicCommand();

        Setup.runAndWaitForFailure(command);
        Assert.assertEquals(command.getCoreCommand().getState(), State.Failed);
        Assert.assertEquals(1, command.getCurrentCycle());
        Assert.assertEquals(10, (int)command.getCurrentElement());  // Enumerator should be at 1st element

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
        Assert.assertEquals(3, cyclicCommand.getCurrentCycle());
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
        Assert.assertEquals(State.Aborted, command.getCoreCommand().getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command = createCyclicCommand();

        Assert.assertEquals(2, command.getCoreCommand().getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(2, command.getCoreCommand().getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(1, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command1 = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var list = new ArrayList<Integer>() { { add(10); add(20); add(30); }};
        var coreCommand = new SequentialCommand();
        var cyclicCommand = new GenericCyclicCommand<>(coreCommand, list);

        coreCommand.add(new SimpleCommand(() -> { int k = cyclicCommand.getCurrentElement(); }))
            .add(new SimpleCommand(() -> sleep((int)(0.6 * Setup.ThreadLatencyDelayMsec)), "Sleep"))
            .add(command1);

        command1.setInput("input");
        cyclicCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private GenericCyclicCommand<Integer> createCyclicCommand() {
        List<Integer> list = Arrays.asList(10, 20, 30);

        var coreCommand = new SequentialCommand();
        var command = new GenericCyclicCommand<>(coreCommand, list);

        coreCommand.add(new SimpleCommand(() -> { int k = command.getCurrentElement(); }))
            .add(new SimpleCommand(() -> sleep((Setup.ThreadLatencyDelayMsec)), "Sleep"));
        return command;
    }

    private GenericCyclicCommand<Integer> createErrorCyclicCommand() {
        List<Integer> list = Arrays.asList(10, 20, 30);

        var coreCommand = new SequentialCommand();
        var command = new GenericCyclicCommand<>(coreCommand, list);

        coreCommand.add(new SimpleCommand(() -> { int k = command.getCurrentElement(); }))
            .add(new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Sleep"));
        return command;
    }

    private GenericCyclicCommand<Integer> createPauseAbortCyclicCommand(boolean pause) {
        List<Integer> list = Arrays.asList(10, 20, 30);

        var coreCommand = new SequentialCommand("Core 2-step sequential command")
            .add(new SimpleCommand(() -> sleep((int)(0.3 * Setup.ThreadLatencyDelayMsec)), "Sleep"));

        var cyclicCommand = new GenericCyclicCommand<>(coreCommand, list, "Cyclic test command");

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
