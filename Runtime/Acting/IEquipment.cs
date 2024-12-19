using System;
using System.Collections.Generic;
using Readymade.Persistence;
using Object = UnityEngine.Object;

namespace Readymade.Machinery.Acting
{
    public interface IEquipment<TSlot, TProp>
        where TSlot : Object, IAssetIdentity
        where TProp : Object, IAssetIdentity
    {
        event Action<(IEquipment<TSlot, TProp> Equipment, TSlot Slot, TProp OldProp, TProp NewProp)> Changed;

        IReadOnlyList<TSlot> Slots { get; }

        void Clear();
        void UnSetAll();

        /// <summary>
        /// Adds or sets the given slot with the given prop.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        bool AddOrSet(TSlot slot, TProp prop);

        /// <summary>
        /// Clears any slot that holds a given prop.
        /// </summary>
        bool UnSet(TProp prop);

        /// <summary>
        /// Clears any prop that is held in the slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        bool UnSet(TSlot slot);

        /// <summary>
        /// Removes the given slot from the equipment component.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        bool Remove(TSlot slot);

        /// <summary>
        /// Gets the prop assigned to the slot, if any.
        /// </summary>
        /// <param name="slot">The slot to get.</param>
        /// <param name="prop">The prop in the slot, if any.</param>
        /// <returns>Whether the slot exists.</returns>
        public bool TryGet(TSlot slot, out TProp prop);

        /// <summary>
        /// Gets the slot that holds a given prop.
        /// </summary>
        /// <param name="prop">The prop to look for.</param>
        /// <param name="slot">The slot that has the prop, if any.</param>
        /// <returns>Whether the prop was found in any slot.</returns>
        /// <remarks>
        /// Same as <see cref="IsSet(TProp,out TSlot)"/>, provided for symmetry.
        /// </remarks>
        bool TryGet(TProp prop, out TSlot slot);

        /// <summary>
        /// Similar to TryGet, but also returns false if the slot is not assigned.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="prop"></param>
        /// <returns>Whether the slot exists and is assigned.</returns>
        bool IsSet(TSlot slot, out TProp prop);

        /// <summary>
        /// Gets the slot that holds a given prop.
        /// </summary>
        /// <remarks>
        /// Same as <see cref="TryGet(TProp,out TSlot)"/>, provided for symmetry.
        /// </remarks>
        /// <param name="prop">The prop to look for.</param>
        /// <param name="slot">The slot that has the prop, if any.</param>
        /// <returns>Whether the prop was found in any slot.</returns>
        bool IsSet(TProp prop, out TSlot slot);

        /// <summary>
        /// Get any slot that accepts the prop. Filled or empty.
        /// </summary>
        /// <param name="prop">The prop to find a slot for.</param>
        /// <param name="slot">The slot that was found, if any.</param>
        /// <returns>Whether a slot was found.</returns>
        bool TryGetAnySlot(TProp prop, out TSlot slot);

        /// <summary>
        /// Get all slots that accept the prop. Filled or empty.
        /// </summary>
        /// <param name="prop">The prop to find a slot for.</param>
        /// <returns>The slots that accept the prop.</returns>
        public IEnumerable<TSlot> GetAllSlots(TProp prop);

        /// <summary>
        /// Gets an empty slot that accepts the prop.
        /// </summary>
        /// <param name="prop">The prop to find a slot for.</param>
        /// <param name="slot">The slot that was found, if any.</param>
        /// <returns>Whether a slot was found.</returns>
        bool TryGetEmptySlot(TProp prop, out TSlot slot);
    }
}