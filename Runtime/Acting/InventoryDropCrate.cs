using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Represents a prefab spawned by an actor that holds dropped SoProps.
    /// </summary>
    public class InventoryDropCrate : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;

        public IInventory<SoProp> Inventory => inventory;
    }
}