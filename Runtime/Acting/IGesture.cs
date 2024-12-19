using System.Collections.Generic;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Interface that allows for untyped references to a gesture and defines default values.
    /// </summary>
    public interface IGesture
    {
        /// <summary>
        /// Value that is treated as an undefined <see cref="IProp"/>.
        /// </summary>
        public static PropCount NoProp => default;

        /// <summary>
        /// Value that is treated as an undefined <see cref="Pose"/>.
        /// </summary>
        public static Pose NoPose => default;

        /// <summary>
        /// Whether the pose is required. If true, the pose can only be started and completed while the <see cref="IActor.Pose"/>
        /// of <typeparamref name="IActor"/> matches <see cref="Pose"/>.
        /// </summary>
        /// <seealso cref="PoseComparer"/>
        public bool IsPoseRequired { get; }

        /// <summary>
        /// A descriptive name for this gesture.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The pose in which the gesture can be started and completed.
        /// </summary>
        public Pose Pose { get; }

        /// <summary>
        /// The <see cref="IProp"/> that is required to complete this gesture.
        /// </summary>
        public PropCount Prop { get; }

        /// <summary>
        /// Whether the <see cref="Prop"/> is required. If true, the pose can only be started and completed while the
        /// <see cref="IActor.Inventory"/> of <typeparamref name="IActor"/> contains at least one <see cref="Prop"/>.
        /// </summary>
        public bool IsPropRequired { get; }

        /// <summary>
        /// Time elapsed since the gesture was started. A value &lt;= 0 means the gesture was not started yet.
        /// </summary>
        public float SinceStarted { get; }

        /// <summary>
        /// Time elapsed since the gesture was last ticked. A value &lt;= 0 means the gesture was not started yet.
        /// </summary>
        public float SinceTick { get; }

        /// <summary>
        /// An optional timeout duration that will cancel the gesture when elapsed. Useful as safeguard against deadlocks.
        /// When left at 0, no timeout will occur. Don't use this to implement a gesture duration. Use
        /// <see cref="IGestureHandler"/> for that.
        /// </summary>
        public float Timeout { get; }

        public RunPhase Phase { get; }

        /// <summary>
        /// Reset the gesture to its initial state. As if it has never run.
        /// </summary>
        internal void Reset();

        /// <summary>
        /// Describes the various phases a gesture can be in.
        /// </summary>
        public enum RunPhase
        {
            /// <summary>
            /// The gesture was created and is waiting to be run.
            /// </summary>
            Waiting,

            /// <summary>
            /// The gesture is being run by an actor.
            /// </summary>
            Running,

            /// <summary>
            /// The gesture was cancelled, aborted or failed for any other reason.
            /// </summary>
            Failed,

            /// <summary>
            /// The gesture has completed successfully.
            /// </summary>
            Complete
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// A gesture represents actions that are executed by an <see cref="T:Readymade.Machinery.Acting.IActor" /> in a
    /// specific <see cref="T:UnityEngine.Pose" /> while holding a specific <see cref="T:Readymade.Machinery.Acting.IProp" />.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor performing the gesture.</typeparam>
    public interface IGesture<TActor> : IGesture
    {
        /// <summary>
        /// Internal API. Sends a completion signal to the gesture. Expected to be called by
        /// <see cref="IPerformance{IActor}"/> implementation.
        /// </summary>
        internal bool TryComplete(TActor actor, IPerformance<TActor> performance);

        /// <summary>
        /// Internal API. Sends a completion signal to start the gesture. Expected to be called by
        /// <see cref="IPerformance{IActor}"/> implementation.
        /// </summary>
        internal bool TryStart(TActor actor, IPerformance<TActor> performance);

        /// <summary>
        /// Internal API. Sends a completion signal to abort the gesture. Expected to be called by
        /// <see cref="IPerformance{IActor}"/> implementation.
        /// </summary>
        internal void Abort(TActor actor, IPerformance<TActor> performance);

        /// <summary>
        /// Tick the gesture. 
        /// </summary>
        /// <remarks>True if the gesture was ticked, false otherwise. False does not mean the gesture failed, merely that the
        /// tick did not execute this time.</remarks>
        internal bool Tick(TActor actor, IPerformance<TActor> performance);

        public Sprite Sprite { get; }

        /// <summary>
        /// Checks whether a given pose is valid for completing this gesture.
        /// </summary>
        bool CheckPose(Pose pose);
    }
}