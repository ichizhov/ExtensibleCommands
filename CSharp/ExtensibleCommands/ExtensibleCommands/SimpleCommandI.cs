using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensibleCommands
{
    /// <summary>
    /// Base class for atomic operation with an input parameter
    /// </summary>
    /// <typeparam name="TInput">Type of input parameter</typeparam>
    public class SimpleCommandI<TInput> : SimpleCommand
    {
        /// <summary> Type of delegate to execute a command with an input parameter </summary>
        public delegate void ExecutionDelegateI(TInput input);

        /// <summary> Command input </summary>
        public TInput Input { get; set; }

        /// <summary> Delegate to execute </summary>
        protected new ExecutionDelegateI _executionMethod;

        /// <summary> Constructor </summary>
        protected SimpleCommandI()
        {
            Name = "Simple(Input)";
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        public SimpleCommandI(ExecutionDelegateI executionDelegate)
            : this(executionDelegate, "Simple(Input)") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executionDelegate">Delegate to execute</param>
        /// <param name="name">Command name</param>
        public SimpleCommandI(ExecutionDelegateI executionDelegate, string name)
        {
            _executionMethod = executionDelegate;
            Name = name;
        }

        protected override void Execute()
        {
            _executionMethod?.Invoke(Input);
        }
    }
}
