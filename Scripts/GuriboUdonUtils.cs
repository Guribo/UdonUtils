using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using VRC.Udon;

namespace Guribo.Scripts
{
    public class GuriboUdonUtils : MonoBehaviour
    {
        private static bool _interactiveMode = true;
#if UNITY_EDITOR
        public class AutoValidator : UnityEditor.AssetModificationProcessor
        {
            static string[] OnWillSaveAssets(string[] paths)
            {
                // disable interactive mode
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

        /// <summary>
        /// checks all UdonBehaviours in the scene for unset public variables. Displays a dialog to skip or show the error.
        /// </summary>
        [MenuItem("Guribo/UDON/Validate UdonBehaviour References")]
        public static void ValidateUdonBehaviours()
        {


            int errorCount = 0;
            var udonBehaviours = FindObjectsOfType<UdonBehaviour>();
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
                var udonBehaviourProgramSource = udonBehaviour.programSource;
                if (udonBehaviourProgramSource == null)
                {
                    Debug.LogWarning("UdonBehaviour on " + udonBehaviour.gameObject.name +
                                     " has no Udon program attached", udonBehaviour);
                    if (_interactiveMode && EditorUtility.DisplayDialog("Empty UdonBehaviour found",
                        "The UdonBehaviour on the GameObject '" +
                        udonBehaviour.gameObject.name + "' has no program attached", "Show me", "Skip"))
                    {
                        Selection.SetActiveObjectWithContext(udonBehaviour.gameObject, udonBehaviour);
                        EditorGUIUtility.PingObject(udonBehaviour.gameObject);
                        return;
                    }

                    errorCount++;
                    continue;
                }

                var udonBehaviourPublicVariables = udonBehaviour.publicVariables;
                if (udonBehaviourPublicVariables == null) continue;

                var variableSymbols = udonBehaviourPublicVariables.VariableSymbols;
                if (variableSymbols == null) continue;

                foreach (var symbols in variableSymbols)
                {
                    if (!udonBehaviourPublicVariables.TryGetVariableValue(symbols, out var variableValue) ||
                        variableValue == null)
                    {
                        Debug.LogWarning(symbols + " is not set", udonBehaviour);
                        if (_interactiveMode && EditorUtility.DisplayDialog("Empty public variable found",
                            "A public variable called '" + symbols +
                            "' is not set on the UdonBehaviour with the program '" +
                            udonBehaviourProgramSource.name + "'. You may want to fix this.", "Show me", "Skip"))
                        {
                            Selection.SetActiveObjectWithContext(udonBehaviour.gameObject, udonBehaviour);
                            EditorGUIUtility.PingObject(udonBehaviour.gameObject);
                            return;
                        }

                        errorCount++;
                    }
                }
            }


            var conclusion = errorCount + " potential error" + (errorCount > 1 ? "s" : "") + " found in " +
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
#endif
    }
}