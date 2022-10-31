using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements composite commands, i.e. containing multiple sub-commands
    /// </summary>
    public abstract class CompositeCommand : Command
    {
        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                return new List<ICommand>(_subCommands);
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>(_subCommands);
                foreach (var subCommand in _subCommands)
                    descendants.AddRange(subCommand.Descendants);
                return descendants;
            }
        }

        /// <summary> List of sub-commands </summary>
        protected List<ICommand> _subCommands = new List<ICommand>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Command name</param>
        protected CompositeCommand(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Adds a sub-command to the execution list
        /// </summary>
        /// <param name="subCommand">Sub-command to add</param>
        /// <returns>This command</returns>
        public CompositeCommand Add(ICommand subCommand)
        {
            if (subCommand == null)
                throw new Exception(string.Format("Attempt to add NULL sub-command to command {0}", Name));

            // Throw an exception if the command is currently executing
            if (CurrentState == State.Executing)
                throw new Exception(string.Format("Attempt to add sub-command {0} to executing command {1}", subCommand.Name, Name));

            _subCommands.Add(subCommand);

            return this;
        }

        /// <summary>
        /// Returns a sub-command by index
        /// </summary>
        /// <param name="index">0-based index of sub-command (in the order of addition)</param>
        /// <returns>Sub-command corresponding to this index</returns>
        public ICommand GetSubCommand(int index)
        {
            if (index < 0 || index >= _subCommands.Count)
                throw new Exception(string.Format("For command {0} sub-command index {1} is out of the allowed range [{2} - {3}]",
                    Name, index, 0, _subCommands.Count));

            return _subCommands[index];
        }

        /// <summary> Set the main command state based on the child commands states </summary>
        protected override void CheckErrors()
        {
            if (CurrentState != State.Aborted)
                CurrentState = State.Completed;

            foreach (ICommand a in _subCommands)
            {
                if (a.CurrentState == State.Aborted)
                {
                    CurrentState = State.Aborted;
                }
            }
            // Only analyze error states if the command has not been aborted
            if (CurrentState != State.Aborted)
            {
                foreach (ICommand a in _subCommands)
                {
                    if (a.CurrentState == State.Failed)
                    {
                        CurrentState = State.Failed;
                        Exception = a.Exception;
                    }
                }
            }
        }
    }
}
