using MathNet.Numerics.Distributions;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Databinding;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting.Traits {
    /// <summary>
    /// Represents a shared definition of a <see cref="T:Builder.Databinding.TraitValue" />. 
    /// </summary>
    /// <remarks>Traits are implemented in the flyweight pattern. This part implements the shared state.</remarks>
    /// <seealso cref="T:Builder.Databinding.TraitValue" />
    /// <remarks>Uses <see cref="MathNet"/> to implement the distributions.</remarks>
    [Serializable]
    [CreateAssetMenu ( menuName = nameof(Readymade) + "/Traits/Trait Definition", fileName = "New Trait Definition", order = 0 )]
    public class TraitDefinition : ScriptableObject {
        [FormerlySerializedAs("_displayName")]
        [Tooltip ( "A descriptive name for this trait." )]
        [SerializeField]
        private string displayName;

        [FormerlySerializedAs("_distribution")]
        [Tooltip ( "The probability distribution to use for generating values from this definition." )]
        [SerializeField]
        private ProbabilityDistribution distribution;

        [FormerlySerializedAs("_paretoShape")]
        [Tooltip ( "The shape of the Pareto distribution. Default is 1.16 (80:20 rule)" )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Pareto )]
        private float paretoShape = 1.16f; // 1.16 == 80:20 rule

        [FormerlySerializedAs("_paretoScale")]
        [Tooltip ( "The scale of the Pareto distribution" )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Pareto )]
        private float paretoScale = 1;

        [FormerlySerializedAs("_normalMean")]
        [Tooltip ( "The median of the normal distribution" )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Normal )]
        private float normalMean = 0f;

        [FormerlySerializedAs("_standardDeviation")]
        [Tooltip ( "The standard deviation of the normal distribution" )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Normal )]
        private float standardDeviation = 25f;

        [FormerlySerializedAs("_cauchyLocation")]
        [Tooltip ( "The location of the Cauchy distribution" )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Cauchy )]
        private float cauchyLocation = 0;

        [FormerlySerializedAs("_cauchyScale")]
        [Tooltip ( "The scale of the Cauchy distribution" )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Cauchy )]
        private float cauchyScale = 25f;

        [FormerlySerializedAs("_logisticMean")]
        [Tooltip ( "The mean of the logistic distribution. Default is 6f." )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Logistic )]
        private float logisticMean = 0;

        [FormerlySerializedAs("_logisticScale")]
        [Tooltip ( "The scale the logistic distribution. Default is 2f." )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Logistic )]
        private float logisticScale = 25f;

        [FormerlySerializedAs("_laplaceLocation")]
        [Tooltip ( "The location of the laplace distribution. Default is 0." )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Laplace )]
        private float laplaceLocation = 0;

        [FormerlySerializedAs("_laplaceScale")]
        [Tooltip ( "The scale of the laplace distribution. Default is 1." )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Laplace )]
        private float laplaceScale = 25f;

        [FormerlySerializedAs("_exponentialRate")]
        [Tooltip ( "The rate of the exponential distribution. Default is 0.05." )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Exponential )]
        [Min ( 0 )]
        private float exponentialRate = 0.05f;

        [FormerlySerializedAs("_gammaShape")]
        [Tooltip ( "The shape of the gamma distribution. Default is 3." )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Gamma )]
        [Min ( 0 )]
        private float gammaShape = 1f;

        [FormerlySerializedAs("_gammaRate")]
        [Tooltip ( "The rate of the gamma distribution. Default is 0.1." )]
        [SerializeField]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Gamma )]
        [Min ( 0 )]
        private float gammaRate = 1f;

        [FormerlySerializedAs("_weibullShape")]
        [Tooltip ( "The shape of the Weibull distribution. Default is 1." )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Weibull )]
        private float weibullShape = 1;

        [FormerlySerializedAs("_weibullScale")]
        [Tooltip ( "The scale of the Weibull distribution. Default is 25." )]
        [SerializeField]
        [Min ( 0.0001f )]
        [ShowIf ( nameof ( distribution ), ProbabilityDistribution.Weibull )]
        private float weibullScale = 25;

        [FormerlySerializedAs("_range")]
        [Tooltip ( "The range of values that this trait allows. This will also truncate distributions, so if their " +
            "parametrization doesn't produce values in the range results may be unexpected. Default is (-100, 100). " +
            "Note that this truncation of the selected distributions (which are generally unbounded) and skew their " +
            "statistical properties. If possible the selected range should allow all plausible and events that the " +
            "distribution produces in the 99.9th percentile to pass unclamped." )]
        [SerializeField]
        private Vector2 range = new ( -100f, 100f );

        [FormerlySerializedAs("_id")]
        [Tooltip (
            "The ID that uniquely identifies this instance. Useful for serialization of references to this object, e.g. in a TraitValue instance." )]
        [SerializeField]
        [ReadOnly]
        private string id;

        /// <summary>
        /// A descriptive name for this trait.
        /// </summary>
        public string Name => displayName;

        /// <inheritdoc cref="Normal.Mean"/>
        public float NormalMean => normalMean;

        /// <inheritdoc cref="Normal.StdDev"/>
        /// <seealso cref="Normal"/>
        public float StandardDeviation => standardDeviation;

        /// <inheritdoc cref="Cauchy.Location"/>
        /// <seealso cref="Cauchy"/>
        public float CauchyLocation => cauchyLocation;

        /// <inheritdoc cref="Cauchy.Scale"/>
        /// <seealso cref="Cauchy"/>
        public float CauchyScale => cauchyScale;

        /// <inheritdoc cref="Pareto.Shape"/>
        /// <seealso cref="Pareto"/>
        public float ParetoShape => paretoShape;

        /// <inheritdoc cref="Pareto.Scale"/>
        /// <seealso cref="Pareto"/>
        public float ParetoScale => paretoScale;

        /// <inheritdoc cref="Logistic.Mean"/>
        /// <seealso cref="Logistic"/>
        public float LogisticMean => logisticMean;

        /// <inheritdoc cref="Logistic.Scale"/>
        /// <seealso cref="Logistic"/>
        public float LogisticScale => logisticScale;

        /// <inheritdoc cref="Laplace.Location"/>
        /// <seealso cref="Laplace"/>
        public float LaplaceLocation => laplaceLocation;

        /// <inheritdoc cref="Laplace.Scale"/>
        /// <seealso cref="Laplace"/>
        public float LaplaceScale => laplaceScale;

        /// <inheritdoc cref="Exponential.Rate"/>
        /// <seealso cref="Exponential"/>
        public float ExponentialRate => exponentialRate;

        /// <inheritdoc cref="Gamma.Rate"/>
        /// <seealso cref="Gamma"/>
        public float GammaRate => gammaRate;

        /// <inheritdoc cref="Gamma.Shape"/>
        /// <seealso cref="Gamma"/>
        public float GammaShape => gammaShape;

        /// <inheritdoc cref="Weibull.Shape"/>
        /// <seealso cref="Weibull"/>
        public float WeibullShape => weibullShape;

        /// <inheritdoc cref="Weibull.Scale"/>
        /// <seealso cref="Weibull"/>
        public float WeibullScale => weibullScale;

        /// <summary>
        /// The rance to which all returned values of <see cref="GenerateValue"/> are clamped. Note that this will truncate
        /// the selected distributions (which are generally unbounded) and skew their statistical properties. If possible
        /// the selected range should allow all plausible and events that the distribution produces in the 99.9th percentile
        /// to pass unclamped.
        /// </summary>
        public Vector2 Range => range;

        /// <inheritdoc />
        public string ID {
            get => id;
            private set => id = value;
        }

        /// <summary>
        /// The probability distribution to used in <see cref="GenerateValue"/>.
        /// </summary>
        public ProbabilityDistribution Distribution => distribution;


        /// <summary>
        /// Generate a sample from the selected <see cref="Distribution"/>.
        /// </summary>
        /// <returns>The sampled value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When an undefined distribution is selected.</exception>
        public float GenerateValue () {
            return Mathf.Clamp ( distribution switch {
                ProbabilityDistribution.Uniform => UnityEngine.Random.Range ( Range.x, Range.y ),
                ProbabilityDistribution.Normal => ( float ) Normal.Sample ( NormalMean, StandardDeviation ),
                ProbabilityDistribution.Cauchy => ( float ) Cauchy.Sample ( CauchyLocation, CauchyScale ),
                ProbabilityDistribution.Pareto => ( float ) Pareto.Sample ( ParetoScale, ParetoShape ),
                ProbabilityDistribution.Logistic => ( float ) Logistic.Sample ( LogisticMean, LogisticScale ),
                ProbabilityDistribution.Laplace => ( float ) Laplace.Sample ( LaplaceLocation, LaplaceScale ),
                ProbabilityDistribution.Gamma => ( float ) Gamma.Sample ( GammaShape, GammaRate ),
                ProbabilityDistribution.Exponential => ( float ) Exponential.Sample ( ExponentialRate ),
                ProbabilityDistribution.Weibull => ( float ) Weibull.Sample ( WeibullShape, WeibullScale ),
                _ => throw new ArgumentOutOfRangeException ()
            }, Range.x, Range.y );
        }

