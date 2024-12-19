using System;
using com.convalise.UnityMaterialSymbols;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A token prop with just a name. 
    /// </summary>
    [Serializable]
    public class Prop : IProp
    {
        ///<inheritdoc/>
        public string Name { get; }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public Guid ID { get; }

        /// <inheritdoc />
        public float DiscoveryRange { get; set; } = float.MaxValue;

        /// <inheritdoc />
        public Sprite IconSprite { get; }

        public Sprite Picture { get; set; }
        public GameObject ModelPrefab { get; set; }
        public Mesh Model { get; set; }
        public MaterialSymbolData IconSymbol { get; set; }
        public int Value { get; set; }
        public int Quality { get; set; }

        /// <inheritdoc />
        public int Bulk { get; set; }

        public bool IsHidden { get; set; }
        public int Category { get; set; }

        /// <summary>
        /// Creates a new instance of a <see cref="Prop"/>.
        /// </summary>
        /// <param name="id">A globally unique ID for this prop.</param>
        /// <param name="name">The name of this <see cref="IProp"/>.</param>
        /// <param name="sprite">A sprite illustrating this prop.</param>
        /// <param name="bulk">A relative value describing the size and mass of this prop.</param>
        public Prop(Guid id = default, string name = default, Sprite sprite = default, int bulk = 0)
        {
            Name = name;
            DisplayName = name;
            IconSprite = sprite;
            Bulk = bulk;
            ID = id == default ? Guid.NewGuid() : id;
        }
    }
}