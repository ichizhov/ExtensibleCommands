package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

public class ConditionalCommandTest {
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
        var command = new ConditionalCommand(() -> true, SimpleCommand.NullCommand,
                SimpleCommand.NullCommand);
        Assert.assertEquals(command.getTrueCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getFalseCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "Conditional");

        command = new ConditionalCommand(() -> true, SimpleCommand.NullCommand,
                SimpleCommand.NullCommand, "MyCommand");
        Assert.assertEquals(command.getTrueCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getFalseCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new ConditionalCommand(() -> true, null, null, "");
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            new ConditionalCommand(() -> true, SimpleCommand.NullCommand, null, "");
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            command = new ConditionalCommand(() -> true, SimpleCommand.NullCommand, SimpleCommand.NullCommand, "");
            command.run();
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertFalse(exceptionCaught);
    }

    @Test
    public void trueCommandTest() throws Exception {
        var conditionalCommand = createConditionalCommand(true);

        Setup.runAndWaitForNormalCompletion(conditionalCommand);

        Assert.assertEquals(State.Completed, conditionalCommand.getState());
        Assert.assertEquals(State.Completed, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
    }

    @Test
    public void falseCommandTest() throws Exception {
        var conditionalCommand = createConditionalCommand(false);

        Setup.runAndWaitForNormalCompletion(conditionalCommand);

        Assert.assertEquals(State.Completed, conditionalCommand.getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Completed, conditionalCommand.getFalseCommand().getState());
    }

    @Test
    public void runTrueCommandErrorTest() throws Exception {
        var trueCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "True");
        var falseCommand = new SimpleCommand(() -> { }, "False");
        var conditionalCommand = new ConditionalCommand(() -> true, trueCommand, falseCommand, "Conditional");

        Setup.runAndWaitForFailure(conditionalCommand);

        Assert.assertEquals(State.Failed, conditionalCommand.getState());
        Assert.assertEquals(State.Failed, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());

        Assert.assertEquals(Setup.TestErrorCode, conditionalCommand.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, conditionalCommand.getException().getText());
    }

    @Test
    public void runFalseCommandErrorTest() throws Exception {
        var trueCommand = new SimpleCommand(() -> { }, "True");
        var falseCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "False");

        var conditionalCommand = new ConditionalCommand(() -> false, trueCommand, falseCommand, "Conditional");

        Setup.runAndWaitForFailure(conditionalCommand);

        Assert.assertEquals(State.Failed, conditionalCommand.getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Failed, conditionalCommand.getFalseCommand().getState());

        Assert.assertEquals(Setup.TestErrorCode, conditionalCommand.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, conditionalCommand.getException().getText());
    }

    @Test
    public void abortTrueTest() throws Exception {

        var conditionalCommand = createConditionalBranchStopAbortCommand(true, false);

        Setup.runAndWaitForAbort(conditionalCommand);

        Assert.assertEquals(State.Aborted, conditionalCommand.getState());
        Assert.assertEquals(State.Aborted, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeTrueTest() throws Exception {
        var conditionalCommand = createConditionalBranchStopAbortCommand(true, true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, conditionalCommand.getState());
            Assert.assertEquals(State.Executing, conditionalCommand.getTrueCommand().getState());
            Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(conditionalCommand, assertAfterPause);

        Assert.assertEquals(State.Completed, conditionalCommand.getState());
        Assert.assertEquals(State.Completed, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortTrueTest() throws Exception {
        var conditionalCommand = createConditionalBranchStopAbortCommand(true, true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, conditionalCommand.getState());
            Assert.assertEquals(State.Executing, conditionalCommand.getTrueCommand().getState());
            Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(conditionalCommand, assertAfterPause);

        Assert.assertEquals(State.Aborted, conditionalCommand.getState());
        Assert.assertEquals(State.Aborted, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getTrueCommand()).getSubCommand(2).getState());
    }

    @Test
    public void abortFalseTest() throws Exception {

        var conditionalCommand = createConditionalBranchStopAbortCommand(false, false);

        Setup.runAndWaitForAbort(conditionalCommand);

        Assert.assertEquals(State.Aborted, conditionalCommand.getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Aborted, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeFalseTest() throws Exception {
        var conditionalCommand = createConditionalBranchStopAbortCommand(false, true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, conditionalCommand.getState());
            Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
            Assert.assertEquals(State.Executing, conditionalCommand.getFalseCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(conditionalCommand, assertAfterPause);

        Assert.assertEquals(State.Completed, conditionalCommand.getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Completed, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortFalseTest() throws Exception {
        var conditionalCommand = createConditionalBranchStopAbortCommand(false, true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, conditionalCommand.getState());
            Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
            Assert.assertEquals(State.Executing, conditionalCommand.getFalseCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(conditionalCommand, assertAfterPause);

        Assert.assertEquals(State.Aborted, conditionalCommand.getState());
        Assert.assertEquals(State.Idle, conditionalCommand.getTrueCommand().getState());
        Assert.assertEquals(State.Aborted, conditionalCommand.getFalseCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)conditionalCommand.getFalseCommand()).getSubCommand(2).getState());    }

    @Test
    public void retrieveSubCommands() {
        var command1 = new SequentialCommand("Core")
            .add(new SimpleCommand("1"))
            .add(new SimpleCommand("2"))
            .add(new SimpleCommand("3"));

        var command2 = new SequentialCommand("True")
            .add(new SimpleCommand("T-1"))
            .add(new SimpleCommand("T-2"))
            .add(new SimpleCommand("T-3"))
            .add(new SimpleCommand("T-4"))
            .add(new SimpleCommand("T-5"));

        var command = new ConditionalCommand(() -> true, command1, command2, "Main");

        Assert.assertEquals(3, command1.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command2.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(10, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command2.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(2, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var conditionalCommand = new ConditionalCommand(() -> true,
                command, SimpleCommand.NullCommand, "");

        command.setInput("input");
        conditionalCommand.run();
    }
    
    //----------------------------------------------------------------------------------------------------------------------

    private ConditionalCommand createConditionalCommand(boolean flag) {
        var trueCommand = new SimpleCommand(() -> { }, "True");
        var falseCommand = new SimpleCommand(() -> { }, "False");
        return new ConditionalCommand(() -> flag, trueCommand, falseCommand, "Conditional");
    }

    private ConditionalCommand createConditionalBranchStopAbortCommand(boolean branch, boolean pause) {
        Command trueCommand, falseCommand;

        SequentialCommand seqCommand;
        if (branch) {
            seqCommand = new SequentialCommand("True");
            trueCommand = seqCommand;
            falseCommand = new SimpleCommand(() -> { }, "False");
        }
        else {
            seqCommand = new SequentialCommand("False");
            trueCommand = new SimpleCommand(() -> { }, "True");
            falseCommand = seqCommand;
        }

        var conditionalCommand = new ConditionalCommand(() -> branch, trueCommand, falseCommand, "Conditional");

        seqCommand.add(new SimpleCommand(() -> { }, "S1"));

        if (pause)
            seqCommand.add(new SimpleCommand(() -> conditionalCommand.pause(), "S2-Pause"));
        else
            seqCommand.add(new SimpleCommand(() -> conditionalCommand.abort(), "S2-abort"));
        seqCommand.add(new SimpleCommand(() -> { }, "S3"));

        return conditionalCommand;
    }
}
