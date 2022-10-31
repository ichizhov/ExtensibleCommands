namespace ExtensibleCommands
{
    /// <summary>
    /// Base class for atomic (non-composite) commands
    /// </summary>
    public class SimpleCommand : Command
    {
        /// <summary> Null command that does nothing </summary>
        public static SimpleCommand NullCommand = new SimpleCommand(() => { }, "Do nothing");

        /// <summary> Type of delegate to execute a command </summary>
        public delegate void ExecutionDelegate();

        /// <summary> Delegate to execute </summary>
        protected ExecutionDelegate _executionMethod;

        /// <summary> Constructor </summary>
        protected SimpleCommand()
        {
            Name = "Simple";
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Command name</param>
        public SimpleCommand(string name) : this(() => { }, name) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        public SimpleCommand(ExecutionDelegate executionDelegate)
            : this(executionDelegate, "Simple") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        /// <param name="name">Command name</param>
        public SimpleCommand(ExecutionDelegate executionDelegate, string name)
        {
            _executionMethod = executionDelegate;
            Name = name;
        }

        /// <summary> Do nothing </summary>
        protected override void CheckErrors() { }

        protected override void Execute()
        {
            _executionMethod?.Invoke();
        }
    }
}
