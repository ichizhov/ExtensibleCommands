package org.extensiblecommands;

/**
 * Interface representing simple log functionality for external dependency injection
 */
public interface Log {
    /**
     * Log message
     * @param timestamp         Timestamp
     * @param level             Log level
     * @param message           Message to log
     */
    void log(String timestamp, String level, String message);
}
