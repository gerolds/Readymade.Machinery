using System;
using com.convalise.UnityMaterialSymbols;
using Readymade.Persistence;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc cref="IProp" />
    /// <summary>
    /// A token scriptable object prop with just a name. 
    /// </summary>
    [CreateAssetMenu(
        menuName = nameof(Readymade) + "/" + nameof(Machinery) + "/" + nameof(Acting) + "/" + nameof(SoProp),
        fileName = "New " + nameof(SoProp), order = 0)]
    public class SoProp : ScriptableObject, IProp, IAssetIdentity, IComparable<SoProp>
    {
        public enum PropCategory
        {
            Default
        }

        [BoxGroup("Identity")]
        [ReadOnly]
        [SerializeField]
        private string id;

        [BoxGroup("Description")]
        [SerializeField]
        private string displayName;

        [BoxGroup("Description")]
        [TextArea(3, 10)]
        [SerializeField]
        private string description;

        [BoxGroup("Metadata")]
        [SerializeField]
        [Tooltip(
            "The quality of the prop. This is a relative value without unit, it only makes sense in comparison with " +
            "other props. Can be used to represent durability, rarity or craftsmanship.")]
        private int quality;

        [BoxGroup("Metadata")]
        [SerializeField]
        [Tooltip(
            "The value of the prop. This is a relative value without unit, it only makes sense in comparison with " +
            "other props. Can be used to represent a monetary value or rarity.")]
        private int value;

        [BoxGroup("Metadata")]
        [SerializeField]
        [Tooltip(
            "The mass and size of the prop. This is a relative value without unit, it only makes sense in comparison " +
            "with other props.")]
        [MinValue(0)]
        private int bulk;

        [BoxGroup("Metadata")]
        [SerializeField]
        [Tooltip(
            "An internal classifier that helps with sorting and filtering. This is not meant to be user-facing or " +
            "overloaded with game-specific meaning.")]
        private PropCategory category;

        [BoxGroup("Metadata")]
        [SerializeField]
        [Tooltip("Marks a prop as internal use only. This is meant to filter props from user-facing interfaces. " +
            "For example a prop that marks a feature unlock or skill should in itself be invisible to the user " +
            "and exposed only by whatever it unlocks.")]
        private bool isHidden = false;

        [BoxGroup("Visuals")]
        [FormerlySerializedAs("<Model>k__BackingField")]
        [SerializeField]
        private Mesh model;

        [BoxGroup("Visuals")]
        [FormerlySerializedAs("<Picture>k__BackingField")]
        [SerializeField]
        // [ShowAssetPreview]
        private Sprite picture;

        [BoxGroup("Visuals")]
        [FormerlySerializedAs("<Icon>k__BackingField")]
        [SerializeField]
        // [ShowAssetPreview]
        private Sprite iconSprite;

        [BoxGroup("Visuals")] [SerializeField] private MaterialSymbolData iconSymbol;

        [BoxGroup("Visuals")]
        [SerializeField]
        [AssetsOnly]
        // [ShowAssetPreview]
        private GameObject modelPrefab;

        ///<inheritdoc/>
        public string Name => name;

        public string DisplayName => displayName;
        public Guid ID => AssetID;

        /// <inheritdoc cref="IAssetIdentity"/>
        public Guid AssetID => Guid.TryParse(id, out var parsedID) ? parsedID : Guid.Empty;

        /// <inheritdoc />
        [field: SerializeField]
        public float DiscoveryRange { get; private set; } = 10f;

        /// <inheritdoc />
        public Sprite IconSprite => iconSprite;

        /// <inheritdoc />
        public MaterialSymbolData IconSymbol => iconSymbol;

        /// <inheritdoc />
        public Sprite Picture => picture;

        /// <inheritdoc />
        public GameObject ModelPrefab => modelPrefab;

        /// <inheritdoc />
        public Mesh Model => model;

        /// <inheritdoc />
        public int Bulk => bulk;

        /// <inheritdoc />

        public int Value => value;

        /// <inheritdoc />
        public int Quality => quality;

        /// <inheritdoc />
        public string Description => description;

        /// <inheritdoc />
        public bool IsHidden => isHidden;

        /// <inheritdoc />
        public int Category => (int)category;

        [Button]
        private void NewID()
        {
            id = Guid.NewGuid().ToString();
        }

        public int CompareTo(SoProp other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(id, other.id, StringComparison.Ordinal);
        }
    }
}