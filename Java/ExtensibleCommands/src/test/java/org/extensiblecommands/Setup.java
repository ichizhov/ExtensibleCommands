package org.extensiblecommands;

import org.junit.Assert;

import static java.lang.Thread.sleep;

/**
 * Common parameters and behaviors for command unit  tests
 */
public class Setup {
    public static final int TestErrorCode = -1;
    public static final String TestErrorDescription = "Command delegate error";

    /// <summary> Standard timeout to abort waiting for synchronization events in all tests. </summary>
    public static final int WaitTimeoutMsec = 5000;

    /// <summary> Standard delay to account for thread latency (to allow time for all threads to complete and update command states). </summary>
    public static final int ThreadLatencyDelayMsec = 200;

    /**
     * Initialize logger
     */
    public static void InitLog()
    {
        Logger.setLogger(new TestLogger());
        Logger.setIsLoggingEnabled(true);
    }

    /**
     * Execute a command and wait for its completion
     * @param command               Command to run
     * @throws Exception            Exception
     */
    public static void runAndWaitForNormalCompletion(Command command) throws Exception {
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();

        // Wait for the command to be completed
        command.waitUntilFinished(WaitTimeoutMsec);
    }

    /**
     * Execute a command and wait for its failure
     * @param command               Command to run
     * @throws Exception            Exception
     */
    public static void runAndWaitForFailure(Command command) throws Exception {

        var d = command.getCurrentStateObservable().filter(s -> s == State.Failed).subscribe(s-> {
            isFailureEventReceived = true;
        });
        isFailureEventReceived = false;

        command.resetFinished();
        try {
            new Thread(() -> {
                try {
                    command.run();
                }
                catch (Exception e) {
                    // Ignore
                }
            }).start();

            // Wait for the command to be completed
            command.waitUntilFinished(WaitTimeoutMsec);

            Assert.assertEquals(State.Failed, command.getState());

            // Verify that the Top Action Failed event has been received
            Assert.assertTrue(isFailureEventReceived);
        }
        finally {
            d.dispose();
        }
    }

    /**
     * Runs the supplied command asynchronously, pauses it, and then resumes.
     * Relies on the supplied command to pause itself.
     * @param command               Command to run
     * @param assertAfterPause      Assertion to execute after pause
     * @throws Exception            Exception
     */
    public static void pauseAndResume(Command command, ExecutionDelegate assertAfterPause) throws Exception {
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(5 * ThreadLatencyDelayMsec);

        Assert.assertEquals(State.Executing, command.getState());

        if (assertAfterPause != null)
            assertAfterPause.execute();

        sleep(ThreadLatencyDelayMsec);

        command.resume();

        sleep(ThreadLatencyDelayMsec);

        // Wait for the command to be completed
        command.waitUntilFinished(3*WaitTimeoutMsec);
    }

    /**
     * Runs the supplied command asynchronously, pauses it, and then aborts.
     * Relies on the timing of the supplied command to pause itself.
     * @param command               Command to run
     * @param assertAfterPause      Assertion to execute after pause
     * @throws Exception            Exception
     */
    public static void pauseAndAbort(Command command, ExecutionDelegate assertAfterPause) throws Exception {
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(2 * ThreadLatencyDelayMsec);

        Assert.assertEquals(State.Executing, command.getState());

        if (assertAfterPause != null)
            assertAfterPause.execute();

        command.abort();

        // Wait for the command to be completed
        command.waitUntilFinished(WaitTimeoutMsec);
    }

    /**
     * Runs the supplied command asynchronously and then aborts it.
     * The supplied command must have the correct timing consistent with the delays in this method!
     * @param command               Command to run
     * @throws Exception            Exception
     */
    public static void runAndAbort(Command command) throws Exception {
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(2*ThreadLatencyDelayMsec);

        command.abort();

        command.waitUntilFinished(WaitTimeoutMsec);
    }

    /**
     * Runs the supplied command and waits for the command to be aborted.
     * Relies on the supplied command to abort itself.
     * @param command               Command to run
     * @throws Exception            Exception
     */
    public static void runAndWaitForAbort(Command command) throws Exception {
        command.resetFinished();
        new Thread(() -> {
            try {
                command.run();
            }
            catch (Exception e) {
                // Ignore
            }
        }).start();
        sleep(2*ThreadLatencyDelayMsec);

        // Wait for the command to be completed
        command.waitUntilFinished(WaitTimeoutMsec);
    }

    private static boolean isFailureEventReceived = false;
}
