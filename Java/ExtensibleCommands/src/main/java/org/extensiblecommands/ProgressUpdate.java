package org.extensiblecommands;

/**
 * Carries command state information
 */
public class ProgressUpdate {
    private final int percentCompleted;
    private final double fractionCompleted;
    private final String progressMessage;

    /**
     * Constructor
     * @param percentCompleted      Percent complete (0-100)
     * @param fractionCompleted     Fraction complete (0-1)
     * @param progressMessage       Progress description
     */
    public ProgressUpdate(int percentCompleted, double fractionCompleted, String progressMessage) {
        this.percentCompleted = percentCompleted;
        this.fractionCompleted = fractionCompleted;
        this.progressMessage = progressMessage;
    }

    /**
     * @return      Percent complete (0-100)
     */
    public final int getPercentCompleted() {
        return percentCompleted;
    }

    /**
     * @return      Fraction complete (0-1)
     */
    public final double getFractionCompleted() {
        return fractionCompleted;
    }

    /**
     * @return      Progress description
     */
    public final String getProgressMessage() {
        return progressMessage;
    }
}
