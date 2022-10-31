package org.extensiblecommands;

import io.reactivex.disposables.Disposable;
import io.reactivex.subjects.PublishSubject;

import java.time.Duration;
import java.time.Instant;
import java.util.*;

/**
 * Base class for all commands. Implements fundamental command functions such as:
 * - execution;
 * - failure handling and notification;
 * - suspension & recovery
 */
public abstract class AbstractCommand implements Command {
    /**
     * Command name (preferably, uniquely identifies the command)
     */
    protected String name;

    /**
     * Exception generated during command execution (if any)
     */
    protected ExtensibleCommandsException exception;

    /**
     * Event signaling the start of command
     */
    protected final ManualResetEvent eventStarted = new ManualResetEvent(true);

    /**
     * Event signaling the completion of command (with any outcome)
     */
    protected final ManualResetEvent eventFinished = new ManualResetEvent(true);

    /**
     * Current state of the command
     */
    private State state = State.Idle;

    /**
     * Observable signaling command state change
     */
    private final PublishSubject<State> currentStateObservable = PublishSubject.create();

    /**
     * Observable signaling when a progress update is ready
     */
    private final PublishSubject<ProgressUpdate> progressUpdateObservable = PublishSubject.create();

    /**
     * Fraction of command completed (between 0 and 1)
     */
    private double fractionCompleted;

    /**
     * Command start time
     */
    private Instant startTime;

    /**
     * Command stop time
     */
    private Instant stopTime;

    /**
     * Local Abort flag (set on every individual command by calling Abort() method
     */
    protected volatile boolean aborted;

    /**
     * Local Pause flag (set on every individual command by calling Pause() method
     */
    private volatile boolean paused;

    /**
     * Event signaling that the command has been resumed
     */
    private final ManualResetEvent eventResuming = new ManualResetEvent(false);

    /**
     * Number of leaf descendant commands
     */
    private int numberOfLeaves = 0;

    /**
     * Number of completed leaf descendant commands (updated during command execution)
     */
    private int numberOfLeavesCompleted = 0;

    /**
     * Lock for leaf descendant command updates
     */
    private final Object leafUpdateLock = new Object();

    /**
     * Subscriptions to progress of leaf descendant commands
     */
    private final List<Disposable> leafSubscriptions = new ArrayList<>();

    /**
     * @return      Command name (preferably, uniquely identifies the command)
     */
    @Override
    public final String getName() {
        return name;
    }

    /**
     * @return      Current state of the command
     */
    @Override
    public final State getState() {
        return state;
    }

    /**
     * @return      Observable signaling command state change
     */
    @Override
    public final PublishSubject<State> getCurrentStateObservable() {
        return currentStateObservable;
    }

    /**
     * @return      Observable signaling when a progress update is ready
     */
    @Override
    public final PublishSubject<ProgressUpdate> getProgressUpdateObservable() {
        return progressUpdateObservable;
    }

    /**
     * @return      Exception generated during command execution (if any)
     */
    @Override
    public final ExtensibleCommandsException getException() {
        return exception;
    }
    
    /**
     * @return      List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        return new ArrayList<>();
    }

    /**
     * @return      List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        return new ArrayList<>();
    }

    /**
     * @return      Current elapsed time of command execution. Can be queried before command completion.
     */
    public final Duration getElapsedTime() {
        if (getState() == State.Idle) {
            return Duration.ZERO;
        }
        else if (getState() == State.Executing) {
            return Duration.between(startTime, Instant.now());
        }
        return Duration.between(startTime, stopTime);
    }

    /**
     * @return      Current elapsed time of command execution (in msec). Can be queried before command completion.
     */
    @Override
    public final long getElapsedTimeMsec() {
        if (getState() == State.Idle) {
            return 0;
        }
        else if (getState() == State.Executing) {
            return Duration.between(startTime, Instant.now()).toMillis();
        }
        return Duration.between(startTime, stopTime).toMillis();
    }

    /**
     * @return      Fraction of command completed (between 0 and 1)
     */
    @Override
    public final double getFractionCompleted() {
        return fractionCompleted;
    }

    /**
     * @return      Fraction of command completed in percents (between 0 and 100)
     */
    @Override
    public final int getPercentCompleted() {
        return (int)(100* fractionCompleted);
    }

    /**
     * Main method to run the command
     */
    @Override
    public synchronized void run() throws Exception {
        // Start timer
        startTime = Instant.now();
        
        paused = false;
        aborted = false;

        numberOfLeaves = getLeaves().size();
        numberOfLeavesCompleted = 0;
        fractionCompleted = 0.0;
        subscribeForLeafProgressUpdates();

        eventResuming.reset();
        eventFinished.reset();

        try {
            setState(State.Executing);
            exception = null;

            eventStarted.set();

            execute();

            // Still need to check for errors even if there was no exception thrown
            checkErrors();
            signalCompletion();
        }
        catch (ExtensibleCommandsException e) {
            setState(State.Failed);
            exception = e;

            checkErrors();
            signalCompletion();
        }
        catch (Exception e) {
            // If any other exception is thrown, terminate command execution.
            // This case is not handled by Extensible Commands!
            setState(State.Failed);
            throw e;
        }
        finally {
            // Even if unhandled exception is thrown, make sure we do this
            unsubscribeFromLeafProgressUpdates();

            // Record elapsed time
            stopTime = Instant.now();

            eventFinished.set();
            eventStarted.reset();
        }
    }

