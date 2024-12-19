using System;
using System.Linq;
using Readymade.Machinery.Shared;
using Readymade.Persistence;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc cref="IActor" />
    /// <summary>
    /// Base class for all editor-assignable <see cref="IActor" /> instances. The <c>-Driver</c> suffix means that this is the
    /// main component of the agent, its brain and decision making hub.
    /// </summary>
    /// <remarks>The <see cref="Actor"/> abstract class only exists to make agents editor assignable and nothing should be
    /// implemented in it. It merely mirrors the <see cref="IActor"/> interface.</remarks>
    public abstract class Actor : MonoBehaviour, IActor
    {
        [BoxGroup("Actor")]
        [SerializeField]
        [Required]
        private InventoryComponent inventory;

        [BoxGroup("Actor")]
        [SerializeField]
        [Required]
        private EquipmentComponent equipment;

        [BoxGroup("Actor")]
        [SerializeField]
        [Required]
        private PropConsumerComponent consumer;

        [BoxGroup("Actor")]
        [SerializeField]
        [Required]
        private Sprite portrait;

        /// <inheritdoc />
        public virtual Pose Pose => PoseExtensions.PoseFrom(transform);

        /// <inheritdoc />
        public virtual IInventory<SoProp> Inventory => inventory;

        /// <inheritdoc />
        public IEquipment<SoSlot, SoProp> Equipment => equipment;

        /// <inheritdoc />
        public virtual string Name => name;

        public IPropConsumer Consumer => consumer;
        public Sprite Portrait => portrait;

        public abstract void OnFx(ActorFx fx);

        public GameObject GameObject => gameObject;

        public abstract Animator Animator { get; }

        protected virtual void OnDrawGizmosSelected()
        {
        }

#if UNITY_EDITOR
#if !ODIN_INSPECTOR
        protected DropdownList<int> GetTriggerParameterIDs()
        {
            DropdownList<int> list = new DropdownList<int>();
            Animator?.parameters
                .Where(it => it.type == AnimatorControllerParameterType.Trigger)
                .ForEach(it => list.Add(it.name, Shader.PropertyToID(it.name)));
            return list;
        }

        protected DropdownList<int> GetFloatParameterIDs()
        {
            DropdownList<int> list = new DropdownList<int>();
            Animator?.parameters
                .Where(it => it.type == AnimatorControllerParameterType.Float)
                .ForEach(it => list.Add(it.name, Shader.PropertyToID(it.name)));
            return list;
        }

        protected DropdownList<int> GetBoolParameterIDs()
        {
            DropdownList<int> list = new DropdownList<int>();
            Animator?.parameters
                .Where(it => it.type == AnimatorControllerParameterType.Bool)
                .ForEach(it => list.Add(it.name, Shader.PropertyToID(it.name)));
            return list;
        }

        protected DropdownList<int> GetIntParameterIDs()
        {
            DropdownList<int> list = new DropdownList<int>();
            Animator?.parameters
                .Where(it => it.type == AnimatorControllerParameterType.Int)
                .ForEach(it => list.Add(it.name, Shader.PropertyToID(it.name)));
            return list;
        }
#endif
#endif
        public Guid EntityID => GetComponentInParent<PackIdentity>(true)?.EntityID ?? Guid.Empty;
        public GameObject GetObject() => gameObject;
    }
}