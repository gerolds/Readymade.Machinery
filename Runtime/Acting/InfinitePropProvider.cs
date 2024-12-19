using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Shared;
using UnityEngine;
using Vertx.Debugging;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Default implementation of a <see cref="IProvider{TProp}"/> useful for mocking and implementing infinite supply. 
    /// </summary>
    /// <typeparam name="SoProp">The type of prop this provider provides.</typeparam>
    public class InfinitePropProvider : IProvider<SoProp>
    {
        /// <summary>
        /// Delegate to dynamically get the pose required for all claims answered by this provider. 
        /// </summary>
        private readonly Func<Pose> _pose;

        private IInventory<SoProp> _inventory;

        /// <summary>
        /// Create an instance of <see cref="InventoryPropProvider"/> that provides the given <paramref name="props"/>. 
        /// </summary>
        /// <param name="pose"></param>
        /// <param name="props"></param>
        public InfinitePropProvider(Pose pose, [NotNull] params SoProp[] props)
        {
            _inventory = new Inventory();
            foreach (SoProp prop in props)
            {
                _inventory.ForceSet(prop, 1);
            }

            _pose = () => pose;
        }

        public InfinitePropProvider(Func<Pose> getPose, [NotNull] params SoProp[] props)
        {
            _inventory = new Inventory();
            foreach (SoProp prop in props)
            {
                _inventory.ForceSet(prop, 1);
            }

            _pose = getPose;
        }

        /// <inheritdoc />
        public event Action<Phase, (SoProp prop, long count, IActor claimant)> Modified;

        /// <inheritdoc />
        public Pose Pose => _pose.Invoke();

        /// <inheritdoc />
        public bool HasPose => _pose != default;

        /// <inheritdoc />
        public bool DebugLog { get; set; }

        public IInventory<SoProp> Inventory => _inventory;

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

            claim = default;
            if (_inventory.GetAvailableCount(prop) == 0)
            {
                return false;
            }

            // return a claim that can be immediately committed.
            claim = new PropClaim<SoProp, IActor>(
                actor,
                prop,
                quantity,
                Pose,
                onCommit: args => args.Claimant.Inventory.TryPut(prop, quantity)
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

        private void ReleasedHandler((SoProp prop, long quantity, IActor claimant) args) =>
            Modified?.Invoke(Phase.Released, args);

        private void CommittedHandler((SoProp prop, long quantity, IActor claimant) args) =>
            Modified?.Invoke(Phase.Committed, args);

        /// <inheritdoc />
        public bool CanProvide([NotNull] SoProp prop) => _inventory.GetAvailableCount(prop) > 0;

        public IEnumerable<SoProp> ProvidedProps => _inventory.UnclaimedRaw.Keys;

        /// <inheritdoc />
        public void Dispose() => _inventory.Dispose();
    }
}