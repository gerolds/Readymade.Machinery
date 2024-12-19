using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Readymade.Machinery.Acting
{
    public interface IDirector : IDisposable
    {
        /// <summary>
        /// Schedule a performance to be picked up by an actor. O(K log N) where K is the number of roles and N the number of
        /// performances that are scheduled. 
        /// </summary>
        /// <param name="key">The key for which scheduled performance must be unique. Use to prevent/detect scheduling conflicts/leaks.</param>
        /// <param name="performance">The performance to schedule.</param>
        /// <param name="getPriority">A delegate that returns the priority of the <paramref name="performance"/> for a given roleID. Will be
        /// evaluated immediately and only once.</param>
        /// <param name="deleteWhenFailed">Whether the task should be cancelled when it fails. If set to false, the task will be
        /// returned to the queue and become claimable by another actor.</param>
        void Schedule(
            [NotNull] int key,
            [NotNull] IPerformance<IActor> performance,
            Func<int, int> getPriority = null,
            bool deleteWhenFailed = true
        );

        /// <summary>
        /// Schedules a performance for a specific actor. 
        /// </summary>
        /// <remarks>Actor specific queues will be compared with role queues by the same priority just that no other
        /// actor has the opportunity to claim this particular performance.</remarks>
        /// <param name="key">The key for which this performance must be unique.</param>
        /// <param name="performance">The performance to schedule.</param>
        /// <param name="actor">The actor to schedule the performance for.</param>
        /// <param name="getPriority">The priority of the performance.</param>
        /// <exception cref="InvalidOperationException"><paramref name="key"/> is already scheduled.</exception>
        void ScheduleFor(
            [NotNull] int key,
            [NotNull] IPerformance<IActor> performance,
            [NotNull] IActor actor,
            [NotNull] Func<int> getPriority
        );

        /// <summary>
        /// Checks whether a performance is available to be claimed by a given actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns>Whether a performance can be claimed by the actor.</returns>
        bool AnyFor(IActor actor);

        /// <summary>
        /// Try to claim a performance for an actor. O(K log N) where K is the number of roles and N the number of
        /// performances that are scheduled. 
        /// </summary>
        /// <param name="actor">The actor making the claim.</param>
        /// <param name="performance">The performance that was claimed by the actor.</param>
        /// <returns>Whether a performance was claimed.</returns>
        /// <remarks>Use the <paramref name="performance"/> as a handle to manage the lifecycle of the performance.
        /// Call <see cref="IPerformance{IActor}.Cancel"/> to cancel for any reason and
        /// <see cref="IPerformance{IActor}.RunAsync"/> to run and complete it by iterating the returned IEnumerator
        /// to the end.</remarks>
        bool TryClaim([NotNull] IActor actor, out IPerformance<IActor> performance);

        /// <summary>
        /// Cancel a given performance. Cancellation is only valid for unclaimed performances. As a result, the performance
        /// will be removed from scheduling. No callbacks will be invoked.
        /// </summary>
        /// <param name="performance">The performance to cancel.</param>
        /// <remarks>This API is intended for use by the entity issuing the <paramref name="performance"/>, not by the actor
        /// running it. The actor should use <see cref="IPerformance.Cancel"/></remarks>
        void CancelWithoutNotify([NotNull] IPerformance<IActor> performance);

        /// <summary>
        /// Cancel the performance associated with a given <paramref name="key"/>. No callbacks will be invoked.
        /// </summary>
        /// <param name="key">The key to cancel.</param>
        /// <remarks>This API is intended for use by the entity managing the key, not by the actor running an associated
        /// performance. The actor should use <see cref="IPerformance.Cancel"/></remarks>
        void CancelWithoutNotify([NotNull] int key);

        /// <summary>
        /// Retrieves the roles a given actor has.
        /// </summary>
        /// <param name="actor">The actor to retrieve roles for.</param>
        /// <returns>The roles of the actor.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>We provide this API so roles can be implemented dynamically and independent of the IActor interface implementation.</remarks>
        RoleMask GetRoleMask([NotNull] IActor actor);


        /// <summary>
        /// Get the <see cref="IPerformance{IActor}"/> scheduled under the given key.
        /// </summary>
        IPerformance<IActor> GetPerformance(int key);

        /// <summary>
        /// Get the <see cref="IPerformance{IActor}"/> scheduled under the given <see cref="IActor"/>.
        /// </summary>
        IPerformance<IActor> GetPerformance([NotNull] IActor actor);

        /// <summary>
        /// Get the key currently associated with a given <see cref="IActor"/>.
        /// </summary>
        int GetKey([NotNull] IActor actor);

        /// <summary>
        /// Get the key currently associated with a given <see cref="IPerformance{IActor}"/>.
        /// </summary>
        int GetKey([NotNull] IPerformance<IActor> performance);

        /// <summary>
        /// Get the <see cref="IActor"/> currently associated with a given key.
        /// </summary>
        IActor GetActor([NotNull] int key);

        /// <summary>
        /// Get the <see cref="IActor"/> currently associated with a given <see cref="IPerformance{IActor}"/>.
        /// </summary>
        IActor GetActor([NotNull] IPerformance<IActor> performance);

        /// <summary>
        /// Checks whether a given <see cref="IPerformance{IActor}"/> is currently scheduled.
        /// </summary>
        bool IsScheduled([NotNull] IPerformance<IActor> performance);

        /// <summary>
        /// Checks whether a <see cref="IPerformance{IActor}"/> is currently scheduled under a given <paramref name="key"/>.
        /// </summary>
        bool IsScheduled([NotNull] int key);

        /// <summary>
        /// Checks Whether a <see cref="IPerformance{IActor}"/> is currently claimed.
        /// </summary>
        bool IsClaimed([NotNull] IPerformance<IActor> performance);

        /// <summary>
        /// Checks whether a given <paramref name="key"/> is currently claimed.
        /// </summary>
        bool IsClaimed([NotNull] int key);

        /// <summary>
        /// Checks whether a <see cref="IActor"/> is currently making a claim.
        /// </summary>
        bool IsClaimed([NotNull] IActor actor);

#if UNITY_EDITOR
        IEnumerable<(object role, IEnumerable<(IPerformance perf, int prio)> queue)> Queues { get; }
        IEnumerable<(IActor actor, IPerformance perf)> Claims { get; }
        IEnumerable<(IActor actor, IEnumerable<(IPerformance perf, int prio)> queue)> ActorQueues { get; }
#endif
    }
}