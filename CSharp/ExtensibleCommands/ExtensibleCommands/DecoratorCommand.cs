using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary> 
    /// Base class for decorator commands (i.e. adding functionality to other commands)
    /// </summary>
    public abstract class DecoratorCommand : Command
    {
        /// <summary> Command to be decorated by the additional functionality </summary>
        public ICommand CoreCommand { get; }
      
        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                var children = new List<ICommand>();
                children.Add(CoreCommand);
                return children;
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>();
                descendants.Add(CoreCommand);
                descendants.AddRange(CoreCommand.Descendants);
                return descendants;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Command to be decorated by the additional functionality</param>
        /// <param name="name">Command name</param>
        protected DecoratorCommand(ICommand coreCommand, string name)
        {
            if (coreCommand == null)
                throw new Exception(string.Format("Core Command is NULL in (Decorator) Command {0}", name));

            Name = name;
            CoreCommand = coreCommand;
        }

        /// <summary> Set the main command state based on the child commands states </summary>
        protected override void CheckErrors()
        {
            CheckErrors(CoreCommand);
        }

        /// <summary>
        /// Set the main command state based on the child commands states
        /// </summary>
        /// <param name="command">Child command</param>
        protected void CheckErrors(ICommand command)
        {
            // NOTE:
            // It is possible that we have a local abort and a failure roughly at the same time
            // In this case failure supersedes abort, i.e. the command will fail as if no abort was issued.
            if (command.CurrentState == State.Failed)
            {
                CurrentState = State.Failed;
                Exception = command.Exception;
                return;
            }
            if (command.CurrentState == State.Aborted)
                CurrentState = State.Aborted;
        }
    }
}
