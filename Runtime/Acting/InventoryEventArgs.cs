using System;

namespace Readymade.Machinery.Acting {
    /// <summary>
    /// Arguments for a <see cref="InventoryUnityEvent"/>.
    /// </summary>
    [Serializable]
    public struct InventoryEventArgs {
        /// <summary>
        /// The prop that is subject of the event. Maybe null if the prop is not a <see cref="SoProp"/>
        /// </summary>
        public SoProp Prop;

        /// <summary>
        /// Count of <see cref="Prop"/> in the inventory before the change.
        /// </summary>
        public long OldCount;

        /// <summary>
        /// Count of <see cref="Prop"/> in the inventory after the change.
        /// </summary>
        public long NewCount;
    }
}