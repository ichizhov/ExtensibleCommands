package org.extensiblecommands;

/**
 * Exception that allows a Retry Command to perform retries,
 * i.e. it does not result in immediate termination of the operation.
 * For example, intermittent failures should be of this type to allow retries.
 * A Retry exception is also a Recovery exception to allow recovery if all retries fail.
 */
public class ExtensibleCommandsAllowRetryException extends ExtensibleCommandsAllowRecoveryException {
    /**
     * Constructor
     * @param id        Error ID
     * @param text      Error description
     */
    public ExtensibleCommandsAllowRetryException(int id, String text) {
        super(id, text);
    }

    /**
     * Constructor
     * @param id        Error ID
     * @param text      Error description
     * @param e         Underlying exception
     */
    public ExtensibleCommandsAllowRetryException(int id, String text, Exception e) {
        super(id, text, e);
    }
}
