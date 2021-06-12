#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer.Utilities;
using Object = UnityEngine.Object;

namespace Guribo.UdonUtils.Scripts
{
    /// <summary>
    /// contains functions to access and modify UdonBehaviour variables in Edit mode
    /// </summary>
    public static class UdonBehaviourExtensions
    {
        #region public

        #region GetVariable

        /// <summary>
        ///     read the default value of a variable that is visible in the UdonBehaviour inspector
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName">name of the variable visible in the inspector on the UdonBehaviour</param>
        /// <param name="variableType">type of the value, can potentially be null!</param>
        /// <returns>the default value</returns>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        /// <exception cref="Exception">e.g. if the symbolName doesn't exist</exception>
        public static object GetInspectorVariableDefaultValue(this UdonBehaviour udonBehaviour,
            string symbolName,
            out Type variableType)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            // if (Application.IsPlaying(udonBehaviour))
            // {
            //     throw new Exception("Only edit mode is supported");
            // }

            var programAsset = (UdonSharpProgramAsset) udonBehaviour.programSource;
            if (!programAsset)
            {
                throw new Exception("UdonBehaviour has no UdonSharpProgramAsset");
            }

            programAsset.UpdateProgram();

            var publicVariables = udonBehaviour.publicVariables;
            if (publicVariables == null)
            {
                throw new Exception("UdonBehaviour has no public Variables");
            }

            GetVariableType(udonBehaviour, symbolName, out variableType, publicVariables, programAsset);

            var defaultValue = programAsset.GetPublicVariableDefaultValue(symbolName);
            if (variableType == null)
            {
                variableType = defaultValue?.GetType();
            }

            return defaultValue;
        }

        /// <summary>
        ///     read the value of a variable that is visible in the UdonBehaviour inspector
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName">name of the variable visible in the inspector on the UdonBehaviour</param>
        /// <param name="variableValue">the current value or the default value if it has not been changed</param>
        /// <param name="variableType">type of the value, can potentially be null!</param>
        /// <returns>True if the value was read and it is not the default value, false if it is the default value</returns>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        /// <exception cref="Exception">e.g. if the symbolName doesn't exist</exception>
        public static bool GetInspectorVariable(this UdonBehaviour udonBehaviour, string symbolName,
            out object variableValue, out Type variableType)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            // if (Application.IsPlaying(udonBehaviour))
            // {
            //     throw new Exception("Only edit mode is supported");
            // }

            var programAsset = (UdonSharpProgramAsset) udonBehaviour.programSource;
            if (!programAsset)
            {
                throw new Exception("UdonBehaviour has no UdonSharpProgramAsset");
            }

            programAsset.UpdateProgram();

            var publicVariables = udonBehaviour.publicVariables;
            if (publicVariables == null)
            {
                throw new Exception("UdonBehaviour has no public Variables");
            }

            variableValue = null;
            var defaultValue = programAsset.GetPublicVariableDefaultValue(symbolName);

            GetVariableType(udonBehaviour, symbolName, out variableType, publicVariables, programAsset);

