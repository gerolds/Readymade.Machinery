using System;

namespace Readymade.Machinery.EDBT
{
    /// <inheritdoc />
    /// <summary>
    /// A Monitor ticks its sub-tasks and returns their state while the given condition is true or fails immediately otherwise. The condition is rechecked each tick.
    /// </summary>
    /// <remarks>This is not (yet) a true monitor as it does not continuously check the condition one each trick.</remarks>
    /// <seealso cref="T:Readymade.Machinery.EDBT.Filter" /><seealso cref="T:Readymade.Machinery.EDBT.Guard" /><seealso cref="T:Readymade.Machinery.EDBT.Parallel" />
    public class Monitor : Parallel
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a monitor task that ticks its child and returns its state while the given condition is true or fails immediately otherwise.
        /// </summary>
        /// <param name="condition">The condition to continuously check.</param>
        /// <param name="task">The task to execute while the condition is true.</param>
        public Monitor(Func<bool> condition, ITask task) : this(default, condition, task)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a monitor task that ticks its child and returns its state while the given condition is true or fails immediately otherwise.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="condition">The condition to continuously check.</param>
        /// <param name="task">The task to execute while the condition is true.</param>
        public Monitor(string name, Func<bool> condition, ITask task)
            : base(
                name,
                Policy.Sequence,
                new Condition(condition), // TODO this needs to be an active check
                task
            )
        {
        }
    }
}