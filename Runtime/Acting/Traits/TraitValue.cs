using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Readymade.Machinery.Acting.Traits
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a trait instance as a way to annotate a particular object. A trait is fundamentally just a float value that is
    /// associated with shared session data that defines its semantics in the context of other traits and associated behaviour.
    /// </summary>
    /// <remarks>
    /// <para>Traits are implemented in the flyweight pattern. This part implements the context state.</para>
    /// <para><see cref="T:Builder.Simulation.Traits.TraitValue" /> instances with different <see cref="P:Builder.Simulation.Traits.TraitValue.Value" /> but the same <see cref="P:Builder.Simulation.Traits.TraitValue.Definition" /> will
    /// have the same hash code. As such <see cref="T:Builder.Simulation.Traits.TraitValue" /> instances must be unique per <see cref="P:Builder.Simulation.Traits.TraitValue.Definition" /> in
    /// datastructures that are indexed by hashcode.</para>
    /// </remarks>
    /// <seealso cref="T:Builder.Simulation.Traits.TraitDefinition" />
    [Serializable]
    public struct TraitValue : IEquatable<TraitValue>
    {
        /// <summary>
        /// Create a readonly trait instance from a definition and a value.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="value"></param>
        public TraitValue(TraitDefinition definition, float value)
        {
            Definition = definition;
            Value = value;
        }

        /// <summary>
        /// The definition of this trait. I.e. the trait this instance is a value of.
        /// </summary>
        [JsonProperty(PropertyName = "Definition")]
        [field: SerializeField]
        public TraitDefinition Definition { get; private set; }

        /// <summary>
        /// The value of this trait.
        /// </summary>
        [JsonProperty(PropertyName = "Value")]
        [field: SerializeField]
        public float Value { get; private set; }

        /// <summary>
        /// The value of this trait normalized to its definition range.
        /// </summary>
        [JsonIgnore]
        public float NormalizedValue => Mathf.InverseLerp(Definition.Range.x, Definition.Range.y, Value);

        /// <inheritdoc />
        public bool Equals(TraitValue other)
        {
            return Definition == other.Definition && Value.Equals(other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TraitValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Definition); // diffused hash
        }
    }
}