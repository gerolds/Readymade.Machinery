using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Readymade.Machinery.Shared;
using UnityEngine;
using Vertx.Debugging;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Plain C# version of an inventory provider. 
    /// </summary>
    public class InventoryPropProvider : IProvider<SoProp>
    {
        public struct Memento
        {
        }

        /// <summary>
        /// the inventory used as backing storage for this provider.
        /// </summary>
        private readonly IInventory<SoProp> _inventory;

        /// <summary>
        /// The pose required for all claims answered by this provider. 
        /// </summary>
        private Func<Pose> _pose;

        // A delegate that determines whether a prop is exposed by this provider.
        private readonly Func<SoProp, bool> _exposure;

        /// <inheritdoc />
        public event Action<Phase, (SoProp prop, long count, IActor claimant)> Modified;

        /// <summary>
        /// The position of this provider (this will evaluate the position delegate and does not return a cached value).
        /// </summary>
        public Vector3 Position => _pose.Invoke().position;

        /// <inheritdoc />
        public Pose Pose => _pose.Invoke();

        /// <inheritdoc />
        public bool HasPose => _pose != default;

        /// <inheritdoc />
        public bool DebugLog { get; set; }

        public IInventory<SoProp> Inventory => _inventory;

        /// <summary>
        /// Create an instance of a <see cref="InventoryPropProvider"/>.
        /// </summary>
        /// <param name="inventory">The <see cref="IInventory{TItem}"/> used for backing this provider.</param>
        /// <param name="pose">The pose this provider will assume.</param>
        /// <param name="exposureProvider">Function to determine whether a prop is exposed by this provider.</param>
        public InventoryPropProvider(
            Pose pose,
            [NotNull] IInventory<SoProp> inventory,
            Func<SoProp, bool> exposureProvider = default
            )
        {
            _inventory = inventory;
            _pose = () => pose;
            _exposure = exposureProvider;
            _inventory.Modified += InventoryModifiedHandler;
        }

        private void InventoryModifiedHandler(Phase message, IInventory<SoProp>.InventoryEventArgs args)
        {
            Modified?.Invoke(message, (args.Identity, args.Delta, IActor.None));
        }

        /// <summary>
        /// Create an instance of a <see cref="InventoryPropProvider"/>.
        /// </summary>
        /// <param name="inventory">The <see cref="IInventory{TItem}"/> used for backing this provider.</param>
        /// <param name="exposureProvider">Function to determine whether a prop is exposed by this provider.</param>
        public InventoryPropProvider(
            [NotNull] IInventory<SoProp> inventory,
            Func<SoProp, bool> exposureProvider = default
            )
        {
            _inventory = inventory;
            _pose = () => default;
            _exposure = exposureProvider;
            _inventory.Modified += InventoryModifiedHandler;
        }

        /// <summary>
        /// Create an instance of a <see cref="InventoryPropProvider"/>.
        /// </summary>
        /// <param name="inventory">The <see cref="IInventory{TItem}"/> used for backing this provider.</param>
        /// <param name="pose"></param>
        /// <param name="exposureProvider"></param>
        public InventoryPropProvider(Func<Pose> pose, [NotNull] IInventory<SoProp> inventory,
            Func<SoProp, bool> exposureProvider = default)
        {
            _inventory = inventory;
            _pose = pose;
            _exposure = exposureProvider;
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

            if (!IsExposed(prop))
            {
                claim = default;
                return false;
            }

            bool isClaimed = _inventory.TryTake(prop, quantity, out int handle);
            if (!isClaimed)
            {
                claim = default;
                return false;
            }

            claim = new PropClaim<SoProp, IActor>(
                claimant: actor,
                prop: prop,
                quantity: quantity,
                pose: Pose,
                onCommit: args =>
                {
                    _inventory.Commit(handle);
                    if (args.Claimant != IActor.None)
                    {
                        args.Claimant.Inventory.TryPut(prop, quantity);
                    }
                },
                onCancel: _ => { _inventory.Release(handle); }
            );
            Modified?.Invoke(Phase.Claimed, (prop, quantity, actor));
            claim.Committed += CommittedHandler;
            claim.Released += ReleasedHandler;
            if (DebugLog)
            {
                Debug.Log(
                    $"[{GetType().GetNiceName()}] responded to prop {prop.Name} claim by {actor.Name} with pose {claim.CommitPose.position}");
            }

            if (HasPose)
            {
                D.raw(new Shape.Arrow(Pose.position, actor.Pose.position - Pose.position), Color.yellow, .5f);
                D.raw(new Shape.Circle(Pose.position, Vector3.up, .6f), Color.yellow, .5f);
            }

            return true;
        }

        private bool IsExposed(SoProp prop) => _exposure == default || _exposure.Invoke(prop);

        /// <inheritdoc />
        public bool CanProvide([NotNull] SoProp prop) => IsExposed(prop) && _inventory.GetAvailableCount(prop) > 0;

        public IEnumerable<SoProp> ProvidedProps => _inventory.UnclaimedRaw.Keys.Where(IsExposed);

        private void ReleasedHandler((SoProp prop, long quantity, IActor claimant) args) =>
            Modified?.Invoke(Phase.Released, args);

        private void CommittedHandler((SoProp prop, long quantity, IActor claimant) args) =>
            Modified?.Invoke(Phase.Committed, args);

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}