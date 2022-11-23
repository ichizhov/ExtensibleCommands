package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

public class RecoverableCommandTest {
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
        var command = new RecoverableCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getRecoveryCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "Recoverable");

        command = new RecoverableCommand(SimpleCommand.NullCommand, SimpleCommand.NullCommand, "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getRecoveryCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new RecoverableCommand(null, null);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            new RecoverableCommand(SimpleCommand.NullCommand, null);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        var coreCommand = new SimpleCommand(() -> { }, "Core");
        var recoveryCommand = new SimpleCommand(() -> { }, "Recovery");
        var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, coreCommand.getState());
        Assert.assertEquals(State.Idle, recoveryCommand.getState());
    }

    @Test
    public void runCriticalErrorTest() throws Exception {
        // If an ExtensibleCommandsException is thrown, the command fails without exercising the recovery command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var recoveryCommand = new SimpleCommand(() -> { }, "Recovery");
        var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Idle, recoveryCommand.getState());

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void runNonCriticalErrorTest1() throws Exception {
        // If an ExtensibleCommandsAllowRecoveryException is thrown, the command succeeds after exercising the recovery command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var recoveryCommand = new SimpleCommand(() -> { }, "Recovery");
        var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Completed, recoveryCommand.getState());

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void runNonCriticalErrorTest2() throws Exception {
        // If an ExtensibleCommandsAllowRetryException is thrown, the command succeeds after exercising the recovery command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var recoveryCommand = new SimpleCommand(() -> { }, "Recovery");
        var command = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(State.Completed, recoveryCommand.getState());

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void abortCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(false);

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getCoreCommand()).getSubCommand(2).getState());
        Assert.assertEquals(State.Idle, command.getRecoveryCommand().getState());
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
        Assert.assertEquals(State.Idle, command.getRecoveryCommand().getState());
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
        Assert.assertEquals(State.Idle, command.getRecoveryCommand().getState());
    }

    @Test
    public void abortRecoveryTest() throws Exception {
        var command = createRecoveryPauseAbortCommand(false);

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Aborted, command.getRecoveryCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeRecoveryTest() throws Exception {
        var command = createRecoveryPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
            Assert.assertEquals(State.Executing, command.getRecoveryCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Completed, command.getRecoveryCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortRecoveryTest() throws Exception {
        var command = createRecoveryPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
            Assert.assertEquals(State.Executing, command.getRecoveryCommand().getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Aborted, command.getRecoveryCommand().getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)command.getRecoveryCommand()).getSubCommand(2).getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command1 = new SequentialCommand("Core")
            .add(new SimpleCommand("1"))
            .add(new SimpleCommand("2"))
            .add(new SimpleCommand("3"));

        var command2 = new SequentialCommand("Recovery")
            .add(new SimpleCommand("R-1"))
            .add(new SimpleCommand("R-2"))
            .add(new SimpleCommand("R-3"))
            .add(new SimpleCommand("R-4"))
            .add(new SimpleCommand("R-5"));

        var command = new RecoverableCommand(command1, command2, "Main");

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
        var recoveryCommand = SimpleCommand.NullCommand;
        var recoverableCommand = new RecoverableCommand(command, recoveryCommand);

        command.setInput("input");
        recoverableCommand.run();
    }
    
    //----------------------------------------------------------------------------------------------------------------------

    private RecoverableCommand createCorePauseAbortCommand(boolean stop) {
        var recoveryCommand = new SimpleCommand(() -> { }, "Recovery");
        var coreCommand = new SequentialCommand("Core")
            .add(new SimpleCommand(() -> { }, "S1"));

        var recoverableCommand = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");

        if (stop)
            coreCommand.add(new SimpleCommand(() -> recoverableCommand.pause(), "S2-Stop"));
        else
            coreCommand.add(new SimpleCommand(() -> recoverableCommand.abort(), "S2-abort"));

        coreCommand.add(new SimpleCommand(() -> { }, "S3"));

        return (recoverableCommand);
    }

    private RecoverableCommand createRecoveryPauseAbortCommand(boolean stop) {
        var recoveryCommand = new SequentialCommand("Recovery")
            .add(new SimpleCommand(() -> { }, "S1"));
        // An ExtensibleCommandsAllowRecoveryException is thrown inside the Core command
        var coreCommand = new SimpleCommand(() -> { throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var recoverableCommand = new RecoverableCommand(coreCommand, recoveryCommand, "Recoverable");


        if (stop)
            recoveryCommand.add(new SimpleCommand(() -> recoverableCommand.pause(), "S2-Stop"));
        else
            recoveryCommand.add(new SimpleCommand(() -> recoverableCommand.abort(), "S2-abort"));

        recoveryCommand.add(new SimpleCommand(() -> { }, "S3"));
        return (recoverableCommand);
    }
}
