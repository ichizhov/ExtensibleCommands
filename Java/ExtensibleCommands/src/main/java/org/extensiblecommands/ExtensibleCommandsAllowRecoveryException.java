package org.extensiblecommands;

/**
 * Exception that allows a Recovery Command to perform recovery,
 * i.e. it does not result in immediate termination of the operation.
 * All recoverable failures should be of this type.
 */
public class ExtensibleCommandsAllowRecoveryException extends ExtensibleCommandsException {
    /**
     * Constructor
     * @param id        Error ID
     * @param text      Error description
     */
    public ExtensibleCommandsAllowRecoveryException(int id, String text){
        super(id, text);
    }

    /**
     * Constructor
     * @param id        Error ID
     * @param text      Error description
     * @param e         Underlying exception
     */
    public ExtensibleCommandsAllowRecoveryException(int id, String text, Exception e){
        super(id, text, e);
    }
}
