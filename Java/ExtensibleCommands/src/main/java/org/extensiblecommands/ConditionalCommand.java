package org.extensiblecommands;

import java.util.ArrayList;

import static java.util.Objects.requireNonNull;

/**
 * Implements command branching based on a conditional boolean flag
 */
public class ConditionalCommand extends AbstractCommand implements Command {
    /**
     * Command to run if the conditional flag is true
     */
    private final Command trueCommand;

    /**
     * Command to run if the conditional flag is false
     */
    private final Command falseCommand;

    /**
     * Predicate to evaluate to decide which sub-command to run
     */
    private final PredicateDelegate predicate;

    /**
     * Constructor
     * @param predicate         Predicate to evaluate to decide which sub-command to run
     * @param trueCommand       Command to run if the conditional flag is true
     * @param falseCommand      Command to run if the conditional flag is false
     */
    public ConditionalCommand(PredicateDelegate predicate, Command trueCommand, Command falseCommand) {
        this(predicate, trueCommand, falseCommand, "Conditional");
    }

    /**
     * Constructor
     * @param predicate         Predicate to evaluate to decide which sub-command to run
     * @param trueCommand       Command to run if the conditional flag is true
     * @param falseCommand      Command to run if the conditional flag is false
     * @param name              Command name
     */
    public ConditionalCommand(PredicateDelegate predicate, Command trueCommand, Command falseCommand, String name) {
        requireNonNull(predicate, String.format("Predicate is NULL in ConditionalCommand %s", name));
        requireNonNull(trueCommand, String.format("True Command is NULL in ConditionalCommand %s", name));
        requireNonNull(falseCommand, String.format("False Command is NULL in ConditionalCommand %s", name));

        this.name = name;
        this.predicate = predicate;
        this.trueCommand = trueCommand;
        this.falseCommand = falseCommand;
    }

    /**
     * @return      Command to run if the conditional flag is true
     */
    public final Command getTrueCommand() {
        return trueCommand;
    }

    /**
     * @return      Command to run if the conditional flag is false
     */
    public final Command getFalseCommand() {
        return falseCommand;
    }

    /**
     * @return      List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        var children = new ArrayList<Command>();
        children.add(trueCommand);
        children.add(falseCommand);
        return children;
    }

    /**
     * @return      List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<Command>();
        descendants.add(trueCommand);
        trueCommand.getDescendants().forEach(c -> descendants.add(c));
        descendants.add(falseCommand);
        falseCommand.getDescendants().forEach(c -> descendants.add(c));
        return descendants;
    }

    @Override
    protected void execute() throws Exception {
        if (predicate.evaluateCondition())
            trueCommand.run();
        else
            falseCommand.run();
    }

    /**
     * Set the main command state based on the child commands states
     */
    @Override
    protected void checkErrors() {
        if (trueCommand.getState() == State.Aborted ||
                falseCommand.getState() == State.Aborted) {
            setState(State.Aborted);
        }
        else if (trueCommand.getState() == State.Failed) {
            setState(State.Failed);
            exception = trueCommand.getException();
        }
        else if (falseCommand.getState() == State.Failed) {
            setState(State.Failed);
            exception = falseCommand.getException();
        }
        else if (trueCommand.getState() == State.Completed ||
                falseCommand.getState() == State.Completed) {
            setState(State.Completed);
        }
    }
}
