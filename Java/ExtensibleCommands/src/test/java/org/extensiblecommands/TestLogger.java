package org.extensiblecommands;

/**
 * Implements logger: print out in Console window
 */
public class TestLogger implements Log {
    /**
     * Log message
     * @param timestamp         Timestamp
     * @param level             Log level
     * @param message           Message to log
     */
    public void log(String timestamp, String level, String message) {
        System.out.println(String.format("%s - %s - %s", timestamp, level, message));
    }
}
