package org.extensiblecommands;

import static java.lang.Thread.sleep;

/**
 * Retries core command N times if it fails
 */
public class RetryCommand extends DecoratorCommand {
    /**
     * Number of command retries
     */
    private final int numberOfRetries;
    /**
     * Retry delay (in msec)
     */
    private final int retryDelayMsec;
    /**
     * Current retry index
     */
    private int currentRetryIndex;

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param numberOfRetries   Number of command retries
     */
    public RetryCommand(Command coreCommand, int numberOfRetries) {
        this(coreCommand, numberOfRetries, 0, "Retry");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param numberOfRetries   Number of command retries
     * @param retryDelayMsec    Retry delay (in msec)
     */
    public RetryCommand(Command coreCommand, int numberOfRetries, int retryDelayMsec) {
        this(coreCommand, numberOfRetries, retryDelayMsec, "Retry");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param numberOfRetries   Number of command retries
     * @param retryDelayMsec    Retry delay (in msec)
     * @param name              Command name
     */
    public RetryCommand(Command coreCommand, int numberOfRetries, int retryDelayMsec, String name) {
        super(coreCommand, name);
        this.numberOfRetries = numberOfRetries;
        this.retryDelayMsec = retryDelayMsec;
    }

    /**
     * @return          Number of command retries
     */
    public final int getNumberOfRetries() {
        return numberOfRetries;
    }

    /**
     * @return          Retry delay (in msec)
     */
    public final int getRetryDelayMsec() {
        return retryDelayMsec;
    }

    /**
     * @return          Current retry index
     */
    public final int getCurrentRetryIndex() {
        return currentRetryIndex;
    }

    @Override
    protected void execute() throws Exception {
        currentRetryIndex = 0;
        for (int i = 0; i < numberOfRetries; i++) {
            currentRetryIndex++;
            coreCommand.run();

            processAbortAndPauseEvents();

            if (coreCommand.getState() == State.Completed || coreCommand.getState() == State.Aborted || getState() == State.Aborted)
                break;
            if (coreCommand.getState() == State.Failed && !(coreCommand.getException() instanceof ExtensibleCommandsAllowRetryException))
                break;

            if (coreCommand.getException() instanceof ExtensibleCommandsAllowRetryException)
                Logger.log(Logger.LogLevel.Error,
                        String.format("ERROR (RECOVERED)[%s] - %s", getCoreCommand().getException().getId(),
                                getCoreCommand().getException().getText()));
        }

        // If after all retries there is still an error, we need to report it.
        if (coreCommand.getState() == State.Failed) {
            setState(State.Failed);
            exception = coreCommand.getException();
        }

        sleep(retryDelayMsec);
    }
}
