namespace ExtensibleCommands
{
    /// <summary>
    /// Implements command to be repeated a specified number of times
    /// </summary>
    public class CyclicCommand : DecoratorCommand
    {
        /// <summary> How many times to repeat the core command </summary>
        public int NumberOfRepeats { get; }

        /// <summary> Current cycle number </summary>
        public int CurrentCycle { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="numberOfRepeats">How many times to repeat the core command</param>
        public CyclicCommand(ICommand coreCommand, int numberOfRepeats)
            : this(coreCommand, numberOfRepeats, "Cyclic") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="numberOfRepeats">How many times to repeat the core command</param>
        /// <param name="name">Command name</param>
        public CyclicCommand(ICommand coreCommand, int numberOfRepeats, string name)
            : base(coreCommand, name)
        {
            NumberOfRepeats = numberOfRepeats;
        }

        protected override void Execute()
        {
            CurrentCycle = 0;

            for (int i = 0; i < NumberOfRepeats; i++)
            {
                CurrentCycle++;
                CoreCommand.Run();

                ProcessAbortAndPauseEvents();

                if (CurrentState == State.Failed || CurrentState == State.Aborted ||
                    CoreCommand.CurrentState == State.Failed || CoreCommand.CurrentState == State.Aborted)
                    break;
            }
        }
    }
}
