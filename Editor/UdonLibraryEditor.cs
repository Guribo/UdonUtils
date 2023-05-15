//#define DEBUG_UDON_LIBRARY_EDITOR

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TLP.UdonUtils.Editor
{
    public abstract class UdonLibraryEditor : UnityEditor.Editor
    {
        protected abstract string GetSymbolName();

        public override void OnInspectorGUI()
        {
            string symbolName = GetSymbolName();
            Debug.Assert(!string.IsNullOrEmpty(symbolName), "GetSymbolName provided valid string");

            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            {
                return;
            }

            var source = target as UdonSharpBehaviour;
            if (!source)
            {
                throw new InvalidOperationException($"{nameof(target)} is no {nameof(UdonSharpBehaviour)}");
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(
                    new GUIContent(
                        $"Set {symbolName} references in scene",
                        $"Links all UdonBehaviours with '{symbolName}' parameter to this object"
                    )
                ))
            {
                var setOfUdonSharpBehaviours = GetAllUdonSharpBehaviours();

                foreach (var behaviour in setOfUdonSharpBehaviours)
                {
                    try
                    {
                        UpdateSymbolOnUdonSharpBehaviour(behaviour, symbolName, source);
                    }
                    catch (Exception e)
                    {
                        // ignored, fails for behaviours that don't have a symbol with that name
                        Debug.LogException(e);
                    }
                }
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private static void UpdateSymbolOnUdonSharpBehaviour(
            UdonSharpBehaviour behaviour,
            string symbolName,
            UdonSharpBehaviour source
        )
        {
            Undo.RecordObject(behaviour, $"Set all '{symbolName}' references");
            if (PrefabUtility.IsPartOfPrefabInstance(behaviour))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }

            var fieldInfo = behaviour.GetType().GetField(
                symbolName,
                BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (fieldInfo == null)
            {
#if DEBUG_UDON_LIBRARY_EDITOR
                Debug.Log($"{behaviour.GetType()} has no public field called '{symbolName}'");
#endif
                return;
            }

            fieldInfo.SetValue(behaviour, null);
            fieldInfo.SetValue(behaviour, source);
            Debug.Log(
                $"Set '{symbolName}' reference on {behaviour.gameObject.name}.{behaviour.GetUdonTypeName()}",
                behaviour
            );
        }

        private static HashSet<UdonSharpBehaviour> GetAllUdonSharpBehaviours()
        {
            var setOfUdonSharpBehaviours = new HashSet<UdonSharpBehaviour>();
            foreach (var transform in FindObjectsOfType<Transform>())
            {
                foreach (var udonSharpBehaviour in transform.gameObject.GetComponents<UdonSharpBehaviour>())
                {
                    if (!setOfUdonSharpBehaviours.Contains(udonSharpBehaviour))
                    {
                        setOfUdonSharpBehaviours.Add(udonSharpBehaviour);
                    }
                }
            }

            return setOfUdonSharpBehaviours;
        }
    }
}
#endif