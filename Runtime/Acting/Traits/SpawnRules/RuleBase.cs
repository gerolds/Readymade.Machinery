using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    /// <summary>
    /// Describes a rule that can be validated against a collection of traits.
    /// </summary>
    public abstract class RuleBase : ScriptableObject
    {
        [FormerlySerializedAs("_displayName")]
        [Tooltip("A descriptive name for this rule.")]
        [SerializeField]
        private string displayName;

        /// <summary>
        /// A descriptive name for this rule.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Checks whether the rule is valid for the given set of traits.
        /// </summary>
        /// <param name="traits">The traits to validate against.</param>
        /// <returns>The validation result.</returns>
        public abstract bool Validate(IEnumerable<TraitValue> traits);
    }
}