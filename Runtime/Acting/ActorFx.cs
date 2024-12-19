using System;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Effects an actor can play when performing an action, either generically, in a gesture or when interacting
    /// with a posed prop provider.
    /// </summary>
    public struct ActorFx : IEquatable<ActorFx>
    {
        /// <summary>
        /// The trigger ID to set on the animator of the <see cref="IActor"/>.
        /// </summary>
        public int AnimationTriggerID;

        /// <summary>
        /// The sound effect to play on the <see cref="IActor"/>.
        /// </summary>
        public AudioClip Sfx;

        /// <summary>
        /// The duration of the effect. This is useful for AI-behaviour to know how long to wait before moving on.
        /// </summary>
        public float Duration;

        /// <inheritdoc />
        public bool Equals(ActorFx other)
        {
            return AnimationTriggerID == other.AnimationTriggerID && Equals(Sfx, other.Sfx) &&
                Mathf.Approximately(Duration, other.Duration);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ActorFx other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(AnimationTriggerID, Sfx, Duration);
        }
    }
}