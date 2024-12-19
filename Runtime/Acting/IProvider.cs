using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// Defines methods to claim <see cref="T:Readymade.Machinery.Acting.IProp" /> derived tokens. 
    /// </summary>
    /// <remarks>
    ///<para>
    /// This API is useful for implementing item, resource and utility based AI
    /// behaviour and abstracting the implementation details on either side as a resource transaction. <i>Providing</i> a prop is
    /// conceptually a virtual transaction that should be thought of as a database UPDATE, no actual instances are created,
    /// no rows in a table are added or removed. Consequently if a prop is claimed and committed successfully with a given actor
    /// reference, the user can expect that the requested quantity was successfully placed into the actor's inventory and removed
    /// from the object's inventory that that is exposed through this interface.
    ///</para>
    /// <para><see cref="IProvider{TProp}"/> can also be used to implement any kind of token lookups and even semaphores.
    /// If backed with a unlimited supply of token props, a <see cref="IProvider{TProp}"/> can serve as a generic lookup
    /// for locations with semantics of the lookup being completely editor-configurable through <see cref="SoProp"/> instances.
    /// </para>
    /// </remarks>
    /// <typeparam name="TProp">The base type of the props this provider can provide.</typeparam>
    public interface IProvider<TProp> : IDisposable where TProp : SoProp
    {
        /// <summary>
        /// The pose of this provider. Defined only if <see cref="HasPose"/> is true.
        /// </summary>
        public Pose Pose { get; }

        /// <summary>
        /// Whether this provider has a pose (i.e. a location in the world).
        /// </summary>
        public bool HasPose { get; }

        /// <summary>
        /// Whether to log debug messages.
        /// </summary>
        public bool DebugLog { get; set; }

        /// <summary>
        /// The inventory backing this provider.
        /// </summary>
        public IInventory<TProp> Inventory { get; }

        /// <summary>
        /// Called whenever the provider's internal state regarding a prop is modified.
        /// </summary>
        public event Action<Phase, (SoProp prop, long count, IActor claimant)> Modified;

        /// <summary>
        /// An actor may try to claim a prop that it needs to complete a <see cref="Readymade.Machinery.Acting.IGesture{TActor}"/>.
        /// </summary>
        /// <param name="prop">The <see cref="IProp"/> to claim.</param>
        /// <param name="actor">The <see cref="IActor"/> making the claim.</param>
        /// <param name="quantity">The number of props to claim. Must be &gt; 1.</param>
        /// <param name="claim">The handle representing the claim, if successful.</param>
        /// <returns>Whether the <paramref name="prop"/> was claimed successfully.</returns>
        public bool TryClaimProp(
            [NotNull] TProp prop,
            [NotNull] IActor actor,
            long quantity,
            out PropClaim<TProp, IActor> claim
        );

        /// <summary>
        /// An actor may try to claim a prop that it needs to complete a <see cref="Readymade.Machinery.Acting.IGesture{TActor}"/>.
        /// </summary>
        /// <param name="prop">The <see cref="IProp"/> to claim.</param>
        /// <param name="actor">The <see cref="IActor"/> making the claim.</param>
        /// <param name="claim">The handle representing the claim, if successful.</param>
        /// <returns>Whether the <paramref name="prop"/> was claimed successfully.</returns>
        public bool TryClaimProp(
            [NotNull] TProp prop,
            [NotNull] IActor actor,
            out PropClaim<TProp, IActor> claim
        ) => TryClaimProp(prop, actor, 1, out claim);

        /// <summary>
        /// Whether this <see cref="IProvider{TProp}"/> can provide a given <see cref="IProp"/>.
        /// </summary>
        /// <param name="prop">The prop to provide.</param>
        /// <returns>Whether the prop can be provided.</returns>
        public bool CanProvide([NotNull] TProp prop);

        public IEnumerable<SoProp> ProvidedProps { get; }
    }
}