package org.extensiblecommands;

import java.util.ArrayList;

import static java.util.Objects.requireNonNull;

/**
 * Allows designating a recovery command in case if the core command fails.
 * If the core command succeeds, nothing happens.
 */
public class RecoverableCommand extends DecoratorCommand {
    /**
     * Recovery command to execute if the core command fails
     */
    private final Command recoveryCommand;

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param recoveryCommand   Recovery command to execute if the core command fails
     */
    public RecoverableCommand(Command coreCommand, Command recoveryCommand) {
        this(coreCommand, recoveryCommand, "Recoverable");
    }

    /**
     * Constructor
     * @param coreCommand       Core command
     * @param recoveryCommand   Recovery command to execute if the core command fails
     * @param name              Command name
     */
    public RecoverableCommand(Command coreCommand, Command recoveryCommand, String name) {
        super(coreCommand, name);

        requireNonNull(recoveryCommand, String.format("Recovery Command is NULL in RecoverableCommand %s", name));

        this.recoveryCommand = recoveryCommand;
    }

    /**
     * @return          Recovery command to execute if the core command fails
     */
    public final Command getRecoveryCommand() {
        return recoveryCommand;
    }

    /**
     * @return          List of all child command objects (1st level only)
     */
    @Override
    public Iterable<Command> getChildren() {
        var children = new ArrayList<Command>();
        super.getChildren().forEach(c -> children.add(c));
        children.add(recoveryCommand);
        return children;
    }

    /**
     * @return          List of all descendant command objects (all levels)
     */
    @Override
    public Iterable<Command> getDescendants() {
        var descendants = new ArrayList<Command>();
        super.getDescendants().forEach(c -> descendants.add(c));
        descendants.add(recoveryCommand);
        recoveryCommand.getDescendants().forEach(c -> descendants.add(c));
        return descendants;
    }

    @Override
    protected void execute() throws Exception {
        // Run core command
        coreCommand.run();

        processAbortAndPauseEvents();

        if (coreCommand.getState() == State.Aborted || getState() == State.Aborted) return;

        if (coreCommand.getState() == State.Failed) {
            // Post the error
            exception = coreCommand.getException();

            // Execute recovery command in case of failure (if error allows to continue)
            if (exception instanceof ExtensibleCommandsAllowRecoveryException) {
                recoveryCommand.run();
                if (recoveryCommand.getState() == State.Failed) {
                    exception = recoveryCommand.getException();
                }
            }
        }
    }

    /**
     * Set the main command state based on the child commands states
     */
    @Override
    protected void checkErrors() {
        if (coreCommand.getState() == State.Aborted ||
                recoveryCommand.getState() == State.Aborted) {
            setState(State.Aborted);
        }
        else if (recoveryCommand.getState() == State.Failed) {
            // If Recovery command failed, the whole command failed
            setState(State.Failed);
            exception = recoveryCommand.getException();
        }
        else if (coreCommand.getState() == State.Failed &&
                !(coreCommand.getException() instanceof ExtensibleCommandsAllowRecoveryException)) {
            // If Core command failed and the error is not recoverable, the whole command failed
            setState(State.Failed);
            exception = coreCommand.getException();
        }
        else if (coreCommand.getState() == State.Completed ||
                recoveryCommand.getState() == State.Completed) {
            setState(State.Completed);

            // Make sure we log the fact that the error was recovered
            if (coreCommand.getException() instanceof ExtensibleCommandsAllowRecoveryException)
                Logger.log(Logger.LogLevel.Error,
                        String.format("ERROR (RECOVERED)[%s] - %s", getCoreCommand().getException().getId(),
                                getCoreCommand().getException().getText()));
        }
    }
}
