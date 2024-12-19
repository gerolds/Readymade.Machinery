using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Executes all children at once and succeeds/fails based on the set <see cref="Policy"/>.
    /// </summary>
    /// <remarks>Since this is an event-based behaviour tree a <see cref="Parallel"/> task is not useful for implementing a
    /// monitor behaviour that continuously performs checks. This is because the event driven tree will only schedule leaf nodes
    /// that report to their immediate ancestor when complete. Inner nodes will therefore only update when their immediate
    /// children report completion.
    /// </remarks>
    public class Parallel : CompositeBase
    {
        /// <summary>
        /// how many children have failed
        /// </summary>
        private int _failureCount;

        /// <summary>
        /// how many children have succeeded
        /// </summary>
        private int _successCount;

        /// <summary>
        /// how many children have completed
        /// </summary>
        private int _completeCount;

        /// <summary>
        /// how many children are running
        /// </summary>
        private int _runningCount;

        /// <summary>
        /// Options to configure execution of a <see cref="Parallel"/> task.
        /// </summary>
        public enum Policy
        {
            /// <summary>
            /// All child tasks are executed until the first one succeeds, then all other children that have not yet completed
            /// are aborted. If all children fail, the <see cref="Parallel"/> will also fail.
            /// </summary>
            Select,

            /// <summary>
            /// All child tasks are executed until the last one succeeds. If any child fails, the <see cref="Parallel"/> will
            /// also fail which causes all other children that have not yet completed to be aborted.
            /// </summary>
            Sequence
        }

        /// <summary>
        /// The policy that decides when the task completes. <see cref="Policy"/>
        /// </summary>
        public Policy CompletionPolicy { get; set; }

        /// <summary>
        /// Create a new <see cref="Parallel"/> instance.
        /// </summary>
        /// <remarks>
        /// Children are not executed in separate threads. Technically they are executed sequentially but during the
        /// same tick.
        /// </remarks>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="policy">The condition that succeeds this task.</param>
        /// <param name="children">The children to execute and consider in the success/failure conditions.</param>
        public Parallel(string name, Policy policy = Policy.Select, params ITask[] children) : base(name, children)
        {
            CompletionPolicy = policy;
        }

        /// <summary>
        /// Create a new <see cref="Parallel"/> instance.
        /// </summary>
        public Parallel(Policy policy = Policy.Select, params ITask[] children) : this(default, policy, children)
        {
        }

        /// <summary>
        /// Create a new <see cref="Parallel"/> instance.
        /// </summary>
        public Parallel(params ITask[] children) : this(default, Policy.Select, children)
        {
        }

        /// <summary>
        /// Create a new <see cref="Parallel"/> instance.
        /// </summary>
        public Parallel() : this(default, Policy.Select)
        {
        }


        /// <inheritdoc />
        protected sealed override void OnResumed()
        {
        }

        /// <inheritdoc />
        protected sealed override void OnSuspended()
        {
        }

        /// <inheritdoc />
        protected sealed override TaskState OnTick() => TaskState.Running;

        /// Handler to be called when a child completes. Facilitates the continuation (event pattern).
        protected sealed override void OnChildComplete(TaskState state)
        {
            _runningCount--;
            _completeCount++;
            switch (state)
            {
                case TaskState.Success:
                    _successCount++;
                    break;
                case TaskState.Failure:
                    _failureCount++;
                    break;
            }

            if (state == TaskState.Success && CompletionPolicy == Policy.Select)
            {
                ((ITask)this).Stop(TaskState.Success);
                return;
            }
            else if (state == TaskState.Failure && CompletionPolicy == Policy.Sequence)
            {
                ((ITask)this).Stop(TaskState.Failure);
                return;
            }

            if (_completeCount == ChildCount)
            {
                switch (CompletionPolicy)
                {
                    case Policy.Sequence:
                        ((ITask)this).Stop(TaskState.Success);
                        break;
                    case Policy.Select:
                        ((ITask)this).Stop(TaskState.Failure);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <inheritdoc />
        protected sealed override void OnStarted()
        {
            _childIterator ??= Children.GetEnumerator();
            _childIterator.Reset();
            _successCount = 0;
            _failureCount = 0;

            while (_childIterator.MoveNext())
            {
                Owner.Start(_childIterator.Current, OnChildComplete);
            }
        }

        /// <inheritdoc />
        protected sealed override void OnReset()
        {
            base.OnReset();
            _childIterator?.Reset();
        }

        /// <inheritdoc />
        protected sealed override void OnStopped(TaskState state)
        {
            _childIterator.Reset();
            while (_childIterator.MoveNext())
            {
                if (_childIterator.Current?.TaskState == TaskState.Running)
                {
                    _childIterator.Current.Abort();
                }
            }
        }

        /// <inheritdoc />
        protected sealed override void OnAborted()
        {
            // propagate abort signal to all subtrees
            _childIterator?.Reset();
            while (_childIterator?.MoveNext() ?? false)
            {
                if (_childIterator?.Current?.IsActive ?? false)
                {
                    _childIterator?.Current.Abort();
                }
            }
        }
    }
}