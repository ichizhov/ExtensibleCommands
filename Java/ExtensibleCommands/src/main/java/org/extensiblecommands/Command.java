package org.extensiblecommands;

import io.reactivex.subjects.PublishSubject;

import java.time.Duration;

/**
 * Command state enumeration
 */
enum State {
    Idle, Executing, Failed, Aborted, Completed
}

/**
 * Interface representing basic command functionality
 */
public interface Command {
    /**
     * @return      Command name (preferably, uniquely identifies the command)
     */
    String getName();

    /**
     * @return      Current state of the command
     */
    State getState();

    /**
     * @return      Observable signaling command state change
     */
    PublishSubject<State> getCurrentStateObservable();

    /**
     * @return      Observable signaling when a progress update is ready
     */
    PublishSubject<ProgressUpdate> getProgressUpdateObservable();

    /**
     * @return      Exception generated during command execution (if any)
     */
    ExtensibleCommandsException getException();

    /**
     * @return      List of all child command objects (1st level only)
     */
    Iterable<Command> getChildren();

    /**
     * @return      List of all descendant command objects (all levels)
     */
    Iterable<Command> getDescendants();

    /**
     * @return      Current elapsed time of command execution. Can be queried before command completion.
     */
    Duration getElapsedTime();

    /**
     * @return      Current elapsed time of command execution (in msec). Can be queried before command completion.
     */
    long getElapsedTimeMsec();

    /**
     * @return      Fraction of command completed (between 0 and 1)
     */
    double getFractionCompleted();

    /**
     * @return      Fraction of command completed in percent (between 0 and 100)
     */
    int getPercentCompleted();

    /**
     * Main method to run the command
     */
    void run() throws Exception;

    /**
     * Pause command execution
     */
    void pause();

    /**
     * Resume command execution
     */
    void resume();

    /**
     * Abort command execution
     */
    void abort();

    /**
     * Force reset of the Finished event to guarantee that this command
     * can be reliably waited upon in a different thread using waitUntilFinished().
     */
    void resetFinished();

    /**
     * Wait until command is finished (with a timeout)
     * @param timeoutMsec       Wait timeout (msec)
     */
    void waitUntilFinished(int timeoutMsec) throws InterruptedException;
}
