namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A task that suspends execution of the tree and awaits an external continuation signal.
    /// </summary>
    public class Suspend : TaskBase
    {
        /// <summary>
        /// Create a new instance of a <see cref="Suspend"/> task.
        /// </summary>
        /// <param name="name"></param>
        public Suspend(string name) : base(name)
        {
        }

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
            return TaskState.Running;
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
            Owner.Suspend(this);
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