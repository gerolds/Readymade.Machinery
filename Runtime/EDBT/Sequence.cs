using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A <see cref="T:Readymade.Machinery.EDBT.IComposite" /> task that succeeds when the all children succeed and fails if any child fails.
    /// Children are executed in the order they are added. <seealso cref="T:Readymade.Machinery.EDBT.IComposite" />
    /// </summary>
    public class Sequence : CompositeBase
    {
        /// <summary>
        /// Create a new instance of a <see cref="T:Readymade.Machinery.EDBT.Sequence" /> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="children">The child <see name="ITask" />s of this <see cref="T:Readymade.Machinery.EDBT.IComposite" />.</param>
        public Sequence(string name = null, params ITask[] children) : base(name, children)
        {
        }

        /// <inheritdoc/>
        public Sequence(params ITask[] children) : this(default, children)
        {
        }

        /// <inheritdoc/>
        public Sequence() : base(default, default)
        {
        }

        /// <inheritdoc/>
        protected sealed override void OnResumed()
        {
        }

        /// <inheritdoc/>
        protected sealed override void OnSuspended()
        {
        }

        /// <inheritdoc/>
        protected sealed override void OnStarted()
        {
            _childIterator ??= Children.GetEnumerator();
            _childIterator.Reset();
            _childIterator.MoveNext();

            Owner.Start(_childIterator.Current, OnChildComplete);
        }

        /// <inheritdoc/>
        protected sealed override void OnReset()
        {
            base.OnReset();
            _childIterator?.Reset();
        }

        /// <inheritdoc/>
        protected sealed override void OnStopped(TaskState state)
        {
        }

        /// <inheritdoc/>
        protected sealed override void OnAborted()
        {
            // propagate abort signal to the current subtree
            // only one child can be active in a selector
            if (_childIterator?.Current?.IsActive ?? false)
            {
                _childIterator?.Current?.Abort();
            }
        }

        /// <inheritdoc/>
        protected sealed override void OnChildComplete(TaskState state)
        {
            if (Current.TaskState == TaskState.Failure)
            {
                ((ITask) this).Stop(TaskState.Failure);
                return;
            }

            Debug.Assert(Current.TaskState == TaskState.Success);

            if (!_childIterator.MoveNext())
            {
                ((ITask) this).Stop(TaskState.Success);
            }
            else
            {
                Owner.Start(Current, OnChildComplete);
            }
        }
    }
}