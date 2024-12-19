using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Sets a property of an animator.
    /// </summary>
    /// <typeparam name="T">The type of value to set on the <see cref="Animator"/>.</typeparam>
    public class AnimatorSetTrigger : TaskBase
    {
        /// <summary>
        /// The <see cref="Animator"/> controlled by this task.
        /// </summary>
        private readonly Animator _animator;

        /// <summary>
        /// cached initial state of the animator before this task was executed. Used to reset the enabled state.
        /// </summary>
        private bool _originalEnabledState;

        private readonly Func<int> _getID;

        /// <summary>
        /// Create a new instance of a <see cref="AnimatorSetValue{T}"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="animator">The animator to control with this task.</param>
        /// <param name="getParameterID">A delegate that returns the animation trigger ID to set.</param>
        public AnimatorSetTrigger(string name, Animator animator, Func<int> getParameterID) : base(name)
        {
            _getID = getParameterID;
            _animator = animator;
        }

        public AnimatorSetTrigger(string name, Animator animator, int parameterID) : this(name, animator,
            () => parameterID)
        {
        }

        /// <inheritdoc />
        public AnimatorSetTrigger(Animator animator, Func<int> getParameterID) : this(default, animator, getParameterID)
        {
        }

        /// <inheritdoc />
        public AnimatorSetTrigger(Animator animator, int parameterID) : this(default, animator, () => parameterID)
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
            return TaskState.Success;
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
            int id = _getID();
            if (id != default && _animator)
            {
                _animator.SetTrigger(id);
            }
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