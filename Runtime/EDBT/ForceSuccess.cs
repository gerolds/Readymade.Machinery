namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A <see cref="ITask"/> that always succeeds.
    /// </summary>
    /// <remarks>Useful for wrapping sequences that conditionally execute tasks but should not cause a whole branch to fail.</remarks>
    public class ForceSuccess : DecoratorBase
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new instance of a <see cref="T:Readymade.Machinery.EDBT.ForceSuccess" /> task.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="child">The child of this <see cref="T:Readymade.Machinery.EDBT.IDecorator" />.</param>
        public ForceSuccess(string name, ITask child) : base(name, child)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new instance of a <see cref="T:Readymade.Machinery.EDBT.ForceSuccess" /> task.
        /// </summary>
        /// <param name="child">The child of this <see cref="T:Readymade.Machinery.EDBT.IDecorator" />.</param>
        public ForceSuccess(ITask child) : base(default, child)
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
        protected override TaskState OnTick() => TaskState.Running;

        /// <inheritdoc />
        protected override void OnStarted()
        {
            Owner.Start(Child, OnChildComplete);
        }

        /// Handler to be called when the child terminates which then facilitates the continuation (event pattern).
        private void OnChildComplete(TaskState taskState)
        {
            ((ITask) this).Stop(TaskState.Success);
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