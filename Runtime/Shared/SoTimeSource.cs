using UnityEngine;

namespace Readymade.Machinery.Shared {
    /// <summary>
    /// A scriptable object derived <see cref="ITimeSource"/> that allows assigning custom time context in the
    /// inspector of any component that need a time source.
    /// </summary>
    /// <seealso cref="ITimeSource"/>
    public abstract class SoTimeSource : ScriptableObject, ITimeSource {
        /// <inheritdoc />
        public abstract float Time { get; }

        /// <inheritdoc />
        public abstract float DeltaTime { get; }

        /// <inheritdoc />
        public abstract float TimeScale { get; }
    }
}