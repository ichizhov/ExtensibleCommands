package org.extensiblecommands;

import static java.util.Objects.requireNonNull;

/**
 * Implements command that can be aborted during its execution by invoking Abort() method.
 * This implies that command execution and its abort should occur in different threads.
 */
public class AbortableCommand extends DecoratorCommand {
    /**
     * Method to abort this command
     */
    private final AbortDelegate abortDelegate;

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param abortDelegate     Delegate to abort operation of the Core command
     */
    public AbortableCommand(Command coreCommand, AbortDelegate abortDelegate) {
        this(coreCommand, abortDelegate, "Abortable");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param abortDelegate     Delegate to abort operation of the Core command
     * @param name              Command name
     */
    public AbortableCommand(Command coreCommand, AbortDelegate abortDelegate, String name) {
        super(coreCommand, name);
        requireNonNull(abortDelegate, String.format("Abort Delegate is NULL in AbortableCommand %s", this.name));

        this.abortDelegate = abortDelegate;
    }

    @Override
    public void abort() {
        aborted = true;
        if (abortDelegate != null)
            abortDelegate.abort();
        super.abort();
    }

    @Override
    protected void execute() throws Exception {
        coreCommand.run();
        processAbortAndPauseEvents();
    }
}
