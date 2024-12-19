using Readymade.Databinding;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    /// <inheritdoc />
    [CreateAssetMenu(menuName = nameof(Readymade) + "/Traits/" + nameof(LongVariableComparerRule),
        fileName = "New " + nameof(LongVariableComparerRule), order = 0)]
    public sealed class LongVariableComparerRule : VariableComparerRule<long>
    {
        [FormerlySerializedAs("global")]
        [FormerlySerializedAs("_variable")]
        [Tooltip("The variable to use in the comparison.")]
        [SerializeField]
        private LongVariable variable;

        [FormerlySerializedAs("_trait")]
        [Tooltip("The trait to use in the comparison.")]
        [SerializeField]
        private TraitDefinition trait;

        /// <inheritdoc />
        protected override SoVariable<long> Variable => variable;

        /// <inheritdoc />
        protected override TraitDefinition Trait => trait;

        /// <inheritdoc />
        public override float RemapVariableToTrait(SoVariable<long> variable, TraitDefinition trait)
        {
            return RemapHelper.Remap(
                variable.ClampMin,
                Variable.ClampMax,
                trait.Range.x,
                trait.Range.y,
                variable.Value
            );
        }
    }
}