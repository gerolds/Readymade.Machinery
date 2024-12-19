using Readymade.Machinery.Acting;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Readymade.Machinery.Editor.Acting
{
    [CustomEditor(typeof(InventoryComponent))]
    public class InventoryComponentEditor :
#if ODIN_INSPECTOR
        OdinEditor
#else
        NaughtyAttributes.NaughtyInspector
#endif
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var component = (InventoryComponent)target;
            if (Application.isPlaying)
            {
                GUILayout.Space(7);
                GUILayout.Label("Unclaimed Items");
                foreach (var item in component.Unclaimed)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    Object obj = EditorGUILayout.ObjectField(item.Prop, typeof(SoProp), false);
                    int count = EditorGUILayout.IntField((int)component.GetAvailableCount((SoProp)obj));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(7);
                GUILayout.Label("Claimed Items");
                foreach (var claim in component.Claims)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    Object obj = EditorGUILayout.ObjectField(claim.Prop, typeof(SoProp), false);
                    int count = EditorGUILayout.IntField((int)claim.Count);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(7);
                GUILayout.Label("Flows Items");
                foreach (var claim in component.Flows)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    Object obj = EditorGUILayout.ObjectField(claim.Prop, typeof(SoProp), false);
                    int pressure = EditorGUILayout.IntField((int)claim.Pressure);
                    int flowIn = EditorGUILayout.IntField((int)claim.In);
                    int flowOut = EditorGUILayout.IntField((int)claim.Out);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}