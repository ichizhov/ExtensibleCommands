package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class WhileCommandTest {
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
        var command = new WhileCommand(() -> true, SimpleCommand.NullCommand);
        Assert.assertNull(command.getInitCommand());
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "While");

        command = new WhileCommand(() -> true, SimpleCommand.NullCommand, SimpleCommand.NullCommand);
        Assert.assertEquals(command.getInitCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "While");

        command = new WhileCommand(() -> true, SimpleCommand.NullCommand,
                SimpleCommand.NullCommand, "MyWhileCommand");
        Assert.assertEquals(command.getInitCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getCoreCommand(), SimpleCommand.NullCommand);
        Assert.assertEquals(command.getName(), "MyWhileCommand");

        // Malformed cases
        boolean exceptionCaught = false;
        try {
            new WhileCommand(null, null, null, "");
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);

        exceptionCaught = false;
        try {
            new WhileCommand(() -> true, null, null, "");
        }
        catch (Exception e) {
            if (e.getMessage().contains("is NULL"))
                exceptionCaught = true;
        }
        Assert.assertTrue(exceptionCaught);
    }

    @Test
    public void runOkTest() throws Exception {
        // With Init command
        var command = createWhileCommand(true);
        command.run();

        Assert.assertEquals(State.Completed, command.getInitCommand().getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(5, command.getCurrentCycle());

        // Without Init command
        command = createWhileCommand(false);
        counter = 0;    // not set by Init command
        command.run();

        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(5, command.getCurrentCycle());
    }

    @Test
    public void runErrorTest() throws Exception {
        // Error in Core command
        var whileCommand = createWhileCommandError(true);
        whileCommand.run();

        Assert.assertEquals(State.Completed, whileCommand.getInitCommand().getState());
        Assert.assertEquals(State.Failed, whileCommand.getCoreCommand().getState());
        Assert.assertEquals(State.Failed, whileCommand.getState());
        Assert.assertEquals(1, whileCommand.getCurrentCycle());

        // Error in Init command
        whileCommand = createWhileCommandError(false);
        counter = 0;
        whileCommand.run();

        Assert.assertEquals(State.Failed, whileCommand.getInitCommand().getState());
        Assert.assertEquals(State.Idle, whileCommand.getCoreCommand().getState());
        Assert.assertEquals(State.Failed, whileCommand.getState());
        Assert.assertEquals(0, whileCommand.getCurrentCycle());

        Assert.assertEquals(Setup.TestErrorCode, whileCommand.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, whileCommand.getException().getText());
    }

    @Test
    public void abortCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(false);

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getInitCommand().getState());
        Assert.assertEquals(State.Aborted, command.getCoreCommand().getState());
        Assert.assertEquals(2, command.getCurrentCycle());
    }

    @Test
    public void pauseResumeCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Completed, command.getInitCommand().getState());
            Assert.assertEquals(State.Executing, command.getCoreCommand().getState());
            Assert.assertEquals(2, command.getCurrentCycle());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getInitCommand().getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertEquals(5, command.getCurrentCycle());
    }

    @Test
    public void pauseAbortCoreTest() throws Exception {
        var command = createCorePauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Completed, command.getInitCommand().getState());
            Assert.assertEquals(State.Executing, command.getCoreCommand().getState());
            Assert.assertEquals(2, command.getCurrentCycle());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Completed, command.getInitCommand().getState());
        Assert.assertEquals(State.Aborted, command.getCoreCommand().getState());
        Assert.assertEquals(2, command.getCurrentCycle());
    }

    @Test
    public void abortInitTest() throws Exception {
        var command = createInitPauseAbortCommand(false);

        Setup.runAndWaitForAbort(command);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Aborted, command.getInitCommand().getState());
        Assert.assertEquals(State.Idle, command.getCoreCommand().getState());
        Assert.assertEquals(0, command.getCurrentCycle());
    }

    @Test
    public void pauseResumeInitTest() throws Exception {
        var command = createInitPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Executing, command.getInitCommand().getState());
            Assert.assertEquals(State.Idle, command.getCoreCommand().getState());
            Assert.assertEquals(0, command.getCurrentCycle());
        };

        Setup.pauseAndResume(command, assertAfterPause);

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(State.Completed, command.getInitCommand().getState());
        Assert.assertEquals(State.Completed, command.getCoreCommand().getState());
        Assert.assertEquals(5, command.getCurrentCycle());
    }

    @Test
    public void pauseAbortInitTest() throws Exception {
        var command = createInitPauseAbortCommand(true);

        ExecutionDelegate assertAfterPause = () -> {
            Assert.assertEquals(State.Executing, command.getState());
            Assert.assertEquals(State.Executing, command.getInitCommand().getState());
            Assert.assertEquals(State.Idle, command.getCoreCommand().getState());
            Assert.assertEquals(0, command.getCurrentCycle());
        };

        Setup.pauseAndAbort(command, assertAfterPause);

        Assert.assertEquals(State.Aborted, command.getState());
        Assert.assertEquals(State.Aborted, command.getInitCommand().getState());
        Assert.assertEquals(State.Idle, command.getCoreCommand().getState());
        Assert.assertEquals(0, command.getCurrentCycle());
    }

    @Test
    public void externalAbortTest() throws Exception {
        var coreCommand = new SimpleCommand(() -> {
            counter++;
            sleep((int)(0.5*Setup.ThreadLatencyDelayMsec));
        });

        var initCommand = new SimpleCommand(() -> counter = 0);
        var whileCommand = new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");

        new Thread(() -> {
            try {
                whileCommand.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();

        sleep(Setup.ThreadLatencyDelayMsec);
        whileCommand.abort();
        sleep(Setup.ThreadLatencyDelayMsec);

        Assert.assertEquals(State.Aborted, whileCommand.getState());
        Assert.assertEquals(State.Completed, whileCommand.getInitCommand().getState());
        Assert.assertEquals(State.Completed, whileCommand.getCoreCommand().getState());
    }

    @Test
    public void retrieveSubCommandsTest() {
        var initCommand = new SequentialCommand("Branch 1")
            .add(new SimpleCommand("B1-1"))
            .add(new SimpleCommand("B1-2"))
            .add(new SimpleCommand("B1-3"));

        var coreCommand = new SequentialCommand("Branch 2")
            .add(new SimpleCommand("B2-1"))
            .add(new SimpleCommand("B2-2"))
            .add(new SimpleCommand("B2-3"))
            .add(new SimpleCommand("B2-4"))
            .add(new SimpleCommand("B2-5"));

        var command = new WhileCommand(() -> true, initCommand, coreCommand, "While");

        Assert.assertEquals(3, initCommand.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, coreCommand.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(10, command.getDescendants().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(3, initCommand.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(5, coreCommand.getChildren().spliterator().getExactSizeIfKnown());
        Assert.assertEquals(2, command.getChildren().spliterator().getExactSizeIfKnown());
    }

    @Test
    public void runInputOutputTest() throws Exception {
        var coreCommand = new SimpleCommand(() -> counter++);
        var initCommand = new SimpleCommandIO<String, Integer>(input -> {
                counter = 0;
                return input.length();
            }, "Test");

        var whileCommand = new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");
        initCommand.setInput("input");
        whileCommand.run();
    }

    //----------------------------------------------------------------------------------------------------------------------

    private int counter;
    
    private WhileCommand createWhileCommand(boolean init) {
        var coreCommand = new SimpleCommand(() -> counter++);

        if (init) {
            var initCommand = new SimpleCommand(() -> counter = 0);
            return new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");
        }
        else {
            return new WhileCommand(() -> counter < 5, null, coreCommand, "While");
        }
    }

    private WhileCommand createWhileCommandError(boolean inCore) {
        AbstractCommand initCommand, coreCommand;
        if (inCore) {
            initCommand = new SimpleCommand(() -> counter = 0);
            coreCommand = new SimpleCommand(() -> {
                counter++;
                throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
            });
        }
        else {
            initCommand = new SimpleCommand(() -> {
                counter = 0;
                throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription);
            });
            coreCommand = new SimpleCommand(() -> counter++);
        }
        return new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");
    }

    private WhileCommand createCorePauseAbortCommand(boolean pause) {
        var initCommand = new SimpleCommand(() -> counter = 0);
        var coreCommand = new SequentialCommand("S")
            .add(new SimpleCommand(() -> counter++ ));
        var whileCommand = new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");

        if (pause)
            coreCommand.add(new SimpleCommand(() -> {
                if (whileCommand.getCurrentCycle() == 2)
                    whileCommand.pause();
                }));
        else
            coreCommand.add(new SimpleCommand(() -> {
                if (whileCommand.getCurrentCycle() == 2)
                    whileCommand.abort();
                }));

        return whileCommand;
    }

    private WhileCommand createInitPauseAbortCommand(boolean pause) {
        var initCommand = new SequentialCommand("S")
            .add(new SimpleCommand(() -> counter = 0));
        var coreCommand = new SimpleCommand(() -> counter++);
        var whileCommand = new WhileCommand(() -> counter < 5, initCommand, coreCommand, "While");

        if (pause)
            initCommand.add(new SimpleCommand(() -> whileCommand.pause()));
        else
            initCommand.add(new SimpleCommand(() -> whileCommand.abort()));
        initCommand.add(new SimpleCommand(() -> sleep(Setup.ThreadLatencyDelayMsec)));

        return whileCommand;
    }
}
