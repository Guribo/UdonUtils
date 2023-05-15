#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace TLP.UdonUtils.Editor
{
    public class UdonUtils : MonoBehaviour
    {
        private static bool _interactiveMode = true;

        public class AutoValidator : UnityEditor.AssetModificationProcessor
        {
            internal static readonly string EditorPreferencesValidateOnSave =
                "TLP.UdonUtils.Editor.UdonUtils.AutoValidator.validateOnSave";

            private static string[] OnWillSaveAssets(string[] paths)
            {
                if (!EditorPrefs.GetBool(EditorPreferencesValidateOnSave, false))
                {
                    return paths;
                }

                // temporarily disable interactive mode
                _interactiveMode = false;
                try
                {
                    ValidateUdonBehaviours();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    _interactiveMode = true;
                }

                return paths;
            }
        }

        [MenuItem(
            "Tools/TLP/UdonUtils/Auto. check for unset UdonSharpBehaviour variables on save (Console only)/Enable",
            true
        )]
        private static bool CheckPublicVariablesOnSaveValidation()
        {
            return !EditorPrefs.GetBool(AutoValidator.EditorPreferencesValidateOnSave, false);
        }

        [MenuItem(
            "Tools/TLP/UdonUtils/Auto. check for unset UdonSharpBehaviour variables on save (Console only)/Enable",
            false,
            2
        )]
        public static void CheckPublicVariablesOnSave()
        {
            EditorPrefs.SetBool(AutoValidator.EditorPreferencesValidateOnSave, true);
        }

        [MenuItem(
            "Tools/TLP/UdonUtils/Auto. check for unset UdonSharpBehaviour variables on save (Console only)/Disable",
            true
        )]
        private static bool DontCheckPublicVariablesOnSaveValidation()
        {
            return EditorPrefs.GetBool(AutoValidator.EditorPreferencesValidateOnSave, false);
        }

        [MenuItem(
            "Tools/TLP/UdonUtils/Auto. check for unset UdonSharpBehaviour variables on save (Console only)/Disable",
            false,
            1
        )]
        public static void DontCheckPublicVariablesOnSave()
        {
            if (EditorPrefs.HasKey(AutoValidator.EditorPreferencesValidateOnSave))
            {
                EditorPrefs.DeleteKey(AutoValidator.EditorPreferencesValidateOnSave);
            }
        }


        [MenuItem("Tools/TLP/UdonUtils/Check selection for all unset UdonSharpBehaviour variables (interactive)")]
        public static void ValidateSelectionInteractive()
        {
            _interactiveMode = true;

            CheckSelection();
        }

        [MenuItem("Tools/TLP/UdonUtils/Check selection for all unset UdonSharpBehaviour variables (console only)")]
        public static void ValidateSelectionConsoleOnly()
        {
            _interactiveMode = false;

            CheckSelection();
        }

        private static void CheckSelection()
        {
            var udonBehaviours = new HashSet<UdonBehaviour>();
            foreach (var o in Selection.gameObjects)
            {
                var componentsInChildren = o.GetComponentsInChildren<UdonBehaviour>(true);
                foreach (var componentsInChild in componentsInChildren)
                {
                    if (!udonBehaviours.Contains(componentsInChild))
                    {
                        udonBehaviours.Add(componentsInChild);
                    }
                }
            }

            CheckBehaviours(udonBehaviours.ToArray());
        }

        /// <summary>
        ///     checks all UdonBehaviours in the scene for unset public variables. Displays a dialog to skip or show the error.
        /// </summary>
        [MenuItem("Tools/TLP/UdonUtils/Check for all unset UdonSharpBehaviour variables now")]
        public static void ValidateUdonBehaviours()
        {
            var udonBehaviours = FindObjectsOfType<UdonBehaviour>();

            CheckBehaviours(udonBehaviours);
        }

        private static void CheckBehaviours(UdonBehaviour[] udonBehaviours)
        {
            int errorCount = 0;
            if (udonBehaviours.Length == 0)
            {
                if (_interactiveMode)
                {
                    EditorUtility.DisplayDialog("Conclusion", "No UdonBehaviours in the scene", "Ok");
                }

                return;
            }

            foreach (var udonBehaviour in udonBehaviours)
            {
                var programSource = udonBehaviour.programSource;
                if (programSource == null)
                {
                    Debug.LogWarning(
                        "UdonBehaviour on " + udonBehaviour.gameObject.name +
                        " has no Udon program attached",
                        udonBehaviour
                    );
                    if (_interactiveMode && EditorUtility.DisplayDialog(
                            "Empty UdonBehaviour found",
                            "The UdonBehaviour on the GameObject '" +
                            udonBehaviour.gameObject.name + "' has no program attached",
                            "Show me",
                            "Skip"
                        ))
                    {
                        Selection.SetActiveObjectWithContext(udonBehaviour.gameObject, udonBehaviour);
                        EditorGUIUtility.PingObject(udonBehaviour.gameObject);
                        return;
                    }

                    errorCount++;
                    continue;
                }

                var symbolNames = udonBehaviour.GetInspectorVariableNames();
                foreach (string symbolName in symbolNames)
                {
                    if (!_interactiveMode && symbolName.StartsWith("optional", true, CultureInfo.CurrentCulture))
                    {
                        continue;
                    }

                    if (udonBehaviour.IsInspectorVariableNull(symbolName, out var variableType))
                    {
                        Debug.LogWarning($"{udonBehaviour}.{symbolName} is null [{variableType}]", udonBehaviour);

                        if (_interactiveMode && EditorUtility.DisplayDialog(
                                "Empty public variable found",
                                "A public variable called '" + symbolName +
                                "' is not set on the UdonBehaviour with the program '" +
                                programSource.name + "'. You may want to fix this.",
                                "Show me",
                                "Skip"
                            ))
                        {
                            Selection.SetActiveObjectWithContext(udonBehaviour.gameObject, udonBehaviour);
                            EditorGUIUtility.PingObject(udonBehaviour.gameObject);
                            return;
                        }

                        errorCount++;
                    }
                }
            }


            string conclusion = errorCount + " potential error" + (errorCount > 1 ? "s" : "") + " found in " +
                                udonBehaviours.Length +
                                " UdonBehaviours." +
                                (errorCount > 0
                                    ? " You may want to fix " + (errorCount > 1 ? "those" : "that") + "."
                                    : "");
            if (errorCount > 0)
            {
                Debug.LogWarning(conclusion);
            }
            else
            {
                Debug.Log(conclusion);
            }

            if (_interactiveMode)
            {
                EditorUtility.DisplayDialog("Conclusion", conclusion, "Ok");
            }
        }
    }
}
#endif