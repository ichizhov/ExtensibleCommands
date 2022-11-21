using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements parallel execution of multiple commands using threads.
    /// Failure in one of the branches leads to the failure of the Parallel command.
    /// If more than one branch fails, only the first one will be registered, and others ignored.
    /// </summary>
    public class ParallelCommand : CompositeCommand
    {
        /// <summary> Default constructor </summary>
        public ParallelCommand() : this("Parallel") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public ParallelCommand(string name) : base(name) { }

        protected override void Execute()
        {        
            // Make sure we arm Finished events in all sub-commands so that we can reliably wait for them
            // in a different thread
            foreach (ICommand subCommand in _subCommands)
                subCommand.ResetFinished();

            var exceptions = new List<Exception>();

            // Launch parallel sub-commands
            foreach (ICommand subCommand in _subCommands)
            {
                // Spawn threads to execute sub-commands
                new Thread(() =>
                {
                    try
                    {
                        subCommand.Run();
                    }
                    catch (Exception ex)
                    {
                        // If there is a fatal exception, don't throw it here.
                        // Store it and process after all sub-commands are completed.
                        exceptions.Add(ex);
                    }
                }).Start();
            }

            // Wait until every sub-command is finished
            foreach (ICommand subCommand in _subCommands)
            {
                subCommand.WaitUntilFinished(0);
            }

            // If there were fatal exceptions in any of the sub-commands, throw the first one in the list.
            // The information about the other fatal exceptions is ignored.
            if (exceptions.Count > 0)
            {
                throw new Exception("Fatal error in one of the sub-commands of a Parallel command", exceptions[0]);
            }

            ProcessAbortAndPauseEvents();
        }
    }
}
