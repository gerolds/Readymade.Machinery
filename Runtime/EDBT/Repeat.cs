using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A decorator task that repeats its child.
    /// </summary>
    public class Repeat : DecoratorBase
    {
        /// <summary>
        /// The current remaining count of repeats.
        /// </summary>
        public int RemainingCount { get; private set; }

        /// <summary>
        /// The count when the task is (re-)started.
        /// </summary>
        public int InitialCount { get; }

        /// <summary>
        /// A task that repeats its <paramref name="child"/> N times.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="count">How often the <paramref name="child"/> task should be repeated. A value of <i>0</i> repeats until aborted.</param>
        /// <param name="child">The <see cref="ITask"/> to repeat.</param>
        public Repeat(string name, int count, ITask child) : base(name, child)
        {
            InitialCount = count;
            RemainingCount = InitialCount;
        }

        /// <summary>
        /// A task that repeats its <paramref name="child"/> N times.
        /// </summary>
        /// <param name="count">How often the <paramref name="child"/> task should be repeated. A value of <i>0</i> repeats until aborted.</param>
        /// <param name="child">The <see cref="ITask"/> to repeat.</param>
        public Repeat(int count, ITask child) : this(default, count, child)
        {
        }

        /// <summary>
        /// A task that repeats its <paramref name="child"/> indefinitely until the task is aborted.
        /// </summary>
        /// <param name="child">The task to repeat.</param>
        public Repeat(ITask child) : this(default, 0, child)
        {
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
            if (InitialCount < 0)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(RemainingCount >= 0, "RemainingCount >= 0");
            Debug.Assert(InitialCount >= 0, "InitialCount >= 0");

            RemainingCount = InitialCount;
            Owner.Start(Child, OnChildComplete);
        }

        /// <summary>
        /// Handler to be called when the child terminates which then facilitates the continuation (event pattern).
        /// </summary>
        private void OnChildComplete(TaskState state)
        {
            if (InitialCount < 0)
            {
                throw new InvalidOperationException();
            }

            Debug.Assert(RemainingCount >= 0, "RemainingCount >= 0");
            Debug.Assert(InitialCount >= 0, "InitialCount >= 0");

            if (InitialCount == 0)
            {
                // repeat forever

                // we simply reset the child since it is already scheduled for next frame
                if (Child is not (CompositeBase or DecoratorBase))
                {
                    Child.Reset();
                    Owner.Defer(Child, OnChildComplete);
                }
                else
                {
                    Child.Reset();
                }
            }
            else
            {
                // repeat with count
                RemainingCount--;
                if (RemainingCount == 0)
                {
                    ((ITask) this).Stop(TaskState.Success);
                }
                else
                {
                    // we simply reset the child since it is already scheduled for next frame 

                    if (Child is not (CompositeBase or DecoratorBase))
                    {
                        Child.Reset();
                        Owner.Defer(Child, OnChildComplete);
                    }
                    else
                    {
                        Child.Reset();
                    }
                }
            }
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
    }
}