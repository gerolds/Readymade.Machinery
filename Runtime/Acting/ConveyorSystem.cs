using System;
using System.Collections.Generic;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    public class ConveyorSystem : MonoBehaviour
    {
        private readonly HashSet<InventoryBalancer> _components = new();
        [SerializeField] private float tickTnterval = 1f;
        private float _nextTick;

        private void Awake()
        {
            Debug.Log("ConveyorSystem Awake");
            _components.Clear();
        }

        private void Update()
        {
            if (_nextTick < Time.time)
            {
                foreach (var component in _components)
                {
                    if (!component)
                    {
                        Debug.Log($"[{nameof(ConveyorSystem)}] Component is null", this);
                        continue;
                    }

                    component.Tick(Time.time - _nextTick);
                }

                _nextTick = Time.time + tickTnterval;
            }
        }

        public void Register(InventoryBalancer component)
        {
            _components.Add(component);
        }

        public void UnRegister(InventoryBalancer component)
        {
            _components.Remove(component);
        }
    }
}