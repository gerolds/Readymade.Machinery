namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A <see cref="ITask"/> that always fails.
    /// </summary>
    /// <remarks>Useful for wrapping <see cref="Select"/> that should execute all child tasks, for example when implementing some sort of initializer.</remarks>
    public class ForceFailure : DecoratorBase
    {
        /// <summary>
        /// Create a new instance of a <see cref="ForceFailure"/> task.
        /// </summary>
        /// <param name="child">The child of this <see cref="IDecorator"/>.</param>
        public ForceFailure(ITask child) : this(default, child)
        {
        }


        /// <summary>
        /// Create a new instance of a <see cref="ForceFailure"/> task with a name.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="child">The child of this <see cref="IDecorator"/>.</param>
        public ForceFailure(string name, ITask child) : base(name, child)
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
            ((ITask) this).Stop(TaskState.Failure);
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
            Child.Abort();
        }
    }
}