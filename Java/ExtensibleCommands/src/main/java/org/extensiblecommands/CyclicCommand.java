package org.extensiblecommands;

/**
 * Implements command to be repeated a specified number of times
 */
public class CyclicCommand extends DecoratorCommand {
    /**
     * How many times to repeat the core command
     */
    private final int numberOfRepeats;

    /**
     * Current cycle number
     */
    private int currentCycle;

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param numberOfRepeats   How many times to repeat the core command
     */
    public CyclicCommand(Command coreCommand, int numberOfRepeats) {
        this(coreCommand, numberOfRepeats, "Cyclic");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param numberOfRepeats   How many times to repeat the core command
     * @param name              Command name
     */
    public CyclicCommand(Command coreCommand, int numberOfRepeats, String name) {
        super(coreCommand, name);
        this.numberOfRepeats = numberOfRepeats;
    }

    /**
     * @return      How many times to repeat the core command
     */
    public final int getNumberOfRepeats() {
        return numberOfRepeats;
    }

    /**
     * @return      Current cycle number
     */
    public final int getCurrentCycle() {
        return currentCycle;
    }

    @Override
    protected void execute() throws Exception {
        currentCycle = 0;

        for (int i = 0; i < numberOfRepeats; i++) {
            currentCycle++;
            coreCommand.run();

            processAbortAndPauseEvents();

            if (getState() == State.Failed || getState() == State.Aborted ||
                    coreCommand.getState() == State.Failed || coreCommand.getState() == State.Aborted)
                break;
        }
    }
}
