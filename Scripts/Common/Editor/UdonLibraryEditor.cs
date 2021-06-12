#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Guribo.UdonUtils.Scripts.Common.Editor
{
    public abstract class UdonLibraryEditor : UnityEditor.Editor
    {
        protected abstract string GetSymbolName();

        public override void OnInspectorGUI()
        {
            var symbolName = GetSymbolName();
            Debug.Assert(!string.IsNullOrEmpty(symbolName), "GetSymbolName provided valid string");

            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent($"Set {symbolName} references in scene",
                $"Links all UdonBehaviours with '{symbolName}' parameter to this object")))
            {
                var udonBehaviours = FindObjectsOfType<UdonBehaviour>();
                var value = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour) target);

                foreach (var udonBehaviour in udonBehaviours)
                {
                    try
                    {
                        udonBehaviour.SetInspectorVariable(symbolName, value);
                        var udonBehaviourGameObject = udonBehaviour.gameObject;
                        Debug.Log($"Set {symbolName} reference on {udonBehaviourGameObject}.{udonBehaviour.name}",
                            udonBehaviourGameObject);
                    }
                    catch (Exception)
                    {
                        // ignored, fails for behaviours that don't have a symbol with that name
                    }
                }
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
#endif