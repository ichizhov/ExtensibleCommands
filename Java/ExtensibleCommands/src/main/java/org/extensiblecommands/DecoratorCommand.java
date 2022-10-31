package org.extensiblecommands;

import java.util.ArrayList;

import static java.util.Objects.requireNonNull;

/**
 * Base class for decorator commands (i.e. adding functionality to other commands)
 */
public abstract class DecoratorCommand extends AbstractCommand implements Command {
    /**
     * Command to be decorated by the additional functionality
     */
    protected Command coreCommand;

    /**
     * Constructor
     * @param coreCommand       Command to be decorated by the additional functionality
     * @param name              Command name
     */
    protected DecoratorCommand(Command coreCommand, String name) {
        requireNonNull(coreCommand, String.format("Core Command is NULL in (Decorator) Command %s", name));

        this.name = name;
        this.coreCommand = coreCommand;
    }

    /**
     * @return      Command to be decorated by the additional functionality
     */
    public final Command getCoreCommand() {
        return coreCommand;
    }

    /**
     * @return      List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        var children = new ArrayList<Command>();
        children.add(coreCommand);
        return children;
    }

    /**
     * @return      List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<Command>();
        descendants.add(coreCommand);
        coreCommand.getDescendants().forEach(c -> descendants.add(c));
        return descendants;
    }

    /**
     * Set the main command state based on the child commands states
     */
    @Override
    protected void checkErrors() {
        checkErrors(coreCommand);
    }

    /**
     * Set the main command state based on the child commands states
     * @param command       Child command
     */
    /// <summary> Set the main command state based on the child commands states </summary>
    protected final void checkErrors(Command command) {
        // NOTE:
        // It is possible that we have a local abort and a failure roughly at the same time
        // In this case failure supersedes abort, i.e. the command will fail as if no abort was issued.
        if (command.getState() == State.Failed) {
            setState(State.Failed);
            exception = command.getException();
            return;
        }

        if (command.getState() == State.Aborted)
            setState(State.Aborted);
    }
}
