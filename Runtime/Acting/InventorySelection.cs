namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Defines the inventory selection method
    /// </summary>
    public enum InventorySelection
    {
        /// <summary>
        /// Use a local reference
        /// </summary>
        LocalReference,

        /// <summary>
        /// Get the inventory from the service locator. Will request <see cref="IInventory{TItem}"/>.
        /// </summary>
        ServiceLocator,

        /// <summary>
        /// Get the inventory from the prop broker.
        /// </summary>
        PropBroker
    }
}