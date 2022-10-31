using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements command branching based on a conditional boolean flag
    /// </summary>
    public class ConditionalCommand : Command
    {
        /// <summary> Command to run if the conditional flag is true </summary>
        public ICommand TrueCommand { get; }

        /// <summary> Command to run if the conditional flag is false </summary>
        public ICommand FalseCommand { get; }
    
        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                var children = new List<ICommand>();
                children.Add(TrueCommand);
                children.Add(FalseCommand);
                return children;
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>();
                descendants.Add(TrueCommand);
                descendants.AddRange(TrueCommand.Descendants);
                descendants.Add(FalseCommand);
                descendants.AddRange(FalseCommand.Descendants);
                return descendants;
            }
        }

        /// <summary> Predicate to evaluate to decide which sub-command to run </summary>
        private readonly Func<bool> _predicate;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="predicate">Predicate to evaluate to decide which sub-command to run</param>
        /// <param name="trueCommand">Command to run if the conditional flag is true</param>
        /// <param name="falseCommand">Command to run if the conditional flag is false</param>
        public ConditionalCommand(Func<bool> predicate, ICommand trueCommand, ICommand falseCommand) 
            : this(predicate, trueCommand, falseCommand, "Conditional") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="predicate">Predicate to evaluate to decide which sub-command to run</param>
        /// <param name="trueCommand">Command to run if the conditional flag is true</param>
        /// <param name="falseCommand">Command to run if the conditional flag is false</param>
        /// <param name="name">Command name</param>
        public ConditionalCommand(Func<bool> predicate, ICommand trueCommand, ICommand falseCommand, string name)
        {
            if (predicate == null)
                throw new Exception(string.Format("Predicate is NULL in ConditionalCommand {0}", name));
            if (trueCommand == null)
                throw new Exception(string.Format("True Command is NULL in ConditionalCommand {0}", Name));
            if (falseCommand == null)
                throw new Exception(string.Format("False Command is NULL in ConditionalCommand {0}", Name));

            Name = name;
            _predicate = predicate;
            TrueCommand = trueCommand;
            FalseCommand = falseCommand;
        }

        protected override void Execute()
        {
            if (_predicate())
                TrueCommand.Run();
            else
                FalseCommand.Run();
        }

        /// <summary> Set the main command state based on the child commands states </summary>
        protected override void CheckErrors()
        {
            if (TrueCommand.CurrentState == State.Aborted ||
               FalseCommand.CurrentState == State.Aborted)
            {
                CurrentState = State.Aborted;
            }
            else if (TrueCommand.CurrentState == State.Failed)
            {
                CurrentState = State.Failed;
                Exception = TrueCommand.Exception;
            }
            else if (FalseCommand.CurrentState == State.Failed)
            {
                CurrentState = State.Failed;
                Exception = FalseCommand.Exception;
            }
            else if (TrueCommand.CurrentState == State.Completed ||
               FalseCommand.CurrentState == State.Completed)
            {
                CurrentState = State.Completed;
            }
        }
    }
}

