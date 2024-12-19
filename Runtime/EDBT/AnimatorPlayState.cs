using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Puts an animator into a given state.
    /// </summary>
    public class AnimatorPlayState : TaskBase
    {
        /// <summary>
        /// Cached parameter name hash for faster access to the parameter.
        /// </summary>
        public int StateID { get; }

        /// <summary>
        /// The <see cref="Animator"/> controlled by this task.
        /// </summary>
        private readonly Animator _animator;

        /// <summary>
        /// cached initial state of the animator before this task was executed. Used to reset the enabled state.
        /// </summary>
        private bool _originalEnabledState;

        /// <summary>
        /// Create a new instance of a <see cref="AnimatorPlayState"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="animator">The animator to control with this task.</param>
        /// <param name="stateName">The name of the state to active in the <paramref name="animator"/>.</param>
        public AnimatorPlayState(string name, Animator animator, string stateName) : base(name)
        {
            StateID = Animator.StringToHash(stateName);
            _animator = animator;
        }

        /// <inheritdoc />
        public AnimatorPlayState(Animator animator, string stateName) : this(default, animator, stateName)
        {
        }

        /// <inheritdoc />
        protected override void OnAborted()
        {
            _animator.enabled = _originalEnabledState;
        }

        /// <inheritdoc />
        protected override void OnResumed()
        {
            _animator.enabled = true;
        }

        /// <inheritdoc />
        protected override void OnSuspended()
        {
            _animator.enabled = _originalEnabledState;
        }

        /// <inheritdoc />
        protected override TaskState OnTick()
        {
            return TaskState.Success;
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
            _originalEnabledState = _animator.enabled;
            _animator.enabled = true;
            _animator.Play(StateID);
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