    /**
     * Pause command execution
     */
    @Override
    public void pause() {
        // Iterate through children objects and pause them first.
        // This works recursively, i.e. each child will pause its children.
        for (var command : getChildren())
            command.pause();

        paused = true;

        if (getState() == State.Executing)
            Logger.log(Logger.LogLevel.Info,
                String.format("Command %s is PAUSED", name));
    }

    /**
     * Resume command execution
     */
    @Override
    public void resume() {
        // Iterate through children objects and resume them first.
        // This works recursively, i.e. each child will resume its children.
        for (var command : getChildren())
            command.resume();

        if (paused) {
            paused = false;
            eventResuming.set();
        }

        if (getState() == State.Executing)
            Logger.log(Logger.LogLevel.Info,
                    String.format("Command %s is RESUMED", name));
    }

    /**
     * Abort command execution
     */
    @Override
    public void abort() {
        // Iterate through children objects and abort them first.
        // This works recursively, i.e. each child will abort its children.
        for (var command : getChildren())
            command.abort();

        paused = false;
        aborted = true;

        eventResuming.set();
        if (getState() == State.Executing)
            Logger.log(Logger.LogLevel.Info,
                    String.format("Command %s is ABORTED", name));
    }

    /**
     * Force reset of the Finished event to guarantee that this command
     * can be reliably waited upon in a different thread using WaitUntilFinished().
     */
    public final void resetFinished() {
        eventFinished.reset();
    }

    /**
     * Wait until command is finished (with a timeout)
     * @param timeoutMsec       Wait timeout (msec)
     */
    public final void waitUntilFinished(int timeoutMsec) throws InterruptedException {
        eventFinished.waitOne(timeoutMsec);

        // Ensure that this event is reset, is important for parallel command
        eventFinished.reset();
    }

    /**
     * The body of the command execution
     */
    protected abstract void execute() throws Exception ;

    /**
     * Set the main command state based on the child commands states
     */
    protected abstract void checkErrors();

    /**
     * Set current state of the command
     * @param state         Current state of the command
     */
    protected final void setState(State state) {
        Logger.log(Logger.LogLevel.Info,
                String.format("Command %s : %s -> %s", name, this.state, state));
        this.state = state;
        currentStateObservable.onNext(this.state);
    }

    /**
     * @return      Descendants that do not have their own descendants (i.e. leaves on the tree)
     */
    private List<Command> getLeaves() {
        var leaves = new ArrayList<Command>();
        for (var subCommand : getDescendants()) {
            if (!subCommand.getChildren().iterator().hasNext()) {
                leaves.add(subCommand);
            }
        }
        return leaves;
    }

    /**
     * Make sure the state is set correctly in case of Abort or Pause
     */
    protected final void processAbortAndPauseEvents() throws InterruptedException {
        if (aborted && getState() != State.Failed) {
            setState(State.Aborted);
            return;  // Do not care about Pause if Abort has been issued
        }

        // What if we already failed?
        if (getState() == State.Failed)
            return;

        if (paused) {
            eventResuming.waitOne(0);
        }
        if (aborted && getState() != State.Failed) {
            setState(State.Aborted);
        }
    }

    /**
     * Generate appropriate events on command completion
     */
    protected final void signalCompletion() {
        if (getState() == State.Executing) {
            setState(State.Completed);
        }
    }

    /**
     * Recalculate fraction completed
     */
    private void updateFractionCompleted() {
        synchronized (leafUpdateLock) {
            numberOfLeavesCompleted++;
            String progressMessage;

            if (numberOfLeaves > 0) {
                fractionCompleted = (double) numberOfLeavesCompleted / numberOfLeaves;
                progressMessage = String.format("%s percent complete", getPercentCompleted());
            }
            else {
                fractionCompleted = 1.0;
                progressMessage = "Complete";
            }

            progressUpdateObservable.onNext(new ProgressUpdate(getPercentCompleted(), fractionCompleted, progressMessage));
        }
    }

    /**
     * Subscribe for progress updates from "leaf" descendants
     */
    private void subscribeForLeafProgressUpdates() {
        // We are only interested in updates from "leaf" commands, not complex command aggregating other commands
        for (var leaf : getLeaves())
            // Only subscribe to successfully completed "leaf" sub-commands.
            // Consider failed or aborted sub-commands not completed.
            // Update fraction completed when any of the descendant command completes.
            leafSubscriptions.add(leaf.getCurrentStateObservable().filter(s->s == State.Completed).subscribe(s -> updateFractionCompleted()));
    }

    /**
     * Unsubscribe from "leaf" progress updates
     */
    private void unsubscribeFromLeafProgressUpdates() {
        for (var leafSubscription : leafSubscriptions)
            leafSubscription.dispose();

        leafSubscriptions.clear();
    }
}
