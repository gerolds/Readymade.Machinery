using System;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Shared;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A handle that represents a claim by an actor on a given prop. Can be committed or cancelled and allows
    /// specification of a <see cref="Pose" /> and quantity.
    /// </summary>
    /// <typeparam name="TProp">The type of <see cref="IProp" /> being claimed.</typeparam>
    /// <typeparam name="TClaimant">Type of <see cref="IActor" /> making the claim.</typeparam>
    /// <remarks>The expectation is that the claimant calls <see cref="IPropClaim{TProp,TClaimant}.TryCommit" /> in <see cref="IPropClaim{TProp,TClaimant}.CommitPose" />
    /// which then causes the <see cref="IPropClaim{TProp,TClaimant}.ClaimedProp" /> to be placed in its <see cref="IActor.Inventory" />. The
    /// claimant should expect that the call to <see cref="IPropClaim{TProp,TClaimant}.TryCommit" /> can fail if the
    /// handle was cancelled for unexpected reasons.</remarks>
    public interface IPropClaim<TProp, TClaimant> : ICancellable, IDisposable
        where TClaimant : IActor
        where TProp : IProp {
        /// <summary>
        /// Event be invoked when the claim is committed.
        /// </summary>
        // We use a tuple argument instead of just passing the IPropClaim to avoid boxing allocations if the IPropClaim is implement as a struct.
        public event Action<(TProp Prop, long Quanity, TClaimant Claimant)> Committed;

        /// <summary>
        /// Event be invoked when the claim is cancelled.
        /// </summary>
        // We use a tuple argument instead of just passing the IPropClaim to avoid boxing allocations if the IPropClaim is implement as a struct. 
        public event Action<(TProp Prop, long Quanity, TClaimant Claimant)> Released;

        /// <summary>
        /// The pose in which a claim can be committed.
        /// </summary>
        public Pose CommitPose { get; }

        /// <summary>
        /// The <typeparamref name="TClaimant"/> of the claim.
        /// </summary>
        [NotNull]
        public TClaimant Claimant { get; }

        /// <summary>
        /// Whether the claim was committed.
        /// </summary>
        public bool IsCommitted { get; }

        /// <summary>
        /// Whether the claim was cancelled.
        /// </summary>
        public bool IsCancelled { get; }

        /// <summary>
        /// The quantity of <see cref="IProp"/> that is claimed.
        /// </summary>
        public long Quantity { get; }

        /// <summary>
        /// The <see cref="IProp"/> that is claimed.
        /// </summary>
        [NotNull]
        public TProp ClaimedProp { get; }

        /// <summary>
        /// Commit the claim.
        /// </summary>
        /// <returns>True if the claim was successfully committed, false otherwise.</returns>
        public bool TryCommit ();
        
        /// <summary>
        /// The optional effects to play by an actor when the claim is committed.
        /// </summary>
        public ActorFx Fx { get; }
    }
}