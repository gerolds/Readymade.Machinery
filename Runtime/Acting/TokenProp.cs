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
    public struct TokenProp : IProp
    {
        /// <inheritdoc />
        [Obsolete("Tokens don't implement a name.")]
        public string Name => throw new NotImplementedException();

        /// <inheritdoc />
        public string DisplayName { get; set; }

        /// <inheritdoc />
        public Guid ID { get; set; }

        /// <inheritdoc />
        public float DiscoveryRange { get; set; }

        /// <inheritdoc />
        public Sprite IconSprite { get; set; }

        /// <inheritdoc />
        public Sprite Picture { get; set; }

        /// <inheritdoc />
        public GameObject ModelPrefab { get; set; }

        /// <inheritdoc />
        public Mesh Model { get; set; }

        /// <inheritdoc />
        public MaterialSymbolData IconSymbol { get; set; }

        /// <inheritdoc />
        public int Value { get; set; }

        /// <inheritdoc />
        public int Quality { get; set; }

        /// <inheritdoc />
        public int Bulk { get; set; }

        /// <inheritdoc />
        public bool IsHidden { get; set; }

        /// <inheritdoc />
        public int Category { get; set; }
    }
}