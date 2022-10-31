package org.extensiblecommands;

import java.util.ArrayList;

import static java.util.Objects.requireNonNull;

/**
 * Executes Core command while a condition evaluated by a predicate function holds true.
 * Corresponds to a standard While loop.
 */
public class WhileCommand extends DecoratorCommand {
    /**
     * Command to run before the main While cycle (to be used to set initial conditions (if any))
     */
    private final Command initCommand;

    /**
     * Current cycle number
     */
    private int currentCycle;

    /**
     * Predicate delegate to execute to evaluate loop condition
     */
    private final PredicateDelegate predicate;

    /**
     * Constructor
     * @param predicate     Predicate delegate to execute to evaluate loop condition
     * @param coreCommand   Core command
     */
    public WhileCommand(PredicateDelegate predicate, Command coreCommand) {
        this(predicate, null, coreCommand, "While");
    }

    /**
     * Constructor
     * @param predicate     Predicate delegate to execute to evaluate loop condition
     * @param initCommand   Command to run before the main While cycle (to be used to set initial conditions (if any))
     * @param coreCommand   Core command
     */
    public WhileCommand(PredicateDelegate predicate, Command initCommand, Command coreCommand) {
        this(predicate, initCommand, coreCommand, "While");
    }

    /**
     * Constructor
     * @param predicate     Predicate delegate to execute to evaluate loop condition
     * @param initCommand   Command to run before the main While cycle (to be used to set initial conditions (if any))
     * @param coreCommand   Core command
     * @param name          Command name
     */
    public WhileCommand(PredicateDelegate predicate, Command initCommand, Command coreCommand,
                        String name) {
        super(coreCommand, name);

        requireNonNull(predicate, String.format("Predicate is NULL in WhileCommand %s", name));

        this.predicate = predicate;
        this.initCommand = initCommand;
    }

    /**
     * @return      Command to run before the main While cycle (to be used to set initial conditions (if any))
     */
    public final Command getInitCommand() {
        return initCommand;
    }

    /**
     * @return      Current cycle number
     */
    public final int getCurrentCycle() {
        return currentCycle;
    }

    /**
     * @return      List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        var children = new ArrayList<Command>();
        super.getChildren().forEach(c -> children.add(c));
        if (initCommand != null)
            children.add(initCommand);
        return children;
    }

    /**
     * @return      List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<Command>();
        super.getDescendants().forEach(c -> descendants.add(c));
        if (initCommand != null) {
            descendants.add(initCommand);
            initCommand.getDescendants().forEach(c -> descendants.add(c));
        }
        return descendants;
    }

    @Override
    protected void execute() throws Exception {
        // Run initial command if it is provided
        if (initCommand != null) {
            initCommand.run();
            processAbortAndPauseEvents();

            // If there is an error or abort in one of the sub-commands, terminate the command
            if (initCommand.getState() == State.Failed || initCommand.getState() == State.Aborted) {
                checkErrors(initCommand);
                return;
            }
        }

        // Run core command in a while cycle
        currentCycle = 0;
        while (predicate.evaluateCondition()) {
            currentCycle++;
            coreCommand.run();

            processAbortAndPauseEvents();

            if (getState() == State.Failed || getState() == State.Aborted ||
                    coreCommand.getState() == State.Failed || coreCommand.getState() == State.Aborted)
                break;
        }
    }
}
