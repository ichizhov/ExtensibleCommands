package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class RetryCommandTest {
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

    private int counter;

    @Test
    public void constructionTest() {
        var command = new RetryCommand(SimpleCommand.NullCommand, 3);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getNumberOfRetries(), 3);
        Assert.assertEquals(command.getRetryDelayMsec(), 0);
        Assert.assertEquals(command.getName(), "Retry");

        command = new RetryCommand(SimpleCommand.NullCommand, 5, 100);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getNumberOfRetries(), 5);
        Assert.assertEquals(command.getRetryDelayMsec(), 100);
        Assert.assertEquals(command.getName(), "Retry");

        command = new RetryCommand(SimpleCommand.NullCommand, 7, 200, "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getNumberOfRetries(), 7);
        Assert.assertEquals(command.getRetryDelayMsec(), 200);
        Assert.assertEquals(command.getName(), "MyCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new RetryCommand(null, 5);
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        counter = 0;
        var coreCommand = new SimpleCommand(() -> counter++ , "Core");
        var command = new RetryCommand(coreCommand, 10, 0,"Retry");

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, coreCommand.getState());
        Assert.assertEquals(1, counter);
    }

    @Test
    public void runCriticalErrorTest() throws Exception {
        counter = 0;
        // If normal exception is thrown, the command should fail without excercising recovery command
        var coreCommand = new SimpleCommand(() -> { counter++; throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var command = new RetryCommand(coreCommand, 10, 0, "Retry");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(1, counter);

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void runNonCriticalErrorTest1() throws Exception {
        counter = 0;
        // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should fail immediately, because recovery exception is higher in hierarchy
        var coreCommand = new SimpleCommand(() -> { counter++; throw new ExtensibleCommandsAllowRecoveryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var command = new RetryCommand(coreCommand, 10, 0, "Retry");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(1, counter);

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void runNonCriticalErrorTest2() throws Exception {
        counter = 0;
        // If ExtensibleCommandsAllowRecoveryException exception is thrown, the command should fail immediately, because recovery exception is higher in hierarchy
        var coreCommand = new SimpleCommand(() -> { counter++; throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "Core");
        var command = new RetryCommand(coreCommand, 10, 0, "Retry");

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, coreCommand.getState());
        Assert.assertEquals(10, counter);

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void abortTest() throws Exception {
        var retryCommand = createPauseAbortRetryCommand(false);

        Setup.runAndWaitForAbort(retryCommand);

        Assert.assertEquals(1, retryCommand.getCurrentRetryIndex());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeTest() throws Exception {
        var retryCommand = createPauseAbortRetryCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndResume(retryCommand, assertAfterPause);

        Assert.assertEquals(State.Completed, retryCommand.getState());
        Assert.assertEquals(1, retryCommand.getCurrentRetryIndex());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortTest() throws Exception {
        var retryCommand = createPauseAbortRetryCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(1).getState());
            Assert.assertEquals(State.Idle, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(retryCommand, assertAfterPause);

        Assert.assertEquals(State.Aborted, retryCommand.getState());
        Assert.assertEquals(1, retryCommand.getCurrentRetryIndex());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, ((SequentialCommand)retryCommand.getCoreCommand()).getSubCommand(2).getState());
    }

    @Test
    public void externalAbortTest() throws Exception {
        // This is the case when we do local abort on an command that failed.
        // abort should be superseded by failure, i.e. no abort event will be issued.
        var coreCommand = new SimpleCommand(() -> {
            sleep(Setup.ThreadLatencyDelayMsec);
            throw new ExtensibleCommandsAllowRetryException(Setup.TestErrorCode, Setup.TestErrorDescription);
            }, "Core");
        var command = new RetryCommand(coreCommand, 10, 0, "Retry");

        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
            }
        }).start();
        sleep(Setup.ThreadLatencyDelayMsec);

        command.abort();
        command.waitUntilFinished(Setup.WaitTimeoutMsec);
        sleep(Setup.ThreadLatencyDelayMsec);

        // Retry command must be in Failed state
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Failed, command.getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command1 = new SequentialCommand("Core")
            .add(new SimpleCommand("1"))
            .add(new SimpleCommand("2"))
            .add(new SimpleCommand("3"));

        var command = new RetryCommand(command1, 3);

        Assert.assertEquals(3, command1.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(1, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<>(new ExecutionDelegateIO<String, Integer>() {
            @Override
            public Integer execute(String input) {
                return input.length();
            }
        }, "Test");

        var retryCommand = new RetryCommand(command, 3);

        command.setInput("input");
        retryCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private RetryCommand createPauseAbortRetryCommand(boolean pause) {
        var seqCommand = new SequentialCommand("Core");
        var retryCommand = new RetryCommand(seqCommand, 5, 0, "Retry");
        seqCommand.add(new SimpleCommand(() -> { }, "S1"));

        if (pause)
            seqCommand.add(new SimpleCommand(() -> retryCommand.pause(), "S2-Stop"));
        else
            seqCommand.add(new SimpleCommand(() -> retryCommand.abort(), "S2-abort"));

        seqCommand.add(new SimpleCommand(() -> { }, "S3"));
        return (retryCommand);
    }
}
