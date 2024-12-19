using System;
using com.convalise.UnityMaterialSymbols;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Persistence;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Defines a slot that can be unlocked by the player and accepts a SoProp.
    /// Can be used to define typed equipment slots.
    /// </summary>
    [CreateAssetMenu(menuName = nameof(Machinery) + "/" + nameof(Acting) + "/" + nameof(SoSlot),
        fileName = "New " + nameof(SoSlot), order = 0)]
    public class SoSlot : ScriptableObject, IAssetIdentity, ISerializationCallbackReceiver, ISlot
    {
        [ReadOnly] [SerializeField] private string assetID;

        [SerializeField] public string displayName;
        [TextArea(0, 30)] [SerializeField] public string description;

        [SerializeField] public MaterialSymbolData icon;
        [SerializeField] [Required] public SoProp unlockedBy;
#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowPaging = false, ShowFoldout = false)]
#else
        [ReorderableList]
#endif
        [SerializeField]
        public SoProp[] accept;
#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowPaging = false, ShowFoldout = false)]
#else
        [ReorderableList]
#endif
        [SerializeField]
        public SoProp[] reject;

        public bool IsUnlocked(IActor actor) => IsUnlocked(actor.Inventory);

        public bool IsUnlocked(IInventory<SoProp> inventory) =>
            !unlockedBy || inventory.GetAvailableCount(unlockedBy) > 0;

        public bool IsAccepting(SoProp prop) =>
            (accept.Length <= 0 || Array.IndexOf(accept, prop) != -1) &&
            (reject.Length <= 0 || Array.IndexOf(reject, prop) == -1);

        public Guid AssetID { get; private set; }
        public SoProp UnlockedBy => unlockedBy;

        [Button]
        public void NewAssetID()
        {
            AssetID = Guid.NewGuid();
            assetID = AssetID.ToString();
        }

        public void OnBeforeSerialize()
        {
            assetID = AssetID.ToString();
        }

        public void OnAfterDeserialize()
        {
            AssetID = Guid.Parse(assetID);
        }
    }
}