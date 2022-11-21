package org.extensiblecommands;

import java.util.ArrayList;

import static java.util.Objects.requireNonNull;

/**
 * Executes Core command, and then executes Finally command,
 * regardless of the result of the Core command.
 * Finally command will always be executed (unless an unhandled exception is thrown in the Core command).
 */
public class TryCatchFinallyCommand extends DecoratorCommand {
    /**
     * Recovery command to execute after the Core command is completed with or without error
     */
    private final Command finallyCommand;

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param finallyCommand    Recovery command to execute after the Core command is completed with or without error
     */
    public TryCatchFinallyCommand(Command coreCommand, Command finallyCommand) {
        this(coreCommand, finallyCommand, "Try-Catch-Finally");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param finallyCommand    Recovery command to execute after the Core command is completed with or without error
     * @param name              Command name
     */
    public TryCatchFinallyCommand(Command coreCommand, Command finallyCommand, String name) {
        super(coreCommand, name);

        requireNonNull(finallyCommand, String.format("Core Command is NULL in TryCatchFinallyCommand %s", name));

        this.finallyCommand = finallyCommand;
    }

    /**
     * @return          Recovery command to execute after the Core command is completed with or without error
     */
    public final Command getFinallyCommand() {
        return finallyCommand;
    }

    /**
     * @return          List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        var children = new ArrayList<Command>();
        super.getChildren().forEach(c -> children.add(c));
        children.add(finallyCommand);
        return children;
    }

    /**
     * @return          List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<Command>();
        super.getDescendants().forEach(c -> descendants.add(c));
        descendants.add(finallyCommand);
        finallyCommand.getDescendants().forEach(c -> descendants.add(c));
        return descendants;
    }

    @Override
    protected void execute() throws Exception {
        // Run core command
        coreCommand.run();

        processAbortAndPauseEvents();

        if (coreCommand.getState() == State.Aborted || getState() == State.Aborted)
            return;

        // Remember Core command exception
        if (coreCommand.getState() == State.Failed)
            exception = coreCommand.getException();

        finallyCommand.run();
    }

    /**
     * Set the main command state based on the child commands states
     */
    @Override
    protected void checkErrors() {
        if (coreCommand.getState() == State.Aborted || finallyCommand.getState() == State.Aborted) {
            setState(State.Aborted);
        }
        if (finallyCommand.getState() == State.Failed) {
            // If Finally command failed, the whole command failed
            setState(State.Failed);
            exception = finallyCommand.getException();
        }
        else if (coreCommand.getState() == State.Failed) {
            exception = coreCommand.getException();
            // If Core command failed, the whole command failed
            setState(State.Failed);
        }
        else if (coreCommand.getState() == State.Completed &&
                finallyCommand.getState() == State.Completed) {
            setState(State.Completed);
        }
    }
}
