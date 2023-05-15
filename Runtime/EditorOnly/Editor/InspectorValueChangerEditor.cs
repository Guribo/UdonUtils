#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TLP.UdonUtils.EditorOnly
{
    [CustomEditor(typeof(InspectorValueChangerExample)), CanEditMultipleObjects]
    public class InspectorValueChangerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var inspectorValueChanger = (InspectorValueChangerExample) target;
            try
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Set Values"))
                {
                    inspectorValueChanger.TrySetVariables();
                }

                if (GUILayout.Button("Set Null"))
                {
                    inspectorValueChanger.TrySetNull();
                }

                if (GUILayout.Button("Default"))
                {
                    inspectorValueChanger.TrySetDefaultValues();
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
            }
        }
    }
}
#endif
