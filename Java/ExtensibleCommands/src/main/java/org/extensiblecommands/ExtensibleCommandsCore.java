package org.extensiblecommands;

import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

/**
 * Static class representing an instance of Extensible Commands.
 * Implements static properties and methods that are applicable to generic Command behavior.
 */
public class ExtensibleCommandsCore {
    /**
     * Enumeration for Log levels
     */
    public enum LogLevel {
        Info, Error
    }

    /**
     * Set Logger object
     * @param externalLogger    Logger object implementing Log interface
     */
    public static void setLogger(Log externalLogger) {
        logger = externalLogger;
    }

    /**
     * @return              Logger object implementing Log interface
     */
    public static Log getLogger() {
        return logger;
    }

    /**
     * @return              Timestamp string
     */
    public static String getTimeStamp() {
        return DateTimeFormatter.ofPattern("yyyy/MM/dd HH:mm:ss:SSS").format(LocalDateTime.now());
    }

    /**
     * Set Logging Enabled flag
     * @param isEnabled     Is logging of command events enabled?
     */
    public static void setIsLoggingEnabled(boolean isEnabled) {
        isLoggingEnabled = isEnabled;
    }

    /**
     * @return              Is logging of command events enabled?
     */
    public static boolean getIsLoggingEnabled() {
        return isLoggingEnabled;
    }

    /**
     * Log informational or error message
     * @param logLevel      Log level
     * @param message       Message to be logged
     */
    public static void log(LogLevel logLevel, String message) {
        if (getLogger() == null) return;

        // Always log errors regardless of the logging enabled flag
        if (getIsLoggingEnabled() || logLevel == LogLevel.Error)
            logger.log(getTimeStamp(), logLevel.toString(), message);
    }

    //----------------------------------------------------------------------------------------------------------------------

    private static Log logger;
    private static boolean isLoggingEnabled = true;
}
