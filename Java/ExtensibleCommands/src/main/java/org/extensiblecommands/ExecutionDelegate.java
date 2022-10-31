package org.extensiblecommands;

/**
 * Type of delegate to execute a command
 */
public interface ExecutionDelegate {
    void execute() throws Exception;
}
