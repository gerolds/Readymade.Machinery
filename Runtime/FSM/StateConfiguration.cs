using System;
using System.Collections.Generic;
using Readymade.Machinery.EDBT;

namespace Readymade.Machinery.FSM
{
    /// <summary>
    /// Configuration for a specific <typeparamref name="TState"/>. Exposes a fluent builder API.
    /// </summary>
    /// <typeparam name="TState">The type used to differentiate states.</typeparam>
    /// <typeparam name="TTrigger">The type used to differentiate triggers.</typeparam>
    public class StateConfiguration<TState, TTrigger>
        where TState : System.Enum
        where TTrigger : System.Enum
    {
        /// <summary>
        /// Storage for reused delegate to replace default arguments with.
        /// </summary>
        private readonly Func<bool> Always = () => true;

        /// <summary>
        /// Storage for entry actions.
        /// </summary>
        internal readonly List<Action<Transition<TState, TTrigger>>> EntryActions = new();

        /// <summary>
        /// Storage for entry actions.
        /// </summary>
        internal readonly List<Action> ParameterlessEntryActions = new();

        /// <summary>
        /// Storage for exit actions.
        /// </summary>
        internal readonly List<Action<Transition<TState, TTrigger>>> ExitActions = new();

        /// <summary>
        /// The state this configuration describes.
        /// </summary>
        public TState State { get; }

        internal TState Initial { get; private set; }
        internal TState Parent { get; private set; }

        internal readonly List<TState> Children = new();

        /// <summary>
        /// Storage for permitted triggers.
        /// </summary>
        internal readonly Dictionary<TTrigger, (Func<bool>, Func<TState>)> Permitted = new();

        /// <summary>
        /// Storage for ignored triggers.
        /// </summary>
        internal readonly Dictionary<TTrigger, Func<bool>> Ignored = new();

        /// <summary>
        /// Storage for internal transitions.
        /// </summary>
        internal readonly Dictionary<TTrigger, List<Action<Transition<TState, TTrigger>>>> InternalActions = new();

        /// <summary>
        /// A reference back to the state machine this state configuration belongs to.
        /// </summary>
        public StateMachine<TState, TTrigger> Machine { get; }

        /// <summary>
        /// Whether this config is for a substate
        /// </summary>
        internal bool IsSubstate => !Equals(Parent, State);

        /// <summary>
        /// A list of states that are ancestors of this state (includes this state as the first node and the root as the last node).
        /// </summary>
        internal List<TState> Ancestors { get; set; } = new();

        public bool HasInitialTransition => !Initial.Equals(State);

        /// <summary>
        /// Creates a new state machine configuration.
        /// </summary>
        /// <remarks>This constructor is called automatically and should not be used directly.</remarks>
        internal StateConfiguration(StateMachine<TState, TTrigger> stateMachine, TState state)
        {
            Machine = stateMachine;
            State = state;
            Initial = state;
            Parent = state;
        }

        // disallow public instantiation
        private StateConfiguration()
        {
        }

        /// <summary>
        /// Permit a transition to <paramref name="state"/> on <paramref name="trigger"/> 
        /// </summary>
        /// <param name="trigger">The <typeparamref name="TTrigger"/> to be permitted </param>
        /// /// <param name="state"></param>
        public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState state)
        {
            EnsureMutable();
            EnsureUniqueness(trigger);
            EnsureNotReentrant(state);

            Permitted[trigger] = (Always, () => state);
            return this;
        }

        /// <summary>
        /// Permit a transition to <typeparamref name="TState"/> on <typeparamref name="TTrigger"/> while <paramref name="predicate"/> is true. 
        /// </summary>
        public StateConfiguration<TState, TTrigger> PermitIf(TTrigger trigger, TState state, Func<bool> predicate)
        {
            EnsureMutable();
            EnsureUniqueness(trigger);
            EnsureNotReentrant(state);

            Permitted[trigger] = (predicate, () => state);
            return this;
        }

        /// <summary>
        /// Permit a transition to a dynamically selected <typeparamref name="TState"/> on a given <typeparamref name="TTrigger"/>. 
        /// </summary>
        public StateConfiguration<TState, TTrigger> PermitDynamic(TTrigger trigger, Func<TState> stateSelector)
        {
            EnsureMutable();
            EnsureUniqueness(trigger);
            EnsureNotReentrant(stateSelector());

            Permitted[trigger] = (Always, stateSelector);
            return this;
        }

        /// <summary>
        /// Permit a transition to a dynamically selected <typeparamref name="TState"/> on a given <typeparamref name="TTrigger"/> while <paramref name="predicate"/> is true.
        /// </summary>
        public StateConfiguration<TState, TTrigger> PermitDynamicIf(
            TTrigger trigger,
            Func<bool> predicate,
            Func<TState> stateSelector
        )
        {
            EnsureMutable();
            EnsureUniqueness(trigger);
            EnsureNotReentrant(stateSelector());

            Permitted[trigger] = (predicate, stateSelector);
            return this;
        }