            try
            {
                if (publicVariables.TryGetVariableValue(symbolName, out variableValue))
                {
                    return defaultValue != variableValue;
                }

                variableValue = defaultValue;
                return false;
            }
            finally
            {
                if (variableType == null)
                {
                    variableType = variableValue?.GetType();
                }
            }
        }

        #endregion

        #region SetVariable

        /// <summary>
        ///     modified version of Merlin's code for drawing UdonBehaviour inspectors used to set variables from any other
        ///     C# script while in Edit mode
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName">name of the variable visible in the inspector on the UdonBehaviour</param>
        /// <param name="newValue"></param>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        /// <exception cref="Exception">e.g. if the symbolName doesn't exist</exception>
        public static void SetInspectorVariable(this UdonBehaviour udonBehaviour, string symbolName, object newValue)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            // if (Application.IsPlaying(udonBehaviour))
            // {
            //     throw new Exception("Only edit mode is supported");
            // }

            var publicVariables = udonBehaviour.publicVariables;
            if (publicVariables == null)
            {
                throw new Exception("UdonBehaviour has no public Variables");
            }

            Undo.RecordObject(udonBehaviour, "Modify variable");
            if (!publicVariables.TrySetVariableValue(symbolName, newValue))
            {
                var symbolTable = GetSymbolTable(udonBehaviour);
                var symbolType = symbolTable.GetSymbolType(symbolName);
                if (!publicVariables.TryAddVariable(CreateUdonVariable(symbolName, newValue,
                    symbolType)))
                {
                    throw new Exception($"Failed to set public variable '{symbolName}' value");
                }
            }

            var foundValue = publicVariables.TryGetVariableValue(symbolName, out var variableValue);
            var foundType = publicVariables.TryGetVariableType(symbolName, out var variableType);

            // copied from Merlin's code, see UdonSharpProgramAsset.cs for reference
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
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(udonBehaviour);
            }
        }

        /// <summary>
        ///     resets a value to the prefab or original script value
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName">name of the variable visible in the inspector on the UdonBehaviour</param>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        /// <exception cref="Exception">e.g. if the symbolName doesn't exist</exception>
        public static void ResetInspectorVariable(this UdonBehaviour udonBehaviour, string symbolName)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            // if (Application.IsPlaying(udonBehaviour))
            // {
            //     throw new Exception("Only edit mode is supported");
            // }

            var programAsset = (UdonSharpProgramAsset) udonBehaviour.programSource;
            if (!programAsset)
            {
                throw new Exception("UdonBehaviour has no UdonSharpProgramAsset");
            }

            programAsset.UpdateProgram();

            udonBehaviour.SetInspectorVariable(symbolName, programAsset.GetPublicVariableDefaultValue(symbolName));
        }

        #endregion


        /// <summary>
        ///     returns a List if names of the variables visible in the inspector on the UdonBehaviour
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        public static List<string> GetInspectorVariableNames(this UdonBehaviour udonBehaviour)
        {
            if (!udonBehaviour)
            {
                throw new ArgumentException("Invalid UdonBehaviour");
            }

            try
            {
                return new List<string>(udonBehaviour.GetSymbolTable().GetExportedSymbols());
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + " (skipping)", udonBehaviour);
                return new List<string>();
            }
        }

        /// <summary>
        ///     checks whether the current value of a variable is null (not nullable types return false)
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <param name="symbolName"></param>
        /// <param name="variableType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">if udonBehaviour is invalid</exception>
        public static bool IsInspectorVariableNull(this UdonBehaviour udonBehaviour,
            string symbolName,
            out Type variableType)
        {
            udonBehaviour.GetInspectorVariable(symbolName, out var variableValue, out variableType);
            return variableType.IsNullable() && variableValue.IsUnityObjectNull();
        }

        #endregion

        #region private

        private static void GetVariableType(UdonBehaviour udonBehaviour, string symbolName, out Type variableType,
            IUdonVariableTable publicVariables, UdonSharpProgramAsset programAsset)
        {
            if (!publicVariables.TryGetVariableType(symbolName, out variableType))
            {
                var symbolTable = udonBehaviour.GetSymbolTable();
                if (symbolTable.HasAddressForSymbol(symbolName))
                {
                    var symbolAddress = symbolTable.GetAddressFromSymbol(symbolName);
                    var program = programAsset.GetRealProgram();
                    variableType = program.Heap.GetHeapVariableType(symbolAddress);
                }
                else
                {
                    variableType = null;
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="udonBehaviour"></param>
        /// <returns></returns>
        /// <exception cref="Exception">if the UdonBehaviour has no public variables</exception>
        private static IUdonSymbolTable GetSymbolTable(this UdonBehaviour udonBehaviour)
        {
            if (!udonBehaviour || !(udonBehaviour.programSource is UdonSharpProgramAsset))
            {
                throw new Exception("ProgramSource is not an UdonSharpProgramAsset");
            }

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


        private static IUdonVariable CreateUdonVariable(string symbolName, object value, Type type)
        {
            var udonVariableType = typeof(UdonVariable<>).MakeGenericType(type);
            return (IUdonVariable) Activator.CreateInstance(udonVariableType, symbolName, value);
        }

        private static bool IsUnityObjectNull(this object value)
        {
            return value == null || value is Object unityEngineObject && unityEngineObject == null;
        }

        private static bool IsNullable(this Type type)
        {
            return type.IsNullableType();
        }

        #endregion
    }
}

#endif