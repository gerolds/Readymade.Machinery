using System;
using System.Collections.Generic;
using System.Linq;
using Readymade.Persistence;
using Readymade.Utils.Patterns;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using UnityEngine;
using UnityEngine.Assertions;

namespace Readymade.Machinery.Acting
{
    public class InventoryComponent : PackableComponent<InventoryComponent.Memento>, IInventory<SoProp>
    {
        [Serializable]
        public struct Memento
        {
            [Serializable]
            public struct Flow
            {
                public Guid Prop;
                public long Pressure;
                public long In;
                public long Out;
            }

            [Serializable]
            public struct PropCount
            {
                public Guid Prop;
                public long Count;
            }

            [Serializable]
            public struct PropClaim
            {
                public Guid Prop;
                public int Handle;
                public long Count;
            }

            public PropCount[] Claimed;
            public PropCount[] Unclaimed;
            public PropClaim[] Claims;
            public Flow[] Flows;
            public int LastClaimHandle;
            public int LastSubscriberHandle;
            public long TotalCapacity;
        }

        [SerializeField] private string displayName;

        [SerializeField] private long capacity;

#if ODIN_INSPECTOR
        [TableList(AlwaysExpanded = true, ShowPaging = false)]
#else
        [ReorderableList]
#endif
        [SerializeField]
        private PropCount[] props;

        [SerializeField] private AssetLookup lookup;

        private AssetLookup _lookup;

        private readonly SortedDictionary<SoProp, long> _unclaimed = new();
        private readonly SortedDictionary<SoProp, long> _claimed = new();
        private readonly Dictionary<int, PropCount> _claims = new();
        private readonly Dictionary<SoProp, (long Pressure, long FlowIn, long FlowOut)> _flows = new();
        private readonly Dictionary<int, SoProp> _handleLookup = new();

        private readonly Dictionary<SoProp, Dictionary<int, IInventory<SoProp>.InventoryEventHandler>> _subscribers =
            new();

        private long _availableCapacity;
        private long _storedBulk;

        // A monotonic serial number
        private int _claimHandleID = 0;

        // A monotonic serial number
        private int _subscriberHandle;

        /// <inheritdoc />
        public string DisplayName => displayName;

        /// <inheritdoc />
        public event IInventory<SoProp>.InventoryEventHandler Modified;

        /// <inheritdoc />
        public bool TryGetFlow(SoProp prop, out (long pressure, long inFlow, long outFlow) flow) =>
            _flows.TryGetValue(prop, out flow);

        /// <inheritdoc />
        /// <remarks>This is a lazily evaluated enumerable backed by a dictionary. It allocates an enumerator.</remarks>
        public IEnumerable<(SoProp Prop, long Count)> Unclaimed => _unclaimed.Select(it => (it.Key, it.Value));

        /// <inheritdoc />
        public IEnumerable<(SoProp Prop, long Count)> Claimed => _claimed.Select(it => (it.Key, it.Value));

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, long> UnclaimedRaw => _unclaimed;

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, long> ClaimedRaw => _claimed;

        /// <inheritdoc />
        public bool IsEmpty => (_unclaimed.Count == 0 || _unclaimed.Values.All(it => it == 0)) && _claims.Count == 0;

        /// <inheritdoc />
        public IEnumerable<(int Handle, SoProp Prop, long Count )> Claims =>
            _claims.Select(it => (it.Key, it.Value.Identity, it.Value.Count));

        /// <inheritdoc />
        public IEnumerable<(SoProp Prop, long Pressure, long In, long Out )> Flows =>
            _flows.Select(it => (it.Key, it.Value.Pressure, it.Value.FlowIn, it.Value.FlowOut));

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, (long Pressure, long In, long Out )> FlowsRaw => _flows;

        /// <inheritdoc />
#if ODIN_INSPECTOR
        [ShowInInspector]
        [ReadOnly]
#else
        [ShowNativeProperty]
#endif
        public long TotalCapacity
        {
            get => capacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be a positive value.");
                }

