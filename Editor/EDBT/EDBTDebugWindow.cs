using Readymade.Machinery.EDBT;
using UnityEditor;
using UnityEngine;

namespace Readymade.Machinery.Editor.EDBT
{
    /// <summary>
    /// A simple debug view of all active behaviour trees that are currently loaded.
    /// </summary>
    public class EDBTDebugWindow : EditorWindow
    {
        /// <summary>
        /// stores the current scroll view position
        /// </summary>
        private Vector2 _scrollPos;

        /// <summary>
        /// Menu-Command to create a new window.
        /// </summary>
        [MenuItem("Window/Machinery/" + nameof(EDBTDebugWindow))]
        private static void Init()
        {
            EDBTDebugWindow window = GetWindow<EDBTDebugWindow>();
            window.Show();
        }

        /// <summary>
        /// Unity OnGUI event handler.
        /// </summary>
        private void OnGUI()
        {
            DrawContent();
        }

        /// <summary>
        /// Draw the window content.
        /// </summary>
        private void DrawContent()
        {
            using EditorGUILayout.HorizontalScope hBox = new();
            using EditorGUILayout.ScrollViewScope scrollView = new(_scrollPos, false, false);
            _scrollPos = scrollView.scrollPosition;

            foreach (BehaviourTree instance in BehaviourTreeRegistry.Instances)
            {
                if (instance.Root == null)
                {
                    GUILayout.Label("[NO-ROOT] " + instance.Name, EditorStyles.boldLabel);
                    continue;
                }

                if (!instance.Scheduler.TryPeekFirst(out ITask first) || first is VoidTask)
                {
                    // GUILayout.Label ( "[IDLE] " + instance.Name, EditorStyles.boldLabel );
                    continue;
                }

                GUILayout.Space(4);
                GUILayout.Label(instance.Name, EditorStyles.boldLabel);
                GUILayout.Space(4);
                foreach (ITask task in instance.Scheduler)
                {
                    using GUILayout.HorizontalScope h1 = new();
                    if (task is VoidTask)
                    {
                        GUILayout.Label("[FRAME-BREAK]", EditorStyles.miniLabel, GUILayout.Width(400));
                    }
                    else
                    {
                        GUILayout.Label(task.TaskState.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                        GUILayout.Label(task.GetType().Name, EditorStyles.miniLabel, GUILayout.Width(100));
                        GUILayout.Label(task.Name, EditorStyles.miniLabel, GUILayout.Width(250));
                        GUILayout.Label(task.GetHashCode().ToString("X")[..4], EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                }
            }
        }

        /// <summary>
        /// Unity Update event handler.
        /// </summary>
        private void Update()
        {
            Repaint();
        }
    }
}