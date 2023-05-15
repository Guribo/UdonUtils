using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Examples
{
    public class EditorTesting : UdonSharpBehaviour
    {
        public int testInt;
        public string testString;
        public Transform testTransform;
        public GameObject testGameObject;
        public EditorTesting testScriptReference;
    }
}
