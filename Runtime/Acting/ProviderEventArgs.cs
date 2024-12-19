using System;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Arguments for a a <see cref="ProviderUnityEvent"/>.
    /// </summary>
    [Serializable]
    public struct ProviderEventArgs
    {
        /// <summary>
        /// The prop that is subject of the event. Maybe null if the prop is not a <see cref="SoProp"/>
        /// </summary>
        public SoProp Prop;

        /// <summary>
        /// Quantity of <see cref="Prop"/> that is subject of the event.
        /// </summary>
        public long Quantity;

        /// <summary>
        /// The claimant of the <see cref="Prop"/> that is subject of the event. Maybe null if the claimant is not a <see cref="UnityEngine.Object"/>.
        /// </summary>
        public UnityEngine.Object Claimant;
    }
}