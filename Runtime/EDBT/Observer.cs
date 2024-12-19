using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// <para><see cref="ITask"/> that invokes an async delegate continuously.</para> 
    /// <para>Use this to implement a continuous update.</para>
    /// </summary>
    public class Observer : TaskBase
    {
        /// <summary>
        /// Storage for the assigned delegate.
        /// </summary>
        private readonly Func<bool> _task;

        private Policy _policy;

        /// <summary>
        /// Create an instance of a <see cref="Do"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="shouldRepeat">The action to invoke when this task is ticked. Return value indicates whether the action should be repeated or not.</param>
        /// <param name="policy"></param>
        public Observer(string name, Func<bool> shouldRepeat, Policy policy = Policy.RunToFailure) : base(name)
        {
            _policy = policy;
            _task = shouldRepeat;
        }

        public enum Policy
        {
            RunToFailure,
            RunToSuccess
        }

        /// <inheritdoc/>
        public Observer(Func<bool> condition) : this(default, condition)
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
            return _task.Invoke()
                ? TaskState.Running
                : _policy switch
                {
                    Policy.RunToFailure => TaskState.Failure,
                    Policy.RunToSuccess => TaskState.Success,
                    _ => throw new ArgumentOutOfRangeException()
                };
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