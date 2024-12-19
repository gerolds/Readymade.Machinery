namespace Readymade.Machinery.Shared {
    /// <inheritdoc />
    /// <summary>
    /// Replicates <see cref="N:UnityEngine" />.<see cref="P:Builder.Machinery.Shared.UnityTimeSource.Time" />.
    /// </summary>
    public class UnityTimeSource : ITimeSource {
        /// <inheritdoc />
        public float Time => UnityEngine.Time.time;

        /// <inheritdoc />
        public float DeltaTime => UnityEngine.Time.deltaTime;

        /// <inheritdoc />
        public float TimeScale => UnityEngine.Time.timeScale;
    }
}