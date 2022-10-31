using System;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements command that can be aborted during its execution by invoking Abort() method.
    /// This implies that command execution and its abort should occur in different threads.
    /// </summary>
    public class AbortableCommand : DecoratorCommand
    {
        /// <summary> Type of delegate to abort a command </summary>
        public delegate void AbortDelegate();

        /// <summary> Method to abort this command </summary>
        private readonly AbortDelegate _abortDelegate;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="abortDelegate">Delegate to abort operation of the Core command</param>
        public AbortableCommand(ICommand coreCommand, AbortDelegate abortDelegate) 
            : this(coreCommand, abortDelegate, "Abortable") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="abortDelegate">Delegate to abort the operation of the Core command</param>
        /// <param name="name">Command name</param>
        public AbortableCommand(ICommand coreCommand, AbortDelegate abortDelegate, string name)
            : base(coreCommand, name)
        {
            if (abortDelegate == null)
                throw new Exception(string.Format("Abort Delegate is NULL in AbortableCommand {0}", Name));

            _abortDelegate = abortDelegate;
        }

        public override void Abort()
        {
            _aborted = true;
            _abortDelegate?.Invoke();
            base.Abort();
        }

        protected override void Execute()
        {
            CoreCommand.Run();
            ProcessAbortAndPauseEvents();
        }
    }
}
