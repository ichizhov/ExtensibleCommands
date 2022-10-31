package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class ParallelCommandTest {
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
    public void modifyWhileExecutingTest() throws Exception {
        boolean exceptionCaught = false;
        var command = new ParallelCommand("P")
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
        var command = new ParallelCommand("Parallel")
            .add(new SimpleCommand(() -> sleep(200), "P1"))
            .add(new SimpleCommand(() -> sleep(300), "P2"))
            .add(new SimpleCommand(() -> sleep(400), "P3"))
            .add(new SimpleCommand(() -> sleep(500), "P4"))
            .add(new SimpleCommand(() -> sleep(600), "P5"))
            .add(new SimpleCommand(() -> sleep(600), "P6"))
            .add(new SimpleCommand(() -> sleep(600), "P7"))
            .add(new SimpleCommand(() -> sleep(600), "P8"))
            .add(new SimpleCommand(() -> sleep(600), "P9"));

        Setup.runAndWaitForNormalCompletion(command);

        Assert.assertTrue(command.getElapsedTimeMsec() < 1500);
    }

    @Test
    public void runErrorTest() throws Exception {
        var command = new ParallelCommand("Parallel")
            .add(new SimpleCommand(() ->sleep(200), "P1"))
            .add(new SimpleCommand(() ->sleep(300), "P2"))
            .add(new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); }, "P3-Error"))
            .add(new SimpleCommand(() -> sleep(500), "P4"))
            .add(new SimpleCommand(() -> sleep(600), "P5"))
            .add(new SimpleCommand(() -> sleep(600), "P6"))
            .add(new SimpleCommand(() -> sleep(600), "P7"));

        Setup.runAndWaitForFailure(command);

        Assert.assertTrue(command.getElapsedTimeMsec() < 1500);
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Failed,    command.getSubCommand(2).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(3).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(4).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(5).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(6).getState());

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());
    }

    @Test
    public void runFatalErrorTest() throws Exception {
        var command = new ParallelCommand("Parallel")
                .add(new SimpleCommand(() ->sleep(200), "P1"))
                .add(new SimpleCommand(() ->sleep(300), "P2"))
                .add(new SimpleCommand(() -> { throw new Exception(Setup.TestErrorDescription); }, "P3-Error"))
                .add(new SimpleCommand(() -> sleep(500), "P4"))
                .add(new SimpleCommand(() -> sleep(600), "P5"))
                .add(new SimpleCommand(() -> sleep(600), "P6"))
                .add(new SimpleCommand(() -> sleep(600), "P7"));

        try {
            command.run();
        }
        catch (Exception ex) {
            Assert.assertTrue(ex.getMessage().startsWith("Fatal error"));
            Assert.assertEquals(ex.getCause().getMessage(), Setup.TestErrorDescription);
        }

        Setup.runAndWaitForFailure(command);

        Assert.assertTrue(command.getElapsedTimeMsec() < 1500);
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Failed,    command.getSubCommand(2).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(3).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(4).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(5).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(6).getState());
    }

    @Test
    public void abortTest() throws Exception {
        var command = new ParallelCommand("P");
        command.add(new SimpleCommand(() -> sleep(100), "P1"));
        command.add(new SimpleCommand(() -> sleep(100), "P2"));
        command.add(new SimpleCommand(() -> command.abort(), "P3-abort"));
        command.add(new SimpleCommand(() -> sleep(500), "P4"));
        command.add(new SimpleCommand(() -> sleep(400), "P5"));

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(2).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(3).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(4).getState());
    }

    @Test
    public void abortTestWithAbortableSubCommands() throws Exception {

        var command = new ParallelCommand("P");
        command.add(new AbortableSleepCommand(2600, "P1"));
        command.add(new AbortableSleepCommand(2700, "P2"));
        command.add(new SimpleCommand(() ->{ sleep(3*Setup.ThreadLatencyDelayMsec); command.abort();}, "P3-abort"));
        command.add(new AbortableSleepCommand(3700, "P4"));

        Setup.runAndWaitForAbort(command);
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Aborted, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Aborted, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(2).getState());
        Assert.assertEquals(State.Aborted, command.getSubCommand(3).getState());
    }

    @Test
    public void pauseResumeTest() throws Exception {
        var command = createPauseAbortParallelCommand();

        ExecutionDelegate assertAfterPause = () -> {
                Assert.assertEquals(State.Executing, command.getState());
                Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
                Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
                Assert.assertEquals(State.Executing, command.getSubCommand(2).getState());
                Assert.assertEquals(State.Executing, command.getSubCommand(3).getState());
                Assert.assertEquals(State.Executing, command.getSubCommand(4).getState());
                Assert.assertEquals(State.Executing, command.getSubCommand(5).getState());
            };

        Setup.pauseAndResume(command, assertAfterPause);

        //Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(2).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(3).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(4).getState());
        Assert.assertEquals(State.Completed, command.getSubCommand(5).getState());
    }

    @Test
    public void pauseAbortTest() throws Exception {
        var command = createPauseAbortParallelCommand();

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(0).getState());
            Assert.assertEquals(State.Completed, command.getSubCommand(1).getState());
            Assert.assertEquals(State.Executing, command.getSubCommand(2).getState());
            Assert.assertEquals(State.Executing, command.getSubCommand(3).getState());
            Assert.assertEquals(State.Executing, command.getSubCommand(4).getState());
            Assert.assertEquals(State.Executing, command.getSubCommand(5).getState());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Aborted, command.getSubCommand(2).getState());
    }

    @Test
    public void externalAbortTest() throws Exception {
        SimpleCommand p1, p2, p3, p4, p5, p6, p7;
        var command = new ParallelCommand("P")
            .add(p1 = new SimpleCommand(() ->sleep(100), "P1"))
            .add(p2 = new SimpleCommand(() ->sleep(100), "P2"))
            .add(p3 = new SimpleCommand(() ->sleep(500), "P3"))
            .add(p4 = new SimpleCommand(() ->sleep(400), "P4"))
            .add(p5 = new SimpleCommand(() ->sleep(600), "P5"))
            .add(p6 = new SimpleCommand(() ->sleep(200), "P6"))
            .add(p7 = new SimpleCommand(() ->sleep(600), "P7"));

        Setup.runAndAbort(command);

        // Sub-command are not aborted, they all should be completed by the time abort has taken effect
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, p1.getState());
        Assert.assertEquals(State.Completed, p2.getState());
        Assert.assertEquals(State.Completed, p3.getState());
        Assert.assertEquals(State.Completed, p4.getState());
        Assert.assertEquals(State.Completed, p5.getState());
        Assert.assertEquals(State.Completed, p6.getState());
        Assert.assertEquals(State.Completed, p7.getState());

        Setup.runAndAbort(command);

        // Sub-command are not aborted, they all should be completed by the time abort has taken effect
        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, p1.getState());
        Assert.assertEquals(State.Completed, p2.getState());
        Assert.assertEquals(State.Completed, p3.getState());
        Assert.assertEquals(State.Completed, p4.getState());
        Assert.assertEquals(State.Completed, p5.getState());
        Assert.assertEquals(State.Completed, p6.getState());
        Assert.assertEquals(State.Completed, p7.getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var command = new ParallelCommand("Main")
            .add(new SimpleCommand("A1"))
            .add(new SimpleCommand("A2"));

        var command1 = new ParallelCommand("Branch 1")
            .add(new SimpleCommand("B1-1"))
            .add(new SimpleCommand("B1-2"))
            .add(new SimpleCommand("B1-3"));

        var command2 = new ParallelCommand("Branch 2")
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
        Assert.assertEquals(3, command1.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, command2.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(4, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        var parallelCommand = new ParallelCommand();
        parallelCommand.add(command);

        command.setInput("input");
        parallelCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private ParallelCommand createPauseAbortParallelCommand() {
        var command = new ParallelCommand("Parallel test command");
        var command1 = new SequentialCommand("P3")
            .add(new SimpleCommand(() -> sleep(Setup.ThreadLatencyDelayMsec), "P3-S1"))
            .add(new SimpleCommand(() -> sleep(2*Setup.ThreadLatencyDelayMsec), "P3-S2"))
            .add(new SimpleCommand(() -> sleep(4*Setup.ThreadLatencyDelayMsec), "P3-S3"));

        var command2 = new SequentialCommand("P6")
            .add(new SimpleCommand(() -> sleep((int)(3*Setup.ThreadLatencyDelayMsec)), "P6-S1"));
        command2.add(new SimpleCommand(() -> command.pause(), "P6-S2"));
        command2.add(new SimpleCommand(() -> sleep(Setup.ThreadLatencyDelayMsec), "P6-S3"));

        command.add(new SimpleCommand(() -> sleep((int)(0.1 * Setup.ThreadLatencyDelayMsec)), "P1"))
            .add(new SimpleCommand(() -> sleep((int)(0.1 * Setup.ThreadLatencyDelayMsec)), "P2"))
            .add(command1)
            .add(new SimpleCommand(() -> sleep(7 * Setup.ThreadLatencyDelayMsec), "P5"))
            .add(new SimpleCommand(() -> sleep(9*Setup.ThreadLatencyDelayMsec), "P4"))
            .add(command2);
        return command;
    }
}
