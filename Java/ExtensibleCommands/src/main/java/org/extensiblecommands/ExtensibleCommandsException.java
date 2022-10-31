package org.extensiblecommands;

/**
 * Basic type for all operational exceptions thrown from within command execution path 
 */
public class ExtensibleCommandsException extends Exception {
    private final int id;
    private final String text;
    private Exception exception;
    private final String timestamp;

    /**
     * Constructor
     * @param id            Error ID
     * @param text          Error description
     */
    public ExtensibleCommandsException(int id, String text) {
        this.id = id;
        this.text = text;
        this.timestamp = Logger.getTimeStamp();
    }

    /**
     * Constructor
     * @param id            Error ID
     * @param text          Error description
     * @param exception     Underlying exception
     */
    public ExtensibleCommandsException(int id, String text, Exception exception) {
        this(id, text);
        this.exception = exception;
    }

    /**
     * @return              Error ID
     */
    public int getId() {
        return id;
    }

    /**
     * @return              Error description
     */
    public String getText() {
        return text;
    }

    /**
     * @return              Underlying exception
     */
    public Exception getException() {
        return exception;
    }

    /**
     * @return              Timestamp of when the failure occurred
     */
    public String getTimestamp() {
        return timestamp;
    }
}
