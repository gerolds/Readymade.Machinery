using UnityEngine;

namespace Readymade.Machinery.Shared {
    /// <inheritdoc cref="SoTimeSource"/>
    /// <remarks>
    /// This time source represents the standard unity time.
    /// </remarks>
    [CreateAssetMenu ( menuName = "Machinery/Time/Unity Time Source", fileName = "New Unity Time Source", order = 0 )]
    public class SoUnityTimeSource : SoTimeSource {
        /// <inheritdoc />
        public override float Time => UnityEngine.Time.time;

        /// <inheritdoc />
        public override float DeltaTime => UnityEngine.Time.deltaTime;

        /// <inheritdoc />
        public override float TimeScale => UnityEngine.Time.timeScale;
    }
}