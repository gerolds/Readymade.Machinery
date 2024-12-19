namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Represents a phase in the lifecycle of a two-phase-commit operation.
    /// </summary>
    public enum Phase
    {
        /// <summary>
        /// A claim on an item-count is issued.
        /// </summary>
        Claimed,

        /// <summary>
        /// A claim on an item-count is released.
        /// </summary>
        Released,

        /// <summary>
        /// A claim on an item-count is committed. Also, an item-count was taken from the inventory.
        /// </summary>
        Committed,

        /// <summary>
        /// An item-count was put into the inventory.
        /// </summary>
        Put,

        /// <summary>
        /// The stored count of an item was overriden.
        /// </summary>
        Set
    }
}