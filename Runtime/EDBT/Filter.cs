using System;

namespace Readymade.Machinery.EDBT
{
    /// <inheritdoc />
    /// <summary>
    /// <see cref="T:Readymade.Machinery.EDBT.ITask" /> that ticks a delegate only if a given condition is true.
    /// </summary>
    /// <remarks>Similar to <see cref="T:Readymade.Machinery.EDBT.Guard" /> but specialized to invoke a delegate.</remarks>
    /// <seealso cref="T:Readymade.Machinery.EDBT.Guard" />
    public class Filter : Sequence
    {
        /// <inheritdoc />
        /// <summary>
        /// Create an instance of a <see cref="T:Readymade.Machinery.EDBT.Filter" /> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="filter">The filter delegate to check before invoking <paramref name="action" /> when the task is ticked.</param>
        /// <param name="action">The action to invoke when the task is ticked.</param>
        public Filter(string name, Func<bool> filter, Action action)
            : base(name, new Condition(filter), new Do(action))
        {
        }

        /// <inheritdoc/>
        public Filter(Func<bool> filter, Action action) : this(default, filter, action)
        {
        }
    }
}