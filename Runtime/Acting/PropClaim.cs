using System;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Shared;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    [Serializable]
    public struct PropClaim
    {
        [SerializeField]
        public int Handle;

        [SerializeField]
        public SoProp Identity;

        [SerializeField]
        [Min(0)]
        public long Count;
    }
    
    /// <inheritdoc />
    public struct PropClaim<TProp, TClaimant> : IPropClaim<TProp, TClaimant>
        where TClaimant : IActor
        where TProp : IProp {
        /// <summary>
        /// Creates an instance of a <see cref="IPropClaim{TProp,TClaimant}"/>.
        /// </summary>
        /// <param name="claimant">The <see cref="IActor"/> making the claim.</param>
        /// <param name="prop">The <see cref="IProp"/> to claim.</param>
        /// <param name="quantity">The quantity of <see cref="IProp"/> that is claimed.</param>
        /// <param name="pose">The pose where this claim can be committed via <see cref="TryCommit"/>.</param>
        /// <param name="fx"></param>
        /// <param name="onCommit">The delegate to execute on commit.</param>
        /// <param name="onCancel">The delegate to execute on cancellation.</param>
        public PropClaim (
            [NotNull]
            TClaimant claimant,
            [NotNull]
            TProp prop,
            long quantity = 1,
            Pose pose = default,
            ActorFx fx = default,
            Action<(TProp Prop, long Quanity, TClaimant Claimant)> onCommit = default,
            Action<(TProp Prop, long Quanity, TClaimant Claimant)> onCancel = default
        ) {
            if ( quantity < 1 ) {
                throw new ArgumentOutOfRangeException ( nameof ( quantity ), "Must be a value > 0" );
            }

            if ( claimant == null ) {
                throw new ArgumentNullException ( nameof ( claimant ) );
            }

            if ( prop == null ) {
                throw new ArgumentNullException ( nameof ( prop ) );
            }

            IsCancelled = false;
            IsCommitted = false;
            CommitPose = pose;
            Claimant = claimant;
            ClaimedProp = prop;
            Quantity = quantity;
            Fx = fx;
            Committed = null; // required because this is a struct
            Released = null; // required because this is a struct
            Committed += onCommit;
            Released += onCancel;
        }

        /// <inheritdoc />
        public bool IsCancelled { get; private set; }

        /// <inheritdoc />
        public long Quantity { get; }

        /// <inheritdoc />
        public TProp ClaimedProp { get; }

        /// <inheritdoc />
        public event Action<(TProp Prop, long Quanity, TClaimant Claimant)> Committed;

        /// <inheritdoc />
        public event Action<(TProp Prop, long Quanity, TClaimant Claimant)> Released;

        /// <inheritdoc />
        public Pose CommitPose { get; }

        /// <inheritdoc />
        public TClaimant Claimant { get; }

        /// <inheritdoc />
        public bool IsCommitted { get; private set; }

        /// <inheritdoc />
        public ActorFx Fx { get; private set; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">If the commit was already cancelled or committed.</exception>
        public bool TryCommit () {
            if ( IsCancelled || IsCommitted ) {
                throw new InvalidOperationException ( "Commit called on an already cancelled or committed claim" );
            }

            if ( CommitPose != default && !PoseComparer.Default.Equals ( Claimant.Pose, CommitPose ) ) {
                return false;
            }

            IsCommitted = true;
            Committed?.Invoke ( (ClaimedProp, Quantity, Claimant) );
            return true;
        }

        /// <inheritdoc />
        /// <remarks>Calls to <see cref="Cancel"/> are idempotent.</remarks>
        public void Cancel () {
            if ( IsCancelled || IsCommitted ) {
                return;
            }

            IsCancelled = true;
            Released?.Invoke ( (ClaimedProp, Quantity, Claimant) );
        }

        /// <inheritdoc />
        /// <remarks>Calls to <see cref="Dispose"/> will cancel the claim and are idempotent.</remarks>
        public void Dispose () {
            if ( !IsCancelled && !IsCommitted ) {
                Cancel ();
            }

            Committed = null;
            Released = null;
        }
    }
}