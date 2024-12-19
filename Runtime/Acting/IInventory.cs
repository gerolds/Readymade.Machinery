using System;
using System.Collections.Generic;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A generic collection of items with two-phase-commit API for item removal.
    /// </summary>
    /// <remarks>
    /// <para>This interface is designed to be used for <see cref="IProp"/> tokens.
    /// </para>
    /// <para>This interface is a lightweight variant of the two-phase-commit pattern (Update resources on multiple
    /// nodes in one operation) in front of a key-value-store. It is designed to be used as a component in an encapsulating type
    /// and to provide simple endpoints for a shared transaction handle.
    /// </para>
    /// <para>Note that only the "take"-side of the API has a 2-phase commit, allowing only immediate changes. This is deliberate
    /// to remove a number of conflicts/deadlocks that arise easily in the usage domain when concurrent put and take claims are
    /// not carefully negotiated. The inventory can also be forced to overflow  which provides means to capture domain edge cases
    /// (temporarily reduced carry capacity, encumbrance or unexpected transaction cancellations) in a simple way.
    /// </para>
    /// </remarks>
    public interface IInventory<TItem> : IStockpile<TItem>, IDisposable
    {
        /// <summary>
        /// Signature for inventory change events.
        /// </summary>
        /// <param name="message">The type of change that happened.</param>
        /// <param name="args">Additional details about the change.</param>
        /// <param name="args.Inventory">The inventory on which the change happened.</param>
        /// 
        public delegate void InventoryEventHandler(Phase message, InventoryEventArgs args);

        public readonly struct InventoryEventArgs
        {
            /// <summary>
            /// The inventory that was affected.
            /// </summary>
            public readonly IInventory<TItem> Inventory;

            /// <summary>
            /// The item that was affected.
            /// </summary>
            public readonly TItem Identity;

            /// <summary>
            /// The change in the count of the item. Whether this delta has affected the claimed or available count is 
            /// determined by the <see cref = "Phase" />.
            /// </summary>
            public readonly long Delta;

            /// <summary>
            /// The count of claimed items in the inventory.
            /// </summary>
            public readonly long Claimed;

            /// <summary>
            /// The count of available items in the inventory.
            /// </summary>
            public readonly long Available;

            public InventoryEventArgs(IInventory<TItem> inventory, TItem item, long delta, long claimed, long available)
            {
                Inventory = inventory;
                Identity = item;
                Delta = delta;
                Claimed = claimed;
                Available = available;
            }
        }

        /// <summary>
        /// The items currently in the inventory.
        /// </summary>
        public IEnumerable<(TItem Prop, long Count)> Unclaimed { get; }

        public IReadOnlyDictionary<TItem, long> UnclaimedRaw { get; }
        public IReadOnlyDictionary<SoProp, (long Pressure, long In, long Out )> FlowsRaw { get; }

        /// <summary>
        /// Whether the inventory is completely empty.
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// Put an <paramref name="item"/> into the <see cref="IInventory{TItem}"/>
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to store.</param>
        /// <param name="count">How many <typeparamref name="TItem"/> to store.</param>
        public bool TryPut(TItem item, long count);

        /// <summary>
        /// Force put an <paramref name="item"/> into the <see cref="IInventory{TItem}"/>. No capacity checks will be performed.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to store.</param>
        /// <param name="count">How many <typeparamref name="TItem"/> to store.</param>
        public void ForcePut(TItem item, long count);

        /// <summary>
        /// Force set a given quantity of <paramref name="item"/> in the <see cref="IInventory{TItem}"/>. No capacity checks will be performed.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to store.</param>
        /// <param name="count">How many <typeparamref name="TItem"/> to store.</param>
        public void ForceSet(TItem item, long count);

        /// <summary>
        /// Force set a given quantity of <paramref name="item"/> in the <see cref="IInventory{TItem}"/>. No capacity checks will be performed. No observers will be notified.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to store.</param>
        /// <param name="count">How many <typeparamref name="TItem"/> to store.</param>
        public void ForceSetWithoutNotify(TItem item, long count);

        /// <summary>
        /// Check if an <paramref name="item"/> can be put into the <see cref="IInventory{TItem}"/>.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to store.</param>
        /// <param name="count">How many <typeparamref name="TItem"/> to store.</param>
        public bool CanPut(TItem item, long count);

        /// <summary>
        /// Delete the claim associated with <paramref name="handle"/>. <see cref="Release"/> is to be called by the claimant that
        /// originally received the handle if the claim is to be released.
        /// </summary>
        /// <param name="handle">The claim handle to delete.</param>
        public void Release(int handle);

        /// <summary>
        /// Commit the claim associated with a given <see cref="handle"/>. <see cref="Commit"/> is to be called by the claimant
        /// that originally received the handle if the claim is to be committed (made persistent).
        /// </summary>
        /// <param name="handle">The claim handle to commit.</param>
        public void Commit(int handle);

        /// <summary>
        /// Attempt to claim an <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to claim.</param>
        /// <param name="count">The count of <typeparamref name="TItem"/> to claim.</param>
        /// <param name="claimHandle">A handle representing the claim.</param>
        public bool TryTake(TItem item, long count, out int claimHandle);

        /// <summary>
        /// Attempt to claim an <paramref name="item"/> and immediately commit the claim.
        /// </summary>
        /// <param name="item">The <typeparamref name="TItem"/> to claim.</param>
        /// <param name="count">The count of <typeparamref name="TItem"/> to claim.</param>
        public bool TryTakeImmediately(TItem item, long count = 1);

        /// <summary>
        /// The total bulk capacity of this inventory. Will be compared to the bulk of each item on <see cref="TryPut"/>. Will never go below 0.
        /// </summary>
        public long TotalCapacity { get; }

        /// <summary>
        /// The available bulk capacity of this inventory as <see cref="TotalCapacity"/> minus the aggregate of all stored items' bulk. Available capacity is unaffected by claim/release. Only put and commit events affect it. This prevents
        /// overbooking an inventory while claims are outstanding. Will never go below 0.
        /// </summary>
        public long AvailableCapacity { get; }

        /// <summary>
        /// The current amount of bulk stored in the inventory. Can be greater than <see cref="TotalCapacity"/> when <see cref="ForcePut"/> was used. Will never go below 0.
        /// </summary>
        public long StoredBulk { get; }

        string DisplayName { get; }

        public bool TryGetFlow(SoProp prop, out (long pressure, long inFlow, long outFlow) flow);

        /// <summary>
        /// Invoked whenever the registered count of an item changes.
        /// </summary>
        public event InventoryEventHandler Modified;

        /// <summary>
        /// Subscribe a listener to a specific item's count changes. The listener will be invoked whenever the count of the item changes.
        /// </summary>
        /// <param name="key">The item to subscribe to.</param>
        /// <param name="handler">The handler to invoke when the item count changes.</param>
        /// <returns>A handle that can be used to unsubscribe the handler via <see cref="Unsubscribe"/>.</returns>
        public int Subscribe(TItem key, InventoryEventHandler handler);

        /// <summary>
        /// Unsubscribe a listener.
        /// </summary>
        /// <param name="handle">The handle that identifies the subscription.</param>
        public void Unsubscribe(int handle);

        /// <summary>
        /// Attempts to partially commit a claim. If the claim is not fully committed, the remaining count is returned.
        /// This is useful for implementing ownership or dedication of certain counts to a particular entity or purpose.
        /// </summary>
        /// <param name="handle">The handle of the claim.</param>
        /// <param name="remaining">The remaining count in the claim.</param>
        /// <param name="count">The count to commit.</param>
        public bool TryPartialCommit(int handle, long count, out long remaining);

        /// <summary>
        /// Gets the remaining count of a claim.
        /// </summary>
        /// <param name="handle">The handle of the claim.</param>
        /// <param name="count">The remaining count in the claim.</param>
        /// <returns>Whether a claim with the given handle was found.</returns>
        public bool TryGetClaimCount(int handle, out long count);

        /// <summary>
        /// Gets the remaining count of a claim.
        /// </summary>
        /// <param name="handle">The handle of the claim.</param>
        /// <returns>The remaining count in the claim.</returns>
        public long GetClaimedCount(int handle);

        /// <summary>
        /// Gets the remaining count of a claim.
        /// </summary>
        /// <param name="item">The item to get the count for.</param>
        /// <returns>The total count of all current claims on the given item.</returns>
        public long GetClaimedCount(TItem item);
    }

    /// <summary>
    /// Represents a pile of item counts that can be queried for their quantity and bulk. 
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public interface IStockpile<TItem>
    {
        /// <summary>
        /// The total count available for taking of a given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public long GetAvailableCount(TItem item);
    }
}