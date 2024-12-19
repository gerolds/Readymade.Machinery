#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using System;
using System.Collections.Generic;
using Readymade.Databinding;
using UnityEngine;

namespace Readymade.Machinery.Acting.Traits
{
    /// <summary>
    /// A rule that compares a <see cref="SoVariable"/> to a <see cref="TraitDefinition"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the <see cref="SoVariable"/></typeparam>
    public abstract class VariableComparerRule<T> : RuleBase where T : struct
    {
        /// <summary>
        /// The variable to use in the comparison.
        /// </summary>
        protected abstract SoVariable<T> Variable { get; }

        /// <summary>
        /// The trait to use in the comparison.
        /// </summary>
        protected abstract TraitDefinition Trait { get; }

        /// <summary>
        /// A remapping function that converts the variables range to the trait's.
        /// </summary>
        public abstract float RemapVariableToTrait(SoVariable<T> variable, TraitDefinition trait);

        /// <summary>
        /// The remapped variable value expressed as fraction of the trait's value range. Useful for simple configuration
        /// and keeping the comparison valid when trait or variable values change.
        /// </summary>
#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
#else
        [ShowNativeProperty]
#endif
        private float VariableAsFractionOfTrait
        {
            get
            {
                if (Variable == null || Trait == null)
                {
                    return float.NaN;
                }

                float feeAsWealth = RemapVariableToTrait(Variable, Trait);
                double asFraction = RemapHelper.InverseLerpUnclamped(Trait.Range.x, Trait.Range.y, feeAsWealth);
                return (float) asFraction;
            }
        }

        [Tooltip("The operator of the comparison.")]
        [BoxGroup("Fail condition")]
        [SerializeField]
        private Operator comparison;

        [Tooltip("The parameter of the comparison. Will be compares to Variable As Fraction Of Trait")]
        [BoxGroup("Fail condition")]
        [SerializeField]
        [Range(0, 1f)]
        private float value;

        /// <inheritdoc />
        public override bool Validate(IEnumerable<TraitValue> traits)
        {
            double feeAsPercentOfWealth = VariableAsFractionOfTrait;
            foreach (TraitValue traitValue in traits)
            {
                if (
                    traitValue.Definition == Trait &&
                    comparison switch
                    {
                        Operator.LessThan    => feeAsPercentOfWealth < value,
                        Operator.GreaterThan => feeAsPercentOfWealth > value,
                        _                    => throw new ArgumentOutOfRangeException()
                    }
                )
                {
                    return false;
                }
            }

            return true;
        }
    }
}