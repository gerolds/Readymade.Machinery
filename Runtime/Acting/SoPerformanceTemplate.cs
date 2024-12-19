using UnityEngine;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting {
    
    /// <summary>
    /// Describes static/shared properties for performances. Can be used as a template in <see cref="ThresholdEvent"/>. 
    /// </summary>
    [CreateAssetMenu (
        fileName = "New" + nameof ( SoPerformanceTemplate ),
        menuName = nameof(Readymade) + "/Acting/" + nameof ( SoPerformanceTemplate )
    )]
    public class SoPerformanceTemplate : ScriptableObject {
        /// <summary>
        /// A descriptive name for the performance.
        /// </summary>
        [field: SerializeField]
        [Tooltip("A descriptive name for the performance.")]
        public string Name { get; private set; }

        /// <summary>
        /// An icon representing this performance.
        /// </summary>
        [field: FormerlySerializedAs("icon")]
        [field: SerializeField]
        [Tooltip("An icon representing this performance.")]
        public Sprite Icon { get; private set; }
        
    }
}