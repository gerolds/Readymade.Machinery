using System;
using Readymade.Persistence;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// <see cref="IActor"/> describes the executing entity of performances. Typically some sort of agent that acts autonomously
    /// in some context. An actor can execute any <see cref="IGesture{IActor}"/> that is part of a <see cref="IPerformance{IActor}"/> and hold any required <see cref="IProp"/> in their <see cref="Inventory"/>.
    /// </summary>
    public interface IActor : IScriptableEntity
    {
        /// <summary>
        /// The current pose of the <see cref="IActor"/>.
        /// </summary>
        Pose Pose { get; }

        /// <summary>
        /// The <see cref="IActor"/>'s inventory that can hold any <see cref="SoProp"/>.
        /// </summary>
        public IInventory<SoProp> Inventory { get; }

        /// <summary>
        /// The <see cref="IActor"/>'s (equipment) slots that can each be assigned to hold any <see cref="SoProp"/>.
        /// </summary>
        public IEquipment<SoSlot, SoProp> Equipment { get; }

        public string Name { get; }

        /*
        /// <summary>
        /// Represents any actor.
        /// </summary>
        public static IActor Any { get; } = new NullActor ();
        */

        /// <summary>
        /// Represents the absence of an actor.
        /// </summary>
        public static IActor None { get; } = new NullActor();

        public IPropConsumer Consumer { get; }

        /// <inheritdoc />
        /// <summary>
        /// Represents an undefined actor.
        /// </summary>
        public class NullActor : IActor
        {
            // we disable instantiation of this class.
            internal NullActor()
            {
            }

            /// <inheritdoc />
            public Pose Pose => default;

            /// <inheritdoc />
            public IInventory<SoProp> Inventory => default;

            public IEquipment<SoSlot, SoProp> Equipment => default;
            public IPropConsumer Consumer => default;
            public Sprite Portrait => default;

            /// <inheritdoc />
            public string Name => string.Empty;


            public void OnFx(ActorFx fx)
            {
            }

            public Guid EntityID => default;
            public GameObject GetObject() => null;
        }

        /// <summary>
        /// Called by the behaviour driver to trigger effects on the actor.
        /// </summary>
        /// <param name="fx">The effects to play immediately on this actor.</param>
        void OnFx(ActorFx fx);
    }
}