namespace Readymade.Machinery.Shared {
    /// <summary>
    /// Describes a generic cancellation contract.
    /// </summary>
    public interface ICancellable {
        /// <summary>
        /// Cancel this object. Signal any running components to abort. Calls to cancel are idempotent.
        /// </summary>
        public void Cancel ();
    }
}