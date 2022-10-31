using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Executes Core command, and then executes Finally command, 
    /// regardless of the result of the Core command.
    /// Finally command will always be executed (unless unhandled exception is thrown in Core command).
    /// </summary>
    public class TryCatchFinallyCommand : DecoratorCommand
    {
        /// <summary> Recovery command to execute after the Core command is completed with or without error </summary>
        public ICommand FinallyCommand { get; }
        
        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                var children = new List<ICommand>(base.Children);
                children.Add(FinallyCommand);
                return children;
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>(base.Descendants);
                descendants.Add(FinallyCommand);
                descendants.AddRange(FinallyCommand.Descendants);
                return descendants;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="finallyCommand">Recovery command to execute after the Core command is completed with or without error</param>
        public TryCatchFinallyCommand(ICommand coreCommand, ICommand finallyCommand)
            : this(coreCommand, finallyCommand, "Try-Catch-Finally") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="finallyCommand">Recovery command to execute after the Core command is completed with or without error</param>
        /// <param name="name">Command name</param>
        public TryCatchFinallyCommand(ICommand coreCommand, ICommand finallyCommand, string name)
            : base(coreCommand, name)
        {
            if (finallyCommand == null)
                throw new Exception(string.Format("Core Command is NULL in TryCatchFinallyCommand {0}", Name));

            FinallyCommand = finallyCommand;
        }

        protected override void Execute()
        {
            // Run core command
            CoreCommand.Run();

            ProcessAbortAndPauseEvents();

            if (CoreCommand.CurrentState == State.Aborted || CurrentState == State.Aborted) return;

            // Remember Core command exception
            if (CoreCommand.CurrentState == State.Failed)
            {
                Exception = CoreCommand.Exception;
            }

            FinallyCommand.Run();
        }

        /// <summary> Set the main command state based on the child commands states </summary>
        protected override void CheckErrors()
        {
            if (CoreCommand.CurrentState == State.Aborted ||
               FinallyCommand.CurrentState == State.Aborted)
            {
                CurrentState = State.Aborted;
            }
            if (FinallyCommand.CurrentState == State.Failed)
            {
                // If Finally command failed, the whole command failed
                CurrentState = State.Failed;
                Exception = FinallyCommand.Exception;
            }
            else if (CoreCommand.CurrentState == State.Failed)
            {
                Exception = CoreCommand.Exception;
                // If Core command failed, the whole command failed
                CurrentState = State.Failed;
            }
            else if (CoreCommand.CurrentState == State.Completed &&
               FinallyCommand.CurrentState == State.Completed)
            {
                CurrentState = State.Completed;
            }
        }
    }
}
