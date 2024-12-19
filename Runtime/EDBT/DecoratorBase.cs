using System.Diagnostics.CodeAnalysis;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Base class for all tasks with exactly one child.
    /// </summary>
    public abstract class DecoratorBase : TaskBase, IDecorator
    {
        /// <summary>
        /// The child of this decorator.
        /// </summary>
        public ITask Child { get; private set; }

        /// <summary>
        /// Create a new instance of <see cref="T:Readymade.Machinery.EDBT.DecoratorBase" /> task. 
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="child">The child for this <see cref="T:Readymade.Machinery.EDBT.IDecorator" />.</param>
        protected DecoratorBase(string name, [NotNull] ITask child) : base(name)
        {
            SetChild(child);
        }

        /// <summary>
        /// Create a new instance of <see cref="T:Readymade.Machinery.EDBT.DecoratorBase" /> task. 
        /// </summary>
        /// <param name="child">The child for this <see cref="T:Readymade.Machinery.EDBT.IDecorator" />.</param>
        protected DecoratorBase([NotNull] ITask child) : this(default, child)
        {
        }

        /// <inheritdoc />
        public virtual void AbortNotifyFromChild(AbortPolicy policy)
        {
            // the child is already aborted so there is nothing else left to abort
            // TODO: maybe implement up-stream propagation
        }

        /// <inheritdoc />
        protected override void OnAborted()
        {
            Child?.Abort();
        }

        protected override void OnReset()
        {
            Child?.Reset();
        }

        /// <summary>
        /// Sets the child of this task.
        /// </summary>
        private void SetChild([NotNull] ITask task)
        {
            ConfigureChild(task);
            Child = task;
        }

        /// Inject dependencies into a child task.
        private void ConfigureChild([NotNull] ITask task)
        {
            task.SetParent(this);
            task.SetOwner(Owner);
        }

        /// <summary>
        /// Stop this performance in a given state.
        /// </summary>
        /// <param name="stop">The state to stop this performance with.</param>
        protected void StopSelf(TaskState stop)
        {
            ((ITask) this).Stop(stop);
        }
    }
}