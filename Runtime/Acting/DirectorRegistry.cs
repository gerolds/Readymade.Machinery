#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// A static registry for active <see cref="IPerformance"/> instances. Used only in the editor for debugging.
    /// </summary>
    public static class DirectorRegistry
    {
        /// <summary>
        /// Storage for registered <see cref="IPerformance"/> instances.
        /// </summary>
        private static List<IDirector> s_instances = new();

        /// <summary>
        /// A list of all behaviour tree instances. Used for debugging.
        /// </summary>
        public static IReadOnlyList<IDirector> Instances => s_instances;


        /// <summary>
        /// Ensure the list of instance is cleared when domain reload is disabled.
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void OnEnterPlaymode()
        {
            foreach (IDirector instance in s_instances.ToList())
            {
                instance.Dispose();
            }

            Debug.Assert(s_instances.Count == 0, "instances.Count == 0");
            s_instances.Clear();
        }

        /// <summary>
        /// Unregister a <see cref="IPerformance"/> instance.
        /// </summary>
        public static void Unregister(IDirector performance)
        {
            s_instances.Remove(performance);
        }

        /// <summary>
        /// Register a <see cref="IPerformance"/> instance.
        /// </summary>
        public static void Register(IDirector performance)
        {
            s_instances.Add(performance);
        }
    }
}
#endif