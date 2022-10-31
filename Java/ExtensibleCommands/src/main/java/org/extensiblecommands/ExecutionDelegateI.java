package org.extensiblecommands;

/**
 * Type of delegate to execute a command with an input parameter
 * @param <TInput>      Type of input parameter
 */
public interface ExecutionDelegateI<TInput> {
    void execute(TInput input) throws Exception;
}
