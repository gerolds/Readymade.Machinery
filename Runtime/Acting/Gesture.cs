using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Shared;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A gesture that that takes a handler reference of type <see cref="IGestureHandler"/> which implements the
    /// callbacks of the <see cref="IGesture{TActor}" /> execution. Provided as an alternative
    /// to <see cref="FunGesture"/>.
    /// </summary>
    /// <seealso cref="FunGesture"/>
    /// <seealso cref="IGestureHandler"/>
    public sealed class Gesture : IGesture<IActor>
    {
        /// <summary>
        /// The configuration of this gesture.
        /// </summary>
        private readonly IGestureHandler _handler;

        /// <summary>
        /// Time this gesture was started.
        /// </summary>
        private float _started;

        /// <summary>
        /// Time when this gesture was last ticked.
        /// </summary>
        private float _lastTick;

        /// <summary>
        /// Creates a new instance of a gesture with given configuration.
        /// </summary>
        /// <param name="handler">The handler for this gesture.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="handler"/> is null.</exception>
        public Gesture([NotNull] IGestureHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc />
        public bool IsPoseRequired => _handler.TryGetPose(out _);

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public Pose Pose
        {
            get
            {
                _handler.TryGetPose(out Pose pose);
                return pose;
            }
        }

        /// <inheritdoc />
        public PropCount Prop
        {
            get
            {
                _handler.TryGetProp(out PropCount prop);
                return prop;
            }
        }

        /// <inheritdoc />
        public bool IsPropRequired => _handler.TryGetProp(out _);

        /// <inheritdoc />
        public float SinceStarted => Timing.Source.Time - _started;

        /// <inheritdoc />
        public float SinceTick => _lastTick;

        /// <inheritdoc />
        public float Timeout { get; set; }

        /// <inheritdoc />
        public IGesture.RunPhase Phase { get; private set; }

        /// <inheritdoc />
        void IGesture.Reset()
        {
            _lastTick = 0;
            _started = 0;
        }

        /// <inheritdoc />
        bool IGesture<IActor>.TryStart(IActor actor, IPerformance<IActor> performance)
        {
            if (_handler.TryGetPose(out Pose requiredPose) &&
                !PoseComparer.Default.Equals(actor.Pose, requiredPose))
            {
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Failed to start, the actor's pose did not match the gesture's required pose.");
                return false;
            }

            if (_handler.TryGetProp(out PropCount requiredProp) &&
                actor.Inventory.GetAvailableCount(requiredProp.Identity) < requiredProp.Count)
            {
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Failed to start, required token was not in the actor's inventory.");
                return false;
            }

            if (requiredProp != default)
            {
                actor.Inventory.TryTakeImmediately(requiredProp.Identity, requiredProp.Count);
            }

            _started = Timing.Source.Time;
            _handler.OnStart(actor, performance, this);
            return true;
        }

        /// <inheritdoc />
        void IGesture<IActor>.Abort(IActor actor, IPerformance<IActor> performance)
        {
            Debug.Log($"[{GetType().GetNiceName()}] Gesture aborted.");
            _handler.OnFailed(actor, performance, this);
        }

        /// <inheritdoc />
        bool IGesture<IActor>.Tick(IActor actor, IPerformance<IActor> performance)
        {
            if (_handler.CanTick(actor, performance, this))
            {
                _handler.OnTick(actor, performance, this);
                _lastTick = Timing.Source.Time;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public Sprite Sprite { get; set; }

        /// <inheritdoc />
        public bool CheckPose(Pose pose) => _handler.CheckPose(pose);

        /// <inheritdoc />
        bool IGesture<IActor>.TryComplete(IActor actor, IPerformance<IActor> performance)
        {
            if (!PoseComparer.Default.Equals(actor.Pose, Pose))
            {
                Debug.Log($"[{GetType().GetNiceName()}] Failed to complete gesture, poses did not match.");
                return false;
            }

            if (!_handler.CanComplete(actor, performance, this))
            {
                Debug.Log($"[{GetType().GetNiceName()}] Failed to complete gesture, completion condition was not met.");
                return false;
            }

            _handler.CanComplete(actor, performance, this);
            return true;
        }
    }
}