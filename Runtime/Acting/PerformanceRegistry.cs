#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A static registry for active <see cref="IPerformance"/> instances. Used only in the editor for debugging.
    /// </summary>
    public static class PerformanceRegistry
    {
        /// <summary>
        /// Storage for registered <see cref="IPerformance"/> instances.
        /// </summary>
        private static List<IPerformance> s_instances = new();

        /// <summary>
        /// A list of all behaviour tree instances. Used for debugging.
        /// </summary>
        public static IReadOnlyList<IPerformance> Instances => s_instances;


        /// <summary>
        /// Ensure the list of instance is cleared when domain reload is disabled.
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void OnEnterPlaymode()
        {
            foreach (IPerformance instance in s_instances.ToList())
            {
                instance.Dispose();
            }

            Debug.Assert(s_instances.Count == 0, "instances.Count == 0");
            s_instances.Clear();
        }

        /// <summary>
        /// Unregister a <see cref="IPerformance"/> instance.
        /// </summary>
        public static void Unregister(IPerformance performance)
        {
            s_instances.Remove(performance);
        }

        /// <summary>
        /// Register a <see cref="IPerformance"/> instance.
        /// </summary>
        public static void Register(IPerformance performance)
        {
            s_instances.Add(performance);
        }
    }
}
#endif