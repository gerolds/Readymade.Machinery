using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Readymade.Machinery.Shared;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Readymade.Machinery.FSM
{
    /// <summary>
    /// <para>A simple, declarative, delegate based finite state machine with fluent API, keyed states, explicit triggers and transition events.</para>
    /// <para><see cref="Fire"/> can be called from transition handlers and will be queued to be processed once the current trigger is finished. Recursive calls to <see cref="Fire"/> are however not permitted.</para>
    /// <typeparam name="TState">The type used to differentiate states. This type is constrained to enums and should be treated as key with state implementation via delegates. The FSM is agnostic towards where and how states are implemented.</typeparam>
    /// <typeparam name="TTrigger">The type used to differentiate triggers. This type is constrained to enums and should be treated as key. The FSM is agnostic towards where triggers originate or what they are.</typeparam>
    /// <example>
    /// Example usage:
    /// <code>
    /// </code>
    /// </example>
    /// </summary>
    public class StateMachine<TState, TTrigger> : IDisposable
        where TState : Enum
        where TTrigger : Enum
    {
        /// <summary>
        /// storage for all state configurations.
        /// </summary>
        private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _configurations = new();

        /// <summary>
        /// Event fired before a transition takes palace (before any OnExit handlers are called).
        /// </summary>
        public event Action<Transition<TState, TTrigger>> OnTransitioning;

        /// <summary>
        /// Event fired after a transition is complete (after any OnExit handlers, but before any OnEntry handlers are called).
        /// </summary>
        public event Action<Transition<TState, TTrigger>> OnTransition;

        /// <summary>
        /// Event fired after a transition is complete (after any OnEntry handlers are called).
        /// </summary>
        public event Action<Transition<TState, TTrigger>> OnTransitioned;

        /// <summary>
        /// Event fired when a trigger is encountered that is not specifically handled in a state (via Permit or Ignore).
        /// </summary>
        public event Action<TState, TTrigger> OnUnhandledTrigger;

        /// <summary>
        /// Event fired at Error sync points.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Event fired at Debug sync points.
        /// </summary>
        public event Action<string> OnDebug;

        /// <summary>
        /// Whether we have a <see cref="UnityEngine.Debug"/> subscriber and should post messages.
        /// </summary>
        internal bool ShouldDebug => OnDebug != null;

        /// <summary>
        /// storage for triggers that were fired while executing another trigger
        /// </summary>
        private ConcurrentQueue<TTrigger> _triggerQueue = new();

        /// <summary>
        /// a counter used for detecting the processor thread.
        /// </summary>
        private int _processMutex = 1;

        /// <summary>
        /// Whether the state machine is currently processing a trigger.
        /// </summary>
        public bool IsFiring => _processMutex == 0;

        /// <summary>
        /// whether the configuration of this state machine is locked.
        /// </summary>
        private int _isConfigurationLocked = 0;

        /// <summary>
        /// The trigger that is currently being processed from which subsequent triggers may have been fired.
        /// </summary>
        private TTrigger _bottomTrigger;

        /// <summary>
        /// Whether the configuration of this state machine is still mutable (not locked).
        /// </summary>
        /// <remarks>The configuration is locked once the <see cref="Fire"/> is called the for the first time.</remarks>
        public bool IsConfigurationMutable => _isConfigurationLocked == 0;

        /// <summary>
        /// Whether a particular trigger is already queued for processing. This is useful for de-duplicating triggers
        /// from the outside that are known to be fired multiple times as part of state updates.
        /// </summary>
        /// <param name="trigger">The trigger to check for.</param>
        /// <returns>Whether the trigger is queued.</returns>
        public bool IsQueued(TTrigger trigger) => _triggerQueue.Contains(trigger);

        /// <summary>
        /// The current state.
        /// </summary>
        public TState State
        {
            get => _state;
            private set => _state = value;
        }

        /// <summary>
        /// A descriptive name for this state machine.
        /// </summary>
        public string Name { get; set; } = "Undefined";

        /// <summary>
        /// Create a new state machine instance.
        /// </summary>
        public StateMachine(TState initial)
        {
            State = initial;
            _configurations[initial] = new StateConfiguration<TState, TTrigger>(this, initial);
            OnError = (m) => throw new InvalidOperationException(m);
        }

        /// <summary>
        /// Configure <paramref name="state"/>
        /// </summary>
        /// <remarks>This returns an instance to a configuration object to allow fluent method chaining.</remarks>
        public StateConfiguration<TState, TTrigger> Configure(TState state)
        {
            if (_isConfigurationLocked != 0)
                throw new InvalidOperationException($"State configuration cannot be changed once an event was fired");

            _configurations.TryAdd(state, new StateConfiguration<TState, TTrigger>(this, state));
            return _configurations[state];
        }

        public void Activate(TState activeState)
        {
            State = activeState;
            if (_configurations.TryGetValue(activeState, out StateConfiguration<TState, TTrigger> activeStateConfig))
            {
                foreach (Action onEntryAction in activeStateConfig.ParameterlessEntryActions)
                {
                    onEntryAction.Invoke();
                }
            }
            else
            {
                throw new InvalidOperationException($"Destination state {activeState} is not configured");
            }
        }

        /// <summary>
        /// Fires a given <paramref name="trigger"/>, i.e. evaluates it against the configuration of the current <see cref="State"/>. 
        /// </summary>
        ///  <param name="trigger">The <typeparamref name="TTrigger"/> to fire</param>
        public void Fire(TTrigger trigger)
        {
            if (_isConfigurationLocked == 0)
            {
                Interlocked.Increment(ref _isConfigurationLocked);
                PostProcessConfiguration();
            }

            // we allow five queued up triggers, this is a safety measure to catch and break event cycles.
            if (_triggerQueue.Count > 9)
            {
                Debug.LogError(
                    $"As a precaution, queueing of only 10 triggers is allowed; Trigger {trigger} will be ignored while processing {_bottomTrigger} in state {State}.");
                return;
            }

            _triggerQueue.Enqueue(trigger);

            // lock free acquisition of the mutex
            bool shouldProcess = false;
            int copy = _processMutex;
            if (copy > 0 && copy == Interlocked.CompareExchange(ref _processMutex, 0, 1))
                shouldProcess = true;

            // only the first call to fire() will process the queue
            if (!shouldProcess)
                return;

            try
            {
                while (_triggerQueue.TryDequeue(out _bottomTrigger))
                {
                    ProcessTrigger(_bottomTrigger);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                _bottomTrigger = default;
                // lock free release of the mutex
                // ISSUE: queued triggers added between finishing the queue and the decrement here will only be processed on the next Fire() calls
                // this would rarely and only happen when Fire() is called from other threads.
                Interlocked.Increment(ref _processMutex);
            }
        }

        private void PostProcessConfiguration()
        {
            foreach (StateConfiguration<TState, TTrigger> configuration in _configurations.Values)
            {
                GetAncestors(configuration.State, configuration.Ancestors);
            }
        }

        /// <summary>
        /// Checks whether the state machine is currently in a given state. Will return true for all ancestors of the current state and the state itself.
        /// </summary>
        /// <param name="state">The state to check.</param>
        /// <returns>Whether the state machine is in the given state.</returns>
        public bool IsInState(TState state) => _configurations[State].Ancestors.Contains(state);

        /// <summary>
        /// Handles the state transition check and sequencing. 
        /// </summary>
        /// <param name="trigger">The <typeparamref name="TTrigger"/> to process.</param>
        private void ProcessTrigger(TTrigger trigger)
        {
            //Debug.Log ( $"trigger {trigger} in state {State}" );
            bool isHandled = false;

            // find the nearest ancestor that has a configuration for the trigger
            StateConfiguration<TState, TTrigger> stateConfig = null;
            foreach (var ancestor in _configurations[State].Ancestors)
            {
                var candidateState = _configurations[ancestor];
                if (candidateState.Ignored.ContainsKey(trigger) ||
                    candidateState.InternalActions.ContainsKey(trigger) ||
                    candidateState.Permitted.ContainsKey(trigger)
                )
                {
                    stateConfig = candidateState;
                    break;
                }
            }

            if (stateConfig == null)
            {
                // if no ancestor has a configuration for the trigger, we're done
                isHandled = false;
            }
            else if (stateConfig.Ignored.TryGetValue(trigger, out Func<bool> shouldIgnore))
            {
                // ignored triggers
                // Note: only triggers configured on the exact state will be ignored (no ancestors)
                if (shouldIgnore())
                {
                    isHandled = true;
                }
            }
            else if (stateConfig.InternalActions.ContainsKey(trigger))
            {
                // internal transitions
                // We trigger internal transitions for the nearest ancestor.

                if (stateConfig.InternalActions.TryGetValue(trigger, out var ancestorActions))
                {
                    foreach (var action in ancestorActions)
                    {
                        action.Invoke(new Transition<TState, TTrigger>(trigger, State, State));
                    }
                }

                isHandled = true;
            }
            else if (stateConfig.Permitted.ContainsKey(trigger))
            {
                // regular state transitions

                // permissions of ancestors are shared by descendants
                stateConfig.Permitted.TryGetValue(trigger, out (Func<bool> predicate, Func<TState> selectState) next);

                if (next.predicate())
                {
                    TState nextState = next.selectState();

                    if (EqualityComparer<TState>.Default.Equals(nextState, State))
                        throw new InvalidOperationException($"Reentrant states are currently not supported");

                    // recursively follow initial state configurations (we assume these are previously validated to be a tree)
                    Transition<TState, TTrigger> transition = new(
                        trigger: trigger,
                        source: State,
                        destination: nextState
                    );
                    StateConfiguration<TState, TTrigger> nextStateConfig = default;
                    for (int i = 0; i < 8; i++)
                    {
                        if (_configurations.TryGetValue(nextState, out nextStateConfig))
                        {
                            if (nextStateConfig.HasInitialTransition)
                            {
                                nextState = nextStateConfig.Initial;
                                transition = new Transition<TState, TTrigger>(
                                    trigger: trigger,
                                    source: State,
                                    destination: nextState
                                );
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Destination state {nextState} is not configured");
                        }
                    }

                    OnTransitioning?.Invoke(transition);
                    bool hasCommonAncestor =
                        TryGetCommonAncestor(transition.Source, transition.Destination, out TState commonAncestor);

                    if (hasCommonAncestor)
                    {
                        int commonAncestorIndex = _configurations[transition.Source].Ancestors.IndexOf(commonAncestor);
                        for (int i = 0; i < commonAncestorIndex; i++)
                        {
                            InvokeExitActions(_configurations[transition.Source].Ancestors[i], transition);
                        }
                    }
                    else
                    {
                        for (int i = _configurations[transition.Source].Ancestors.Count - 1; i >= 0; i--)
                        {
                            InvokeExitActions(_configurations[transition.Source].Ancestors[i], transition);
                        }
                    }

                    State = transition.Destination;
                    OnTransition?.Invoke(transition);

                    if (hasCommonAncestor)
                    {
                        int i = _configurations[transition.Destination].Ancestors.IndexOf(commonAncestor) - 1;
                        for (; i >= 0; i--)
                        {
                            InvokeEntryActions(_configurations[transition.Destination].Ancestors[i], transition);
                        }
                    }
                    else
                    {
                        for (int i = _configurations[transition.Destination].Ancestors.Count - 1; i >= 0; i--)
                        {
                            InvokeEntryActions(_configurations[transition.Destination].Ancestors[i], transition);
                        }
                    }

                    OnTransitioned?.Invoke(transition);
                }

                isHandled = true;
            }

            if (!isHandled)
            {
                OnUnhandledTrigger?.Invoke(State, trigger);
            }

            return;

            void InvokeEntryActions(TState node, Transition<TState, TTrigger> transition)
            {
                foreach (Action onEntryAction in _configurations[node].ParameterlessEntryActions)
                {
                    onEntryAction.Invoke();
                }

                foreach (Action<Transition<TState, TTrigger>> onEntryAction in
                    _configurations[node].EntryActions)
                {
                    onEntryAction.Invoke(transition);
                }
            }

            void InvokeExitActions(TState node, Transition<TState, TTrigger> transition)
            {
                foreach (Action<Transition<TState, TTrigger>> onExitAction in _configurations[node].ExitActions)
                {
                    onExitAction.Invoke(transition);
                }
            }
        }

        private ISpawner _spawner;
        private TState _state;
        private bool _isProcTrigger;

        /// <summary>
        /// A generic spawner.
        /// </summary>
        public interface ISpawner
        {
            /// <summary>
            /// Spawns an instance of whatever. 
            /// </summary>
            /// <returns>A handle representing the spawn.</returns>
            public IDisposable Spawn();
        }

        /// <remarks>This is a Mock class.</remarks>
        public class SpawnerMock : ISpawner
        {
            public GameObject PrefabToSpawn { private get; set; }
            public int SpawnCallCount { get; private set; }

            /// <inheritdoc />
            public IDisposable Spawn()
            {
                SpawnCallCount++;
                GameObject instance = Object.Instantiate(PrefabToSpawn);
                return new DisposableGameObject(instance);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Wraps a GameObject in a disposable handle.
        /// </summary>
        public class DisposableGameObject : IDisposable
        {
            private readonly GameObject _go;

            /// <summary>
            /// Creates a new instance of a disposable GameObject handle.
            /// </summary>
            /// <param name="go">The GameObject to wrap into a disposable handle.</param>
            public DisposableGameObject(GameObject go)
            {
                _go = go;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                Object.Destroy(_go);
            }
        }

        /// <summary>
        /// Inject all dependencies this instance requires to operate.
        /// </summary>
        /// <param name="spawner">A spawner to spawn objects.</param>
        private void Configure(ISpawner spawner)
        {
            _spawner = spawner;
        }


        private bool TryGetCommonAncestor(TState source, TState destination, out TState commonAncestor)
        {
            StateConfiguration<TState, TTrigger> sourceConfig = _configurations[source];
            StateConfiguration<TState, TTrigger> destinationConfig = _configurations[destination];
            if (_configurations[source] == _configurations[destination])
            {
                commonAncestor = source;
                return true;
            }

            /*
            if ( !sourceConfig.IsSubstate ) {
                commonAncestor = default;
                return false;
            }

            if ( !destinationConfig.IsSubstate ) {
                commonAncestor = default;
                return false;
            }
            */

            bool hasCommonAncestor =
                TryGetFirstIntersection(sourceConfig.Ancestors, destinationConfig.Ancestors, out commonAncestor);
            if (hasCommonAncestor)
            {
                return true;
            }
            else
            {
                commonAncestor = default;
                return false;
            }
        }

        /// <summary>
        /// Finds the first intersecting node in two paths in a tree.
        /// </summary>
        /// <param name="pathA">A list of nodes of a tree structure, ordered by decreasing depth (last node is the root node).</param>
        /// <param name="pathB">A list of nodes of a tree structure, ordered by decreasing depth (last node is the root node).</param>
        /// <param name="intersection">The node, shared by both paths, where they diverge.</param>
        /// <returns>Whether an intersection was found.</returns>
        private static bool TryGetFirstIntersection(List<TState> pathA, List<TState> pathB, out TState intersection)
        {
            int aIndex = pathA.Count - 1;
            int bIndex = pathB.Count - 1;
            TState commonAncestor = default;
            int commonAncestorCount = 0;
            while (aIndex >= 0 && bIndex >= 0)
            {
                if (Equals(pathA[aIndex], pathB[bIndex]))
                {
                    commonAncestor = pathA[aIndex];
                    aIndex--;
                    bIndex--;
                    commonAncestorCount++;
                }
                else
                {
                    break;
                }
            }

            if (commonAncestorCount > 0)
            {
                // we've branched into divergent ancestors, so the last common ancestor we found is the lowest common ancestor
                intersection = commonAncestor;
                return true;
            }
            else
            {
                intersection = default;
                return false;
            }
        }

        /// <summary>
        /// Gets the path from a given node to the root.
        /// </summary>
        /// <param name="node">The node where the path starts.</param>
        /// <param name="path">Empty list to write the path to.</param>
        /// <remarks>This should only be called once during post-processing of a state configuration.</remarks>
        /// <exception cref="InvalidOperationException">When a cyclic parent-child relationship is detected.</exception>
        private void GetAncestors(TState node, List<TState> path)
        {
            TState next = node;
            HashSet<TState> visited = new();
            path.Add(next);
            visited.Add(next);
            while (_configurations[next].IsSubstate)
            {
                if (!visited.Add(_configurations[next].Parent))
                {
                    throw new InvalidOperationException(
                        $"Cyclic path detected. {_configurations[next].Parent} is both ancestor and descendant of {next}.");
                }

                path.Add(_configurations[next].Parent);
                next = _configurations[next].Parent;
            }
        }

        /// <summary>
        /// Invoke the OnDebug event with a message.
        /// </summary>
        internal void PostDebug(string message) => OnDebug?.Invoke(message);

        /// <inheritdoc />
        public void Dispose()
        {
            // unregister from debug observer
        }

        internal StateConfiguration<TState, TTrigger> GetStateConfig(TState state) => _configurations[state];

        public void PostError(InvalidOperationException invalidOperationException) =>
            OnError?.Invoke(invalidOperationException.Message);
    }
}