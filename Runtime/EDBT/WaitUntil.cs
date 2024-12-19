using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// <see cref="ITask"/> that waits until a given condition is true.
    /// </summary>
    public class WaitUntil : TaskBase
    {
        /// <summary>
        /// Wait until the given condition becomes true.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="condition">The condition to check each tick.</param>
        public WaitUntil(string name, Func<bool> condition) : base(name)
        {
            Condition = condition;
        }

        /// <summary>
        /// The condition checked on each tick.
        /// </summary>
        public Func<bool> Condition { get; set; }

        /// <inheritdoc />
        protected override void OnAborted()
        {
        }

        /// <inheritdoc />
        protected override void OnResumed()
        {
        }

        /// <inheritdoc />
        protected override void OnSuspended()
        {
        }

        /// <inheritdoc />
        protected override TaskState OnTick()
        {
            if (Condition?.Invoke() ?? false)
            {
                return TaskState.Success;
            }
            else
            {
                return TaskState.Running;
            }
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
        }
    }
}