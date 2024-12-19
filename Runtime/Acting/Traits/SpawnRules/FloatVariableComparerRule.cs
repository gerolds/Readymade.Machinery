using Readymade.Databinding;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    /// <inheritdoc />
    [CreateAssetMenu(menuName = nameof(Readymade) + "/Traits/" + nameof(FloatVariableComparerRule),
        fileName = "New " + nameof(FloatVariableComparerRule), order = 0)]
    public sealed class FloatVariableComparerRule : VariableComparerRule<float>
    {
        [FormerlySerializedAs("global")]
        [FormerlySerializedAs("_variable")]
        [Tooltip("The variable to use in the comparison.")]
        [SerializeField]
        private FloatVariable variable;

        [FormerlySerializedAs("_trait")]
        [Tooltip("The trait to use in the comparison.")]
        [SerializeField]
        private TraitDefinition trait;

        /// <inheritdoc />
        protected override SoVariable<float> Variable => variable;

        /// <inheritdoc />
        protected override TraitDefinition Trait => trait;

        /// <inheritdoc />
        public override float RemapVariableToTrait(SoVariable<float> variable, TraitDefinition trait)
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