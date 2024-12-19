using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Vertx.Debugging;
using Random = UnityEngine.Random;

namespace Readymade.Machinery.Acting
{
    [Serializable]
    public class DropUnityEvent : UnityEvent<GameObject>
    {
    }

    /// <summary>
    /// Component that allows configuration of a drop type instantiation event of a prefab. Drop here means, that the prefab is
    /// instantiated nearby on the ground in a random orientation. Intended to be called with a <see cref="UnityEvent"/>.
    /// </summary>
    /// <remarks>This is a prototyping component.</remarks>
    public class DropComponent : MonoBehaviour
    {
        [Tooltip("The prefab to instantiate as a drop.")]
        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private bool inPlace;

        [SerializeField]
        [HideIf(nameof(inPlace))]
        [Tooltip(
            "Range of the random radial distance from the transform of this object where the Prefab will be dropped. Actual distance will be minDropDistance + Random(0, dropRange).")]
        private float _dropRange = 0.5f;

        [Tooltip("Minimum radial distance from the transform of this object where the Prefab will be dropped.")]
        [HideIf(nameof(inPlace))]
        private float _minDropDistance = 0.5f;

        [SerializeField]
        [Tooltip("Height from which the object will be dropped (this is the start of the ray along the transform's up-axis)")]
        [HideIf(nameof(inPlace))]
        private float _dropHeight = 1f;

        [Tooltip("Collider mask onto which the prefab will be dropped.")]
        [SerializeField]
        [HideIf(nameof(inPlace))]
        private LayerMask _groundMask;

        [Tooltip("Whether to print debug messages.")]
        [SerializeField]
        private bool _debug;

        [SerializeField]
        [Min(0)]
        private float destroyDelayed = 0;

        [Tooltip("Called whenever a Prefab is dropped.")]
        [SerializeField]
        private DropUnityEvent _onDrop;

        /// <summary>
        /// Drops the configured prefab at a location on the <see cref="groundMask"/> nearby the the transform of this object. Intended to be called from a <see cref="UnityEvent"/>.
        /// </summary>
        public void Drop()
        {
            if (_prefab)
            {
                if (inPlace)
                {
                    GameObject instance = Instantiate(_prefab, transform.position, transform.rotation);
                    InvokeEvents(instance);
                }
                else
                {
                    Vector3 dropOffset = (Random.insideUnitSphere * _dropRange);
                    dropOffset.Scale(new Vector3(1f, 0f, 1f));
                    Vector3 p = transform.position + dropOffset + transform.up * _dropHeight;
                    if (DrawPhysics.Raycast(
                            origin: p,
                            direction: -transform.up,
                            out RaycastHit hit,
                            maxDistance: _dropHeight * 2f,
                            layerMask: _groundMask,
                            queryTriggerInteraction: QueryTriggerInteraction.Ignore
                        )
                    )
                    {
                        GameObject instance = Instantiate(_prefab, hit.point,
                            Quaternion.AngleAxis(Random.Range(-180f, 180f), transform.up));
                        if (_debug)
                        {
                            Debug.Log($"Prefab {_prefab.name} instantiated at {hit.point}.");
                        }

                        InvokeEvents(instance);
                    }
                    else
                    {
                        if (_prefab)
                        {
                            Debug.LogWarning(
                                $"Failed to drop {_prefab.name} at {transform.position} because no ground collider was found.");
                        }
                    }
                }
            }
        }

        private void InvokeEvents(GameObject instance)
        {
            _onDrop.Invoke(instance);
            if (destroyDelayed > 0)
            {
                Destroy(instance, destroyDelayed);
            }
        }
    }
}