#if UNITY_EDITOR
        /// <summary>
        /// Generate a new unique ID (GUID) for this instance.
        /// </summary>
        [Button]
        public void GenerateNewID () {
            ID = Guid.NewGuid ().ToString ();
            UnityEditor.EditorUtility.SetDirty ( this );
        }

        /// <summary>
        /// Editor event.
        /// </summary>
        protected virtual void Reset () {
            GenerateNewID ();
        }

        /// <summary>
        /// Editor event.
        /// </summary>
        protected virtual void OnValidate () {
            EnsureID ();
        }

        /// <summary>
        /// Ensure that the instance has a valid ID.
        /// </summary>
        private void EnsureID () {
            if ( string.IsNullOrEmpty ( ID ) ) {
                GenerateNewID ();
            }
        }
#endif
    }

    /// <summary>
    /// A set of probability distribution.
    /// </summary>
    public enum ProbabilityDistribution {
        /// <summary>
        /// Default standard deviation (plain random value in a defined range).
        /// </summary>
        /// <seealso cref="UnityEngine.Random"/>
        Uniform,

        /// <summary>
        /// Normal (gaussian) distribution (bell curve with median and standard deviation).
        /// </summary>
        /// <seealso cref="Normal"/>
        Normal,

        /// <summary>
        /// Cauchy distribution (fatter tails than normal).
        /// </summary>
        /// <seealso cref="Cauchy"/>
        Cauchy,

        /// <summary>
        /// Pareto distribution.
        /// </summary>
        /// <seealso cref="Pareto"/>
        Pareto,

        /// <summary>
        /// Logistic distribution.
        /// </summary>
        /// <seealso cref="Logistic"/>
        Logistic,

        /// <summary>
        /// Laplace distribution.
        /// </summary>
        /// <seealso cref="Laplace"/>
        Laplace,

        /// <summary>
        /// Gamma distribution.
        /// </summary>
        /// <seealso cref="Gamma"/>
        Gamma,

        /// <summary>
        /// Exponential distribution.
        /// </summary>
        /// <seealso cref="Exponential"/>
        Exponential,

        /// <summary>
        /// Weibull distribution.
        /// </summary>
        /// <seealso cref="Weibull"/>
        Weibull
    }
}