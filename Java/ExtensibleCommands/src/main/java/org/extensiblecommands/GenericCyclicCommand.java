package org.extensiblecommands;

import java.util.Collection;

import static java.util.Objects.requireNonNull;

/**
 * Implements generic cyclic command that iterates through the collection and
 * executes the core command for every element.
 * If the core command does not make use of the CurrentElement in the collection,
 * then the result is equivalent to CyclicCommand (i.e. the same command is executed multiple times).
 * However, if the core command makes use of CurrentElement, then the result is going to be different on every cycle.
 * This makes it possible to model the "for" loop.
 * The class is parametrized by the type of the basic element of the collection.
 * @param <T>           Type of collection element
 */
public class GenericCyclicCommand<T> extends DecoratorCommand {
    /**
     * Current cycle number
     */
    private int currentCycle;

    /**
     * Current element of the collection (valid during iteration)
     */
    private T currentElement;

    /**
     * Collection to iterate through
     */
    private final Collection<T> collection;

    /**
     * Constructor
     * @param coreCommand   Core command
     * @param collection    Collection to iterate through
     */
    public GenericCyclicCommand(Command coreCommand, Collection<T> collection) {
        this(coreCommand, collection, "Generic Cyclic");
    }

    /**
     * Constructor
     * @param coreCommand   Core command
     * @param collection    Collection to iterate through
     * @param name          Command name
     */
    public GenericCyclicCommand(Command coreCommand, Collection<T> collection, String name) {
        super(coreCommand, name);

        requireNonNull(collection, String.format("Collection is NULL in GenericCyclicCommand %s", name));

        this.collection = collection;
    }

    /**
     * @return          Current cycle number
     */
    public final int getCurrentCycle() {
        return currentCycle;
    }

    /**
     * @return          Current element of the collection (valid during iteration)
     */
    public final T getCurrentElement() {
        return currentElement;
    }

    @Override
    protected void execute() throws Exception {
        currentCycle = 0;
        var iterator = collection.iterator();

        while (iterator.hasNext()) {
            currentElement = iterator.next();
            currentCycle++;

            coreCommand.run();

            processAbortAndPauseEvents();

            if (getState() == State.Failed || getState() == State.Aborted ||
                    coreCommand.getState() == State.Failed || coreCommand.getState() == State.Aborted)
                break;
        }
    }
}
