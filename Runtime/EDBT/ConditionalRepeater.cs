using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A task that repeats its child while a given condition is true.
    /// </summary>
    public class ConditionalRepeater : DecoratorBase
    {
        /// <summary>
        /// storage for the assigned the condition.
        /// </summary>
        private readonly Func<bool> _condition;

        /// <summary>
        /// storage for the created condition task.
        /// </summary>
        private readonly Condition _conditionTask;

        /// <summary>
        /// The current remaining count of repeats.
        /// </summary>
        public int RemainingCount { get; private set; }

        /// <summary>
        /// The count when the task is (re-)started.
        /// </summary>
        public int InitialCount { get; }

        /// <summary>
        /// Create an instance of a <see cref="ConditionalRepeater"/> task.
        /// </summary>
        public ConditionalRepeater(string name, int count, Func<bool> condition, ITask child) : base(name, child)
        {
            _condition = condition;
            _conditionTask = new Condition(condition);
            InitialCount = count;
            RemainingCount = InitialCount;
        }

        /// <inheritdoc/>
        public ConditionalRepeater(Func<bool> condition, int count, ITask child) : this(default, count, condition, child)
        {
        }

        /// <inheritdoc/>
        public ConditionalRepeater(Func<bool> condition, ITask child) : this(default, 0, condition, child)
        {
        }

        /// <inheritdoc/>
        public ConditionalRepeater(string name, Func<bool> condition, ITask child) : this(name, 0, condition, child)
        {
        }

        /// <summary>
        /// Handler to be called when the child terminates which then facilitates the continuation (event pattern).
        /// </summary>
        protected void OnChildComplete(TaskState state)
        {
            if (InitialCount < 0)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(RemainingCount >= 0, "_currentCount >= 0");
            Debug.Assert(InitialCount >= 0, "_initialCount >= 0");

            // repeat forever
            if (InitialCount == 0)
            {
                Owner.Start(Child, OnChildComplete);
                return;
            }

            RemainingCount--;
            if (RemainingCount == 0)
            {
                ((ITask) this).Stop(TaskState.Success);
                return;
            }
            else
            {
                // repeat
                Owner.Start(Child, OnChildComplete);
                return;
            }
        }

        /// <summary>
        /// Handler to be called when the observer terminates which then facilitates the continuation (event pattern).
        /// </summary>
        private void OnObserverComplete(TaskState taskState)
        {
            if (taskState == TaskState.Failure)
            {
                Abort(AbortPolicy.Both);
            }
            else
            {
                Owner.Defer(_conditionTask, OnObserverComplete);
            }
        }

        /// <inheritdoc/>
        protected override void OnStarted()
        {
            if (InitialCount < 0)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(RemainingCount >= 0, "_currentCount >=0");
            Debug.Assert(InitialCount >= 0, "_initialCount >=0");

            RemainingCount = InitialCount;
            if (_condition?.Invoke() ?? false)
            {
                Owner.Defer(_conditionTask, OnObserverComplete);
                Owner.Start(Child, OnChildComplete);
            }
        }

        /// <inheritdoc/>
        protected override void OnReset()
        {
            RemainingCount = InitialCount;
        }

        /// <inheritdoc/>
        protected override void OnStopped(TaskState state)
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
            return TaskState.Running;
        }
    }
}