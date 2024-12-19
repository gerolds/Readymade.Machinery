using System;
using System.Collections.Generic;
using System.Linq;
using Readymade.Persistence;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Readymade.Machinery.Acting
{
    public class Equipment<TSlot, TProp> : IEquipment<TSlot, TProp>, IDisposable
        where TSlot : SoSlot, IAssetIdentity
        where TProp : SoProp, IAssetIdentity
    {
        public struct Memento
        {
            public (Guid Slot, Guid Prop)[] Assignments;

            public static Memento Create(Equipment<TSlot, TProp> source)
            {
                return new Memento
                {
                    Assignments = source._assignments
                        .Select(it => (Slot: it.Key.AssetID, Prop: it.Value.AssetID))
                        .ToArray()
                };
            }

            public static void Populate(Memento package, Equipment<TSlot, TProp> destination,
                AssetLookup lookup)
            {
                destination._assignments.Clear();
                foreach (var tuple in package.Assignments)
                {
                    TSlot slot = lookup.GetObjectByID<TSlot>(tuple.Slot);
                    TProp prop = lookup.GetObjectByID<TProp>(tuple.Prop);
                    destination._assignments[slot] = prop;
                }
            }
        }

        /// <summary>
        /// Creates an equipment container with no slots.
        /// </summary>
        public Equipment()
        {
            _slots = new List<TSlot>();
        }

        /// <summary>
        /// Creates an equipment container with the given slots. The input array is copied.
        /// </summary>
        /// <param name="slots"></param>
        public Equipment(params TSlot[] slots)
        {
            _slots = slots.ToList();
        }

        private readonly Dictionary<TSlot, TProp> _assignments = new();
        private readonly List<TSlot> _slots;

        /// <summary>
        /// Called whenever a slot assignment changes.
        /// </summary>
        public event Action<(IEquipment<TSlot, TProp> Equipment, TSlot Slot, TProp OldProp, TProp NewProp)> Changed;

        /// <inheritdoc />
        public IReadOnlyList<TSlot> Slots => _slots;

        /// <inheritdoc />
        public bool AddOrSet(TSlot slot, TProp prop)
        {
            bool found = _assignments.ContainsKey(slot);
            if (!slot.IsAccepting(prop))
            {
                return false;
            }

            TProp old = found ? _assignments[slot] : default;
            _assignments[slot] = prop;
            Changed?.Invoke((this, slot, old, prop));
            return found;
        }

        /// <inheritdoc />
        public bool UnSet(TProp prop)
        {
            bool found = TryGet(prop, out var slot);
            TProp old = found ? _assignments[slot] : default;
            _assignments[slot] = default;
            Changed?.Invoke((this, slot, old, default));
            return found;
        }

        /// <inheritdoc />
        public void Clear() => _assignments.Clear();

        /// <inheritdoc />
        public bool UnSet(TSlot slot)
        {
            bool found = TryGet(slot, out TProp old);
            if (found)
            {
                _assignments[slot] = default;
                Changed?.Invoke((this, slot, old, default));
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void UnSetAll()
        {
            foreach (TSlot slot in _assignments.Keys)
            {
                TProp old = _assignments[slot];
                _assignments[slot] = default;
                Changed?.Invoke((this, slot, old, default));
            }
        }

        /// <inheritdoc />
        public bool Remove(TSlot slot) => _assignments.Remove(slot);

        /// <inheritdoc />
        public bool TryGet(TSlot slot, out TProp prop) => _assignments.TryGetValue(slot, out prop);

        /// <inheritdoc />
        public bool TryGet(TProp prop, out TSlot slot)
        {
            slot = _assignments.FirstOrDefault(it => it.Value == prop).Key;
            return slot;
        }

        /// <inheritdoc />
        public bool IsSet(TSlot slot, out TProp prop) => TryGet(slot, out prop) && prop;

        /// <inheritdoc />
        public bool IsSet(TProp prop, out TSlot slot) => TryGet(prop, out slot);

        /// <inheritdoc />
        public void Dispose()
        {
            _assignments.Clear();
            _slots.Clear();
        }

        /// <inheritdoc />
        public bool TryGetAnySlot(TProp prop, out TSlot slot)
        {
            foreach (var candidate in _slots)
            {
                if (candidate.IsAccepting(prop))
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = default;
            return false;
        }

        /// <inheritdoc />
        public IEnumerable<TSlot> GetAllSlots(TProp prop) => _slots.Where(it => it.IsAccepting(prop));

        /// <inheritdoc />
        public bool TryGetEmptySlot(TProp prop, out TSlot slot)
        {
            foreach (var candidate in _slots)
            {
                if (!IsSet(candidate, out _) && candidate.IsAccepting(prop))
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = default;
            return false;
        }
    }
}