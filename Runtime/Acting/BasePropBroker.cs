using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Readymade.Machinery.Shared;
using Readymade.Machinery.Shared.PriorityQueue;
using UnityEngine;
using Vertx.Debugging;
using Random = UnityEngine.Random;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// Base implementation of a <see cref="IPropBroker{SoProp}" />. To avoid cycles, all brokers should
    /// derive from this class.
    /// </summary>
    public class BasePropBroker : IPropBroker<SoProp>
    {
        private readonly HashSet<IProvider<SoProp>> _providers = new();
        private readonly Collider[] _overlapResults = new Collider[64];
        private readonly LayerMask _mask;
        private readonly SimplePriorityQueue<IProvider<SoProp>, float> _candidates = new();
        private readonly List<PropClaim<SoProp, IActor>> _claimBuffer = new();
        private static HashSet<object> s_visited = new();
        private static object s_first;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void OnEnterPlaymode()
        {
            s_first = default;
            s_visited.Clear();
        }
#endif

        /// <inheritdoc />
        public event Action<Phase, (SoProp prop, long quantity, IActor claimant)> Updated;

        /// <inheritdoc />
        public Vector3 Position { get; set; } = default;

        /// <inheritdoc />
        public Pose Pose { get; set; } = default;

        /// <inheritdoc />
        public bool HasPose { get; set; } = false;

        /// <inheritdoc />
        public bool DebugLog { get; set; }

        public BasePropBroker(LayerMask mask)
        {
            _mask = mask;
        }

        /// <inheritdoc />
        public void AddProvider(IProvider<SoProp> provider)
        {
            _providers.Add(provider);
            provider.Modified += UpdatedEventHandler;
        }


        /// <inheritdoc />
        public void RemoveProvider(IProvider<SoProp> provider)
        {
            _providers.Remove(provider);
            provider.Modified -= UpdatedEventHandler;
        }

        private void UpdatedEventHandler(Phase phase,
            (SoProp prop, long quantity, IActor claimant) args) => Updated?.Invoke(phase, args);

        /// <inheritdoc />
        public bool TryClaimProp(
            [NotNull] SoProp prop,
            [NotNull] IActor actor,
            long quantity,
            out PropClaim<SoProp, IActor> claim,
            SpatialHeuristic heuristic
        )
        {
            claim = default;
            bool found = TryFindProp(prop, actor, quantity, out IProvider<SoProp> provider, heuristic);
            bool claimed = found && provider.TryClaimProp(prop, actor, quantity, claim: out claim);
            if (DebugLog)
            {
                Debug.Log(
                    claimed
                        ? $"[{nameof(BasePropBroker)}] {actor?.Name} claimed {prop?.Name} at {provider?.Pose.position}"
                        : $"[{nameof(BasePropBroker)}] {actor?.Name} failed to claim {prop?.Name}; {(!found ? "no provider found" : "provider rejected claim")}");
            }

            return claimed;
        }

        /// <inheritdoc />
        public bool TryFindProp(
            [NotNull] SoProp prop,
            [NotNull] IActor actor,
            long quantity,
            out IProvider<SoProp> result,
            SpatialHeuristic heuristic
        )
        {
            try
            {
                if (quantity < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(quantity), "Must be at least 1");
                }

                // we make sure we don't get stuck in a find-cycle by tracking the object that starts the find and all brokers
                // visited on the way. Cycles would be caused by brokers being used in components that are discovered
                // through the spatial query below.
                if (s_visited.Count != 0)
                {
                    if (s_visited.Contains(this))
                    {
                        Debug.LogWarning(
                            $"{GetType().GetNiceName()} Cycle detected while evaluating {nameof(TryFindProp)}");
                        result = default;
                        return false;
                    }
                }
                else
                {
                    s_first = this;
                    s_visited.Add(this);
                }

                _candidates.Clear();

                // lazy initial version: select the next best provider
                // TODO: add selection heuristic (distance, path cost, other factors)
                foreach (IProvider<SoProp> provider in _providers)
                {
                    if (provider == null)
                    {
                        continue;
                    }

                    if (provider.CanProvide(prop))
                    {
                        // if the provider is a component, queue it up to be compared with the spatial query below.
                        if (provider.HasPose)
                        {
                            _candidates.Enqueue(provider, CalculatePriority(actor, prop, provider, heuristic));
                        }
                        else
                        {
                            if (provider is IPropBroker<SoProp> broker)
                            {
                                return broker.TryFindProp(prop, actor, quantity, out result);
                            }
                            else
                            {
                                result = provider;
                                return true;
                            }
                        }
                    }
                }

                // run a spatial provider query based on the property's discovery range
                int size = DrawPhysics.OverlapSphereNonAlloc(
                    actor == IActor.None ? Vector3.zero : actor.Pose.position,
                    actor == IActor.None ? Mathf.Infinity : prop.DiscoveryRange,
                    _overlapResults,
                    _mask,
                    QueryTriggerInteraction.Collide
                );

                // filter candidates (only keep those with IPropProvider component)
                for (int i = 0; i < size; i++)
                {
                    Collider other = _overlapResults[i];
                    if (other.TryGetComponent(out IProvider<SoProp> provider) &&
                        provider.CanProvide(prop) &&
                        provider.HasPose
                    )
                    {
                        // early exit with greedy heuristic
                        if (heuristic == SpatialHeuristic.Greedy)
                        {
                            result = provider;
                            return true;
                        }

                        if (provider is IPropBroker<SoProp> broker &&
                            broker.TryFindProp(prop, actor, quantity, out IProvider<SoProp> candidate, heuristic)
                        )
                        {
                            _candidates.Enqueue(candidate, CalculatePriority(actor, prop, candidate, heuristic));
                        }
                        else
                        {
                            _candidates.Enqueue(provider, CalculatePriority(actor, prop, provider, heuristic));
                        }
                    }
                }

                // return the best candidate
                if (_candidates.Count > 0)
                {
                    result = _candidates.Dequeue();

                    if (DebugLog)
                    {
                        // Debug.Log($"[{GetType().GetNiceName()}] {(result is Component c ? c.name : "")} responded to prop {prop?.Name} claim by {actor?.Name} with pose {result?.Pose.position}");
                    }

                    return true;
                }

                result = default;
                return false;
            }
            finally
            {
                // reset the find-cycle tracking
                if (s_first == this)
                {
                    s_first = default;
                    s_visited.Clear();
                }
            }
        }

        /// <summary>
        /// An actor may try to claim a props that it needs to complete a Gesture.
        /// </summary>
        /// <param name="props"></param>
        /// <param name="actor"></param>
        /// <param name="claims">The collection that the claims will be added to.</param>
        /// <param name="heuristic">The <see cref="SpatialHeuristic"/> to use for selecting props from registered and nearby providers.</param>
        /// <returns>Whether all <paramref name="props"/> were claimed successfully.</returns>
        public bool TryClaimProps(
            [NotNull] IEnumerable<SoProp> props,
            [NotNull] IActor actor,
            ref ICollection<PropClaim<SoProp, IActor>> claims,
            SpatialHeuristic heuristic = SpatialHeuristic.Default
        )
        {
            return TryClaimProps(props.Select(it => (it, 1)), actor, ref claims, heuristic);
        }

        /// <summary>
        /// An actor may try to claim props in specific quantities that it needs to complete a <see cref="Readymade.Machinery.Acting.IGesture{TActor}"/>.
        /// </summary>
        /// <param name="props"></param>
        /// <param name="actor"></param>
        /// <param name="claims">The collection that the claims will be added to.</param>
        /// <param name="heuristic">The <see cref="SpatialHeuristic"/> to use for selecting props from registered and nearby providers.</param>
        /// <returns>Whether all <paramref name="props"/> were claimed successfully.</returns>
        public bool TryClaimProps(
            IEnumerable<(SoProp prop, int qty)> props,
            IActor actor,
            ref ICollection<PropClaim<SoProp, IActor>> claims,
            SpatialHeuristic heuristic
        )
        {
            foreach ((SoProp prop, int qty) in props)
            {
                if (TryClaimProp(prop, actor, qty, out PropClaim<SoProp, IActor> claim, heuristic))
                {
                    _claimBuffer.Add(claim);
                }
                else
                {
                    foreach (PropClaim<SoProp, IActor> propClaim in _claimBuffer)
                    {
                        propClaim.Cancel();
                    }

                    return false;
                }
            }

            foreach (PropClaim<SoProp, IActor> claim in _claimBuffer)
            {
                claims.Add(claim);
            }

            _claimBuffer.Clear();
            return true;
        }

        /// <summary>
        /// Calculate a priority value for a given actor from euclidean distance with a bias towards components on a similar y-level.
        /// </summary>
        private static float CalculatePriority(IActor actor, IProp prop, IProvider<SoProp> provider,
            SpatialHeuristic selectionHeuristic)
        {
            const float Y_PENALTY = 10f;

            // max distance to use for farthest and random priority to stay within a float
            // range that is still accurate and would not sort nearby values into the same
            // bucket which would happen with values like float.maxValue
            const float MAXDISTANCE = 4000f;

            switch (selectionHeuristic)
            {
                case SpatialHeuristic.Default:
                case SpatialHeuristic.Closest:
                {
                    return CalculateDistancePriority(actor, provider.Pose);
                }
                case SpatialHeuristic.Farthest:
                {
                    float distancePrio = CalculateDistancePriority(actor, provider.Pose);
                    return Mathf.Min(MAXDISTANCE, MAXDISTANCE - distancePrio);
                }
                case SpatialHeuristic.Random:
                {
                    return Random.Range(0, MAXDISTANCE);
                }
                case SpatialHeuristic.RandomProximity:
                {
                    float distancePrio = CalculateDistancePriority(actor, provider.Pose);
                    return Mathf.Sqrt(distancePrio) * Random.Range(0, 1f);
                }
                case SpatialHeuristic.Greedy:
                    // a greedy heuristic should have returned before reaching this method, if it does come here anyway,
                    // we ignore it.
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectionHeuristic), selectionHeuristic, null);
            }

            return 0; // method ends here

            float CalculateDistancePriority(IActor a, Pose p)
            {
                float distance = Vector3.Distance(a.Pose.position, p.position);
                float yPenalty = Mathf.Abs(a.Pose.position.y - p.position.y) * Y_PENALTY;
                return distance + yPenalty;
            }
        }


        /// <inheritdoc />
        public bool CanProvide(SoProp prop)
        {
            foreach (IProvider<SoProp> provider in _providers)
            {
                if (provider.CanProvide(prop))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _providers.Clear();
        }
    }
}