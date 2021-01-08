#if UNITY_EDITOR
using UnityEngine;
using VRC.Udon;

namespace Guribo.UdonUtils.Scripts.Examples
{
    public class InspectorValueChangerExample : MonoBehaviour
    {
        public UdonBehaviour editorTestingBehaviour;


        [ContextMenu("TrySetVariables")]
        public void TrySetVariables()
        {
            editorTestingBehaviour.SetInspectorVariable("testInt", 100);
            editorTestingBehaviour.SetInspectorVariable("testString", "Hello World");
            editorTestingBehaviour.SetInspectorVariable("testTransform", gameObject.transform);
            editorTestingBehaviour.SetInspectorVariable("testGameObject", gameObject);
            editorTestingBehaviour.SetInspectorVariable("testScriptReference", editorTestingBehaviour);
        }

        [ContextMenu("TrySetNull")]
        public void TrySetNull()
        {
            editorTestingBehaviour.SetInspectorVariable("testInt", 0);
            editorTestingBehaviour.SetInspectorVariable("testString", null);
            editorTestingBehaviour.SetInspectorVariable("testTransform", null);
            editorTestingBehaviour.SetInspectorVariable("testGameObject", null);
            editorTestingBehaviour.SetInspectorVariable("testScriptReference", null);
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