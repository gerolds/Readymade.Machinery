using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Simple action task that runs its delegate and completes successfully.
    /// </summary>
    public class AnimatorStop : TaskBase
    {
        /// <summary>
        /// the animator to control
        /// </summary>
        private readonly Animator _animator;

        /// <summary>
        /// cached initial state of the animator before this task was executed. Used to reset the enabled state.
        /// </summary>
        private bool _originalEnabledState;

        /// <summary>
        /// Create a new instance of a <see cref="AnimatorStop"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="animator">The animator to control with this task.</param>
        public AnimatorStop(string name, Animator animator) : base(default)
        {
            _animator = animator;
        }

        /// <inheritdoc/>
        public AnimatorStop(Animator animator) : base(default)
        {
            _animator = animator;
        }

        /// <inheritdoc/>
        protected override void OnAborted()
        {
            _animator.enabled = _originalEnabledState;
        }

        /// <inheritdoc/>
        protected override void OnResumed()
        {
            _animator.enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnSuspended()
        {
            _animator.enabled = _originalEnabledState;
        }

        /// <inheritdoc/>
        protected override TaskState OnTick()
        {
            return TaskState.Success;
        }

        /// <inheritdoc/>
        protected override void OnStarted()
        {
            _originalEnabledState = _animator.enabled;
            _animator.enabled = false;
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