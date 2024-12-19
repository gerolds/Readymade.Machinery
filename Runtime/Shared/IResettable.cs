namespace Readymade.Machinery.Shared {
    
    /// <summary>
    /// Describes a generic reset contract.
    /// </summary>
    public interface IResettable {
        /// <summary>
        /// Reset this object and prepare it to be used again.
        /// </summary>
        public void Reset ();
    }
}