#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using System;
using System.Collections.Generic;
using Readymade.Machinery.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Can be used to post a <see cref="IPerformance"/> for an <see cref="IActor"/> to complete.
    /// </summary>
    public abstract class PerformanceGenerator : MonoBehaviour
    {
        public abstract Director Director { get; }

        [FormerlySerializedAs("_enableQuickView")]
        [BoxGroup("View")]
        [SerializeField]
        [Tooltip("Whether to enable the quick view widget.")]
        private bool enableQuickView;

        [FormerlySerializedAs("_quickViewRoot")]
        [BoxGroup("View")]
        [ShowIf(nameof(enableQuickView))]
        [SerializeField]
        [Tooltip("The root object of the view widget, used to toggle the view on/off.")]
        private GameObject quickViewRoot;

        [FormerlySerializedAs("_fill")]
        [BoxGroup("View")]
        [ShowIf(nameof(enableQuickView))]
        [SerializeField]
        [Tooltip("Quick way to visualize this components state. Completely optional.")]
        private Image fill;

        [FormerlySerializedAs("_label")]
        [BoxGroup("View")]
        [ShowIf(nameof(enableQuickView))]
        [SerializeField]
        [Tooltip("A quick way to visualize this components name. Completely optional.")]
        private TMP_Text label;

        [FormerlySerializedAs("_displayName")]
        [BoxGroup("View")]
        [SerializeField]
        [Tooltip("A descriptive name for this object.")]
        private string displayName;

        [FormerlySerializedAs("_initialValueRange")]
        [BoxGroup("Behaviour")]
        [InfoBox(
            "The unit used here for timing values depends on the time source set in Timing.Source. By default UnityTime is used, so in that case values in this component represent seconds.")]
        [SerializeField]
        [Tooltip("The range of possible initial values for " + nameof(Value) + ".")]
        private Vector2 initialValueRange = new(0.5f, 1f);

        [FormerlySerializedAs("_decayDurationRange")]
        [BoxGroup("Behaviour")]
        [SerializeField, Min(0f)]
        [Tooltip("The range driving initial values for " + nameof(DecayRate) + ".")]
        private Vector2 decayDurationRange = new(30f, 60f);

        [BoxGroup("Behaviour")]
        [Tooltip("The value decays automatically, if set to true.")]
        [SerializeField]
        public bool decayEnabled;

        [FormerlySerializedAs("_decayInterval")]
        [SerializeField, MaxValue(10f), MinValue(0.05f)]
        [BoxGroup("Behaviour")]
        [Tooltip("The time between decay events.")]
        private float decayInterval = 0.5f;

        [BoxGroup("Behaviour")]
        [SerializeField]
        [Tooltip("Collection of directional events that can be triggered by this location.")]
        #if ODIN_INSPECTOR
        [ListDrawerSettings(ShowPaging = false, ShowFoldout = false)]
#else
        [ReorderableList]
#endif
        private List<ThresholdEvent> _thresholdEvents = new();

        [FormerlySerializedAs("_value")]
        [Tooltip("The current value driving this generator's events.")]
        [SerializeField, Range(0, 1f)]
        [BoxGroup("State")]
        private float value = 1f;

        private readonly Dictionary<ThresholdEvent, IPerformance<IActor>> _performances = new();
        private float _lastDecay;
        private Func<int, int> _getPriorityDelegate;

        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = value;
                if (label)
                {
                    label.text = displayName;
                }
            }
        }

        /// <summary>
        /// The rate at which <see cref="Value"/> decays automatically if enabled.
        /// </summary>
        public float DecayRate { get; set; }

        /// <summary>
        /// The current backing value driving the event triggers.
        /// </summary>
        public float Value => value;

        private void UpdateLabel()
        {
            if (label)
            {
                label.text = displayName;
            }
        }

        private void Awake()
        {
            // remove misconfigured events
            for (int i = _thresholdEvents.Count - 1; i >= 0; i--)
            {
                ThresholdEvent thresholdEvent = _thresholdEvents[i];
                if (thresholdEvent.Template == null)
                {
                    Debug.LogWarning(
                        $"[{GetType().GetNiceName()}]: Template of {nameof(ThresholdEvent)} is null, the event will be ignored",
                        this);
                    _thresholdEvents.RemoveAt(i);
                }
            }

            // create a performance for each event and cache them
            foreach (ThresholdEvent thresholdEvent in _thresholdEvents)
            {
                Debug.Assert(thresholdEvent.Template != null, "ASSERTION FAILED: thresholdEvent.Template != null",
                    this);
                _performances[thresholdEvent] = new Performance(
                    name, CreateGestureFromEvent(thresholdEvent)
                );
            }

            // setup initial state
            SetValue(Random.Range(initialValueRange.x, initialValueRange.y));
            DecayRate = 1f / Random.Range(decayDurationRange.x, decayDurationRange.y);
        }

        protected virtual void Start()
        {
            Debug.Assert(Director != null, "ASSERTION FAILED: Director != null");
        }

        private FunGesture CreateGestureFromEvent(ThresholdEvent thresholdEvent)
        {
            Func<Pose> pose = thresholdEvent.RequirePosition
                ? () => PoseExtensions.PoseFrom(transform.position)
                : default;

            Func<PropCount> prop = thresholdEvent.RequireProp != null
                ? () => thresholdEvent.RequireProp
                : default;

            FunGesture.Args args = new()
            {
                Name = $"{thresholdEvent.DisplayName} {name}",
                GetPose = pose,
                GetProp = prop,
                CanComplete = args => Value >= 1.0f || args.gesture.SinceStarted > thresholdEvent.Duration,
                CanTick = args => args.gesture.SinceTick >= thresholdEvent.TickInterval,
                OnStart = args =>
                {
                    Debug.Log(
                        $"[{GetType().GetNiceName()}] Actor '{args.actor.Name}' has started work on '{name}'");
                    thresholdEvent.onStarted.Invoke();
                    return true;
                },
                OnTick = args =>
                {
                    float oldValue = Value;
                    SetValue(Mathf.Min(Value + thresholdEvent.TickIncrement, 1.0f));
                    Debug.Log(
                        $"[{GetType().GetNiceName()}] Actor '{args.actor.Name}' has incremented '{nameof(Value)}' on '{name}' from {oldValue:f2} to {Value:f2}");
                    thresholdEvent.onTick.Invoke();
                    return true;
                },
                OnComplete = args =>
                {
                    Debug.Log(
                        $"[{GetType().GetNiceName()}] Actor '{args.actor.Name}' has completed work on '{name}'");
                    thresholdEvent.onCompleted.Invoke();
                },
                OnFailed = args =>
                {
                    Debug.Log(args.actor == default
                        ? $"[{GetType().GetNiceName()}] Work on '{name}' was aborted"
                        : $"[{GetType().GetNiceName()}] Actor '{args.actor.Name}' has aborted work on '{name}'");
                    thresholdEvent.onFailed.Invoke();
                }
            };
            FunGesture fun = new FunGesture(args);
            if (thresholdEvent.Template)
            {
                fun.Sprite = thresholdEvent.Template.Icon;
            }
            else
            {
                fun.Sprite = null;
            }

            return fun;
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            UpdateLabel();
        }

        /// <summary>
        /// Unity message.
        /// </summary>
        private void Update()
        {
            if (quickViewRoot)
            {
                quickViewRoot.SetActive(enableQuickView);
            }

            if (decayEnabled && _lastDecay + decayInterval < Timing.Source.Time)
            {
                _lastDecay = Timing.Source.Time;
                SetValue(Mathf.Max(0, Value - DecayRate * decayInterval));
            }
        }

        /// <summary>
        /// Set a new value and evaluate triggers based on the delta.
        /// </summary>
        /// <param name="value">The new value.</param>
        private void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            if (enableQuickView && fill)
            {
                fill.fillAmount = value;
            }

            bool shouldEvalEvents = this.value != value && _thresholdEvents.Count > 0;
            if (shouldEvalEvents)
            {
                bool isRising = value > this.value;
                foreach (ThresholdEvent tEvent in _thresholdEvents)
                {
                    bool isRisingTrigger = isRising &&
                        tEvent.TriggerMode is TriggerMode.Rising or TriggerMode.Both &&
                        this.value < tEvent.TriggerThreshold &&
                        value >= tEvent.TriggerThreshold;

                    bool isFallingTrigger = !isRising &&
                        tEvent.TriggerMode is TriggerMode.Falling or TriggerMode.Both &&
                        (
                            this.value > tEvent.TriggerThreshold && value <= tEvent.TriggerThreshold
                            ||
                            // reschedule if we must have passed the trigger before and haven't scheduled anything,
                            // which might happen if the scheduled performance failed to increment the value above
                            // the threshold. 
                            this.value < tEvent.TriggerThreshold &&
                            !Director.IsScheduled(tEvent.GetKey())
                        );

                    if (isRisingTrigger || isFallingTrigger)
                    {
                        OnTrigger(tEvent);
                    }
                }
            }

            this.value = value;
        }

        /// <summary>
        /// Schedules a <see cref="IPerformance{IActor}"/> based on a given <see cref="ThresholdEvent"/>.
        /// </summary>
        /// <param name="thresholdEvent">The <see cref="ThresholdEvent"/> that was triggered.</param>
        private void OnTrigger(ThresholdEvent thresholdEvent)
        {
            _performances[thresholdEvent].Reset();
            if (thresholdEvent.RequireActor != null)
            {
                Director.ScheduleFor(
                    thresholdEvent.GetKey(),
                    _performances[thresholdEvent],
                    thresholdEvent.RequireActor,
                    () => (int)(100 - value * 100)
                );
            }
            else
            {
                Director.Schedule(
                    thresholdEvent.GetKey(),
                    _performances[thresholdEvent],
                    roleID => GetPriorityForRole(roleID, thresholdEvent.RequireRole));
            }

            int GetPriorityForRole(int roleID, RoleMask mask)
            {
                // no role requirement
                if (mask == RoleMask.None)
                {
                    return 0;
                }

                // match role-ID against bitmask
                bool isMaskMatch = ((int)mask & (1 << roleID)) != 0;
                return isMaskMatch ? 1 : -1;
            }
        }

        /// <summary>
        /// Tick this location.
        /// </summary>
        /// <param name="delta">The total delta applied to <see cref="Value"/>. Can be out of range, negative and positive.</param>
        /// <returns>True, if <paramref name="delta"/> was negative and <see cref="Value"/> is now less or equal to zero
        /// <br/> <b>or</b> if <paramref name="delta"/> was positive and <see cref="Value"/> is now greater or equal to one.</returns>
        public bool Tick(float delta)
        {
            if (delta == 0f)
            {
                return false;
            }

            SetValue(Value + delta);
            return delta > 0f ? Value >= 1f : Value <= 0f;
        }
    }
}