#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    [CreateAssetMenu(menuName = nameof(Readymade) + "/Traits/Trait Validator", fileName = "New Trait Validator",
        order = 0)]
    public class TraitValidator : ScriptableObject
    {
        [FormerlySerializedAs("_traitDefinitions")]
        [InfoBox(
            "This validator generates a set of traits with randomized values and validates them against a set of rules. If all rules pass. The trait set is considered to be valid.")]
        [Tooltip("These traits will be sampled and assigned to all spawned visitors.")]
        [SerializeField]
        private TraitDefinition[] traitDefinitions;

        [FormerlySerializedAs("_rules")]
        [FormerlySerializedAs("_spawnRules")]
        [Tooltip("These rules will be validated against the generated traits. If all pass a visitor will spawn with them.")]
        [SerializeField]
        private RuleBase[] rules;

        /// <summary>
        /// Generate a set of <see cref="TraitValue"/> instances based on the configured <see cref="TraitDefinition"/> collection.
        /// </summary>
        /// <returns>The generated set of trait values.</returns>
        public TraitValue[] GenerateTraits()
        {
            TraitValue[] result = new TraitValue[traitDefinitions.Length];
            for (int i = 0; i < traitDefinitions.Length; i++)
            {
                TraitDefinition definition = traitDefinitions[i];
                result[i] = new TraitValue(definition, definition.GenerateValue());
            }

            return result;
        }

        /// <summary>
        /// Checks if all rules defined in these settings are valid for a given array of traits.
        /// </summary>
        /// <param name="traits">The traits to check.</param>
        /// <returns>Whether all rules are valid.</returns>
        public bool ValidateTraits(TraitValue[] traits)
        {
            foreach (RuleBase rule in rules)
            {
                if (!rule.Validate(traits))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if all rules defined in these settings are valid for a given set of traits.
        /// </summary>
        /// <param name="traits">The traits to check.</param>
        /// <returns>Whether all rules are valid.</returns>
        public bool ValidateTraits(IEnumerable<TraitValue> traits)
        {
            TraitValue[] traitArray = traits.ToArray();
            foreach (RuleBase rule in rules)
            {
                if (!rule.Validate(traitArray))
                {
                    return false;
                }
            }

            return true;
        }
    }
}