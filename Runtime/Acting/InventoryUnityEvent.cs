using System;
using UnityEngine.Events;

namespace Readymade.Machinery.Acting {
    /// <inheritdoc />
    /// <summary>
    /// Describes a event on a <see cref="T:PropInventoryComponent" />.
    /// </summary>
    [Serializable]
    public class InventoryUnityEvent : UnityEvent<InventoryEventArgs> {
    }
}