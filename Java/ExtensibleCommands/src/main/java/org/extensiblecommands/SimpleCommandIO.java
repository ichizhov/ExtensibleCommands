package org.extensiblecommands;

/**
 * Base class for atomic operation with input and output parameters
 * @param <TInput>      Type of input parameter
 * @param <TOutput>     Type of output parameter
 */
public class SimpleCommandIO<TInput, TOutput> extends SimpleCommandI<TInput> {
    /**
     * Command output
     */
    private TOutput output;

    /**
     * Delegate to execute
     */
    protected ExecutionDelegateIO<TInput, TOutput> executionMethod;

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     */
    public SimpleCommandIO(ExecutionDelegateIO<TInput, TOutput> executionDelegate) {
        this(executionDelegate, "Simple(Input, Output)");
    }

    /**
     * Constructor
     * @param executionDelegate     Delegate to execute
     * @param name                  Command name
     */
    public SimpleCommandIO(ExecutionDelegateIO<TInput, TOutput> executionDelegate, String name) {
        executionMethod = executionDelegate;
        this.name = name;
    }

    /**
     * @return          Command output
     */
    public TOutput getOutput() {
        return output;
    }

    protected SimpleCommandIO() {
        this.name = "Simple(Input, Output)";
    }

    @Override
    protected void execute() throws Exception {
        if (executionMethod != null)
            this.output = executionMethod.execute(getInput());
    }
}
