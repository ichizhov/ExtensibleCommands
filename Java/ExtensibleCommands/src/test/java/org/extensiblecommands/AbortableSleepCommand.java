package org.extensiblecommands;

/**
 * Implements sleep command that can be aborted at any time.
 */
public class AbortableSleepCommand extends SimpleCommand {
    /**
     * Internal event to be used to detect aborts
     */
    private final ManualResetEvent eventAborted = new ManualResetEvent(false);

    /**
     * Sleep time (in msec). When it expires, command terminates.
     */
    private final int sleepTimeMsec;

    /**
     * Constructor
     * @param sleepTimeMsec     Sleep time (msec)
     */
    public AbortableSleepCommand(int sleepTimeMsec) {
        this(sleepTimeMsec, "Abortable Sleep");
    }

    /**
     * Constructor
     * @param sleepTimeMsec     Sleep time (msec)
     * @param name              Command name
     */
    public AbortableSleepCommand(int sleepTimeMsec, String name) {
        this.name = name;
        this.sleepTimeMsec = sleepTimeMsec;
    }

    /**
     * @return       Sleep time (in msec). When it expires, command terminates.
     */
    public int getSleepTimeMsec() {
        return sleepTimeMsec;
    }

    /**
     * Aborts execution
     */
    @Override
    public void abort() {
        aborted = true;
        eventAborted.set();
    }

    /**
     * Sleep for a specified period of time unless interrupted by Abort() earlier.
     */
    @Override
    protected void execute() throws InterruptedException {
        eventAborted.reset();
        eventAborted.waitOne(sleepTimeMsec);
        
        processAbortAndPauseEvents();
    }
}
