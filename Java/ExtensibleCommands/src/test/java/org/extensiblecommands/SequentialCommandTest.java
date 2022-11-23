package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class SequentialCommandTest {
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

    private int percentComplete;
    private int numberOfUpdates;

    @Test
    public void modifyWhileExecutingTest() throws Exception {
        boolean exceptionCaught = false;
        var command = new SequentialCommand("Seq")
            .add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec))
            .add(new AbortableSleepCommand(Setup.ThreadLatencyDelayMsec));

        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep((int)(0.5*Setup.ThreadLatencyDelayMsec));

        try {
            command.add(SimpleCommand.NullCommand);
        }
        catch (Exception e) {
            if (e.getMessage().contains("Attempt to add"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        var command = new SequentialCommand("S")
            .add(new AbortableSleepCommand(1, "A1"))
            .add(new AbortableSleepCommand(1, "A2"))
            .add(new AbortableSleepCommand(1, "A3"));

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(2).getState());
    }

    @Test
    public void runErrorTest() throws Exception {
        var command = new SequentialCommand("Seq")
            .add(new AbortableSleepCommand(1, "A1"))
            .add(new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);}))
            .add(new AbortableSleepCommand(1, "A3"));

        Setup.runAndWaitForFailure(command);

        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Failed,    command.getSubCommand(1).getState());
        Assert.assertEquals(State.Idle,      command.getSubCommand(2).getState());

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void abortTest() throws Exception {
        var command = new SequentialCommand("S");
        command.add(new SimpleCommand(() -> { }));
        command.add(new SimpleCommand(() -> command.abort()));
        command.add(new SimpleCommand(() -> { }));

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Aborted,   command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Idle,      command.getSubCommand(2).getState());
    }

    @Test
    public void pauseResumeTest() throws Exception {
        var command = new SequentialCommand();
        command.add(new SimpleCommand(() -> {}, "S1"));
        command.add(new SimpleCommand(() -> command.pause(), "S2-Pause"));
        command.add(new SimpleCommand(() -> {}, "S3"));

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
            Assert.assertEquals(State.Idle,     command.getSubCommand(2).getState());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(2).getState());
    }

    @Test
    public void pauseAbortTest() throws Exception {
        var command = new SequentialCommand();
        command.add(new SimpleCommand(() -> {}, "S1"));
        command.add(new SimpleCommand(() -> command.pause(), "S2-Pause"));
        command.add(new SimpleCommand(() -> {}, "S3"));

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
            Assert.assertEquals(State.Idle,     command.getSubCommand(2).getState());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Idle, command.getSubCommand(2).getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command = new SequentialCommand("Main")
            .add(new SimpleCommand("A1"))
            .add(new SimpleCommand("A2"));

        var command1 = new SequentialCommand("Branch 1")
            .add(new SimpleCommand("B1-1"))
            .add(new SimpleCommand("B1-2"))
            .add(new SimpleCommand("B1-3"));

        var command2 = new SequentialCommand("Branch 2")
            .add(new SimpleCommand("B2-1"))
            .add(new SimpleCommand("B2-2"))
            .add(new SimpleCommand("B2-3"))
            .add(new SimpleCommand("B2-4"))
            .add(new SimpleCommand("B2-5"));

        command.add(command1);
        command.add(command2);

        Assert.assertEquals(3, command1.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command2.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(12, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(12, command.getDescendants().spliterator().getExactSizeIfKnown());    // Make sure multiple calls produce the same result
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command2.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getChildren().spliterator().getExactSizeIfKnown());        // Make sure multiple calls produce the same result
    }

    @Test
    public void progressUpdateTest() throws Exception {
        //--------------------- Normal completion ------------
        var command = new SequentialCommand("Main")
            .add(new SimpleCommand("A1"))
            .add(new SimpleCommand("A2"));

        var command1 = new SequentialCommand("Branch 1")
            .add(new SimpleCommand("B1-1"))
            .add(new SimpleCommand("B1-2"))
            .add(new SimpleCommand("B1-3"));

        var command2 = new SequentialCommand("Branch 2")
            .add(new SimpleCommand("B2-1"))
            .add(new SimpleCommand("B2-2"))
            .add(new SimpleCommand("B2-3"))
            .add(new SimpleCommand("B2-4"))
            .add(new SimpleCommand("B2-5"));

        command.add(command1);
        command.add(command2);

        var d = command.getProgressUpdateObservable().subscribe(p -> onProgressUpdate(p));
        numberOfUpdates = 0;
        command.run();

        Assert.assertEquals(1.0, command.getFractionCompleted(), 0.0001);
        Assert.assertEquals(100, command.getPercentCompleted());
        Assert.assertEquals(100, percentComplete);
        Assert.assertEquals(10, numberOfUpdates);

        // Run again to check if we are unsubscribing correctly
        numberOfUpdates = 0;
        command.run();

        Assert.assertEquals(1.0, command.getFractionCompleted(), 0.0001);
        Assert.assertEquals(100, command.getPercentCompleted());
        Assert.assertEquals(100, percentComplete);
        Assert.assertEquals(10, numberOfUpdates);

        //--------------------- Failure ------------
        // Reformat command to produce failure in the middle
        command2 = new SequentialCommand("Branch 2")
            .add(new SimpleCommand("B2-1"))
            .add(new SimpleCommand("B2-2"))     // This is the last sub-command successfully completed
            .add(new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }))
            .add(new SimpleCommand("B2-4"))
            .add(new SimpleCommand("B2-5"));

        command = new SequentialCommand("Main")
            .add(new SimpleCommand("A1"))
            .add(new SimpleCommand("A2"))
            .add(command1)
            .add(command2);

        d.dispose();
        d = command.getProgressUpdateObservable().subscribe(p -> onProgressUpdate(p));

        numberOfUpdates = 0;
        command.run();

        Assert.assertEquals(0.7, command.getFractionCompleted(), 0.0001);
        Assert.assertEquals(70, command.getPercentCompleted());
        Assert.assertEquals(70, percentComplete);
        Assert.assertEquals(7, numberOfUpdates);

        // Run again to check if we are unsubscribing correctly
        numberOfUpdates = 0;
        command.run();

        Assert.assertEquals(0.7, command.getFractionCompleted(), 0.0001);
        Assert.assertEquals(70, command.getPercentCompleted());
        Assert.assertEquals(70, percentComplete);
        Assert.assertEquals(7, numberOfUpdates);

        //--------------------- abort ------------
        // Reformat command to abort in the middle
        var commandA = new SequentialCommand("Main")
            .add(new SimpleCommand("A1"))
            .add(new SimpleCommand("A2"))
            .add(command1);

        command2 = new SequentialCommand("Branch 2")
            .add(new SimpleCommand("B2-1"))
            .add(new SimpleCommand("B2-2"))
            .add(new SimpleCommand(() -> commandA.abort()))   // This is the last sub-command successfully completed
            .add(new SimpleCommand("B2-4"))
            .add(new SimpleCommand("B2-5"));

        commandA.add(command2);

        d.dispose();
        d = commandA.getProgressUpdateObservable().subscribe(p -> onProgressUpdate(p));

        numberOfUpdates = 0;
        commandA.run();

        Assert.assertEquals(0.8, commandA.getFractionCompleted(), 0.0001);
        Assert.assertEquals(80, commandA.getPercentCompleted());
        Assert.assertEquals(80, percentComplete);
        Assert.assertEquals(8, numberOfUpdates);

        // Run again to check if we are unsubscribing correctly
        numberOfUpdates = 0;
        commandA.run();

        Assert.assertEquals(0.8, commandA.getFractionCompleted(), 0.0001);
        Assert.assertEquals(80, commandA.getPercentCompleted());
        Assert.assertEquals(80, percentComplete);
        Assert.assertEquals(8, numberOfUpdates);

        d.dispose();
    }

    @Test
    public void externalAbortTest() throws Exception {
        var command = new SequentialCommand("S");
        var command1 = new AbortableSleepCommand( 12 * Setup.ThreadLatencyDelayMsec, "A1");
        var command2 = new AbortableSleepCommand(10 * Setup.ThreadLatencyDelayMsec, "A2");
        command.add(command1)
            .add(command2);

        Setup.runAndAbort(command);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Aborted, command1.getState());
        Assert.assertEquals(State.Idle, command2.getState());
        Assert.assertEquals(State.Aborted, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Idle, command.getSubCommand(1).getState());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var sequentialCommand = new SequentialCommand();
        sequentialCommand.add(command);

        command.setInput("input");
        sequentialCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private void onProgressUpdate(ProgressUpdate progressUpdate) {
        percentComplete = progressUpdate.getPercentCompleted();
        numberOfUpdates++;
    }
}