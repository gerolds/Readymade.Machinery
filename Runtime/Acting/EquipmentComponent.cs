using System;
using System.Collections.Generic;
using Readymade.Persistence;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    public class EquipmentComponent : PackableComponent<Equipment<SoSlot, SoProp>.Memento>, IEquipment<SoSlot, SoProp>
    {
        [SerializeField] private SoSlot[] slots;

        private Equipment<SoSlot, SoProp> _equipment;

        public event Action<(
            IEquipment<SoSlot, SoProp> Equipment,
            SoSlot Slot,
            SoProp OldProp,
            SoProp NewProp
            )> Changed;

        public IReadOnlyList<SoSlot> Slots => _equipment.Slots;

        private void Awake()
        {
            _equipment = new Equipment<SoSlot, SoProp>(slots);
            _equipment.Changed += OnEquipmentOnSlotChanged;
        }

        private void OnDestroy()
        {
            // technically not necessary, but good practice
            _equipment.Changed -= OnEquipmentOnSlotChanged;
            _equipment.Dispose();
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public bool AddOrSet(SoSlot slot, SoProp prop) => _equipment.AddOrSet(slot, prop);

        /// <inheritdoc />
        public void Clear() => _equipment.Clear();

        /// <inheritdoc />
        public bool UnSet(SoSlot slot) => _equipment.UnSet(slot);

        /// <inheritdoc />
        public bool UnSet(SoProp prop) => _equipment.UnSet(prop);

        /// <inheritdoc />
        public void UnSetAll() => _equipment.UnSetAll();

        /// <inheritdoc />
        public bool Remove(SoSlot slot) => _equipment.Remove(slot);

        /// <inheritdoc />
        public bool TryGet(SoSlot slot, out SoProp prop) => _equipment.TryGet(slot, out prop);

        /// <inheritdoc />
        public bool TryGet(SoProp prop, out SoSlot slot) => _equipment.TryGet(prop, out slot);

        /// <inheritdoc />
        public bool IsSet(SoSlot slot, out SoProp prop) => _equipment.IsSet(slot, out prop);

        /// <inheritdoc />
        public bool IsSet(SoProp prop, out SoSlot slot) => _equipment.IsSet(prop, out slot);
        
        private void OnEquipmentOnSlotChanged(
            (IEquipment<SoSlot, SoProp> Equipment, SoSlot Slot, SoProp OldProp, SoProp NewProp) args) =>
            Changed?.Invoke(args);

        /// <inheritdoc />
        protected override void OnUnpack(Equipment<SoSlot, SoProp>.Memento package, AssetLookup lookup)
            => Equipment<SoSlot, SoProp>.Memento.Populate(package, _equipment, lookup);

        /// <inheritdoc />
        protected override Equipment<SoSlot, SoProp>.Memento OnPack()
            => Equipment<SoSlot, SoProp>.Memento.Create(_equipment);

        /// <inheritdoc />s
        public IEnumerable<SoSlot> GetAllSlots(SoProp prop) => _equipment.GetAllSlots(prop);

        /// <inheritdoc />
        public bool TryGetEmptySlot(SoProp prop, out SoSlot slot) => _equipment.TryGetEmptySlot(prop, out slot);

        /// <inheritdoc />
        public bool TryGetAnySlot(SoProp prop, out SoSlot slot) => _equipment.TryGetAnySlot(prop, out slot);
    }
}