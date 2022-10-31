using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements sequential execution of multiple commands. If any of the sub-commands fails, 
    /// the command fails.
    /// </summary>
    public class SequentialCommand : CompositeCommand
    {
        /// <summary> Constructor </summary>
        public SequentialCommand() : this("Sequential") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Command name</param>
        public SequentialCommand(string name) : base(name) { }

        protected override void Execute()
        {
            foreach (ICommand subCommand in _subCommands)
            {
                if (CurrentState == State.Aborted) break;

                subCommand.Run();

                ProcessAbortAndPauseEvents();

                // If there is an error or abort in one of the subcommands, terminate the command
                if (subCommand.CurrentState == State.Failed || subCommand.CurrentState == State.Aborted)
                    break;
            }
        }
    }
}
