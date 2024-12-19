namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A <see cref="IComposite"/> that succeeds when the first child succeeds and fails if all children fail. Children
    /// are executed in the order they are added. 
    /// </summary>
    public class Select : CompositeBase
    {
        /// <summary>
        /// Create a new instance of a <see cref="Select"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="children">The children of this task.</param>
        public Select(string name = null, params ITask[] children) : base(name, children)
        {
        }

        /// <inheritdoc />
        public Select(params ITask[] children) : this(default, children)
        {
        }

        /// <inheritdoc />
        public Select() : this(name: default, children: default)
        {
        }

        /// <inheritdoc />
        protected sealed override void OnChildComplete(TaskState state)
        {
            if (Current.TaskState == TaskState.Success)
            {
                ((ITask) this).Stop(TaskState.Success);
                return;
            }

            if (!_childIterator.MoveNext())
            {
                ((ITask) this).Stop(TaskState.Failure);
            }
            else
            {
                Owner.Start(_childIterator.Current, OnChildComplete);
            }
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
        protected override void OnStarted()
        {
            _childIterator ??= Children.GetEnumerator();
            _childIterator.Reset();
            _childIterator.MoveNext();
            Owner.Start(_childIterator.Current, OnChildComplete);
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
            base.OnReset();
            _childIterator?.Reset();
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
        }

        /// <inheritdoc />
        protected override void OnAborted()
        {
            // propagate abort signal to the current subtree
            // only one child can be active in a selector
            if (_childIterator?.Current?.IsActive ?? false)
            {
                _childIterator?.Current.Abort();
            }
        }
    }
}