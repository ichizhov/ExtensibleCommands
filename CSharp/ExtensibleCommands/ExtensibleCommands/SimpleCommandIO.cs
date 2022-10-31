using System;

namespace ExtensibleCommands
{
    /// <summary>
    /// Base class for atomic operation with input and output parameters
    /// </summary>
    /// <typeparam name="TInput">Type of input parameter</typeparam>
    /// <typeparam name="TOutput">Type of output parameter</typeparam>
    public class SimpleCommandIO<TInput, TOutput> : SimpleCommandI<TInput>
    {
        /// <summary> Type of delegate to execute a command with an input and an output parameters </summary>
        public delegate TOutput ExecutionDelegateIO(TInput input);

        /// <summary> Command output </summary>
        public TOutput Output { get; private set; }

        /// <summary> Delegate to execute </summary>
        protected new ExecutionDelegateIO _executionMethod;

        /// <summary> Constructor </summary>
        protected SimpleCommandIO()
        {
            Name = "Simple(Input, Output)";
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        public SimpleCommandIO(ExecutionDelegateIO executionDelegate)
            : this(executionDelegate, "Simple(Input, Output)") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        /// <param name="name"></param>
        public SimpleCommandIO(ExecutionDelegateIO executionDelegate, string name)
        {
            _executionMethod = executionDelegate;
            Name = name;
        }

        protected override void Execute()
        {
            if (_executionMethod != null)
                Output = _executionMethod.Invoke(Input);
        }
    }
}
