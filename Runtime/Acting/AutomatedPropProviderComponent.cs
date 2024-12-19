using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Machinery.Shared;
using UnityEngine;
using UnityEngine.Serialization;
using Vertx.Debugging;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A <see cref="IProvider{TProp}"/> implemented as a <see cref="MonoBehaviour"/> so it can be configured and
    /// used in the Unity Editor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class AutomatedPropProviderComponent : MonoBehaviour, IProvider<SoProp>
    {
        private struct ProviderPackage
        {
            public float Timeout;
        }

        [InfoBox(
            "Use this component to make props accessible in the world. So long as the providers have a collider on the correct " +
            "layer, they will be discoverable by agents.")]
        [Tooltip("The prop provided by this component.")]
        [SerializeField]
        [Required]
        private SoProp _providedProp;

        [Tooltip("Whether this component has an infinite supply of props.")]
        [ShowIf(nameof(IsPropDefined))]
        [SerializeField]
        private bool _infiniteSupply = false;

        [Tooltip("The initial supply of props this component starts with.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(IsFinite), nameof(IsPropDefined))]
#else
        [ShowIf(EConditionOperator.And, nameof(IsFinite), nameof(IsPropDefined))]
#endif
        [SerializeField]
        [Min(0)]
        private int _initialQuantity = 0;

        [Tooltip("The time in seconds until new props are added to the supply of this component.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(IsFinite), nameof(IsPropDefined))]
#else
        [ShowIf(EConditionOperator.And, nameof(IsFinite), nameof(IsPropDefined))]
#endif
        [SerializeField]
        [Min(0)]
        private float _interval = 5f;

        [Tooltip("The count of props added each interval.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(IsReplenishing), nameof(IsPropDefined))]
#else
        [ShowIf(EConditionOperator.And, nameof(IsPropDefined), nameof(IsReplenishing))]
#endif
        [SerializeField]
        [Min(0)]
        private int _addPerInterval = 1;

        [SerializeField] private bool requirePosition = true;

        [SerializeField]
        [ShowIf(nameof(requirePosition))]
        private bool requireOrientation = true;

        [BoxGroup("Animation")]
        [SerializeField]
#if ODIN_INSPECTOR
        [ShowIf(nameof(requirePosition))]
#else
        [ShowIf(EConditionOperator.And, nameof(requirePosition))]
#endif
        private bool hasAnimation = false;

        [BoxGroup("Animation")]
        [SerializeField]
#if ODIN_INSPECTOR
        [ShowIf(nameof(requirePosition), nameof(hasAnimation))]
#else
        [ShowIf(EConditionOperator.And, nameof(requirePosition), nameof(hasAnimation))]
#endif
        [Tooltip("This trigger will be invoked on the actor when this prop is claimed.")]
        private string animationTrigger;

        [FormerlySerializedAs("thoughtView")]
        [SerializeField]
        [BoxGroup("View")]
        private ThoughtDisplay bubbleDisplay;

        [Tooltip("Invoked whenever a prop is claimed.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent _onClaimed;

        [Tooltip("Invoked whenever a prop claim is committed.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent _onClaimCommitted;

        [Tooltip("Invoked whenever a prop claim is cancelled.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent _onClaimCancelled;

        [SerializeField]
        [Tooltip("Whether to print debug messages.")]
        private bool _debug;

        [Tooltip("The current supply of props in this component.")]
#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
#else
        [ShowNativeProperty]
#endif
        private long Count => (!_infiniteSupply && _providedProp)
            ? _inventory?.GetAvailableCount(_providedProp) ?? -1
            : -1;

        [Tooltip("Time until the next interval will elapse.")]

#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
#else
        [ShowNativeProperty]
#endif
        private float NextInterval
        {
            get
            {
                if (Timing.Source != null)
                {
                    return Timing.Source.Time - _providerPackage.Timeout;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <inheritdoc />
        public event Action<Phase, (SoProp prop, long count, IActor claimant)> Modified;

        /// <inheritdoc />
        public Pose Pose => requirePosition
            ? new(
                position: transform.position,
                rotation: requireOrientation ? transform.rotation : default
            )
            : default;

        /// <inheritdoc />
        public bool HasPose => requirePosition;

        public bool HasAnimation => hasAnimation && requirePosition;

        public string AnimationState => animationTrigger;

        /// <inheritdoc />
        public bool DebugLog
        {
            get => _debug;
            set => _debug = value;
        }

        /// <summary>
        /// Whether this component provides an infinite supply of <see cref="ProvidedProp"/>.
        /// </summary>
        public bool HasInfiniteSupply => _infiniteSupply;

        /// <summary>
        /// Whether this component provides a finite supply of <see cref="ProvidedProp"/>.
        /// </summary>
        public bool IsFinite => !_infiniteSupply;

        /// <summary>
        /// Whether this component is repleneshing its supply of <see cref="ProvidedProp"/> over time.
        /// </summary>
        public bool IsReplenishing => !_infiniteSupply && _interval > 0;

        /// <summary>
        /// Whether this component has a valid <see cref="SoProp"/> instance defined in <see cref="ProvidedProp"/>.
        /// </summary>
        public bool IsPropDefined => _providedProp != null;

        /// <summary>
        /// The <see cref="SoProp"/> instance provided by this component.
        /// </summary>
        public SoProp ProvidedProp => _providedProp;

        /// <summary>
        /// The backing inventory of this provider. Only defined if this is a non-infinite provider.
        /// </summary>
        public IInventory<SoProp> Inventory => _inventory;

        private readonly Inventory _inventory = new();
        private InventoryPropProvider _inventoryProvider;
        private InfinitePropProvider _infiniteProvider;

        private ProviderPackage _providerPackage;

        /// <summary> Unity event. </summary>
        private void Awake()
        {
            Debug.Assert(_providedProp != null, "ASSERTION FAILED: providedProp != null", this);
            if (_providedProp)
            {
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary> Unity event. </summary>
        private void Start()
        {
            if (_providedProp)
            {
                Pose pose = new(transform.position, default);
                Func<Pose> poseGetter = () => PoseExtensions.PoseFrom(transform).AnyRotation();
                if (HasInfiniteSupply)
                {
                    _infiniteProvider = new InfinitePropProvider(poseGetter, _providedProp);
                    _infiniteProvider.Modified += UpdatedEventHandler;
                }
                else
                {
                    _inventoryProvider = new InventoryPropProvider(poseGetter, _inventory);
                    _inventoryProvider.Modified += UpdatedEventHandler;
                    if (_initialQuantity > 0)
                    {
                        _inventory.TryPut(_providedProp, _initialQuantity);
                    }
                }
            }
        }

        /// <summary> Unity event. </summary>
        private void OnEnable()
        {
            if (!bubbleDisplay)
            {
                return;
            }

            // toggle the display of the prop provided by this component
            if (_providedProp)
            {
                bubbleDisplay.SetVisible(true, _providedProp.IconSprite);
            }
            else
            {
                bubbleDisplay.SetVisible(false);
            }
        }

        /// <summary>Unity event. Tick the replenishing behaviour of this component, based on settings.</summary>
        private void Update()
        {
            if (_infiniteSupply)
            {
                return;
            }

            if (_interval <= 0)
            {
                return;
            }

            if (_providerPackage.Timeout < Timing.Source.Time)
            {
                _providerPackage.Timeout = Timing.Source.Time + _interval;
                if (_providedProp && _inventory.CanPut(_providedProp, _addPerInterval))
                {
                    _inventory.TryPut(_providedProp, _addPerInterval);
                }
            }
        }

        /// <summary> Unity event. </summary>
        private void OnDestroy()
        {
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_inventoryProvider != null)
            {
                _inventoryProvider.Modified -= UpdatedEventHandler;
                _inventoryProvider.Dispose();
            }

            if (_infiniteProvider != null)
            {
                _infiniteProvider.Modified -= UpdatedEventHandler;
            }
        }

        private void UpdatedEventHandler(Phase phase,
            (SoProp prop, long quantity, IActor claimant) args)
        {
            Modified?.Invoke(Phase.Claimed, args);
            switch (phase)
            {
                case Phase.Claimed:
                    _onClaimed.Invoke(new ProviderEventArgs
                    {
                        Prop = args.prop as SoProp,
                        Quantity = args.quantity,
                        Claimant = args.claimant as Component
                    });
                    break;
                case Phase.Committed:
                    _onClaimCommitted.Invoke(new ProviderEventArgs
                    {
                        Prop = args.prop as SoProp,
                        Quantity = args.quantity,
                        Claimant = args.claimant as Component
                    });
                    break;
                case Phase.Released:
                    _onClaimCancelled.Invoke(new ProviderEventArgs
                    {
                        Prop = args.prop as SoProp,
                        Quantity = args.quantity,
                        Claimant = args.claimant as Component
                    });
                    break;
                case Phase.Put:
                    break;
                case Phase.Set:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }

            _onClaimed.Invoke(new ProviderEventArgs
            {
                Prop = args.prop as SoProp,
                Quantity = args.quantity,
                Claimant = args.claimant as Component
            });
        }

        /// <inheritdoc />
        public bool TryClaimProp(
            [NotNull] SoProp prop,
            [NotNull] IActor actor,
            long quantity,
            out PropClaim<SoProp, IActor> claim
        )
        {
            if (quantity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Must be at least 1");
            }

            bool success = IsFinite
                ? _inventoryProvider.TryClaimProp(prop, actor, quantity, out claim)
                : _infiniteProvider.TryClaimProp(prop, actor, quantity, out claim);

            if (success)
            {
                if (_debug)
                {
                    Debug.Log(
                        $"[{GetType().GetNiceName()}] {name} responded to prop {prop.Name} claim by {actor.Name} with pose {claim.CommitPose.position}");
                }
            }
            else
            {
                if (_debug)
                {
                    Debug.LogWarning(
                        $"[{GetType().GetNiceName()}] {name} failed to provide prop {prop.Name} claimed by {actor.Name}.");
                }
            }

            return success;
        }

        /// <inheritdoc />
        public bool CanProvide(SoProp prop)
        {
            return IsFinite
                ? _inventoryProvider.CanProvide(prop)
                : _infiniteProvider.CanProvide(prop);
        }

        public IEnumerable<SoProp> ProvidedProps => IsFinite
            ? _inventoryProvider.ProvidedProps
            : _infiniteProvider.ProvidedProps;

        /// <inheritdoc />
        public object Pack() => throw new NotImplementedException();

        /// <inheritdoc />
        public void Unpack(object data) => throw new NotImplementedException();

        /// <summary>
        /// Increments the count of <see cref="ProvidedProp"/> this provider will have. Quantities below 1 will be ignored.
        /// </summary>
        /// <param name="quantity">The quantity by which to increment the prop count.</param>
        /// <seealso cref="TryClaimProp"/>
        public bool TryPut(int quantity = 1)
        {
            if (quantity < 1)
            {
                return false;
            }

            return _inventory.TryPut(_providedProp, quantity);
        }

        /// <summary> Unity event. No API behaviour. </summary>
        private void OnDrawGizmosSelected()
        {
            if (_providedProp != null)
            {
                D.raw(new Shape.Circle(transform.position, Vector3.up, _providedProp.DiscoveryRange));
                D.raw(new Shape.Text(transform.position, _providedProp.Name), Color.white, Color.black);
            }
            else
            {
                D.raw(new Shape.Text(transform.position, "Missing Prop"), Color.red, Color.white);
            }
        }

        /// <summary> Unity event. No API behaviour. </summary>
        private void OnDrawGizmos()
        {
            if (!_providedProp)
            {
                D.raw(new Shape.Text(transform.position, "Missing Prop"), Color.red, Color.white);
            }

            D.raw(new Shape.Circle(transform.position, Camera.main.transform.forward, .3f));
            D.raw(new Shape.Arrow(transform.position, transform.up * .5f));
        }

        /// <summary>
        /// Sets the inventory to the given count.
        /// </summary>
        /// <param name="count"></param> The count of props.
        public void SetInventoryCount(int count)
        {
            _inventory.TryPut(_providedProp, count);
        }
    }
}