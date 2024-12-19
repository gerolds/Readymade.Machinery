using Readymade.Databinding;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits
{
    /// <inheritdoc />
    [CreateAssetMenu(
        menuName = nameof(Readymade) +
            "/" + nameof(Machinery) +
            "/" + nameof(Acting) +
            "/" + nameof(Traits) +
            "/" + nameof(IntVariableComparerRule),
        fileName = "New " + nameof(IntVariableComparerRule), order = 0)]
    public sealed class BoolVariableComparerRule : VariableComparerRule<bool>
    {
        [FormerlySerializedAs("global")]
        [FormerlySerializedAs("_variable")]
        [Tooltip("The variable to use in the comparison.")]
        [SerializeField]
        private BoolVariable variable;

        [FormerlySerializedAs("_trait")]
        [Tooltip("The trait to use in the comparison.")]
        [SerializeField]
        private TraitDefinition trait;

        /// <inheritdoc />
        protected override SoVariable<bool> Variable => variable;

        /// <inheritdoc />
        protected override TraitDefinition Trait => trait;

        /// <inheritdoc />
        public override float RemapVariableToTrait(SoVariable<bool> variable, TraitDefinition trait)
        {
            return RemapHelper.Remap(
                0,
                1,
                trait.Range.x,
                trait.Range.y,
                variable.Value ? 1f : 0
            );
        }
    }
}