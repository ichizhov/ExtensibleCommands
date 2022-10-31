namespace ExtensibleCommands
{
    /// <summary> Carries command state information </summary>
    public class ProgressUpdate
    {
        /// <summary> Percent complete (0-100) </summary>
        public int PercentCompleted { get; }

        /// <summary> Fraction complete (0-1) </summary>
        public double FractionCompleted { get; }

        /// <summary> Progress description </summary>
        public string ProgressMessage { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="percentCompleted">Percent complete (0-100)</param>
        /// <param name="fractionCompleted">Fraction complete (0-1)</param>
        /// <param name="progressMessage">Progress description</param>
        public ProgressUpdate(int percentCompleted, double fractionCompleted, string progressMessage)
        {
            PercentCompleted = percentCompleted;
            FractionCompleted = fractionCompleted;
            ProgressMessage = progressMessage;
        }
    }
}