        public StateConfiguration<TState, TTrigger> InitialTransition(TState state)
        {
            EnsureMutable();
            Initial = state;
            return this;
        }

        public StateConfiguration<TState, TTrigger> SubstateOf(TState state)
        {
            EnsureMutable();
            Parent = state;
            return this;
        }

        /// <summary>
        /// Permit a reentrant transition on <paramref name="trigger"/>.
        /// </summary>
        /// <param name="trigger">The <typeparamref name="TTrigger"/> to be permitted </param>
        /// <exception cref="NotImplementedException"></exception>
        [Obsolete("Not yet implemented")]
        public StateConfiguration<TState, TTrigger> PermitReentry(TTrigger trigger)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Permit a reentrant transition on <paramref name="trigger"/>.
        /// </summary>
        /// <param name="trigger">The <typeparamref name="TTrigger"/> to be permitted.</param>
        /// <param name="predicate">The predicate to decide whether the trigger is permitted.</param>
        /// <exception cref="NotImplementedException"></exception>
        [Obsolete("Not yet implemented")]
        public StateConfiguration<TState, TTrigger> PermitReentryIf(TTrigger trigger, Func<bool> predicate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ignore all <typeparamref name="TTrigger"/>s fired while in this state.
        /// </summary>
        /// <remarks>Triggers will only be ignored for the exact state they are configured in. Triggers ignored only by ancestors will be executed.</remarks>
        public StateConfiguration<TState, TTrigger> Ignore(TTrigger trigger)
        {
            EnsureMutable();
            EnsureUniqueness(trigger);

            Ignored[trigger] = Always;
            return this;
        }

        public StateConfiguration<TState, TTrigger> Ignore(params TTrigger[] triggers)
        {
            EnsureMutable();
            foreach (var trigger in triggers)
            {
                EnsureUniqueness(trigger);
                Ignored[trigger] = Always;
            }

            return this;
        }

        /// <summary>
        /// Ignore all <typeparamref name="TTrigger"/>s fired while in this state and while <param name="predicate"> is true.</param>.
        /// </summary>
        /// <remarks>Triggers will only be ignored for the exact state they are configured in. Triggers ignored only by ancestors will be executed.</remarks>
        [Obsolete("Not yet implemented")]
        public StateConfiguration<TState, TTrigger> IgnoreIf(TTrigger trigger, Func<bool> predicate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time <paramref name="trigger"/> is fired without leaving this state. Entry and exit events will not be triggered.
        /// </summary>
        /// <param name="trigger">The trigger this transition responds to.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The state configuration.</returns>
        /// <remarks>Internal transitions will only be executed if the state machine is in the exact state. Ancestor's internal transitions will not be executed.</remarks>
        public StateConfiguration<TState, TTrigger> InternalTransition(
            TTrigger trigger,
            Action<Transition<TState, TTrigger>> action
        )
        {
            EnsureMutable();
            //EnsureUniqueness(trigger);

            if (!InternalActions.ContainsKey(trigger))
                InternalActions[trigger] = new List<Action<Transition<TState, TTrigger>>>();
            InternalActions[trigger].Add(action);
            return this;
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time <paramref name="trigger"/> is fired without leaving this state. Entry and exit events will not be triggered.
        /// </summary>
        /// <param name="trigger">The trigger this transition responds to.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="condition">The condition to check for the transition to be valid.</param>
        /// <returns>The state configuration.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>Internal transitions will only be executed if the state machine is in the exact state. Ancestor's internal transitions will not be executed.</remarks>
        [Obsolete("Not yet implemented")]
        public StateConfiguration<TState, TTrigger> InternalTransitionIf(
            TTrigger trigger,
            Action<Transition<TState, TTrigger>> action,
            Func<bool> condition
        )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Throws an exception if the configuration is locked
        /// </summary>
        private void EnsureMutable()
        {
            if (!Machine.IsConfigurationMutable)
            {
                throw new InvalidOperationException($"State configuration cannot be changed once an event was fired.");
            }
        }

        /// <summary>
        /// Throws an exception when attempting to create a reentrant state
        /// </summary>
        // TODO: provide API and execution of reentrant states
        private void EnsureNotReentrant(TState state)
        {
            if (EqualityComparer<TState>.Default.Equals(state, State))
            {
                throw new InvalidOperationException(
                    $"Reentrant states are currently not supported. Use {nameof(InternalTransition)} instead.");
            }
        }

        /// <summary>
        /// Throws an exception when the given trigger is not uniquely defined
        /// </summary>
        private void EnsureUniqueness(TTrigger trigger)
        {
            if (
                Permitted.ContainsKey(trigger) ||
                Ignored.ContainsKey(trigger) ||
                InternalActions.ContainsKey(trigger)
            )
            {
                throw new InvalidOperationException(
                    $"Target states for each trigger {trigger} in state {State} must be unique");
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time this <typeparamref name="TState"/> is entered.
        /// </summary>
        public StateConfiguration<TState, TTrigger> OnEntry(Action action)
        {
            ParameterlessEntryActions.Add(action);
            return this;
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time this <typeparamref name="TState"/> is entered from <paramref name="state" />.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [Obsolete("Not yet implemented")]
        public StateConfiguration<TState, TTrigger> OnEntryFrom(TState state, Action action)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start executing a <see cref="BehaviourTree"/> each time this <typeparamref name="TState"/> is entered.
        /// </summary>
        /// <remarks>
        /// This should be combined with a corresponding <see cref="OnExit(BehaviourTree)"/> event and a
        /// <see cref="InternalTransition(TTrigger,BehaviourTree)"/> to tick the
        /// tree.
        /// </remarks>
        /// <seealso cref="BehaviourTree"/>
        public StateConfiguration<TState, TTrigger> OnEntry(
            BehaviourTree tree,
            TTrigger onSuccess,
            TTrigger onFailure,
            TTrigger onError
        )
        {
            OnEntry(_ =>
            {
                if (Machine.ShouldDebug)
                {
                    Machine.PostDebug(
                        $"{GetBehaviourTreeDebugName(tree)} root-task {tree.Root.GetType().Name} '{tree.Root.Name}' started.");
                    tree.OnTransition += TransitionDebugHandler;
                }

                tree.Start(Observer);
            });

            void Observer(TaskState result)
            {
                if (Machine.ShouldDebug)
                {
                    tree.OnTransition -= TransitionDebugHandler;
                    Machine.PostDebug(
                        $"{GetBehaviourTreeDebugName(tree)} root {tree.Root.GetType().Name} '{tree.Root.Name}' completed with status {tree.Root.TaskState}");
                }

                TTrigger trigger = result switch
                {
                    TaskState.Success => onSuccess,
                    TaskState.Failure => onFailure,
                    _ => onError,
                };
                Machine.Fire(trigger);
            }

            return this;
        }

        /// <summary>
        /// Generates a name of a given tree for debug purposes.
        /// </summary>
        /// <param name="tree">The tree to generate a name for.</param>
        private string GetBehaviourTreeDebugName(BehaviourTree tree) =>
            (string.IsNullOrEmpty(tree.Name) ? nameof(BehaviourTree) : tree.Name);

        /// <summary>
        /// Handler that routes transition information into the debug delegate.
        /// </summary>
        private void TransitionDebugHandler(BehaviourTree.Transition transition)
        {
            if (Machine.ShouldDebug)
            {
                Machine.PostDebug(
                    $"{GetBehaviourTreeDebugName(transition.Task.Owner)} task {transition.Task.GetType().Name} '{transition.Task.Name}' transitioned from {transition.Source} to {transition.Destination}");
            }
        }

        /// <summary>
        /// Abort a <see cref="BehaviourTree"/> each time this <typeparamref name="TState"/> is exited.
        /// </summary>
        /// <remarks>
        /// This should be combined with a corresponding
        /// <see cref="OnEntry(BehaviourTree,TTrigger,TTrigger,TTrigger)"/> event and an
        /// <see cref="InternalTransition(TTrigger,BehaviourTree)"/> to tick the tree.
        /// </remarks>
        /// <seealso cref="BehaviourTree"/>
        public StateConfiguration<TState, TTrigger> OnExit(BehaviourTree tree)
        {
            OnExit(_ => tree.Abort());
            return this;
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time this <typeparamref name="TState"/> is entered.
        /// </summary>
        public StateConfiguration<TState, TTrigger> OnEntry(Action<Transition<TState, TTrigger>> action)
        {
            EntryActions.Add(action);
            return this;
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time this <typeparamref name="TState"/> is exited.
        /// </summary>
        public StateConfiguration<TState, TTrigger> OnExit(Action action) => OnExit(_ => action());

        /// <summary>
        /// Execute <paramref name="action"/> each time this <typeparamref name="TState"/> is exited.
        /// </summary>
        public StateConfiguration<TState, TTrigger> OnExit(Action<Transition<TState, TTrigger>> action)
        {
            ExitActions.Add(action);
            return this;
        }

        /// <summary>
        /// Execute <paramref name="action"/> each time <paramref name="trigger"/> is fired without leaving this state.
        /// </summary>
        public StateConfiguration<TState, TTrigger> InternalTransition(TTrigger trigger, Action action)
            => InternalTransition(trigger, _ => action());

        /// <summary>
        /// Tick <paramref name="tree"/> each time <paramref name="trigger"/> is fired without leaving this state.
        /// </summary>
        public StateConfiguration<TState, TTrigger> InternalTransition(TTrigger trigger, BehaviourTree tree)
            => InternalTransition(trigger, tree.Tick);

        /// <summary>
        /// Checks whether a given trigger is associated with an internal transition in this state configuration.
        /// </summary>
        /// <param name="trigger">The trigger to check.</param>
        /// <returns>Whether the trigger is internal.</returns>
        public bool IsInternal(TTrigger trigger) => InternalActions.ContainsKey(trigger);
    }
}