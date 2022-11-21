package org.extensiblecommands;

import java.util.ArrayList;

/**
 * Implements parallel execution of multiple commands using threads.
 * Failure in one of the branches leads to the failure of the Parallel command.
 * If more than one branch fails, only the first one will be registered, and others ignored.
 */
public class ParallelCommand extends CompositeCommand {
    public ParallelCommand() {
        this ("Parallel");
    }

    /**
     * Constructor
     * @param name      Command name
     */
    public ParallelCommand(String name) {
        super(name);
    }

    @Override
    protected void execute() throws Exception {
        var exceptions = new ArrayList<Exception>();

        // Make sure we arm Finished events in all sub-commands so that we can reliably wait for them
        // in a different thread
        for (var subCommand : subCommands)
            subCommand.resetFinished();

        // Launch parallel sub-commands
        for (var subCommand : subCommands) {
            // Spawn threads to execute sub-commands
            new Thread(() -> {
                try {
                    subCommand.run();
                }
                catch (Exception e) {
                    // If there is a fatal exception, don't throw it here.
                    // Store it and process after all sub-commands are completed.
                    exceptions.add(e);
                }
            }).start();
        }

        // Wait until every sub-command is finished
        for (var subCommand : subCommands) {
            subCommand.waitUntilFinished(0);
        }

        // If there were fatal exceptions in any of the sub-commands, throw the first one in the list.
        // The information about the other fatal exceptions is ignored.
        if (!exceptions.isEmpty())
            throw new Exception("Fatal error in one of the sub-commands of a Parallel command", exceptions.get(0));

        processAbortAndPauseEvents();
    }
}
