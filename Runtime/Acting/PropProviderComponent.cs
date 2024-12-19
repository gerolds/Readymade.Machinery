using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Acting;
using Readymade.Machinery.Shared;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using Vertx.Debugging;


namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A <see cref="IProvider{SoProp}"/> that exposes its backing <see cref="IInventory{SoProp}"/> implemented as a
    /// <see cref="MonoBehaviour"/> so they can be configured and used in the Unity Editor. This component is useful for
    /// simulating resource flow as its provided prop count can be dynamically affected from the outside.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PropProviderComponent : MonoBehaviour, IProvider<SoProp>
    {
        [InfoBox(
            "This component is a PropProvider that exposes its backing Inventory so it can be configured and used in the Unity Editor. " +
            "It is useful for simulating resource flow as its provided prop count can be dynamically affected from " +
            "the outside.")]
        [SerializeField]
        private bool useExternalInventory;

        [BoxGroup("External Inventory")]
        [ShowIf(nameof(useExternalInventory))]
        [SerializeField]
        [Required]
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
        [InlineButton(nameof(AddInventoryComponent), "Create", ShowIf = nameof(IsMissingInventory))]
#endif
        private InventoryComponent inventory;

#if ODIN_INSPECTOR
        private void AddInventoryComponent() => inventory = gameObject.AddComponent<InventoryComponent>();
        private bool IsMissingInventory => !inventory;
#endif

        [BoxGroup("Internal Inventory")]
        [Tooltip("The prop provided by this component.")]
        [SerializeField]
        [ShowIf(nameof(UseInternalInventory))]
        [Required]
        private SoProp providedProp;

        [BoxGroup("Internal Inventory")]
        [Tooltip("The initial supply of props this component starts with.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(UseInternalInventory))]
        [ShowIf(nameof(IsPropDefined))]
#else
        [ShowIf(EConditionOperator.And, nameof(UseInternalInventory), nameof(IsPropDefined))]
#endif
        [SerializeField]
        [Min(0)]
        private int initialQuantity = 0;

        [BoxGroup("Internal Inventory")]
        [Tooltip("The capacity of the inventory. Will be compared against the bulk of each prop.")]
#if ODIN_INSPECTOR
        [ShowIf(nameof(UseInternalInventory))]
        [ShowIf(nameof(IsPropDefined))]
#else
        [ShowIf(EConditionOperator.And, nameof(UseInternalInventory), nameof(IsPropDefined))]
#endif
        [SerializeField]
        [Min(0)]
        private int bulkCapacity = 0;

        [SerializeField] private bool requirePosition = true;

        [SerializeField]
        [ShowIf(nameof(requirePosition))]
        private bool requireOrientation = false;

        [Tooltip("Invoked whenever a prop is claimed.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent onClaimed;

        [Tooltip("Invoked whenever a prop claim is committed.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent onClaimCommitted;

        [FormerlySerializedAs("onClaimCancelled")]
        [Tooltip("Invoked whenever a prop claim is cancelled.")]
        [BoxGroup("Events")]
        [SerializeField]
        private ProviderUnityEvent onClaimReleased;

        [SerializeField]
        [Tooltip("Whether to print debug messages.")]
        private bool debug;

        private Inventory _internalInventory;
        private InventoryPropProvider _inventoryProvider;
        private float _timeout;

        /// <inheritdoc />
        public event Action<(SoProp prop, long quantity, IActor claimant)> Claimed;

        /// <inheritdoc />
        public event Action<(SoProp prop, long quantity, IActor claimant)> Committed;

        /// <inheritdoc />
        public event Action<(SoProp prop, long quantity, IActor claimant)> Cancelled;

        [Tooltip("The current supply of props in this component.")]
#if ODIN_INSPECTOR
        [ShowInInspector]
#else
        [ShowNativeProperty]
#endif
        private long Count => (providedProp)
            ? Inventory?.GetAvailableCount(providedProp) ?? -1
            : -1;

        public IInventory<SoProp> Inventory => useExternalInventory ? inventory : _internalInventory;
        public event Action<Phase, (SoProp prop, long count, IActor claimant)> Modified;

        private bool UseInternalInventory => !useExternalInventory;
        private bool UseExternalInventory => useExternalInventory;

        /// <inheritdoc />
        public Pose Pose => requirePosition
            ? new(
                position: transform.position,
                rotation: requireOrientation ? transform.rotation : default
            )
            : default;

        /// <inheritdoc />
        public bool HasPose => requirePosition;

        /// <inheritdoc />
        public bool DebugLog
        {
            get => debug;
            set => debug = value;
        }

        /// <summary>
        /// Whether this component has a valid <see cref="SoProp"/> instance defined in <see cref="ProvidedProp"/>.
        /// </summary>
        public bool IsPropDefined => providedProp != null;

        /// <summary> Unity event. </summary>
        private void Awake()
        {
            if (!useExternalInventory)
            {
                Debug.Assert(providedProp != null, "ASSERTION FAILED: providedProp != null", this);
                if (providedProp)
                {
                }
                else
                {
                    gameObject.SetActive(false);
                }

                _internalInventory = new Inventory(bulkCapacity > 0 ? bulkCapacity : long.MaxValue);
            }

            Debug.Assert(Inventory != null, "ASSERTION FAILED: Inventory != null", this);
            _inventoryProvider = new InventoryPropProvider(PoseGetter, Inventory);
            if (providedProp)
            {
                Inventory.ForcePut(providedProp, initialQuantity);
            }

            Debug.Assert(_inventoryProvider != null, "_inventoryProvider != null", this);
            _inventoryProvider.Modified += ProviderModifiedHandler;

            return;

            Pose PoseGetter()
            {
                Pose pose = new(transform.position, default);
                return PoseExtensions.PoseFrom(transform).AnyRotation();
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
                _inventoryProvider.Modified -= ProviderModifiedHandler;
                _inventoryProvider.Dispose();
            }
        }

        private void ProviderModifiedHandler(Phase phase,
            (SoProp prop, long quantity, IActor claimant) args)
        {
            switch (phase)
            {
                case Phase.Claimed:
                    onClaimed.Invoke(new ProviderEventArgs
                    {
                        Prop = args.prop,
                        Quantity = args.quantity,
                        Claimant = args.claimant as Component
                    });
                    break;
                case Phase.Committed:
                    onClaimCommitted.Invoke(new ProviderEventArgs
                    {
                        Prop = args.prop as SoProp,
                        Quantity = args.quantity,
                        Claimant = args.claimant as Component
                    });
                    break;
                case Phase.Released:
                    onClaimReleased.Invoke(new ProviderEventArgs
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

            Modified?.Invoke(phase, args);
        }

        /// <inheritdoc />
        /// <remarks>Does not implement a search so <paramref name="heuristic"/> will be ignored.</remarks>
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

            Debug.Assert(_inventoryProvider != null, "_inventoryProvider != null", this);

            bool success = _inventoryProvider.TryClaimProp(prop, actor, quantity, out claim);

            if (success)
            {
                if (debug)
                {
                    Debug.Log(
                        $"[{GetType().GetNiceName()}] {name} responded to prop {prop.Name} claim by {actor.Name} with pose {claim.CommitPose.position}");
                }
            }
            else
            {
                if (debug)
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
            Debug.Assert(_inventoryProvider != null, "_inventoryProvider != null", this);
            return _inventoryProvider?.CanProvide(prop) ?? false;
        }

        public IEnumerable<SoProp> ProvidedProps => _inventoryProvider.ProvidedProps;

        /// <summary>
        /// Increments the count of <see cref="ProvidedProp"/> this provider will have. Quantities below 1 will be ignored.
        /// </summary>
        /// <param name="quantity">The quantity by which to increment the prop count.</param>
        /// <seealso cref="TryClaimProp"/>
        /// <remarks>This is a convenience API useful for use with unity events. For script usage, directly accessing the
        /// <see cref="Inventory"/> is preferred.</remarks>
        public void Put(int quantity = 1)
        {
            if (quantity < 1)
            {
                return;
            }


            if (!Inventory.TryPut(providedProp, quantity))
            {
                Debug.LogWarning(
                    $"{nameof(PropProviderComponent)} Failed to put {providedProp} into inventory",
                    this);
            }
        }

        /// <summary> Unity event. No API behaviour.</summary>
        private void OnDrawGizmosSelected()
        {
            if (providedProp)
            {
                D.raw(new Shape.Circle(transform.position, Vector3.up, providedProp.DiscoveryRange));
                D.raw(new Shape.Text(transform.position, providedProp.Name), Color.white, Color.black);
            }
            else
            {
                D.raw(new Shape.Text(transform.position, "Missing Prop"), Color.red, Color.white);
            }
        }

        /// <summary> Unity event. No API behaviour. </summary>
        private void OnDrawGizmos()
        {
            if (!providedProp)
            {
                D.raw(new Shape.Text(transform.position, "Missing Prop"), Color.red, Color.white);
            }

            D.raw(new Shape.Circle(transform.position, transform.up, .5f));
            D.raw(new Shape.Arrow(transform.position, transform.up * .5f));
        }
    }
}