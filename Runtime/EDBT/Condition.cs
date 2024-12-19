using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Identical to <see cref="TryDo"/> in functionality but with the convention that <see cref="Condition"/> does NOT produce side effects while <see cref="TryDo"/> would transform state outside this tasks tree.
    /// </summary>
    public class Condition : TaskBase
    {
        /// <summary>
        /// <see cref="ITask"/> that invokes a predicate and completes or fails based on its return value.
        /// </summary>
        private readonly Func<bool> _condition;

        /// <summary>
        /// Create a new <see cref="Condition"/> instance.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="condition">The delegate to invoke.</param>
        /// <param name="decorations">Decorators to modify the execution of this task.</param>
        public Condition(string name, Func<bool> condition, Decorations decorations = default) : base(name)
        {
            _condition = condition;
        }

        /// <summary>
        /// Create a new <see cref="Condition"/> instance.
        /// </summary>
        /// <param name="condition">The delegate to invoke.</param>
        /// <param name="decorations">Decorators to modify the execution of this task.</param>
        public Condition(Func<bool> condition, Decorations decorations = default) : this(default, condition)
        {
            _condition = condition;
        }

        /// <inheritdoc/>
        protected override void OnAborted()
        {
        }

        /// <inheritdoc/>
        protected override void OnResumed()
        {
        }

        /// <inheritdoc/>
        protected override void OnSuspended()
        {
        }

        /// <inheritdoc/>
        protected override TaskState OnTick()
        {
            bool value = (_condition?.Invoke() ?? false);
            if (value)
            {
                return TaskState.Success;
            }
            else
            {
                return TaskState.Failure;
            }
        }

        /// <inheritdoc/>
        protected override void OnStarted()
        {
        }

        /// <inheritdoc/>
        protected override void OnReset()
        {
        }

        /// <inheritdoc/>
        protected override void OnStopped(TaskState state)
        {
        }
    }
}