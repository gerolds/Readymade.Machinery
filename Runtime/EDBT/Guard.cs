using System;
using System.Diagnostics.CodeAnalysis;

namespace Readymade.Machinery.EDBT
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="T:Readymade.Machinery.EDBT.ITask" /> that ticks its sub-tree and returns its state if the given condition is true or succeeds immediately otherwise. The condition is not re-checked.
    /// </summary>
    /// <remarks>Similar to <see cref="T:Readymade.Machinery.EDBT.Filter" /> but can execute a subtree (<see cref="T:Readymade.Machinery.EDBT.ITask" />).</remarks>
    /// <seealso cref="T:Readymade.Machinery.EDBT.Filter" />
    public class Guard : Select
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new instance of a <see cref="T:Readymade.Machinery.EDBT.Guard" />.
        /// </summary>
        /// <param name="condition">the condition to check when the task is started.</param>
        /// <param name="task">The task to conditionally execute.</param>
        public Guard([NotNull] Func<bool> condition, [NotNull] ITask task)
            : this(default, condition, task)
        {
        }

        /// <inheritdoc/>
        public Guard(string name, [NotNull] Func<bool> condition, [NotNull] ITask task)
            : base(name, new Condition($"?{name}", () => !condition()), task)
        {
        }
    }
}