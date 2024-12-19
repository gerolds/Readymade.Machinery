using UnityEngine;

namespace Readymade.Machinery.Shared {
    /// <summary>
    /// Provides timing for the acting system. By default uses <see cref="UnityEngine"/>.<see cref="UnityEngine.Time"/> via <see cref="UnityTimeSource"/>.
    /// </summary>
    /// <remarks>All time queries in <see cref="Machinery"/> are made through this interface, allowing <see cref="Machinery"/> to
    /// operate on a timeline independent from the default time in <see cref="UnityEngine"/>.</remarks>
    public static class Timing {
        /// <summary>
        /// The <see cref="ITimeSource"/> used by the acting system.
        /// </summary>
        /// <remarks>This should only ever be set once during project initialization. Behaviour of the acting system is undefined
        /// if this value changes after any type in <see cref="Readymade.Machinery"/> was instantiated.</remarks>
        public static ITimeSource Source { get; private set; } = new UnityTimeSource ();

        public static void SetTimeSource ( ITimeSource timeSource ) {
            Debug.LogWarning ( $"[{nameof ( Timing )}] Time source was changed" );
            Source = timeSource;
        }
    }
}