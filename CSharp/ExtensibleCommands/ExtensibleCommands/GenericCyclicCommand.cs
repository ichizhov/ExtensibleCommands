using System;
using System.Collections.Generic;

namespace ExtensibleCommands
{
    /// <summary>
    /// Implements generic cyclic command that iterates through the collection and 
    /// executes the core command for every element.
    /// If the core command does not make use of the CurrentElement in the collection, 
    /// then the result is equivalent to CyclicCommand (i.e. the same command is executed multiple times).
    /// However, if the core command makes use of CurrentElement, then the result is going to be different on every cycle.
    /// This makes it possible to model the "for" loop.
    /// The class is parametrized by the type of the basic element of the collection.
    /// </summary>
    /// <typeparam name="T">Type of collection element</typeparam>
    public class GenericCyclicCommand<T> : DecoratorCommand
    {
        /// <summary> Current element of the collection (valid during iteration) </summary>
        public T CurrentElement { get; private set; }

        /// <summary> Current cycle number </summary>
        public int CurrentCycle { get; private set; }

        /// <summary> Collection to iterate through </summary>
        private ICollection<T> _collection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="collection">Collection to iterate through</param>
        public GenericCyclicCommand(ICommand coreCommand, ICollection<T> collection)
            : this(coreCommand, collection, "Generic Cyclic") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coreCommand">Core command</param>
        /// <param name="collection">Collection to iterate through</param>
        /// <param name="name">Command name</param>
        public GenericCyclicCommand(ICommand coreCommand, ICollection<T> collection, string name)
            : base(coreCommand, name)
        {
            if (collection == null)
                throw new Exception(string.Format("Collection is NULL in GenericCyclicCommand {0}", Name));

            _collection = collection;
        }

        protected override void Execute()
        {
            CurrentCycle = 0;
            var enumerator = _collection.GetEnumerator();

            while (enumerator.MoveNext())
            {
                CurrentElement = enumerator.Current;
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
