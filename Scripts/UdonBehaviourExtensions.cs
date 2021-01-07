#if UNITY_EDITOR
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace Guribo.UdonUtils.Scripts
{
    public static class UdonBehaviourExtensions
    {
        /// <summary>
        /// modified version of Merlin's code for drawing UdonBehaviour inspectors used to set variables from any other
        /// C# script while in Editmode 
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName"></param>
        /// <param name="newValue"></param>
        public static void SetInspectorVariable(this UdonBehaviour udonBehaviour, string symbolName, object newValue)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            if (Application.IsPlaying(udonBehaviour))
            {
                throw new Exception("Only edit mode is supported");
            }

            var symbolTable = GetSymbolTable(udonBehaviour);
            var publicVariables = udonBehaviour.publicVariables;
            if (publicVariables == null)
            {
                throw new Exception("UdonBehaviour has no public Variables");
            }

            Undo.RecordObject(udonBehaviour, "Modify variable");
            if (!publicVariables.TrySetVariableValue(symbolName, newValue))
            {
                var symbolType = symbolTable.GetSymbolType(symbolName);
                if (!publicVariables.TryAddVariable(CreateUdonVariable(symbolName, newValue,
                    symbolType)))
                {
                    throw new Exception($"Failed to set public variable '{symbolName}' value");
                }
            }

            var foundValue = publicVariables.TryGetVariableValue(symbolName, out var variableValue);
            var foundType = publicVariables.TryGetVariableType(symbolName, out var variableType);

            // Remove this variable from the publicVariable list since UdonBehaviours set all null GameObjects, UdonBehaviours, and Transforms to the current behavior's equivalent object regardless of if it's marked as a `null` heap variable or `this`
            // This default behavior is not the same as Unity, where the references are just left null. And more importantly, it assumes that the user has interacted with the inspector on that object at some point which cannot be guaranteed. 
            // Specifically, if the user adds some public variable to a class, and multiple objects in the scene reference the program asset, 
            //   the user will need to go through each of the objects' inspectors to make sure each UdonBehavior has its `publicVariables` variable populated by the inspector
            if (foundValue
                && foundType
                && variableValue.IsUnityObjectNull()
                && (variableType == typeof(GameObject)
                    || variableType == typeof(UdonBehaviour)
                    || variableType == typeof(Transform)))
            {
                udonBehaviour.publicVariables.RemoveVariable(symbolName);
            }

            GUI.changed = true;

            if (PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
                PrefabUtility.RecordPrefabInstancePropertyModifications(udonBehaviour);
        }

        public static void ResetInspectorVariable(this UdonBehaviour udonBehaviour, string symbolName)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            if (Application.IsPlaying(udonBehaviour))
            {
                throw new Exception("Only edit mode is supported");
            }

            var programAsset = (UdonSharpProgramAsset) udonBehaviour.programSource;
            if (!programAsset)
            {
                throw new Exception("UdonBehaviour has no UdonSharpProgramAsset");
            }

            udonBehaviour.SetInspectorVariable(symbolName, programAsset.GetPublicVariableDefaultValue(symbolName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <returns></returns>
        /// <exception cref="Exception">if the UdonBehaviour has no public variables</exception>
        private static IUdonSymbolTable GetSymbolTable(this UdonBehaviour udonBehaviour)
        {
            var programAsset = (UdonSharpProgramAsset) udonBehaviour.programSource;
            if (!programAsset)
            {
                throw new Exception("UdonBehaviour has no UdonSharpProgramAsset");
            }

            programAsset.UpdateProgram();
            var program = programAsset.GetRealProgram();
            if (program?.SymbolTable == null)
            {
                throw new Exception("UdonBehaviour has no public variables");
            }

            return program.SymbolTable;
        }

        public static string[] GetExportedSymbolNames(this UdonBehaviour udonBehaviour)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            try
            {
                return udonBehaviour.GetSymbolTable().GetExportedSymbols();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message, udonBehaviour);
                return new string[0];
            }
        }

        private static IUdonVariable CreateUdonVariable(string symbolName, object value, Type type)
        {
            Type udonVariableType = typeof(UdonVariable<>).MakeGenericType(type);
            return (IUdonVariable) Activator.CreateInstance(udonVariableType, symbolName, value);
        }

        private static bool IsUnityObjectNull(this object value)
        {
            return value == null || value is UnityEngine.Object unityEngineObject && unityEngineObject == null;
        }
    }
}

#endif