                long delta = value - capacity;
                capacity = value;
                _availableCapacity += delta;
            }
        }

        /// <inheritdoc />
#if ODIN_INSPECTOR
        [ShowInInspector]
        [ReadOnly]
#else
        [ShowNativeProperty]
#endif
        public long AvailableCapacity => _availableCapacity;

        /// <inheritdoc />
#if ODIN_INSPECTOR
        [ShowInInspector]
        [ReadOnly]
#else
        [ShowNativeProperty]
#endif
        public long StoredBulk => _storedBulk;

        private void Awake()
        {
            Debug.Assert(capacity > 0, "Capacity should be greater than 0", this);
            _lookup = lookup;
            _availableCapacity = capacity;
            _storedBulk = 0;
            _unclaimed.Clear();
            foreach (var initialItem in props)
            {
                _unclaimed.Add(initialItem.Identity, initialItem.Count);
                _availableCapacity -= initialItem.Count * initialItem.Identity.Bulk;
                _storedBulk += initialItem.Count * initialItem.Identity.Bulk;
            }

            EnsureCapacityBounds();
        }

        private void Start()
        {
            if (!_lookup)
            {
                // the lookup is required to resolve SoProp objects from their GUIDs
                _lookup = Services.Get<AssetLookup>();
            }

            EnsureCapacityBounds();
        }

        private void Reset()
        {
            displayName = name;
        }

        /// <inheritdoc />
        public bool TryPut(SoProp prop, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Must be a value > 0 ({prop.name})");
            }

            if (CanPut(prop, count))
            {
                ForcePut(prop, count);
                return true;
            }
            else
            {
                _flows.TryGetValue(prop, out (long pressure, long flowIn, long flowOut) flow);
                _flows[prop] = (flow.pressure + count, flow.flowIn, flow.flowOut);
                return false;
            }
        }

        public IInventory<SoProp>.InventoryEventArgs CreateEventArgs(SoProp prop, long delta) =>
            new(
                inventory: this,
                item: prop,
                delta: delta,
                claimed: _claimed.GetValueOrDefault(prop, 0),
                available: _unclaimed.GetValueOrDefault(prop, 0)
            );

        /// <inheritdoc />
        public void ForcePut(SoProp prop, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a value > 0");
            }

            _unclaimed.TryGetValue(prop, out long oldCount);
            _unclaimed[prop] = oldCount + count;
            long bulk = count * prop.Bulk;
            _availableCapacity -= bulk;
            _storedBulk += bulk;
            _flows.TryGetValue(prop, out (long Pressure, long FlowIn, long FlowOut) flow);
            _flows[prop] = (flow.Pressure, flow.FlowIn + count, flow.FlowOut);
            Debug.Assert(_storedBulk >= count * prop.Bulk, "_storedBulk >= count * prop.Bulk");
            EnsureCapacityBounds();
            var newCount = _unclaimed[prop];
            Modified?.Invoke(Phase.Put, CreateEventArgs(prop, newCount - oldCount));
        }

        /// <inheritdoc />
        public void ForceSet(SoProp prop, long count)
        {
            _unclaimed.TryGetValue(prop, out long oldCount);
            ForceSetWithoutNotify(prop, count);
            EnsureCapacityBounds();
            long newCount = _unclaimed[prop];
            Modified?.Invoke(Phase.Set, CreateEventArgs(prop, newCount - oldCount));
        }

        /// <inheritdoc />
        public void ForceSetWithoutNotify(SoProp prop, long count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a positive value");
            }

            long oldCount = _unclaimed.GetValueOrDefault(prop);
            _storedBulk -= prop.Bulk * oldCount;
            _availableCapacity += prop.Bulk * oldCount;
            _unclaimed[prop] = count;
            _storedBulk += count * prop.Bulk;
            _availableCapacity -= count - prop.Bulk;
            EnsureCapacityBounds();
        }

        /// <inheritdoc />
        public bool CanPut(SoProp item, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a value > 0");
            }

            return _availableCapacity >= item.Bulk * MathF.Max(count, 0);
        }


        /// <inheritdoc />
        public long GetAvailableCount(SoProp prop) => gameObject.scene != default
            ? _unclaimed.GetValueOrDefault(prop, 0)
            : props.FirstOrDefault(it => it.Identity == prop).Count;

        /// <inheritdoc />
        public long GetClaimedCount(int handle) =>
            _claims.TryGetValue(handle, out PropCount claim) ? claim.Count : 0;

        /// <inheritdoc />
        public bool TryGetClaimCount(int handle, out long count)
        {
            if (handle < 1)
            {
                count = 0;
                return false;
            }

            if (_claims.TryGetValue(handle, out PropCount claim) && claim.Count > 0)
            {
                count = claim.Count;
                return true;
            }

            count = 0;
            return false;
        }

        /// <inheritdoc />
        public long GetClaimedCount(SoProp item) => _claimed.GetValueOrDefault(item, 0);

        /// <inheritdoc />
        public bool TryTakeImmediately(SoProp prop, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a value > 0");
            }

            if (TryTake(prop, count, out int handle))
            {
                Commit(handle);
                return true;
            }
            else
            {
                _flows.TryGetValue(prop, out (long Pressure, long FlowIn, long FlowOut) flow);
                _flows[prop] = (flow.Pressure - count, flow.FlowIn, flow.FlowOut);
                return false;
            }
        }

        /// <inheritdoc />
        public void Commit(int handle)
        {
            if (handle < 1)
            {
                return;
            }

            PropCount claim = _claims[handle];
            long oldCount = GetAvailableCount(claim.Identity);

            _claims.Remove(handle);
            _claimed[claim.Identity] = _claimed.GetValueOrDefault(claim.Identity, 0) - claim.Count;
            Assert.IsTrue(_claimed[claim.Identity] >= 0,
                "Invariant failed: Sum of all outstanding claims is equal to the value in _claimCounts[item].");

            long newCount = GetAvailableCount(claim.Identity);
            _availableCapacity += claim.Count * claim.Identity.Bulk;
            _storedBulk -= claim.Count * claim.Identity.Bulk;

            _flows.TryGetValue(claim.Identity, out (long Pressure, long FlowIn, long FlowOut) flow);
            _flows[claim.Identity] = (flow.Pressure, flow.FlowIn, flow.FlowOut + claim.Count);

            EnsureCapacityBounds();
            Modified?.Invoke(Phase.Committed, CreateEventArgs(claim.Identity, claim.Count));
            NotifySubscribers(Phase.Committed, CreateEventArgs(claim.Identity, claim.Count));
        }

        public bool TryPartialCommit(int handle, long count, out long remaining)
        {
            if (handle < 1)
            {
                remaining = 0;
                return false;
            }

            if (count < 1)
            {
                _claims.TryGetValue(handle, out PropCount claim);
                remaining = claim.Count;
                return false;
            }
            else if (_claims.TryGetValue(handle, out PropCount claim) && claim.Count >= count)
            {
                SoProp prop = claim.Identity;
                long oldCount = GetAvailableCount(prop);
                PropCount updatedClaim = new PropCount(prop, claim.Count - count);
                remaining = updatedClaim.Count;
                if (updatedClaim.Count == 0)
                {
                    _claims.Remove(handle);
                }
                else
                {
                    _claims[handle] = updatedClaim;
                }

                _claimed[prop] = _claimed.GetValueOrDefault(prop, 0) - count;
                Assert.IsTrue(_claimed[prop] >= 0,
                    "Invariant failed: Sum of all outstanding claims is equal to the value in _claimCounts[item].");

                _flows.TryGetValue(prop, out (long pressure, long flowIn, long flowOut) flow);
                _flows[prop] = (flow.pressure, flow.flowIn, flow.flowOut + count);

                var delta = count * prop.Bulk;
                _availableCapacity += delta;
                _storedBulk -= delta;
                EnsureCapacityBounds();
                Modified?.Invoke(Phase.Committed, CreateEventArgs(prop, count));
                return true;
            }

            remaining = 0;
            return false;
        }

        /// <inheritdoc />
        public void Release(int handle)
        {
            if (handle < 1)
            {
                Debug.LogWarning($"[{nameof(InventoryComponent)}] Attempted to release an invalid handle: {handle}");
                return;
            }

            if (!_claims.TryGetValue(handle, out PropCount claim))
            {
                Debug.LogWarning($"[{nameof(InventoryComponent)}] Attempted to release an unknown handle: {handle}");
                return;
            }

            long oldCount = GetAvailableCount(claim.Identity);

            _unclaimed.TryGetValue(claim.Identity, out long value);
            _unclaimed[claim.Identity] = _unclaimed.GetValueOrDefault(claim.Identity, 0) + claim.Count;
            _claimed[claim.Identity] = _claimed.GetValueOrDefault(claim.Identity, 0) - claim.Count;
            _claims.Remove(handle);
            EnsureCapacityBounds();
            long newCount = GetAvailableCount(claim.Identity);
            Modified?.Invoke(Phase.Released, CreateEventArgs(claim.Identity, claim.Count));
            NotifySubscribers(Phase.Released, CreateEventArgs(claim.Identity, claim.Count));
        }

        /// <summary>
        /// We deliberately allow the inventory to hold items beyond its capacity as a way to simplify game-design decisions.
        /// Therefore, we have to manually ensure bounds are not exceeded by any edge case we haven't considered yet. 
        /// </summary>
        private void EnsureCapacityBounds()
        {
            Debug.Assert(_storedBulk >= 0 && _availableCapacity >= 0 && _availableCapacity <= TotalCapacity,
                "Inventory bounds check failed. This should not happen but sometimes it " +
                $"is unavoidable ({_storedBulk} + {_availableCapacity} = {TotalCapacity}).");
            _availableCapacity = Math.Max(0, Math.Min(TotalCapacity, _availableCapacity));
            _storedBulk = Math.Max(0, _storedBulk);
        }

        /// <inheritdoc />
        public bool TryTake(SoProp prop, long count, out int handle)
        {
            if (_unclaimed.TryGetValue(prop, out long oldCount) && oldCount >= count)
            {
                handle = ++_claimHandleID;
                Debug.Assert(handle > 0);
                _unclaimed[prop] = oldCount - count;
                _claimed[prop] = _claimed.GetValueOrDefault(prop, 0) + count;
                _claims.Add(handle, new PropCount(prop, count));
                long newCount = GetAvailableCount(prop);
                Modified?.Invoke(Phase.Claimed, CreateEventArgs(prop, count));
                NotifySubscribers(Phase.Claimed, CreateEventArgs(prop, count));

                return true;
            }

            _flows.TryGetValue(prop, out (long pressure, long flowIn, long flowOut) flow);
            _flows[prop] = (flow.pressure - count, flow.flowIn, flow.flowOut);
            handle = -1;
            return false;
        }

        /// <summary>
        /// Clears the inventory of all items. This will not cancel any outstanding claims but delete all internals
        /// records of them. This means a claimant may still have a claim handle but cannot use it anymore.
        /// Use this with caution!
        /// </summary>
        private void ClearWithoutNotify()
        {
            _flows.Clear();
            _claims.Clear();
            _claimed.Clear();
            _unclaimed.Clear();
            _handleLookup.Clear();
            _availableCapacity = TotalCapacity;
            _storedBulk = 0;
        }

        /// <summary>
        /// Clears the inventory of all items. This will cancel any outstanding claims.
        /// </summary>
        private void Clear()
        {
            foreach (var claimsKey in _claims.Keys)
            {
                Release(claimsKey);
            }

            ClearWithoutNotify();
        }

        protected override void OnUnpack(Memento package, AssetLookup lookup)
        {
            capacity = package.TotalCapacity;
            _availableCapacity = package.TotalCapacity;
            _storedBulk = 0;
            if (package.Unclaimed != null)
            {
                _unclaimed.Clear();
                foreach (Memento.PropCount unclaimed in package.Unclaimed)
                {
                    SoProp prop = lookup.GetObjectByID<SoProp>(unclaimed.Prop);
                    _unclaimed.Add(prop, unclaimed.Count);
                    long bulk = prop.Bulk * unclaimed.Count;
                    _availableCapacity -= bulk;
                    _storedBulk += bulk;
                }
            }

            if (package.Flows != null)
            {
                _flows.Clear();
                foreach (Memento.Flow flow in package.Flows)
                {
                    SoProp prop = lookup.GetObjectByID<SoProp>(flow.Prop);
                    _flows.Add(prop, (flow.Pressure, flow.In, flow.Out));
                }
            }

            if (package.Claims != null)
            {
                _claims.Clear();
                foreach (Memento.PropClaim claim in package.Claims)
                {
                    SoProp prop = _lookup.GetObjectByID<SoProp>(claim.Prop);
                    _claims.Add(claim.Handle, new PropCount(prop, claim.Count));
                    long bulk = prop.Bulk * claim.Count;
                    _availableCapacity -= bulk;
                    _storedBulk += bulk;
                }
            }

            if (package.Claimed != null)
            {
                _claimed.Clear();
                foreach (Memento.PropCount claimed in package.Claimed)
                {
                    SoProp prop = _lookup.GetObjectByID<SoProp>(claimed.Prop);
                    _claimed.Add(prop, claimed.Count);
                }
            }
        }

        protected override Memento OnPack()
        {
            var package = new Memento
            {
                TotalCapacity = capacity,
                LastClaimHandle = _claimHandleID,
                LastSubscriberHandle = _subscriberHandle,
                Unclaimed = _unclaimed
                    .Select(it => new Memento.PropCount { Prop = it.Key.ID, Count = it.Value })
                    .ToArray(),
                Claimed = _claimed
                    .Select(it => new Memento.PropCount { Prop = it.Key.ID, Count = it.Value })
                    .ToArray(),
                Flows = _flows
                    .Select(it => new Memento.Flow
                    {
                        Prop = it.Key.ID,
                        Pressure = it.Value.Pressure,
                        In = it.Value.FlowIn,
                        Out = it.Value.FlowOut
                    })
                    .ToArray(),
                Claims = _claims
                    .Select(it => new Memento.PropClaim
                    {
                        Handle = it.Key,
                        Prop = it.Value.Identity.ID,
                        Count = it.Value.Count
                    })
                    .ToArray(),
            };
            return package;
        }

        /// <inheritdoc />
        public int Subscribe(SoProp key, IInventory<SoProp>.InventoryEventHandler handler)
        {
            if (!_subscribers.TryGetValue(key,
                out Dictionary<int, IInventory<SoProp>.InventoryEventHandler> subscribes))
            {
                _subscribers[key] = subscribes = new Dictionary<int, IInventory<SoProp>.InventoryEventHandler>();
            }

            _subscriberHandle++;
            subscribes.Add(_subscriberHandle, handler);
            _handleLookup[_subscriberHandle] = key;
            return _subscriberHandle;
        }

        /// <inheritdoc />
        public void Unsubscribe(int handle)
        {
            if (!_handleLookup.TryGetValue(handle, out SoProp key))
                return;

            _subscribers[key].Remove(handle);
            _handleLookup.Remove(handle);
        }

        private void NotifySubscribers(Phase phase, IInventory<SoProp>.InventoryEventArgs args)
        {
            if (_subscribers.TryGetValue(args.Identity,
                out Dictionary<int, IInventory<SoProp>.InventoryEventHandler> subscribers))
            {
                foreach (var it in subscribers)
                {
                    it.Value.Invoke(phase, args);
                }
            }
        }

        public void Dispose()
        {
            _flows.Clear();
            _claims.Clear();
            _claimed.Clear();
            _unclaimed.Clear();
            _subscribers.Clear();
            _handleLookup.Clear();
            Destroy(this);
        }
    }
}