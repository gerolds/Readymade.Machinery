using Readymade.Machinery.Acting;
using UnityEditor;
using UnityEngine;

namespace Readymade.Machinery.Editor.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A simple debug view of all active behaviour trees that are currently loaded.
    /// </summary>
    public class PerformanceDebugWindow : EditorWindow
    {
        /// <summary>
        /// stores the current scroll view position
        /// </summary>
        private Vector2 _scrollPos;

        /// <summary>
        /// Menu-Command to create a new window.
        /// </summary>
        [MenuItem("Window/Machinery/" + nameof(PerformanceDebugWindow))]
        private static void Init()
        {
            PerformanceDebugWindow window = GetWindow<PerformanceDebugWindow>();
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
            GUILayout.Label($"All Active Performances", EditorStyles.boldLabel);
            using EditorGUILayout.HorizontalScope hBox = new();
            using EditorGUILayout.ScrollViewScope scrollView = new(_scrollPos, false, false);
            _scrollPos = scrollView.scrollPosition;

            foreach (IPerformance instance in PerformanceRegistry.Instances)
            {
                GUILayout.Label($"Performance {instance.GetHashCode().ToString("X")[..4]} |Phase  {instance.Phase} |Run {instance.RunCount} |Name {instance.Name}", EditorStyles.boldLabel);
                foreach (IGesture gesture in instance.Gestures)
                {
                    GUILayout.Label($"+ Gesture {gesture.GetHashCode().ToString("X")[..4]} |Phase {gesture.Phase} |Pos {gesture.Pose.position} |Prop {gesture.Prop.Identity.Name} |Name {gesture.Name} ", EditorStyles.miniLabel);
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