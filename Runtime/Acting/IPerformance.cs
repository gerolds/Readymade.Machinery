using System;
using System.Collections;
using System.Collections.Generic;
using Readymade.Machinery.Shared;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A performance represents a sequence of gestures (<see cref="IGesture"/>).
    /// </summary>
    public interface IPerformance : IResettable, ICancellable, IDisposable
    {
        public string Name { get; set; }

        /// <summary>
        /// The run phase this performance is currently in.
        /// </summary>
        public RunPhase Phase { get; }

        /// <summary>
        /// All the gestures that compose this performance.
        /// </summary>
        IEnumerable<IGesture> Gestures { get; }

        /// <summary>
        /// How often this performance has been re-used.
        /// </summary>
        public int RunCount { get; }

        /// <summary>
        /// The current gesture being executed, if any.
        /// </summary>
        public IGesture CurrentGesture { get; }

        /// <summary>
        /// Describes the phases a performance can be in.
        /// </summary>
        public enum RunPhase
        {
            /// <summary>
            /// The performance was created and is waiting to be run.
            /// </summary>
            Waiting,

            /// <summary>
            /// The performance is being run by an actor.
            /// </summary>
            Running,

            /// <summary>
            /// The performance was cancelled, aborted or failed for any other reason.
            /// </summary>
            Failed,

            /// <summary>
            /// The performance has completed successfully.
            /// </summary>
            Complete
        }
    }

    /// <summary>
    /// A performance represents a sequence of gestures (<see cref="IGesture{IActor}"/>).
    /// </summary>
    /// <typeparam name="IActor">The type of actor that can execute the performance.</typeparam>
    public interface IPerformance<IActor> : IPerformance
    {
        /// <summary>
        /// Event invoked before any gesture in the performance have is started.
        /// </summary>
        public event Action<Performance, IActor> Started;

        /// <summary>
        /// Event invoked after all gestures in the performance have completed.
        /// </summary>
        public event Action<Performance, IActor> Completed;

        /// <summary>
        /// Event invoked after the performance was aborted or failed for any reason.
        /// </summary>
        public event Action<Performance, IActor> Failed;

        /// <summary>
        /// Event invoked after the current gesture was completed and the next gesture was selected but before the next gesture is
        /// started. <see cref="CurrentGesture"/> will return the selected next gesture.
        /// </summary>
        public event Action<Performance, IActor> NextGesture;

        /// <summary>
        /// The current gesture being executed, if any. During <see cref="NextGesture"/> callbacks this will already return said next
        /// gesture.
        /// </summary>
        public new IGesture<IActor> CurrentGesture { get; }

        /// <summary>
        /// The actor currently running this performance.
        /// </summary>
        public IActor RunActor { get; }

        /// <summary>
        /// Whether this performance can be started.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Whether this performance has started but has not yet been cancelled or completed.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Whether this performance has been cancelled or otherwise prematurely aborted.
        /// </summary>
        bool IsFailed { get; }

        /// <summary>
        /// Whether the optional timeout of this performance has elapsed.
        /// </summary>
        /// <returns></returns>
        internal bool IsTimeoutElapsed();

        /// <summary>
        /// Runs this performance. This creates an <see cref="IEnumerator"/> that has to be iterated manually (by the agent) or used as a
        /// coroutine. To cancel the performance, or to reuse it, call <see cref="Cancel"/> or <see cref="Reset"/> respectively.
        /// </summary>
        /// <param name="actor">The <typeparamref name="IActor"/> that evaluates/runs the performance and receives all its callbacks.</param>
        /// <returns>The <see cref="IEnumerator"/> that represents the incremental, asynchronous evaluation of the performance.</returns>
        public IEnumerator RunAsync(IActor actor);

        /// <summary>
        /// Builder API. Appends a gesture to the internal gesture-sequence of this performance.
        /// </summary>
        /// <param name="gesture">The gesture to append.</param>
        /// <returns>The instance of the performance to which this gesture was added (Builder API).</returns>
        public IPerformance<IActor> AppendGesture(IGesture<IActor> gesture);

        /// <summary>
        /// Set the delegate to run when the performance completes.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <returns>The instance of the performance to which this gesture was added (Builder API).</returns>
        public IPerformance<IActor> OnCompleted(Action<Performance, IActor> action);

        /// <summary>
        /// Set the delegate to run when the performance has failed.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <returns>The instance of the performance to which this gesture was added (Builder API).</returns>
        public IPerformance<IActor> OnFailed(Action<Performance, IActor> action);

        /// <summary>
        /// Set the delegate to run when the performance has started.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <returns>The instance of the performance to which this gesture was added (Builder API).</returns>
        public IPerformance<IActor> OnStarted(Action<Performance, IActor> action);
    }
}