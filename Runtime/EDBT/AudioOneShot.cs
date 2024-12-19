using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Plays a one shot audio clip on an audio source.
    /// </summary>
    public class AudioOneShot : TaskBase
    {
        /// <summary>
        /// The <see cref="Animator"/> controlled by this task.
        /// </summary>
        private readonly AudioSource _source;

        /// <summary>
        /// cached initial state of the animator before this task was executed. Used to reset the enabled state.
        /// </summary>
        private bool _originalEnabledState;

        private readonly Func<AudioClip> _getClip;

        /// <summary>
        /// Create a new instance of a <see cref="AudioOneShot"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="source">The audio source to control with this task.</param>
        /// <param name="getClip">A delegate that returns the audio clip to play.</param>
        public AudioOneShot(string name, AudioSource source, Func<AudioClip> getClip) : base(name)
        {
            _getClip = getClip;
            _source = source;
        }

        /// <inheritdoc />
        public AudioOneShot(AudioSource source, Func<AudioClip> getClip) : this(default, source, getClip)
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
            AudioClip clip = _getClip();
            if (clip != default && _source)
            {
                _source.PlayOneShot(clip);
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