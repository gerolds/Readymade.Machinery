using System;
using com.convalise.UnityMaterialSymbols;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Describes a prop (from: short for "theatrical property") that an actor needs to complete a <see cref="IGesture{TActor}"/>.
    /// Props are claimed through the <see cref="IProvider{TProp}"/> interface.
    /// </summary>
    /// <remarks>Note that <see cref="IProp"/> instances are classifiers and as such do not represent individual items but
    /// instead a type of object. They are the data-equivalent to a C# class definition.</remarks>
    public interface IProp
    {
        /// <summary>
        /// A internal name for this instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A descriptive name for this instance.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// A globally unique identifier for this prop.
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// Maximum range up to which this prop should be discoverable by spatial queries.
        /// </summary>
        public float DiscoveryRange { get; }

        /// <summary>
        /// The <see cref="IconSprite" /> representing this prop. 
        /// </summary>
        public Sprite IconSprite { get; }

        public Sprite Picture { get; }

        public GameObject ModelPrefab { get; }

        public Mesh Model { get; }

        public MaterialSymbolData IconSymbol { get; }

        public int Value { get; }

        public int Quality { get; }

        /// <summary>
        /// An abstract value describing mass and size of something. It has no unit and is only meaningful in
        /// relation to the values of other props. Can be used to define a storage limit.
        /// </summary>
        public int Bulk { get; }
        
        public bool IsHidden { get; }
        
        public int Category { get; }
    }
}