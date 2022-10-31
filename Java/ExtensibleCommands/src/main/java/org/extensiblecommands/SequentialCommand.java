package org.extensiblecommands;

/**
 * Implements sequential execution of multiple commands. If any of the sub-commands fails,
 * the command fails.
 */
public class SequentialCommand extends CompositeCommand {
    public SequentialCommand() {
        this("Sequential");
    }

    /**
     * Constructor
     * @param name      Command name
     */
    public SequentialCommand(String name) {
        super(name);
    }

    @Override
    protected void execute() throws Exception {
        for (var subCommand : subCommands) {
            if (getState() == State.Aborted)
                break;

            subCommand.run();

            processAbortAndPauseEvents();

            if (subCommand.getState() == State.Failed || subCommand.getState() == State.Aborted)
                break;
        }
    }
}
