package org.extensiblecommands;

import org.junit.*;
import org.junit.rules.TestName;

import static java.lang.Thread.sleep;

public class SimpleCommandTest {
    @Rule
    public TestName name = new TestName();

    @BeforeClass
    public static void setUpClass() {
        Setup.InitLog();
    }

    @Before
    public void setUpTest() {
        commandStartedCount = 0;
        commandCompletedCount = 0;
        commandFailedCount = 0;

        Logger.log(Logger.LogLevel.Info,
                "----------------------------------------------------------------------------------------------------------");
        Logger.log(Logger.LogLevel.Info,
                String.format("Starting Test %s:%s", this.getClass().getName(), name.getMethodName()));
    }

    @Test
    public void constructionTest() {
        var command = new SimpleCommand(() -> { });
        Assert.assertEquals(command.getName(), "Simple");

        command = new SimpleCommand("MyCommand");
        Assert.assertEquals(command.getName(), "MyCommand");

        command = new SimpleCommand(() -> { }, "MyCommand");
        Assert.assertEquals(command.getName(), "MyCommand");

        var command1 = new SimpleCommandI<Integer>(a -> { });
        Assert.assertEquals(command1.getName(), "Simple(Input)");

        command1 = new SimpleCommandI<Integer>(a -> { }, "MyCommand");
        Assert.assertEquals(command1.getName(), "MyCommand");

        var command2 = new SimpleCommandIO<Integer, Integer>(a -> 2);
        Assert.assertEquals(command2.getName(), "Simple(Input, Output)");

        command2 = new SimpleCommandIO<Integer, Integer>(a -> 2, "MyCommand");
        Assert.assertEquals(command2.getName(), "MyCommand");
    }

    @Test
    public void runNullCommandTest() throws Exception {
        var command = SimpleCommand.NullCommand;

        var started     = command.getCurrentStateObservable().filter(s -> s == State.Executing).subscribe(s-> commandStartedCount++);
        var completed   = command.getCurrentStateObservable().filter(s -> s == State.Completed).subscribe(s-> commandCompletedCount++);
        var failed      = command.getCurrentStateObservable().filter(s -> s == State.Failed).subscribe(s-> commandFailedCount++);

        command.run();

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(1, commandStartedCount);
        Assert.assertEquals(1, commandCompletedCount);
        Assert.assertEquals(0, commandFailedCount);

        started.dispose();
        completed.dispose();
        failed.dispose();
    }

    @Test
    public void runOkTest() throws Exception {
        SimpleCommand command = new SimpleCommand(() -> sleep(10));

        var started     = command.getCurrentStateObservable().filter(s -> s == State.Executing).subscribe(s-> commandStartedCount++);
        var completed   = command.getCurrentStateObservable().filter(s -> s == State.Completed).subscribe(s-> commandCompletedCount++);
        var failed      = command.getCurrentStateObservable().filter(s -> s == State.Failed).subscribe(s-> commandFailedCount++);

        command.run();

        Assert.assertEquals(State.Completed, command.getState());
        Assert.assertEquals(1, commandStartedCount);
        Assert.assertEquals(1, commandCompletedCount);
        Assert.assertEquals(0, commandFailedCount);

        Assert.assertTrue(command.getElapsedTimeMsec() > 9);
        Assert.assertTrue(command.getElapsedTimeMsec() < 50);    // Allow some buffer

        Assert.assertTrue(command.getElapsedTime().toMillis() > 9);
        Assert.assertTrue(command.getElapsedTime().toMillis() < 50);    // Allow some buffer

        started.dispose();
        completed.dispose();
        failed.dispose();
    }

    @Test
    public void runErrorTest() throws Exception {
        SimpleCommand command = new SimpleCommand(() -> { throw new ExtensibleCommandsException(Setup.TestErrorCode, Setup.TestErrorDescription); });

        var started     = command.getCurrentStateObservable().filter(s -> s == State.Executing).subscribe(s-> commandStartedCount++);
        var completed   = command.getCurrentStateObservable().filter(s -> s == State.Completed).subscribe(s-> commandCompletedCount++);
        var failed      = command.getCurrentStateObservable().filter(s -> s == State.Failed).subscribe(s-> commandFailedCount++);

        command.run();

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(1, commandStartedCount);
        Assert.assertEquals(0, commandCompletedCount);
        Assert.assertEquals(1, commandFailedCount);

        Assert.assertEquals(Setup.TestErrorCode, command.getException().getId());
        Assert.assertEquals(Setup.TestErrorDescription, command.getException().getText());

        started.dispose();
        completed.dispose();
        failed.dispose();
    }

    @Test
    public void runFatalErrorTest() {
        SimpleCommand command = new SimpleCommand(() -> { throw new Exception(Setup.TestErrorDescription); });

        var started     = command.getCurrentStateObservable().filter(s -> s == State.Executing).subscribe(s-> commandStartedCount++);
        var completed   = command.getCurrentStateObservable().filter(s -> s == State.Completed).subscribe(s-> commandCompletedCount++);
        var failed      = command.getCurrentStateObservable().filter(s -> s == State.Failed).subscribe(s-> commandFailedCount++);

        try {
            command.run();
        }
        catch (Exception e) {
            Assert.assertEquals(Setup.TestErrorDescription, e.getMessage());
        }

        Assert.assertEquals(State.Failed, command.getState());
        Assert.assertEquals(1, commandStartedCount);
        Assert.assertEquals(0, commandCompletedCount);
        Assert.assertEquals(1, commandFailedCount);

        started.dispose();
        completed.dispose();
        failed.dispose();
    }

    @Test
    public void multiThreadedTest() throws Exception {
        var command = new SimpleCommand(() -> a++);

        var t1 = new Thread(() -> {
            try {
                for (int i = 0; i < 100; i++) {
                    command.run();
                }
            }
            catch (Exception e) {
                // Ignore
            }
        });

        var t2 = new Thread(() -> {
            try {
                for (int i = 0; i < 100; i++) {
                    command.run();
                }
            }
            catch (Exception e) {
                // Ignore
            }
        });

        var t3 = new Thread(() -> {
            try {
                for (int i = 0; i < 100; i++) {
                    command.run();
                }
            }
            catch (Exception e) {
                // Ignore
            }
        });

        var t4 = new Thread(() -> {
            try {
                for (int i = 0; i < 100; i++) {
                    command.run();
                }
            }
            catch (Exception e) {
                // Ignore
            }
        });

        var t5 = new Thread(() -> {
            try {
                for (int i = 0; i < 100; i++) {
                    command.run();
                }
            }
            catch (Exception e) {
                // Ignore
            }
        });

        t1.start();
        t2.start();
        t3.start();
        t4.start();
        t5.start();

        t1.join();
        t2.join();
        t3.join();
        t4.join();
        t5.join();

        Assert.assertEquals(500, a);
    }

    @Test
    public void SimpleInputTest() throws Exception {
        var command = new SimpleCommandI<String>(input -> { }, "Test");

        // Run #1 : Directly set input
        command.setInput("333");
        command.run();

        Assert.assertEquals("333", command.getInput());
    }

    @Test
    public void SimpleInputOutputTest() throws Exception {
        var command = new SimpleCommandIO<String, Integer>(input -> input.length(), "Test");

        // Run #1 : Directly set input
        command.setInput("333");
        command.run();

        Assert.assertEquals("333", command.getInput());
        Assert.assertEquals(3, (int)command.getOutput());
    }

    //----------------------------------------------------------------------------------------------------------------------

    private int commandStartedCount;
    private int commandCompletedCount;
    private int commandFailedCount;
    private int a;
}