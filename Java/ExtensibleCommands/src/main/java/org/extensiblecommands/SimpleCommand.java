package org.extensiblecommands;

/**
 * Base class for atomic (non-composite) commands
 */
public class SimpleCommand extends AbstractCommand implements Command {
    /**
     * Null command that does nothing
     */
    public static SimpleCommand NullCommand = new SimpleCommand(() -> {}, "Do nothing");

    /**
     * Delegate to execute
     */
    protected ExecutionDelegate executionMethod;

    protected SimpleCommand() {
        this.name = "Simple";
    }

    /**
     * Constructor
     * @param name      Command name
     */
    public SimpleCommand(String name) {
        this(() -> {}, name);
    }

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     */
    public SimpleCommand(ExecutionDelegate executionDelegate) {
        this(executionDelegate, "Simple");
    }

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     * @param name                  Command name
     */
    public SimpleCommand(ExecutionDelegate executionDelegate, String name) {
        executionMethod = executionDelegate;
        this.name = name;
    }

    /**
     * Do nothing
     */
    @Override
    protected void checkErrors() { }

    @Override
    protected void execute() throws Exception {
        if (executionMethod != null)
            executionMethod.execute();
    }
}
