using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Machinery.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Gesture handler component with <see cref="UnityEvent"/> callbacks for prototyping.
    /// </summary>
    /// <seealso cref="Gesture"/>
    /// <seealso cref="FunGesture"/>
    /// <seealso cref="IGestureHandler"/>
    public class GestureHandlerComponent : MonoBehaviour, IGestureHandler
    {
        /// <summary>
        /// Unity event to be invoked when the gesture is complete.
        /// </summary>
        [FormerlySerializedAs("_onComplete")]
        [SerializeField]
        [Tooltip("Called when the gesture has completed without failure.")]
        private UnityEvent onComplete;

        /// <summary>
        /// Unity event to be invoked when the gesture is ticked.
        /// </summary>
        [FormerlySerializedAs("_onTick")]
        [SerializeField]
        [Tooltip("Called when the gesture has been ticked.")]
        private UnityEvent onTick;

        /// <summary>
        /// Unity event to be invoked when the gesture is started.
        /// </summary>
        [FormerlySerializedAs("_onStarted")]
        [SerializeField]
        [Tooltip("Called when the gesture has started.")]
        private UnityEvent onStarted;

        /// <summary>
        /// Unity event to be invoked when the gesture is aborted or has has failed otherwise.
        /// </summary>
        [FormerlySerializedAs("_onFailed")]
        [SerializeField]
        [Tooltip("Called when the gesture has failed.")]
        private UnityEvent onFailed;

        /// <summary>
        /// The required prop for this gesture, leave empty if none.
        /// </summary>
        [FormerlySerializedAs("_prop")]
        [Tooltip("The required prop for this gesture, leave empty if none.")]
        [SerializeField]
        private PropCount prop;

        /// <summary>
        /// The interval between <see cref="onTick"/> invocations.
        /// </summary>
        /// <remarks>The unit used here depends on the time source set in <see cref="Timing.Source"/>. By default <see cref="UnityTimeSource"/> is used, so this value represents seconds.</remarks>
        [FormerlySerializedAs("_tickInterval")]
        [BoxGroup("Timing")]
        [InfoBox(
            "The unit used here depends on the time source set in Timing.Source. By default UnityTime is used, so in that case values in this component represent seconds.")]
        [SerializeField]
        [Min(0)]
        [Tooltip("The time interval between ticks on this gesture invocations.")]
        private float tickInterval;

        /// <summary>
        /// The max duration this gesture will keep running.
        /// </summary>
        /// <remarks>The unit used here depends on the time source set in <see cref="Timing.Source"/>. By default <see cref="UnityTimeSource"/> is used, so this value represents seconds.</remarks>
        [FormerlySerializedAs("_maxDuration")]
        [BoxGroup("Timing")]
        [SerializeField]
        [Min(0)]
        [Tooltip("The max duration this gesture will keep running.")]
        private float maxDuration;

        private IEqualityComparer<Pose> _comparer;
        [SerializeField] private float upVariance = 1f;
        [SerializeField] private float approachDistance = 1f;

        private void Awake()
        {
            _comparer = new PoseComparer(180f, approachDistance, upVariance);
        }

        /// <inheritdoc />
        public void OnComplete(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            onComplete.Invoke();
        }

        /// <inheritdoc />
        public void OnStart(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            onStarted.Invoke();
        }

        /// <inheritdoc />
        public void OnTick(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            if (CanTick(actor, performance, gesture))
            {
                onTick.Invoke();
            }
        }

        /// <inheritdoc />
        public void OnFailed(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            onFailed.Invoke();
        }

        /// <inheritdoc />
        public bool CanComplete(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            return gesture.SinceStarted > maxDuration;
        }

        /// <inheritdoc />
        public bool CanTick(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
        {
            return gesture.SinceTick > tickInterval;
        }

        /// <inheritdoc />
        public bool TryGetPose(out Pose pose)
        {
            pose = PoseExtensions.PoseFrom(transform).AnyRotation();
            return true;
        }

        /// <inheritdoc />
        public bool TryGetProp(out PropCount prop)
        {
            prop = this.prop;
            return this.prop != null;
        }

        public bool CheckPose(Pose pose) => TryGetPose(out var p) && _comparer.Equals(p, pose);
    }
}