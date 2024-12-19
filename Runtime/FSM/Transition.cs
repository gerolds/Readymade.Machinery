namespace Readymade.Machinery.FSM
{
    /// <summary>
    /// Describes a state transition.
    /// </summary>
    public struct Transition<TState, TTrigger>
        where TState : System.Enum
        where TTrigger : System.Enum
    {
        /// <summary>
        /// The <typeparamref name="TState"/> the state machine was in before the transition.
        /// </summary>
        public readonly TState Source;

        /// <summary>
        /// The <typeparamref name="TState"/> the state will be in after the transition.
        /// </summary>
        public readonly TState Destination;

        /// <summary>
        /// The <typeparamref name="TTrigger"/> that initiated the transition.
        /// </summary>
        public readonly TTrigger Trigger;

        /// <summary>
        /// Creates a new transition structure.
        /// </summary>
        /// <param name="trigger">The <typeparamref name="TTrigger"/> that initiated the transition.</param>
        /// <param name="source">The <typeparamref name="TState"/> the state machine was in before the transition. <seealso cref="StateMachine{TState,TTrigger}.State"/></param>
        /// <param name="destination">The <typeparamref name="TState"/> the state will be in after the transition. <seealso cref="StateMachine{TState,TTrigger}.State"/></param>
        public Transition(TTrigger trigger, TState source, TState destination)
        {
            Source = source;
            Destination = destination;
            Trigger = trigger;
        }
    }
}