package org.extensiblecommands;

/**
 * Base class for atomic operation with an input parameter
 * @param <TInput>      Type of input parameter
 */
public class SimpleCommandI<TInput> extends SimpleCommand {
    /// <summary> Delegate to execute </summary>
    /**
     * Delegate to execute
     */
    protected ExecutionDelegateI<TInput> executionMethod;

    /**
     * Command input
     */
    private TInput input;

    protected SimpleCommandI() {
        this.name = "Simple(Input)";
    }

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     */
    public SimpleCommandI(ExecutionDelegateI<TInput> executionDelegate) {
        this(executionDelegate, "Simple(Input)");
    }

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     * @param name                  Command name
     */
    public SimpleCommandI(ExecutionDelegateI<TInput> executionDelegate, String name) {
        executionMethod = executionDelegate;
        this.name = name;
    }

    /**
     * @return          Command input
     */
    public final TInput getInput() {
        return input;
    }

    /**
     * Set command input
     * @param input     Command input
     */
    public final void setInput(TInput input) {
        this.input = input;
    }

    @Override
    protected void execute() throws Exception {
        if (executionMethod != null)
            executionMethod.execute(input);
    }
}
