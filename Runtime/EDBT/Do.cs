using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// <see cref="ITask"/> that invokes a delegate and completes successfully.
    /// </summary>
    public class Do : TaskBase
    {
        /// <summary>
        /// Storage for the assigned delegate.
        /// </summary>
        private readonly Action _task;

        /// <summary>
        /// Create an instance of a <see cref="Do"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="task">The action to invoke when this task is ticked.</param>
        public Do(string name, Action task) : base(name)
        {
            _task = task;
        }

        /// <inheritdoc/>
        public Do(Action task) : this(default, task)
        {
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
            _task?.Invoke();
            return TaskState.Success;
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