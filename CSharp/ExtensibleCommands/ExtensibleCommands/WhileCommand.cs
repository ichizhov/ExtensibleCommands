using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Executes Core command while a condition evaluated by a predicate function holds true.
    /// Corresponds to a standard While loop.
    /// </summary>
    public class WhileCommand : DecoratorCommand
    {
        /// <summary> Command to run before the main While cycle (to be used to set initial conditions (if any)) </summary>
        public ICommand InitCommand { get; }
    
        /// <summary> Current cycle number </summary>
        public int CurrentCycle { get; private set; }

        /// <summary> List of all child command objects (1st level only) </summary>
        public override IEnumerable<ICommand> Children
        {
            get
            {
                var children = new List<ICommand>(base.Children);
                if (InitCommand != null)
                    children.Add(InitCommand);
                return children;
            }
        }

        /// <summary> List of all descendant command objects (all levels) </summary>
        public override IEnumerable<ICommand> Descendants
        {
            get
            {
                var descendants = new List<ICommand>(base.Descendants);
                if (InitCommand != null)
                {
                    descendants.Add(InitCommand); 
                    descendants.AddRange(InitCommand.Descendants);
                }
                return descendants;
            }
        }

        /// <summary> Predicate delegate to execute to evaluate loop condition </summary>
        private readonly Func<bool> _predicate;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="predicate">Predicate delegate to execute to evaluate loop condition</param>
        /// <param name="coreCommand">Core command</param>
        public WhileCommand(Func<bool> predicate, ICommand coreCommand) 
            : this(predicate, null, coreCommand, "While") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="predicate">Predicate delegate to execute to evaluate loop condition</param>
        /// <param name="initCommand">Command to run before the main While cycle (to be used to set initial conditions (if any))</param>
        /// <param name="coreCommand">Core command</param>
        public WhileCommand(Func<bool> predicate, ICommand initCommand, ICommand coreCommand) 
            : this(predicate, initCommand, coreCommand, "While") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="predicate">Predicate delegate to execute to evaluate loop condition</param>
        /// <param name="initCommand">Command to run before the main While cycle (to be used to set initial conditions (if any))</param>
        /// <param name="coreCommand">Core command</param>
        /// <param name="name">Command name</param>
        public WhileCommand(Func<bool> predicate, ICommand initCommand, ICommand coreCommand, string name)
            : base(coreCommand, name)
        {
            if (predicate == null)
                throw new Exception(string.Format("Predicate is NULL in WhileCommand {0}", Name));

            _predicate = predicate;
            InitCommand = initCommand;
        }

        protected override void Execute()
        {
            // Run initial command if it is provided
            if (InitCommand != null)
            {
                InitCommand.Run();

                ProcessAbortAndPauseEvents();

                // If there is an error or abort in one of the sub-commands, terminate the command
                if (InitCommand.CurrentState == State.Failed || InitCommand.CurrentState == State.Aborted)
                {
                    CheckErrors(InitCommand);
                    return;
                 }
            }

            // Run core command in a while cycle
            CurrentCycle = 0;
            while (_predicate.Invoke())
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
