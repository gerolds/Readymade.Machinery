using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks.Triggers;
using Readymade.Machinery.Shared;
using Readymade.Utils.Patterns;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Use this component to keep the inventory at a desired count of props via scheduling procurement performances.
    /// </summary>
    public class InventoryBalancer : MonoBehaviour
    {
        [FormerlySerializedAs("inventory")]
        [Tooltip("The inventory that should be kept at the desired counts.")]
        [SerializeField]
        private InventoryComponent target;

        [Tooltip(
            "Surplus items are moved to this inventory. Attach a provider to this inventory to make the surplus available to other actors.")]
        [SerializeField]
        private InventoryComponent surplus;

        [Tooltip("The desired counts of props in the target inventory.")]
        [SerializeField]
        private PropCount[] desiredCounts;

        [SerializeField] private RoleMask requireRole;

        private IDirector _director;
        private ConveyorSystem _conveyorSystem;
        private IPropBroker<SoProp> _broker;
        private readonly Dictionary<SoProp, (long count, int handle)> _requests = new();
        private Dictionary<SoProp, long> _desiredCounts;
        private Queue<SoProp> _removeBuffer = new();
        private int _i;
        private Func<int, int> _getPriorityForRoleDelegate;
        private Performance _performance;
        private HashSet<Performance> _trackedPerformances = new();
        [SerializeField] private bool debug;
        [SerializeField] private float actorApproachDistance = 4f;
        [SerializeField] private float actorApproachUpVariation = 1f;
        private SpoofActor _spoofActor;
        private IEqualityComparer<Pose> _comparer;

        private void Awake()
        {
            _comparer = new PoseComparer(180f, actorApproachDistance, actorApproachUpVariation);
            _spoofActor = new SpoofActor(transform);
            if (_desiredCounts == null)
            {
                OverrideDesiredCounts(desiredCounts);
            }
        }

        private void Start()
        {
            _director = Services.Get<IDirector>();
            _broker = Services.Get<IPropBroker<SoProp>>();
            _conveyorSystem = Services.Get<ConveyorSystem>();
            _conveyorSystem.Register(this);
        }

        public void Tick(float deltaTime)
        {
            if (surplus)
            {
                UpdateSurplus();
            }

            CancelObsoleteRequests();
            IssueBalancingRequests();
        }

        /// <summary>
        /// Override the desired counts of props in the target inventory. Any in-progress requests will still be fulfilled. 
        /// </summary>
        public void OverrideDesiredCounts(PropCount[] counts)
        {
            _desiredCounts = desiredCounts.ToDictionary(it => it.Identity, it => it.Count);
        }

        private void UpdateSurplus()
        {
            foreach (var desired in desiredCounts)
            {
                long current = target.GetAvailableCount(desired.Identity);

                if (current > desired.Count)
                {
                    // make surplus available to other actors
                    surplus.ForcePut(desired.Identity, current - desired.Count);
                }
                else if (desired.Count > current)
                {
                    // move any unclaimed surplus back to the main inventory
                    long surplusCount = surplus.GetAvailableCount(desired.Identity);
                    if (surplusCount > 0)
                    {
                        long moveCount = Math.Min(desired.Count - current, surplusCount);
                        surplus.TryTakeImmediately(desired.Identity, moveCount);
                        target.ForcePut(desired.Identity, moveCount);
                    }
                }
            }
        }

        private void IssueBalancingRequests()
        {
            foreach (var desired in desiredCounts)
            {
                bool hasOpenRequest = _requests.TryGetValue(desired.Identity, out (long count, int handle) openRequest);
                long current = target.GetAvailableCount(desired.Identity);
                long afterRequests = current + openRequest.count;
                long missingAfterRequests = desired.Count - afterRequests;
                long currentlyMissing = desired.Count - current;
                if (missingAfterRequests > 0)
                {
                    if (hasOpenRequest && _director.IsClaimed(openRequest.handle))
                    {
                        // do nothing when a request is already being delivered
                    }
                    else
                    {
                        // cancel any existing request and make a new one with the updated count
                        if (hasOpenRequest)
                        {
                            _director.CancelWithoutNotify(openRequest.handle);
                        }

                        bool providerExists = _broker.TryFindProp(
                            desired.Identity,
                            _spoofActor,
                            currentlyMissing,
                            SpatialHeuristic.Greedy
                        );
                        if (providerExists)
                        {
                            int handle = Procure(new PropCount(desired.Identity, currentlyMissing));
                            _requests[desired.Identity] = (count: currentlyMissing, handle: handle);
                        }
                    }
                }
            }
        }

        private void CancelObsoleteRequests()
        {
            List<SoProp> buffer = ListPool<SoProp>.Get();
            foreach (var request in _requests)
            {
                if (!_desiredCounts.ContainsKey(request.Key) && !_director.IsClaimed(request.Value.handle))
                {
                    _director.CancelWithoutNotify(request.Value.handle);
                    buffer.Add(request.Key);
                }
            }

            foreach (var item in buffer)
            {
                _requests.Remove(item);
            }

            ListPool<SoProp>.Release(buffer);
        }

        private int Procure(PropCount args)
        {
            var pose = PoseExtensions.PoseFrom(transform).AnyRotation();
            Performance performance = new Performance(
                $"Get {args} to {pose.position}",
                new FunGesture(new FunGesture.Args
                    {
                        GetPose = () => pose,
                        GetProp = () => args,
                        OnComplete = null,
                        OnTick = null,
                        Comparer = _comparer,
                        OnStart = context =>
                        {
                            // handle situation where the target/self/surplus inventories are destroyed
                            if (!this || !target || !surplus)
                            {
                                return false;
                            }

                            _requests.Remove(context.gesture.Prop.Identity);

                            bool isPut = target.TryPut(context.gesture.Prop.Identity, context.gesture.Prop.Count);
                            Debug.Assert(isPut, "Put success.", this);

                            Debug.Assert(
                               target.GetAvailableCount(context.gesture.Prop.Identity) >=
                                context.gesture.Prop.Count, "Target has at least the requested count of props in its inventory.", this);
                            if (debug)
                            {
                                Debug.Log(
                                    $"[{nameof(InventoryBalancer)}] Actor {context.actor.Name} has delivered {context.gesture.Prop} to {pose.position}.",
                                    this);
                            }


                            return true;
                        },
                        OnFailed = context =>
                        {
                            // handle situation where this component is destroyed
                            if (this)
                            {
                                _requests.Remove(context.gesture.Prop.Identity);
                            }

                            Debug.LogWarning(
                                $"[{nameof(InventoryBalancer)}] Actor {context.actor.Name} failed to complete gesture; " +
                                $"Expected prop {context.gesture.Prop} at {context.gesture.Pose.position}; " +
                                $"Actual prop {context.actor.Inventory.GetAvailableCount(context.gesture.Prop.Identity)} at {context.actor.Pose.position} ({context.actor.Pose.DistanceTo(context.gesture.Pose):F1} away).",
                                this);
                        },
                        CanComplete = null,
                        CanTick = null,
                        Name = $"Get {args}"
                    }
                )
            );

            // keep track of the performance, so we can cancel it when this component is destroyed
            performance.Completed += CancelTracking;
            performance.Failed += CancelTracking;
            _trackedPerformances.Add(performance);

            int key = performance.GetHashCode() ^ args.Identity.GetHashCode();
            _getPriorityForRoleDelegate ??= GetPriorityForRole;
            _director.Schedule(key, performance, _getPriorityForRoleDelegate);
            return key;
        }

        private void CancelTracking(Performance performance, IActor _)
        {
            _trackedPerformances.Remove(performance);
        }

        private int GetPriorityForRole(int roleID) => requireRole.MatchRoleID(roleID);

        private void OnDestroy()
        {
            // when we are destroyed, we need to cancel all in-progress performances since they have us as a target
            // and cannot be executed successfully anymore. 
            Debug.Log($"[{nameof(InventoryBalancer)}] Destroyed; canceling {_trackedPerformances} performances", this);
            foreach (Performance performance in _trackedPerformances)
            {
                performance.Completed -= CancelTracking;
                performance.Failed -= CancelTracking;
                performance.Cancel();
            }

            _trackedPerformances.Clear();

            if (_conveyorSystem)
            {
                _conveyorSystem.UnRegister(this);
            }
        }
    }

    public class SpoofActor : IActor
    {
        private readonly Transform _transform;

        public SpoofActor(Transform transform)
        {
            _transform = transform;
            Pose = PoseExtensions.PoseFrom(transform);
            Name = "Spoof";
        }

        public Guid EntityID { get; } = Guid.NewGuid();
        public GameObject GetObject() => _transform.gameObject;
        public Pose Pose { get; }
        public IInventory<SoProp> Inventory => throw new NotImplementedException();
        public IEquipment<SoSlot, SoProp> Equipment => throw new NotImplementedException();
        public string Name { get; }
        public IPropConsumer Consumer => throw new NotImplementedException();

        public void OnFx(ActorFx fx)
        {
        }
    }
}