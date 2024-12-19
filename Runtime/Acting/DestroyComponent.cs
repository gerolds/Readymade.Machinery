using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Destroys the target object with some configurable parameters. Intended to be called from a <see cref="UnityEvent"/>.
    /// </summary>
    /// <remarks>This is a prototyping component.</remarks>
    public class DestroyComponent : CommandComponent
    {
        [Tooltip("Delay destruction by this many seconds.")]
        [Min(0)]
        [SerializeField]
        private float _delay = 0;

        [FormerlySerializedAs("triggerOnEnable")]
        [SerializeField]
        private bool destroySelfOnEnable = false;

        private void OnEnable()
        {
            if (destroySelfOnEnable)
            {
                DestroySelf();
            }
        }

        /// <summary>
        /// Destroys the GameObject of this component.
        /// </summary>
        public void DestroySelf()
        {
            Destroy(gameObject, _delay);
        }

        /// <summary>
        /// Destroys the <paramref name="target"/> GameObject.
        /// </summary>
        public void Destroy(GameObject target)
        {
            Destroy(target, _delay);
        }
        
        protected override void OnExecute() => DestroySelf();
    }
}