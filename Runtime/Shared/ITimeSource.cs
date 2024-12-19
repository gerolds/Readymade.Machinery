namespace Readymade.Machinery.Shared {
    
    /// <summary>
    /// Methods and properties to access a time context.
    /// </summary>
    public interface ITimeSource {
        /// <summary>
        /// Time since application start
        /// </summary>
        public float Time { get; }

        /// <summary>
        /// Time since the start of the last frame
        /// </summary>
        public float DeltaTime { get; }

        /// <summary>
        /// The scale at which time passes.
        /// </summary>
        public float TimeScale { get; }
    }
}