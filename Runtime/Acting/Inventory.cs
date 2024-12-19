using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc cref="IInventory{TItem}"/>
    [Serializable]
    public sealed class Inventory : IInventory<SoProp>, ISerializationCallbackReceiver
    {
        [SerializeField] private PropCount[] unclaimedSerializable;
        [SerializeField] private long totalCapacity;

        // A monotonic serial number
        [NaughtyAttributes.ReadOnly]
        [SerializeField]
        private int claimHandleID;

        // A monotonic serial number
        [NaughtyAttributes.ReadOnly]
        [SerializeField]
        private int subscriberHandle;

        private readonly SortedDictionary<SoProp, long> _unclaimed = new();
        private readonly SortedDictionary<SoProp, long> _claimed = new();
        private readonly Dictionary<int, PropCount> _claims = new();
        private readonly Dictionary<SoProp, (long Pressure, long FlowIn, long FlowOut)> _flows = new();
        private readonly Dictionary<int, SoProp> _handleLookup = new();

        private readonly Dictionary<SoProp, Dictionary<int, IInventory<SoProp>.InventoryEventHandler>> _subscribers =
            new();

        private long _availableCapacity;
        private long _storedBulk;

        /// <inheritdoc />
        public string DisplayName { get; set; } = "Undefined";

        /// <inheritdoc />
        public bool TryGetFlow(SoProp prop, out (long pressure, long inFlow, long outFlow) flow) =>
            _flows.TryGetValue(prop, out flow);

        /// <inheritdoc />
        public event IInventory<SoProp>.InventoryEventHandler Modified;

        /// <inheritdoc />
        /// <remarks>This is a lazily evaluated enumerable backed by a dictionary. It allocates an enumerator.</remarks>
        public IEnumerable<(SoProp Prop, long Count)> Unclaimed =>
            _unclaimed.Select(it => (Prop: it.Key, Count: it.Value));

        /// <inheritdoc />
        public IEnumerable<(SoProp Prop, long Count)> Claimed =>
            _claimed.Select(it => (Prop: it.Key, Count: it.Value));

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, long> UnclaimedRaw => _unclaimed;

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, long> ClaimedRaw => _claimed;

        /// <inheritdoc />
        public bool IsEmpty => (_unclaimed.Count == 0 || _unclaimed.Values.All(it => it == 0)) && _claims.Count == 0;

        /// <inheritdoc />
        public IEnumerable<(int Handle, PropCount)> Claims => _claims.Select(it => (Handle: it.Key, Claim: it.Value));

        /// <inheritdoc />
        public IEnumerable<(SoProp Prop, long Pressure, long In, long Out )> Flows => _flows.Select(it =>
            (it.Key, pressure: it.Value.Pressure, flowIn: it.Value.FlowIn, flowOut: it.Value.FlowOut));

        /// <inheritdoc />
        public IReadOnlyDictionary<SoProp, (long Pressure, long In, long Out)> FlowsRaw => _flows;

        /// <inheritdoc />
        public long TotalCapacity
        {
            get => totalCapacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be a positive value.");
                }

                long delta = value - totalCapacity;
                totalCapacity = value;
                AvailableCapacity += delta;
            }
        }

        /// <inheritdoc />
        public long AvailableCapacity
        {
            get => _availableCapacity;
            private set => _availableCapacity = value;
        }

        /// <inheritdoc />
        public long StoredBulk
        {
            get => _storedBulk;
            private set => _storedBulk = value;
        }

        /// <summary>
        /// Creates an instance with a defined maximum capacity.
        /// </summary>
        /// <param name="capacity">The desired bulk capacity.</param>
        /// <seealso cref="TotalCapacity"/><seealso cref="AvailableCapacity"/><seealso cref="StoredBulk"/>
        public Inventory(long capacity = long.MaxValue)
        {
            TotalCapacity = capacity;
            AvailableCapacity = capacity;
            Modified += NotifySubscribers;
        }

        /// <inheritdoc />
        public bool TryPut(SoProp prop, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a value > 0");
            }

            if (CanPut(prop, count))
            {
                ForcePut(prop, count);
                return true;
            }
            else
            {
                _flows.TryGetValue(prop, out (long Pressure, long FlowIn, long FlowOut) flow);
                _flows[prop] = (flow.Pressure + count, flow.FlowIn, flow.FlowOut);
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
            AvailableCapacity -= bulk;
            StoredBulk += bulk;
            _flows.TryGetValue(prop, out (long Pressure, long FlowIn, long FlowOut) flow);
            _flows[prop] = (flow.Pressure, flow.FlowIn + count, flow.FlowOut);
            EnsureCapacityBounds();
            var newCount = _unclaimed[prop];
            Modified?.Invoke(Phase.Put, CreateEventArgs(prop, newCount - oldCount));
        }

        /// <inheritdoc />
        public void ForceSet(SoProp prop, long count)
        {
            _unclaimed.TryGetValue(prop, out long oldCount);
            ForceSetWithoutNotify(prop, count);
            var newCount = _unclaimed[prop];
            Modified?.Invoke(Phase.Set, CreateEventArgs(prop, newCount - oldCount));
        }

        /// <inheritdoc />
        public void ForceSetWithoutNotify(SoProp prop, long count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a positive value");
            }

            _unclaimed[prop] = count;
            long bulk = count * prop.Bulk;
            StoredBulk = bulk;
            AvailableCapacity = TotalCapacity - StoredBulk;
            EnsureCapacityBounds();
        }

        /// <inheritdoc />
        public bool CanPut(SoProp item, long count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Must be a value > 0");
            }

            return AvailableCapacity >= item.Bulk * MathF.Max(count, 0);
        }

        /// <inheritdoc />
        public long GetAvailableCount(SoProp prop) =>
            _unclaimed != null && _unclaimed.TryGetValue(prop, out long value) ? value : 0;

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
            AvailableCapacity += claim.Count * claim.Identity.Bulk;
            StoredBulk -= claim.Count * claim.Identity.Bulk;

            _flows.TryGetValue(claim.Identity, out (long Pressure, long FlowIn, long FlowOut) flow);
            _flows[claim.Identity] = (flow.Pressure, flow.FlowIn, flow.FlowOut + claim.Count);

            EnsureCapacityBounds();
            Modified?.Invoke(Phase.Committed, CreateEventArgs(claim.Identity, newCount - oldCount));
            NotifySubscribers(Phase.Committed, CreateEventArgs(claim.Identity, newCount - oldCount));
        }

        /// <inheritdoc />
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
                long oldCount = GetAvailableCount(claim.Identity);
                PropCount newPropCount = new PropCount(claim.Identity, claim.Count - count);
                remaining = newPropCount.Count;
                if (remaining == 0)
                {
                    _claims.Remove(handle);
                }
                else
                {
                    _claims[handle] = newPropCount;
                }

                _claimed[claim.Identity] = _claimed.GetValueOrDefault(claim.Identity, 0) - count;
                Assert.IsTrue(_claimed[claim.Identity] >= 0,
                    "Invariant failed: Sum of all outstanding claims is equal to the value in _claimCounts[item].");

                _flows.TryGetValue(claim.Identity, out (long pressure, long flowIn, long flowOut) flow);
                _flows[claim.Identity] = (flow.pressure, flow.flowIn, flow.flowOut + count);

                long newCount = GetAvailableCount(claim.Identity);
                AvailableCapacity += count * claim.Identity.Bulk;
                StoredBulk -= count * claim.Identity.Bulk;
                EnsureCapacityBounds();
                Modified?.Invoke(Phase.Committed,
                    CreateEventArgs(claim.Identity, newCount - oldCount));
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

            PropCount claim = _claims[handle];
            long oldCount = GetAvailableCount(claim.Identity);

            _unclaimed.TryGetValue(claim.Identity, out long value);
            _unclaimed[claim.Identity] = _unclaimed.GetValueOrDefault(claim.Identity, 0) + claim.Count;
            _claimed[claim.Identity] = _claimed.GetValueOrDefault(claim.Identity, 0) - claim.Count;
            _claims.Remove(handle);
            long newCount = GetAvailableCount(claim.Identity);
            Modified?.Invoke(Phase.Released, CreateEventArgs(claim.Identity, newCount - oldCount));
            NotifySubscribers(Phase.Released, CreateEventArgs(claim.Identity, newCount - oldCount));
        }

        /// <summary>
        /// We deliberately allow the inventory to hold items beyond its capacity as a way to simplify game-design decisions.
        /// Therefore we have to manually ensure bounds are not exceeded by any edge case we haven't considered yet. 
        /// </summary>
        private void EnsureCapacityBounds()
        {
            Debug.Assert(AvailableCapacity >= 0 && AvailableCapacity <= TotalCapacity,
                "Inventory bounds check failed. This should not happen but sometimes it is unavoidable.");
            AvailableCapacity = Math.Max(0, Math.Min(TotalCapacity, AvailableCapacity));
            StoredBulk = Math.Max(0, StoredBulk);
        }

        /// <inheritdoc />
        public bool TryTake(SoProp prop, long count, out int handle)
        {
            if (_unclaimed.TryGetValue(prop, out long oldCount) && oldCount >= count)
            {
                handle = ++claimHandleID;
                Debug.Assert(handle > 0);
                _unclaimed[prop] = oldCount - count;
                _claimed[prop] = _claimed.GetValueOrDefault(prop, 0) + count;
                _claims.Add(handle, new PropCount(prop, count));
                long newCount = GetAvailableCount(prop);
                Modified?.Invoke(Phase.Released, CreateEventArgs(prop, newCount - oldCount));
                NotifySubscribers(Phase.Released, CreateEventArgs(prop, newCount - oldCount));

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
            AvailableCapacity = TotalCapacity;
            StoredBulk = 0;
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


        public void OnBeforeSerialize()
        {
            if (_unclaimed?.Count > 0)
            {
                unclaimedSerializable = _unclaimed
                    .Select(it => new PropCount(prop: it.Key, count: (int)it.Value))
                    .ToArray();
            }
            else
            {
                unclaimedSerializable = Array.Empty<PropCount>();
            }
        }

        public void OnAfterDeserialize()
        {
            _availableCapacity = totalCapacity;
            _storedBulk = 0;
            if (unclaimedSerializable != null)
            {
                foreach (var unclaimed in unclaimedSerializable)
                {
                    _unclaimed.Clear();
                    _unclaimed.Add(unclaimed.Identity, unclaimed.Count);
                    long bulk = unclaimed.Identity.Bulk * _unclaimed[unclaimed.Identity];
                    _availableCapacity -= bulk;
                    _storedBulk += bulk;
                }
            }
        }

        /// <inheritdoc />
        public int Subscribe(SoProp key, IInventory<SoProp>.InventoryEventHandler handler)
        {
            if (!_subscribers.TryGetValue(key,
                out Dictionary<int, IInventory<SoProp>.InventoryEventHandler> subscribes))
            {
                _subscribers[key] = subscribes = new Dictionary<int, IInventory<SoProp>.InventoryEventHandler>();
            }

            subscriberHandle++;
            subscribes.Add(subscriberHandle, handler);
            _handleLookup[subscriberHandle] = key;
            return subscriberHandle;
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
        }
    }
}