#if UNITY_EDITOR
using TLP.UdonUtils.Editor;
using UnityEngine;
using VRC.Udon;

namespace TLP.UdonUtils.EditorOnly
{
    public class InspectorValueChangerExample : MonoBehaviour
    {
        public UdonBehaviour editorTestingBehaviour;

        [ContextMenu("TrySetVariables")]
        public void TrySetVariables()
        {
            editorTestingBehaviour.SetInspectorVariable<int>("testInt", 100);
            editorTestingBehaviour.SetInspectorVariable<string>("testString", "Hello World");
            editorTestingBehaviour.SetInspectorVariable<Transform>("testTransform", gameObject.transform);
            editorTestingBehaviour.SetInspectorVariable<GameObject>("testGameObject", gameObject);
            editorTestingBehaviour.SetInspectorVariable<UdonBehaviour>("testScriptReference", editorTestingBehaviour);
        }

        [ContextMenu("TrySetNull")]
        public void TrySetNull()
        {
            editorTestingBehaviour.SetInspectorVariable<int>("testInt", 0);
            editorTestingBehaviour.SetInspectorVariable<string>("testString", null);
            editorTestingBehaviour.SetInspectorVariable<Transform>("testTransform", null);
            editorTestingBehaviour.SetInspectorVariable<GameObject>("testGameObject", null);
            editorTestingBehaviour.SetInspectorVariable<UdonBehaviour>("testScriptReference", null);
        }

        [ContextMenu("TrySetDefaultVariables")]
        public void TrySetDefaultValues()
        {
            editorTestingBehaviour.ResetInspectorVariable("testInt");
            editorTestingBehaviour.ResetInspectorVariable("testString");
            editorTestingBehaviour.ResetInspectorVariable("testTransform");
            editorTestingBehaviour.ResetInspectorVariable("testGameObject");
            editorTestingBehaviour.ResetInspectorVariable("testScriptReference");
        }
    }
}
#endif
