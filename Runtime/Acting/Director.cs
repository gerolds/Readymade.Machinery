using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Readymade.Machinery.Shared;
using Readymade.Machinery.Shared.PriorityQueue;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// <para>
    /// Management component of the acting system. All performances are scheduled and claimed through it. 
    /// </para>
    /// <para>
    /// The goal of the acting system is to allow any system to define arbitrary effects that get executed through
    /// <see cref="IGesture{IActor}"/>-sequences (<see cref="IPerformance{IActor}"/>)
    /// by a pool of agents via their <see cref="IActor"/>. While having the agents know nothing about the details
    /// of these actions. This allows the agent behaviour to be implemented generically as sequences of "move to
    /// pose, wait, repeat" cycles with all side-effects injected (even dynamically) by the performance issuer. As a
    /// result, additional systems that issue performances can be added without any changes to the behaviours that
    /// execute them.
    /// </para>
    /// <para>
    /// The director uses a key to differentiate performance subjects for which performance claims and scheduling must
    /// be unique. The key itself does not care what it represents, it is merely a token. A performance issuing system
    /// may arbitrarily define what separation of performances it wants to maintain by issuing discrete tokens for each
    /// grouping. Each <see cref="RoleMask"/> has a separate queue, which in turn represent different priorities. A
    /// reference to the <see cref="IActor"/> gets passed into the gesture event handlers.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The director is purely responsible for scheduling of performance and enforcing its invariants, it is not responsible for
    /// executing any <see cref="IPerformance{IActor}"/> instances or its <see cref="IGesture{IActor}"/> components. It  provides
    /// coordination via start-, tick-, fail- and completion-events such that a performance's effects are transparent to its
    /// executing <see cref="IActor"/> and the acting system itself (inversion of control).
    /// </para>
    /// <para>
    ///  Invariants:
    ///  <list type="numbered">
    ///  <item> At any point in time, only one <see name="IActor"/> can have a claim on a given performance</item>
    ///  <item> At any point in time, only one <see cref="IPerformance{IActor}"/> can be scheduled for a given key</item>
    ///  <item> At any point in time, only one <see name="IActor"/> can be associated with a given key (transitive property)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Any <see cref="IGesture"/> may depend on a <see cref="IProp"/>. Props are tokens that can be stored in an
    /// <see cref="IInventory{TItem}"/> and acquired through an <see cref="IProvider{TProp}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IProp"/><seealso cref="IProvider{TProp}"/>
    public class Director : IDirector
    {
        /// <summary>
        /// Map that keeps track of key to performance associations.
        /// </summary>
        private readonly Dictionary<int, IPerformance<IActor>> _keyPerformanceMap = new();

        /// <summary>
        /// Map that keeps track of performance to key associations.
        /// </summary>
        private readonly Dictionary<IPerformance<IActor>, int> _performanceKeyMap = new();

        /// <summary>
        /// Map that keeps track of actor to performance associations once a performance was claimed.
        /// </summary>
        private readonly Dictionary<IActor, IPerformance<IActor>> _actorPerformanceMap = new();

        /// <summary>
        /// Tracks the priorities of a performance once evaluated.
        /// </summary>
        private readonly Dictionary<IPerformance<IActor>, Func<int, int>> _performancePriorities = new();

        /// <summary>
        /// Map that keeps track of performance to actor associations once a performance was claimed.
        /// </summary>
        private readonly Dictionary<IPerformance<IActor>, IActor> _performanceActorMap = new();

        /// <summary>
        /// Priority queues that keep track of all scheduled performances.
        /// </summary>
        private readonly Dictionary<int, SimplePriorityQueue<IPerformance<IActor>, int>> _roleQueues = new();

        /// <summary>
        /// Priority queues that keep track of all scheduled performances.
        /// </summary>
        private readonly Dictionary<IPerformance<IActor>, SimplePriorityQueue<IPerformance<IActor>, int>>
            _performancesInActorQueues = new();

        private readonly Dictionary<IActor, SimplePriorityQueue<IPerformance<IActor>, int>> _actorQueues = new();

        /// <summary>
        /// The delegate we shall use to query the role of a given actor
        /// </summary>
        private readonly Func<IActor, RoleMask> _getRoles;

        /// <summary>
        /// Reused buffer for candidates while evaluating the priority in <see cref="TryClaim"/>
        /// </summary>
        private SimplePriorityQueue<IPerformance<IActor>, int> _candidates = new();

        private int _monotonicSequence = 0;

        public bool Log { get; set; }

        /// <summary>
        /// Create a new <see cref="Director"/> instance.
        /// </summary>
        /// <param name="getRoleMask">The delegate to query the roles of a given actor. We provide this API so roles can be implemented dynamically and independent of the IActor interface implementation.</param>
        public Director(Func<IActor, RoleMask> getRoleMask)
        {
            _getRoles = getRoleMask ?? (_ => RoleMask.None);

            for (int i = 0; i < 0x20; i++)
            {
                _roleQueues[i] = new SimplePriorityQueue<IPerformance<IActor>, int>();
            }

#if UNITY_EDITOR
            DirectorRegistry.Register(this);
#endif
        }

        /// <summary>
        /// Generate a positive integer value that is incremented each time this method is called.
        /// </summary>
        /// <returns>A unique, monotonic, positive integer value.</returns>
        public int GetNextKey() => _monotonicSequence++;

        /// <summary>
        /// The pose comparer for the <typeparamref name="IActor"/> type.
        /// </summary>
        /// <remarks>
        /// This should never be changed once assigned, otherwise unexpected behaviour is practically guaranteed.
        /// </remarks>
        public PoseComparer PoseComparer => PoseComparer.Default;

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
        public void Schedule(
            [NotNull] int key,
            [NotNull] IPerformance<IActor> performance,
            Func<int, int> getPriority = null,
            bool deleteWhenFailed = true
        )
        {
            getPriority ??= Zero;

            // Ensure uniqueness of key-performance pair
            if (IsScheduled(key) || IsScheduled(performance))
            {
                // duplicate key
                throw new InvalidOperationException(
                    "Attempt to schedule a performance for an existing key. This is not allowed. Cancel the performance or its key before scheduling a new one.");
            }

            // ensure any existing key-performances association is not contradicted
            // TODO: fix boxing allocation
            if (IsScheduled(key) && (!Equals(GetKey(performance), key) || GetPerformance(key) != performance))
            {
                throw new InvalidOperationException(
                    "Attempt to schedule invalid key-performance association. Cancel the existing key/performance before scheduling a different one.");
            }

            // we ensure this performance is in a valid initial state.
            if (performance.IsRunning)
            {
                throw new InvalidOperationException(
                    "Attempt to schedule a running performance. Cancel the performance before scheduling it.");
            }

            // all validations on the performance is now complete, and we force a valid initial state on the performance.
            performance.Reset();

            // track the key-performance association
            _keyPerformanceMap[key] = performance;
            _performanceKeyMap[performance] = key;

            // the priority queue we're using has O(log n) remove so we can safely maintain a separate queue for each role.
            _performancePriorities[performance] = getPriority;

            foreach (int roleID in _roleQueues.Keys)
            {
                _roleQueues[roleID].Enqueue(performance, getPriority(roleID));
            }

            // - we subscribe to the status events on the performance to make sure we are tracking it correctly even when it isn't
            //   updated from this manager. This enables us to treat a reference to a performance also as a handle which makes the
            //   external management of performance creation a bit simpler.
            // - we use a unsub-sub pattern to make subscriptions idempotent.
            performance.Failed -= ReschedulePerformance;
            performance.Failed -= DeletePerformance;

            if (deleteWhenFailed)
            {
                performance.Failed += DeletePerformance;
            }
            else
            {
                performance.Failed += ReschedulePerformance;
            }

            performance.Completed -= DeletePerformance;
            performance.Completed += DeletePerformance;
        }

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
        public void ScheduleFor(
            [NotNull] int key,
            [NotNull] IPerformance<IActor> performance,
            [NotNull] IActor actor,
            [NotNull] Func<int> getPriority
        )
        {
            // Ensure uniqueness of key-performance pair
            if (IsScheduled(key) || IsScheduled(performance))
            {
                // duplicate key
                throw new InvalidOperationException(
                    "Attempt to schedule a performance for an existing key. This is not allowed. Cancel the performance or its key before scheduling a new one.");
            }

            // ensure any existing key-performances association is not contradicted
            // TODO: fix boxing allocation
            if (IsScheduled(key) && (!Equals(GetKey(performance), key) || GetPerformance(key) != performance))
            {
                throw new InvalidOperationException(
                    "Attempt to schedule invalid key-performance association. Cancel the existing key/performance before scheduling a different one.");
            }

            // we ensure this performance is in a valid initial state.
            if (performance.IsRunning)
            {
                throw new InvalidOperationException(
                    "Attempt to schedule a running performance. Cancel the performance before scheduling it.");
            }

            // all validations on the performance is now complete, and we force a valid initial state on the performance.
            performance.Reset();

            // track the key-performance association
            _keyPerformanceMap[key] = performance;
            _performanceKeyMap[performance] = key;

            if (!_actorQueues.ContainsKey(actor))
            {
                _actorQueues[actor] = new SimplePriorityQueue<IPerformance<IActor>, int>();
            }

            _actorQueues[actor].Enqueue(performance, getPriority.Invoke());
            _performancesInActorQueues[performance] = _actorQueues[actor];

            // - we subscribe to the status events on the performance to make sure we are tracking it correctly even when it isn't
            //   updated from this manager. This enables us to treat a reference to a performance also as a handle which makes the
            //   external management of performance creation a bit simpler.
            // - we use a unsub-sub pattern to make subscriptions idempotent.
            performance.Failed -= ReschedulePerformance;
            performance.Failed -= DeletePerformance;
            performance.Failed += DeletePerformance;
            performance.Completed -= ReschedulePerformance;
            performance.Completed -= DeletePerformance;
            performance.Completed += DeletePerformance;
        }

        /// <summary>
        /// Used as fallback priority delegate. Always returns 0.
        /// </summary>
        private int Zero(int roleID) => 0;

        /// <summary>
        /// Checks whether a performance is available to be claimed by a given actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns>Whether a performance can be claimed by the actor.</returns>
        public bool AnyFor(IActor actor)
        {
            if (_actorQueues.TryGetValue(actor, out var actorQueue) && actorQueue.Count > 0)
            {
                if (Log)
                {
                    Debug.Log(
                        $"[{nameof(Director)}] AnyFor: {actor.Name} has an actor-queue with {actorQueue.Count} entries.");
                }

                return true;
            }

            int mask = (int)_getRoles(actor);
            for (int roleID = 0; roleID < 0x20; roleID++)
            {
                if ((mask & (1 << roleID)) > 0)
                {
                    if (_roleQueues.TryGetValue(roleID, out var roleQueue) && roleQueue.Count > 0)
                    {
                        if (Log)
                        {
                            Debug.Log(
                                $"[{nameof(Director)}] AnyFor: {actor.Name} has a role-queue ({roleID}) with {roleQueue.Count} entries.");
                        }

                        return true;
                    }
                }
            }

            return false;
        }

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
        public bool TryClaim([NotNull] IActor actor, out IPerformance<IActor> performance)
        {
            performance = default;
            if (_actorPerformanceMap.ContainsKey(actor))
            {
                throw new InvalidOperationException(
                    $"Actor '{actor.Name}' has already claimed a performance ({GetKey(actor)}, {GetPerformance(actor).Name}). Release, complete or cancel the performance before trying again.");
                return false;
            }

            // find the performance with the highest priority in all allowed roles the actor has
            _candidates.Clear();
            int roleMask = (int)GetRoleMask(actor);
            for (int roleID = 0; roleID < 0x20; roleID++)
            {
                if ((roleMask & (1 << roleID)) > 0)
                {
                    _roleQueues.TryGetValue(roleID, out SimplePriorityQueue<IPerformance<IActor>, int> queue);
                    Debug.Assert(queue != null, "ASSERTION FAILED: queue != null");
                    if (queue?.TryFirst(out performance) ?? false)
                    {
                        // filter positive priorities (negative == exclude)
                        int priority = queue.GetPriority(performance);
                        if (priority >= 0)
                        {
                            _candidates.Enqueue(performance, priority);
                        }
                    }
                }
            }

            _actorQueues.TryGetValue(actor, out SimplePriorityQueue<IPerformance<IActor>, int> actorQueue);
            if (actorQueue?.TryFirst(out IPerformance<IActor> actorSpecificPerformance) ?? false)
            {
                _candidates.Enqueue(actorSpecificPerformance, actorQueue.GetPriority(actorSpecificPerformance));
            }

            if (Log)
            {
                Debug.Log($"[{nameof(Director)}] TryClaim: {actor.Name} has {_candidates.Count} candidates to claim.");
            }

            // pick the highest priority candidate, if any, then remove the performance from queues and create a claim.
            if (_candidates.TryFirst(out performance))
            {
                RemoveFromQueue(performance);
                AddClaim(actor, performance);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Cancel a given performance. Cancellation is only valid for unclaimed performances. As a result, the performance
        /// will be removed from scheduling. No callbacks will be invoked.
        /// </summary>
        /// <param name="performance">The performance to cancel.</param>
        /// <remarks>This API is intended for use by the entity issuing the <paramref name="performance"/>, not by the actor
        /// running it. The actor should use <see cref="IPerformance.Cancel"/></remarks>
        public void CancelWithoutNotify([NotNull] IPerformance<IActor> performance)
        {
            if (IsClaimed(performance))
            {
                throw new InvalidOperationException(
                    "Can only cancel an unclaimed performance. Call Cancel() on the performance first.");
            }

            DeletePerformance(performance, default);
        }

        /// <summary>
        /// Cancel the performance associated with a given <paramref name="key"/>. No callbacks will be invoked.
        /// </summary>
        /// <param name="key">The key to cancel.</param>
        /// <remarks>This API is intended for use by the entity managing the key, not by the actor running an associated
        /// performance. The actor should use <see cref="IPerformance.Cancel"/></remarks>
        public void CancelWithoutNotify([NotNull] int key) => CancelWithoutNotify(GetPerformance(key));

        /// <summary>
        /// Removes a given performance from all queues.
        /// </summary>
        private void RemoveFromQueue([NotNull] IPerformance<IActor> performance)
        {
            foreach (SimplePriorityQueue<IPerformance<IActor>, int> roleQ in _roleQueues.Values)
            {
                roleQ.TryRemove(performance);
            }

            if (_performancesInActorQueues.TryGetValue(performance,
                out SimplePriorityQueue<IPerformance<IActor>, int> actorQ))
            {
                actorQ?.Remove(performance);
                _performancesInActorQueues.Remove(performance);
            }
        }

        /// <summary>
        /// Retrieves the roles a given actor has.
        /// </summary>
        /// <param name="actor">The actor to retrieve roles for.</param>
        /// <returns>The roles of the actor.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>We provide this API so roles can be implemented dynamically and independent of the IActor interface implementation.</remarks>
        public RoleMask GetRoleMask([NotNull] IActor actor) => _getRoles(actor);

        /// <summary>
        /// Removes the claim on a performance and reschedules it.
        /// </summary>
        private void ReschedulePerformance([NotNull] IPerformance<IActor> performance, [NotNull] IActor actor)
        {
            // to reschedule a performance we simply cache its still existing priority and key, then delete it, and use the
            // caches to schedule it again

            Func<int, int> priorities = _performancePriorities[performance];
            int key = GetKey(performance);
            DeletePerformance(performance, actor);
            Schedule(key, performance, priorities);

            Debug.Log($"[{GetType().GetNiceName()}] Rescheduled performance '{performance}'");
        }

        /// <summary>
        /// Removes a performance from the scheduler.
        /// </summary>
        private void DeletePerformance([NotNull] IPerformance<IActor> performance, [NotNull] IActor actor)
        {
            // depending on whether the performance is claimed we either need to remove the claim or the entry in the queues 
            if (IsClaimed(performance))
            {
                RemoveClaim(performance);
            }

            // besides the claims we also associate a performance with a key and cache their priorities so
            // we need to delete all those too.
            int key = GetKey(performance);
            _keyPerformanceMap.Remove(key);
            _performanceKeyMap.Remove(performance);
            _performancePriorities.Remove(performance);
            RemoveFromQueue(performance);

            // this is potentially a manual cancellation so we need to unsubscribe the handlers that implement the automation
            performance.Failed -= ReschedulePerformance;
            performance.Failed -= DeletePerformance;
            performance.Completed -= ReschedulePerformance;

            Debug.Log($"[{GetType().GetNiceName()}] Deleted performance '{performance}'");
        }

        /// <summary>
        /// Removes the association actor-key and actor-performance. No checking is performed. We call this only when we are
        /// sure the components of the triple are associated with each other.
        /// </summary>
        private void RemoveClaim([NotNull] IPerformance<IActor> performance)
        {
            // to remove a claim we delete the 2-way mappings between actor and performance
            IActor actor = GetActor(performance);
            _actorPerformanceMap.Remove(actor);
            _performanceActorMap.Remove(performance);
            Debug.Log(
                $"[{GetType().GetNiceName()}] Removed claim on performance '{performance}' by actor '{actor.Name}'");
        }

        /// <summary>
        /// Creates the association actor-key and actor-performance.
        /// </summary>
        private void AddClaim(IActor actor, IPerformance<IActor> performance)
        {
            // to add a claim we store a 2-way mapping between actor and performance
            _actorPerformanceMap.Add(actor, performance);
            _performanceActorMap.Add(performance, actor);
            Debug.Log(
                $"[{GetType().GetNiceName()}] Added claim on performance '{performance}' by actor '{actor.Name}'");
        }

        /// <summary>
        /// Get the <see cref="IPerformance{IActor}"/> scheduled under the given key.
        /// </summary>
        public IPerformance<IActor> GetPerformance(int key) => _keyPerformanceMap[key];

        /// <summary>
        /// Get the <see cref="IPerformance{IActor}"/> scheduled under the given <see cref="IActor"/>.
        /// </summary>
        public IPerformance<IActor> GetPerformance([NotNull] IActor actor) => _actorPerformanceMap[actor];

        /// <summary>
        /// Get the key currently associated with a given <see cref="IActor"/>.
        /// </summary>
        public int GetKey([NotNull] IActor actor) => GetKey(GetPerformance(actor));

        /// <summary>
        /// Get the key currently associated with a given <see cref="IPerformance{IActor}"/>.
        /// </summary>
        public int GetKey([NotNull] IPerformance<IActor> performance) => _performanceKeyMap[performance];

        /// <summary>
        /// Get the <see cref="IActor"/> currently associated with a given key.
        /// </summary>
        public IActor GetActor([NotNull] int key) => GetActor(GetPerformance(key));

        /// <summary>
        /// Get the <see cref="IActor"/> currently associated with a given <see cref="IPerformance{IActor}"/>.
        /// </summary>
        public IActor GetActor([NotNull] IPerformance<IActor> performance) => _performanceActorMap[performance];

        /// <summary>
        /// Checks whether a given <see cref="IPerformance{IActor}"/> is currently scheduled.
        /// </summary>
        public bool IsScheduled([NotNull] IPerformance<IActor> performance) =>
            _performanceKeyMap.ContainsKey(performance);

        /// <summary>
        /// Checks whether a <see cref="IPerformance{IActor}"/> is currently scheduled under a given <paramref name="key"/>.
        /// </summary>
        public bool IsScheduled([NotNull] int key) => _keyPerformanceMap.ContainsKey(key);

        /// <summary>
        /// Checks Whether a <see cref="IPerformance{IActor}"/> is currently claimed.
        /// </summary>
        public bool IsClaimed([NotNull] IPerformance<IActor> performance) =>
            _performanceActorMap.ContainsKey(performance);

        /// <summary>
        /// Checks whether a given <paramref name="key"/> is currently claimed.
        /// </summary>
        public bool IsClaimed([NotNull] int key) => _performanceActorMap.ContainsKey(GetPerformance(key));

        /// <summary>
        /// Checks whether a <see cref="IActor"/> is currently making a claim.
        /// </summary>
        public bool IsClaimed([NotNull] IActor actor) => _actorPerformanceMap.ContainsKey(actor);

#if UNITY_EDITOR
        /// <summary>
        /// The performances currently claimed by an actor.
        /// </summary>
        /// <remarks>Editor-only debug API. Do not use this property in performance critical code paths.</remarks>
        public IEnumerable<(IActor actor, IPerformance perf)> Claims =>
            _actorPerformanceMap.Select(pair => (
                    actor: (IActor)pair.Key,
                    perf: (IPerformance)pair.Value
                )
            );

        /// <summary>
        /// The performances currently queued to be claimed, indexed by role.
        /// </summary>
        /// <remarks>Editor-only debug API. Do not use this property in performance critical code paths.</remarks>
        public IEnumerable<(object role, IEnumerable<(IPerformance perf, int prio)> queue)> Queues =>
            _roleQueues.Select(pair => (
                    key: (object)pair.Key,
                    queue: pair.Value.Select(jt => (
                            perf: (IPerformance)jt,
                            prio: pair.Value.GetPriority(jt)
                        )
                    )
                )
            );

        public IEnumerable<(IActor actor, IEnumerable<(IPerformance perf, int prio)> queue)> ActorQueues =>
            _actorQueues.Select(pair => (
                    key: (IActor)pair.Key,
                    queue: pair.Value.Select(jt => (
                            perf: (IPerformance)jt,
                            prio: pair.Value.GetPriority(jt)
                        )
                    )
                )
            );


#endif

        /// <inheritdoc />
        public void Dispose()
        {
#if UNITY_EDITOR
            DirectorRegistry.Unregister(this);
#endif
        }
    }
}