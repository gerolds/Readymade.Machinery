using System.Collections.Generic;
using System.Linq;
using Readymade.Machinery.Acting;
using UnityEditor;
using UnityEngine;

namespace Readymade.Machinery.Editor.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A simple debug view of all active directors that are currently loaded.
    /// </summary>
    public class ActingDebugWindow : EditorWindow
    {
        /// <summary>
        /// stores the current scroll view position
        /// </summary>
        private Vector2 _scrollPos;

        /// <summary>
        /// Menu-Command to create a new window.
        /// </summary>
        [MenuItem("Window/Machinery/" + nameof(ActingDebugWindow))]
        private static void Init()
        {
            ActingDebugWindow window = GetWindow<ActingDebugWindow>();
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
            if (!Application.isPlaying)
            {
                GUILayout.Label("Nothing to display in edit-mode.", EditorStyles.miniLabel, GUILayout.Width(280));
                return;
            }
            using EditorGUILayout.HorizontalScope hBox = new();
            using EditorGUILayout.ScrollViewScope scrollView = new(_scrollPos, false, false);
            _scrollPos = scrollView.scrollPosition;

            int i = 0;
            using (GUILayout.HorizontalScope h0 = new())
            {
                using (GUILayout.VerticalScope v1 = new())
                {
                    // role queued performances
                    foreach (IDirector director in DirectorRegistry.Instances)
                    {
                        i++;
                        GUILayout.Label($"Role Queued Performances", EditorStyles.boldLabel);
                        if (!director.Queues.First().queue.Any())
                        {
                            GUILayout.Label("Nothing queued", EditorStyles.miniLabel, GUILayout.Width(280));
                            continue;
                        }

                        foreach ((object key, IEnumerable<(IPerformance perf, int prio)> queue) in director.Queues)
                        {
                            GUILayout.Space(4);
                            GUILayout.Label($"[{key}] Queue", EditorStyles.label);
                            GUILayout.Space(4);
                            using (GUILayout.HorizontalScope h1 = new())
                            {
                                GUILayout.Label("!", EditorStyles.miniLabel, GUILayout.Width(30));
                                GUILayout.Label("Phase", EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label("Run", EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label("Hash", EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label("Name", EditorStyles.miniLabel, GUILayout.Width(100));
                            }

                            GUILayout.Space(4);
                            foreach ((IPerformance perf, int prio) in queue)
                            {
                                using GUILayout.HorizontalScope h2 = new();
                                GUILayout.Label(prio.ToString(), EditorStyles.miniLabel, GUILayout.Width(30));
                                GUILayout.Label(perf.Phase.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.RunCount.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.GetHashCode().ToString("X")[..4], EditorStyles.miniLabel,
                                    GUILayout.Width(50));
                                GUILayout.Label(string.IsNullOrEmpty(perf.Name) ? "-" : perf.Name, EditorStyles.miniLabel,
                                    GUILayout.Width(100));
                            }
                        }
                    }

                    // actor queued performances
                    foreach (IDirector director in DirectorRegistry.Instances)
                    {
                        i++;
                        GUILayout.Label($"Actor Queued Performances", EditorStyles.boldLabel);
                        if (!director.ActorQueues.Any())
                        {
                            GUILayout.Label("Nothing queued", EditorStyles.miniLabel, GUILayout.Width(280));
                            continue;
                        }

                        foreach ((IActor actor, IEnumerable<(IPerformance perf, int prio)> queue) in director.ActorQueues)
                        {
                            int row = -1;
                            foreach ((IPerformance perf, int prio) in queue)
                            {
                                row++;
                                if (row == 0)
                                {
                                    GUILayout.Space(4);
                                    GUILayout.Label($"[{actor.Name}] Queue", EditorStyles.label);
                                    GUILayout.Space(4);
                                    using (GUILayout.HorizontalScope h3 = new())
                                    {
                                        GUILayout.Label("!", EditorStyles.miniLabel, GUILayout.Width(30));
                                        GUILayout.Label("Phase", EditorStyles.miniLabel, GUILayout.Width(50));
                                        GUILayout.Label("Run", EditorStyles.miniLabel, GUILayout.Width(50));
                                        GUILayout.Label("Hash", EditorStyles.miniLabel, GUILayout.Width(50));
                                        GUILayout.Label("Name", EditorStyles.miniLabel, GUILayout.Width(100));
                                    }

                                    GUILayout.Space(4);
                                }

                                using GUILayout.HorizontalScope h4 = new();
                                GUILayout.Label(prio.ToString(), EditorStyles.miniLabel, GUILayout.Width(30));
                                GUILayout.Label(perf.Phase.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.RunCount.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.GetHashCode().ToString("X")[..4], EditorStyles.miniLabel,
                                    GUILayout.Width(50));
                                GUILayout.Label(string.IsNullOrEmpty(perf.Name) ? "-" : perf.Name,
                                    EditorStyles.miniLabel, GUILayout.Width(100));
                            }
                        }
                    }
                }


                // claimed performances
                using (GUILayout.VerticalScope v2 = new())
                {
                    foreach (IDirector director in DirectorRegistry.Instances)
                    {
                        GUILayout.Space(4);
                        GUILayout.Label($"Claimed Performances", EditorStyles.boldLabel);
                        if (!director.Claims.Any())
                        {
                            GUILayout.Label("No claims", EditorStyles.miniLabel, GUILayout.Width(350));
                            continue;
                        }

                        GUILayout.Space(4);
                        using (GUILayout.HorizontalScope h1 = new())
                        {
                            GUILayout.Label("Actor", EditorStyles.miniLabel, GUILayout.Width(100));
                            GUILayout.Label("Phase", EditorStyles.miniLabel, GUILayout.Width(50));
                            GUILayout.Label("Run", EditorStyles.miniLabel, GUILayout.Width(50));
                            GUILayout.Label("Hash", EditorStyles.miniLabel, GUILayout.Width(100));
                            GUILayout.Label("Name", EditorStyles.miniLabel, GUILayout.Width(200));
                        }

                        GUILayout.Space(4);
                        foreach ((IActor actor, IPerformance perf) in director.Claims)
                        {
                            if (actor == null){
                                continue;
                            }
                            using (GUILayout.HorizontalScope h1 = new())
                            {
                                GUILayout.Label(actor.Name, EditorStyles.miniLabel, GUILayout.Width(100));
                                GUILayout.Label(perf.Phase.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.RunCount.ToString(), EditorStyles.miniLabel, GUILayout.Width(50));
                                GUILayout.Label(perf.GetHashCode().ToString("X")[..4], EditorStyles.miniLabel,
                                    GUILayout.Width(100));
                                GUILayout.Label(string.IsNullOrEmpty(perf.Name) ? "-" : perf.Name,
                                    EditorStyles.miniLabel, GUILayout.Width(200));
                            }

                            if (perf.CurrentGesture != default)
                            {
                                using (GUILayout.HorizontalScope h1 = new())
                                {
                                    GUILayout.Label("+", EditorStyles.miniLabel, GUILayout.Width(100));
                                    GUILayout.Label("+", EditorStyles.miniLabel, GUILayout.Width(50));
                                    GUILayout.Label(
                                        perf.CurrentGesture.IsPoseRequired
                                            ? $"{Vector3.Distance(perf.CurrentGesture.Pose.position, actor.Pose.position):f2}"
                                            : "-",
                                        EditorStyles.miniLabel,
                                        GUILayout.Width(50)
                                    );
                                    GUILayout.Label(
                                        perf.CurrentGesture.IsPropRequired ? perf.CurrentGesture.Prop.Identity.Name : "-",
                                        EditorStyles.miniLabel,
                                        GUILayout.Width(100)
                                    );
                                    GUILayout.Label(
                                        string.IsNullOrEmpty(perf.CurrentGesture.Name) ? "-" : perf.CurrentGesture.Name,
                                        EditorStyles.miniLabel,
                                        GUILayout.Width(200)
                                    );
                                }
                            }

                            GUILayout.Space(4);
                        }
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