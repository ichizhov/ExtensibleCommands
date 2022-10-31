using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Allows designating a recovery command in case if the core command fails. 
    /// If the core command succeeds, nothing happens.
    /// </summary>
    public class RecoverableCommand : DecoratorCommand
    {
        /// <summary> Recovery command to execute if the core command fails </summary>
        public ICommand RecoveryCommand { get; }
       
        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                var children = new List<ICommand>(base.Children);
                children.Add(RecoveryCommand);
                return children;
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>(base.Descendants);
                descendants.Add(RecoveryCommand);
                descendants.AddRange(RecoveryCommand.Descendants);
                return descendants;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="recoveryCommand">Recovery command to execute if the core command fails</param>
        public RecoverableCommand(ICommand coreCommand, ICommand recoveryCommand)
            : this(coreCommand, recoveryCommand, "Recoverable")
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="recoveryCommand">Recovery command to execute if the core command fails</param>
        /// <param name="name">Command name</param>
        public RecoverableCommand(ICommand coreCommand, ICommand recoveryCommand, string name)
            : base(coreCommand, name)
        {
            if (recoveryCommand == null)
                throw new Exception(string.Format("Recovery Command is NULL in RecoverableCommand {0}", Name));

            RecoveryCommand = recoveryCommand;
        }

        protected override void Execute()
        {
            // Run core command
            CoreCommand.Run();

            ProcessAbortAndPauseEvents();

            if (CoreCommand.CurrentState == State.Aborted || CurrentState == State.Aborted) return;

            if (CoreCommand.CurrentState == State.Failed)
            {
                // Post the error
                Exception = CoreCommand.Exception;

                // Execute recovery command in case of failure (if error allows to continue)
                if (Exception is ExtensibleCommandsAllowRecoveryException)
                {
                    RecoveryCommand.Run();
                    if (RecoveryCommand.CurrentState == State.Failed)
                    {
                        Exception = RecoveryCommand.Exception;
                    }
                }
            }
        }

        /// <summary> Set the main command state based on the child commands states </summary>
        protected override void CheckErrors()
        {
            if (CoreCommand.CurrentState == State.Aborted ||
               RecoveryCommand.CurrentState == State.Aborted)
            {
                CurrentState = State.Aborted;
            }
            else if (RecoveryCommand.CurrentState == State.Failed)
            {
                // If Recovery command failed, the whole command failed
                CurrentState = State.Failed;
                Exception = RecoveryCommand.Exception;
            }
            else if (CoreCommand.CurrentState == State.Failed && !(CoreCommand.Exception is ExtensibleCommandsAllowRecoveryException))
            {
                // If Core command failed and the error is not recoverable, the whole command failed
                CurrentState = State.Failed;
                Exception = CoreCommand.Exception;
            }
            else if (CoreCommand.CurrentState == State.Completed ||
               RecoveryCommand.CurrentState == State.Completed)
            {
                CurrentState = State.Completed;

                // Make sure we log the fact that the error was recovered
                if (CoreCommand.Exception is ExtensibleCommandsAllowRecoveryException)
                    Logger.Log(Logger.LogLevel.Error,
                        string.Format("ERROR (RECOVERED)[{0}] - {1}", CoreCommand.Exception.ID, CoreCommand.Exception.Text));
            }
        }
    }
}
