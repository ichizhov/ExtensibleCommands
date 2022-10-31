package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class TryCatchFinallyCommandTest {
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
        var command = new TryCatchFinallyCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getFinallyCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "Try-Catch-Finally");

        command = new TryCatchFinallyCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand, "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getFinallyCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new TryCatchFinallyCommand(null, null);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            new TryCatchFinallyCommand(SimpleCommand.NullCommand, null);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        var coreCommand = new SimpleCommand(() -> {});
        var finallyCommand = new SimpleCommand(() -> { });
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, coreCommand.getState());
        Assert.assertEquals(State.Completed, finallyCommand.getState());
    }

    @Test
    public void runCoreCommandErrorTest1() throws Exception {
        // If normal exception is thrown, the command should fail but Finally command should still execute
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var finallyCommand = new SimpleCommand(() -> { }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Completed, finallyCommand.getState());
    }

    @Test
    public void runCoreCommandErrorTest2() throws Exception {
        // If normal exception is thrown, the command should fail but Finally command should still execute
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var finallyCommand = new SimpleCommand(() -> { }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Completed, finallyCommand.getState());
    }

    @Test
    public void runCoreCommandErrorTest3() throws Exception {
        // If normal exception is thrown, the command should fail but Finally command should still execute
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var finallyCommand = new SimpleCommand(() -> { }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Completed, finallyCommand.getState());
    }

    @Test
    public void runFinallyCommandErrorTest() throws Exception {
        // If normal exception is thrown, the command should fail but Finally command should still execute
        var coreCommand = new SimpleCommand(() -> {}, "Core");
        var finallyCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Completed, coreCommand.getState());
        Assert.assertEquals(State.Failed, finallyCommand.getState());
    }

    @Test
    public void runCoreAndFinallyCommandErrorTest() throws Exception {
        // If normal exception is thrown, the command should fail but Finally command should still execute
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var finallyCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Failed, finallyCommand.getState());
    }

    @Test
    public void abortTest() throws Exception {
        var command = createCorePauseAbortCommand(false);

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        Assert.assertEquals(State.Idle, command.getFinallyCommand().getState());
    }

    @Test
    public void pauseResumeCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        Assert.assertEquals(State.Completed, command.getFinallyCommand().getState());
    }

    @Test
    public void pauseAbortCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        Assert.assertEquals(State.Idle, command.getFinallyCommand().getState());
    }

    @Test
    public void abortFinallyTest() throws Exception {
        var command = createFinallyPauseAbortCommand(false);

        // This is pecial case, the whole command fails because the Core acton failed despite the abort during Finally command
        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Aborted, command.getFinallyCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeFinallyTest() throws Exception {
        var command = createFinallyPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
            Assert.assertEquals(State.Executing, command.getFinallyCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Completed, command.getFinallyCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortFinallyTest() throws Exception {
        var command = createFinallyPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
            Assert.assertEquals(State.Executing, command.getFinallyCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Aborted, command.getFinallyCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getFinallyCommand()).getSubCommand(2).getState());
    }

    @Test
    public void externalAbortTest() throws Exception {
        // If ExtensibleCommandsException exception is thrown in the Core command, the command should still be aborted
        var coreCommand = new SimpleCommand(() -> {
            sleep(2 * Setup.ThreadLatencyDelayMsec);
            throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
            }, "Core");
        var finallyCommand = new SimpleCommand(() -> { }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        Setup.runAndAbort(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var coreCommand = new ParallelCommand("P")
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand);

        var finallyCommand = new SequentialCommand("S")
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand)
            .add(SimpleCommand.NullCommand);

        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand);

        Assert.assertEquals(5, command.getCoreCommand().getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command.getFinallyCommand().getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(12, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command.getCoreCommand().getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command.getFinallyCommand().getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(2, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var tryCatchFinallyCommand = new TryCatchFinallyCommand(command, SimpleCommand.NullCommand);
        command.setInput("input");
        tryCatchFinallyCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private TryCatchFinallyCommand createCorePauseAbortCommand(boolean pause) {
        var coreCommand = new SequentialCommand("Core")
            .add(new SimpleCommand(() -> { }, "S1"));
        var finallyCommand = new SimpleCommand(() -> { }, "Finally");
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        if (pause)
            coreCommand.add(new SimpleCommand(() -> command.pause(), "S2-Stop"));
        else
            coreCommand.add(new SimpleCommand(() -> command.abort(), "S2-abort"));

        coreCommand.add(new SimpleCommand(() -> { }, "S3"));

        return command;
    }

    private TryCatchFinallyCommand createFinallyPauseAbortCommand(boolean pause) {
        // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should succeed after excercising recovery command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var finallyCommand = new SequentialCommand("Recovery")
            .add(new SimpleCommand(() -> { }, "S1"));
        var command = new TryCatchFinallyCommand(coreCommand, finallyCommand, "Try-Catch-Finally");

        if (pause)
            finallyCommand.add(new SimpleCommand(() -> command.pause(), "S2-Stop"));
            else
        finallyCommand.add(new SimpleCommand(() -> command.abort(), "S2-abort"));

        finallyCommand.add(new SimpleCommand(() -> { }, "S3"));
        return command;
    }
}
