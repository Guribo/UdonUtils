using UnityEditor;
using UnityEngine;

namespace Guribo.UdonUtils.Scripts.Examples.Editor
{
    [CustomEditor(typeof(InspectorValueChangerExample))]
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