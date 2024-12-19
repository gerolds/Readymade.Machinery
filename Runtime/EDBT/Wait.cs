using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// <see cref="ITask"/> that waits until a timer has elapsed.
    /// </summary>
    public class Wait : WaitUntil
    {
        /// timestamp when the task was started.
        private float _started;

        /// the duration this task will wait before returning <see cref="TaskState.Success"/>.
        private TimeSpan _duration;

        /// <inheritdoc />
        /// <summary>
        /// Wait for a given time span.
        /// </summary>
        /// <param name="duration">The duration this task will wait before returning <see cref="TaskState.Success"/>.</param>
        public Wait(string name, TimeSpan duration) : base(name, default)
        {
            _duration = duration;
            Condition = IsTimeout;
        }

        /// <inheritdoc />
        /// <summary>
        /// Wait for N milliseconds.
        /// </summary>
        public Wait(string name, int milliseconds) : this(name, TimeSpan.FromMilliseconds(milliseconds))
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Wait for x seconds.
        /// </summary>
        public Wait(string name, float seconds) : this(name, TimeSpan.FromSeconds(seconds))
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Wait for a given time span.
        /// </summary>
        /// <param name="duration">The duration this task will wait before returning <see cref="TaskState.Success"/>.</param>
        public Wait(TimeSpan duration) : base(default, default)
        {
            _duration = duration;
            Condition = IsTimeout;
        }

        /// <inheritdoc />
        /// <summary>
        /// Wait for N milliseconds.
        /// </summary>
        public Wait(int milliseconds) : this(default, TimeSpan.FromMilliseconds(milliseconds))
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Wait for x seconds.
        /// </summary>
        public Wait(float seconds) : this(default, TimeSpan.FromSeconds(seconds))
        {
        }

        /// <summary>
        /// Has the timer run out?
        /// </summary>
        private bool IsTimeout() => (_started + _duration.Seconds) < Time.time;

        /// <inheritdoc />
        protected override void OnStarted()
        {
            _started = Time.time;
        }
    }
}