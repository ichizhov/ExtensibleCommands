package org.extensiblecommands;

import java.util.*;

import static java.util.Objects.requireNonNull;

/**
 * Implements composite commands, i.e. containing multiple sub-commands
 */
public abstract class CompositeCommand extends AbstractCommand implements Command {
    /**
     * Constructor
     * @param name      Command name
     */
    protected CompositeCommand(String name) {
        this.name = name;
    }

    /**
     * @return      List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        return new ArrayList<>(subCommands);
    }

    /**
     * @return      List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<>(subCommands);
        for (var subCommand : subCommands)
            subCommand.getDescendants().forEach(c -> descendants.add(c));
        return descendants;
    }

    /**
     * Adds a sub-command to the execution list
     * @param subCommand    Sub-command to add
     * @return              This command
     */
    public final CompositeCommand add(Command subCommand) {

        requireNonNull(subCommand, String.format("Attempt to add NULL sub-command to command %s", name));

        // Throw an exception if the command is currently executing
        if (getState() == State.Executing)
            throw new RuntimeException(String.format("Attempt to add sub-command %s to executing command %s", subCommand.getName(), name));

        subCommands.add(subCommand);
        return this;
    }

    /**
     * Returns a sub-command by index
     * @param index     0-based index of sub-command (in the order of addition)
     * @return          Sub-command corresponding to this index
     */
    public final Command getSubCommand(int index) {
        if (index < 0 || index >= subCommands.size())
            throw new RuntimeException(String.format("For command %s sub-command index %s is out of the allowed range [%s - %s]",
                name, index, 0, subCommands.size()));

        return subCommands.get(index);
    }

    /**
     * List of sub-commands
     */
    protected final List<Command> subCommands = new ArrayList<>();

    /**
     * Set the main command state based on the child commands states
     */
    @Override
    protected void checkErrors() {
        if (getState() != State.Aborted)
            setState(State.Completed);

        for (var subCommand : subCommands)  {
            if (subCommand.getState() == State.Aborted) {
                setState(State.Aborted);
            }
        }
        // Only analyze error states if the command has not been aborted
        if (getState() != State.Aborted) {
            for (var subCommand : subCommands)  {
                if (subCommand.getState() == State.Failed) {
                    setState(State.Failed);
                    exception = subCommand.getException();
                }
            }
        }
    }
}
