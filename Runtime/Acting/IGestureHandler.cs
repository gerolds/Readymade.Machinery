using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Handler implementation for <see cref="Gesture"/>.
    /// </summary>
    /// <seealso cref="Gesture"/>
    /// <seealso cref="FunGesture"/>
    public interface IGestureHandler
    {
        /// <summary>Called when the gesture is completed by the <see cref="IActor"/>.</summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        public void OnComplete(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>Called when the gesture is started by the <see cref="IActor"/>.</summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        public void OnStart(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>Called when the gesture is ticked by the <see cref="IActor"/>.</summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        public void OnTick(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>Called when the gesture has failed for any reason. Typically when aborted.</summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        /// <returns>True if the <paramref name="gesture"/> can complete, false otherwise.</returns>
        public void OnFailed(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>
        /// Called on each tick on the <see cref="IGesture{IActor}"/> to check whether the gesture can complete.
        /// </summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        /// <returns>True if the <paramref name="gesture"/> can complete, false otherwise.</returns>
        public bool CanComplete(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>
        /// Called on each tick on the <see cref="IGesture{IActor}"/> to check whether the gesture can be updated.
        /// </summary>
        /// <param name="actor">The <see cref="IActor"/> running the <paramref name="gesture"/>.</param>
        /// <param name="performance">The <see cref="IPerformance{IActor}"/> the <paramref name="gesture"/> is part of.</param>
        /// <param name="gesture">The <see cref="IGesture{IActor}"/> calling this method.</param>
        /// <returns>True if the <paramref name="gesture"/> can update, false otherwise.</returns>
        public bool CanTick(
            IActor actor,
            IPerformance<IActor> performance,
            IGesture<IActor> gesture
        );

        /// <summary>Called to query the required pose for the gesture.</summary>
        /// <param name="pose">The required <see cref="Pose"/>, if any.</param>
        /// <returns>True if a <see cref="Pose"/> is required, false otherwise.</returns>
        public bool TryGetPose(out Pose pose);

        /// <summary>Called to query the required property for the gesture.</summary>
        /// <param name="prop">The required <see cref="SoProp"/>, if any.</param>
        /// <returns>True if a <see cref="SoProp"/> is required, false otherwise.</returns> 
        public bool TryGetProp(out PropCount prop);
        
        public bool CheckPose(Pose pose);
    }
}