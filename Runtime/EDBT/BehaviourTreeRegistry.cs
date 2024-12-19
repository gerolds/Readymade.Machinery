#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A static registry for active <see cref="BehaviourTree"/> instances. Used only in the editor for debugging.
    /// </summary>
    public static class BehaviourTreeRegistry
    {
        /// <summary>
        /// Storage for registered <see cref="BehaviourTree"/> instances.
        /// </summary>
        private static List<BehaviourTree> s_instances = new();

        /// <summary>
        /// A list of all behaviour tree instances. Used for debugging.
        /// </summary>
        public static IReadOnlyList<BehaviourTree> Instances => s_instances;


        /// <summary>
        /// Ensure the list of instance is cleared when domain reload is disabled.
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void OnEnterPlaymode()
        {
            foreach (BehaviourTree instance in s_instances.ToList())
            {
                instance.Dispose();
            }

            Debug.Assert(s_instances.Count == 0, "instances.Count == 0");
            s_instances.Clear();
        }

        /// <summary>
        /// Unregister a <see cref="BehaviourTree"/> instance.
        /// </summary>
        public static void Unregister(BehaviourTree behaviourTree)
        {
            s_instances.Remove(behaviourTree);
        }

        /// <summary>
        /// Register a <see cref="BehaviourTree"/> instance.
        /// </summary>
        public static void Register(BehaviourTree behaviourTree)
        {
            s_instances.Add(behaviourTree);
        }
    }
}
#endif