using Readymade.Databinding;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    /// <inheritdoc />
    [CreateAssetMenu(menuName = nameof(Readymade) + "/Traits/" + nameof(IntVariableComparerRule),
        fileName = "New " + nameof(IntVariableComparerRule), order = 0)]
    public sealed class IntVariableComparerRule : VariableComparerRule<int>
    {
        [FormerlySerializedAs("global")]
        [FormerlySerializedAs("_variable")]
        [Tooltip("The variable to use in the comparison.")]
        [SerializeField]
        private IntegerVariable variable;

        [FormerlySerializedAs("_trait")]
        [Tooltip("The trait to use in the comparison.")]
        [SerializeField]
        private TraitDefinition trait;

        /// <inheritdoc />
        protected override SoVariable<int> Variable => variable;

        /// <inheritdoc />
        protected override TraitDefinition Trait => trait;

        /// <inheritdoc />
        public override float RemapVariableToTrait(SoVariable<int> variable, TraitDefinition trait)
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