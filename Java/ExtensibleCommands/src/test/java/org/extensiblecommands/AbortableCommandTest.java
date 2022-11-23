package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import java.util.concurrent.Semaphore;

import static java.lang.Thread.sleep;

public class AbortableCommandTest {
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
        var command = new AbortableCommand(SimpleCommand.NullCommand, () -> { });
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "Abortable");

        command = new AbortableCommand(SimpleCommand.NullCommand, () -> { }, "MyCommand");
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyCommand");
    }
    
    @Test
    public void runOkTest() throws Exception {
        var command = new AbortableCommand(new SimpleCommand(() -> sleep(100)),
                () -> {},  "Test abortable command");

        command.run();

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertTrue(command.getElapsedTimeMsec() < 500);
        Assert.assertTrue(command.getElapsedTimeMsec() > 80);

        // Run again
        command.run();

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertTrue(command.getElapsedTimeMsec() < 500);
        Assert.assertTrue(command.getElapsedTimeMsec() > 80);
    }

    @Test
    public void runErrorTest() throws Exception {
        var command = new AbortableCommand(new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }),
            () -> { }, "Test abortable command");

        command.run();

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(State.Failed, command.getCoreCommand().getState());
    }

    @Test
    public void pauseAndResumeTest() throws Exception {
        var command = createAbortableCommand();
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(Setup.ThreadLatencyDelayMsec);

        // Pause
        command.pause();

        sleep(Setup.ThreadLatencyDelayMsec);

        // Verify that commands are still executing - Pause has no effect here
        Assert.assertEquals(State.Executing, command.getState());
        Assert.assertEquals(State.Executing, command.getCoreCommand().getState());

        // Resume (has no effect either)
        command.resume();
        sleep(Setup.ThreadLatencyDelayMsec);

        // Verify that commands are still in Executing state
        Assert.assertEquals(State.Executing, command.getState());
        Assert.assertEquals(State.Executing, command.getCoreCommand().getState());

        // Now abort
        command.abort();

        command.waitUntilFinished(Setup.WaitTimeoutMsec);

        // Verify that commands are in Aborted state
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());    // Core command completes normally
    }

    @Test
    public void pauseAndAbortTest() throws Exception {
        var command = createAbortableCommand();
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(Setup.ThreadLatencyDelayMsec);

        // Pause
        command.pause();

        sleep(Setup.ThreadLatencyDelayMsec);

        // Verify that commands are still executing - Pause has no effect here
        Assert.assertEquals(State.Executing, command.getState());
        Assert.assertEquals(State.Executing, command.getCoreCommand().getState());

        // Now abort
        command.abort();

        command.waitUntilFinished(Setup.WaitTimeoutMsec);

        // Verify that commands are in Aborted state
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());    // Core command completes normally
    }

    @Test
    public void externalAbortTest() throws Exception {
        var command = createAbortableCommand();

        Setup.runAndAbort(command);

        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Aborted, command.getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command1 = new SequentialCommand("Core")
            .add(new SimpleCommand("1"))
            .add(new SimpleCommand("2"))
            .add(new SimpleCommand("3"));

        var command = new AbortableCommand(command1, () -> { }, "Main");

        Assert.assertEquals(3, command1.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(1, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var abortableCommand = new AbortableCommand(command, () -> { }, "");

        command.setInput("input");
        abortableCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private AbortableCommand createAbortableCommand() throws Exception {
        final var semaphore = new Semaphore(1);
        semaphore.acquire();

        ExecutionDelegate execute = () -> {
            try {
                // Try to acquire semaphore that is already fully acquired.
                // This results in waiting until semaphore is released.
                semaphore.acquire();
            }
            catch (Exception e) {
                // Ignore
            }
        };
        AbortDelegate abort = () -> {
            // Release the semaphore. This allows the execution of command to complete.
            semaphore.release();
        };

        return new AbortableCommand(new SimpleCommand(execute),
                abort,  "Test abortable command");
    }
}
