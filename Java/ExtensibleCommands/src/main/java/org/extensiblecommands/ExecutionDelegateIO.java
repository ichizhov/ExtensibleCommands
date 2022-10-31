package org.extensiblecommands;

/**
 * Type of delegate to execute a command with an input and an output parameters
 * @param <TInput>          Type of input parameter
 * @param <TOutput>         Type of output parameter
 */
public interface ExecutionDelegateIO<TInput, TOutput> {
    TOutput execute(TInput input) throws Exception ;
}
