#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace TLP.UdonUtils.Editor
{
    /// <summary>
    /// Adds the given define symbols to PlayerSettings define symbols.
    ///
    /// Original available under MIT License @
    /// https://github.com/UnityCommunity/UnityLibrary/blob/ac3ae833ee4b1636c521ca01b7e2d0c452fe37e7/Assets/Scripts/Editor/AddDefineSymbols.cs
    /// </summary>
    [InitializeOnLoad]
    public class CompileSymbols : UnityEditor.Editor
    {
        private const string EditorPreferencesEnableDebugLogging = "Tools/TLP.UdonUtils.Editor.enableDebugLogging";
        private const string EditorPreferencesEnableUnitTesting = "Tools/TLP.UdonUtils.Editor.enableUnitTesting";

        private const string DebugCompileSymbol = "TLP_DEBUG";
        private const string UnitTesting = "TLP_UNIT_TESTING";

        /// <summary>
        /// Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        static CompileSymbols()
        {
            UpdateSymbols();
        }

        private static void UpdateSymbols()
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );
            var allDefines = definesString.Split(';').ToList();

            UpdateDefinition(EditorPreferencesEnableDebugLogging, DebugCompileSymbol, allDefines);
            UpdateDefinition(EditorPreferencesEnableUnitTesting, UnitTesting, allDefines);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray())
            );
        }

        private static void UpdateDefinition(string settingsKey, string definition, List<string> allDefinitions)
        {
            var result = new List<string>();
            if (EditorPrefs.GetBool(settingsKey, false))
            {
                if (!allDefinitions.Contains(definition))
                {
                    result.Add(definition);
                }
            }
            else
            {
                if (allDefinitions.Contains(definition))
                {
                    allDefinitions.Remove(definition);
                }
            }

            allDefinitions.AddRange(result.Except(allDefinitions));
            var uniqueEntries = new HashSet<string>(allDefinitions);
            allDefinitions = uniqueEntries.ToList();
            allDefinitions.Sort();
        }

        #region Unit Testing Editor Menu

        #region Enable

        [MenuItem("Tools/TLP/UdonUtils/Unit Testing/Enable", true)]
        private static bool UnitTestingEnableValidation()
        {
            return !EditorPrefs.GetBool(EditorPreferencesEnableUnitTesting, false);
        }

        [MenuItem("Tools/TLP/UdonUtils/Unit Testing/Enable", false, 3)]
        public static void UnitTestingEnable()
        {
            EditorPrefs.SetBool(EditorPreferencesEnableUnitTesting, true);
            InteractiveRefresh(true, $"Unit testing definition '{UnitTesting}'");
        }

        #endregion

        #region Disable

        [MenuItem("Tools/TLP/UdonUtils/Unit Testing/Disable", true)]
        private static bool UnitTestingDisableValidation()
        {
            return EditorPrefs.GetBool(EditorPreferencesEnableUnitTesting, false);
        }

        [MenuItem("Tools/TLP/UdonUtils/Unit Testing/Disable", false, 1)]
        public static void UnitTestingDisable()
        {
            if (EditorPrefs.HasKey(EditorPreferencesEnableUnitTesting))
            {
                EditorPrefs.DeleteKey(EditorPreferencesEnableUnitTesting);
            }

            InteractiveRefresh(false, $"Unit testing definition '{UnitTesting}'");
        }

        #endregion

        #endregion

        #region Debug Logging Editor Menu

        #region Enable

        [MenuItem("Tools/TLP/UdonUtils/Log Assertion Errors/Enable", true)]
        private static bool LogAssertionErrorsEnableValidation()
        {
            return !EditorPrefs.GetBool(EditorPreferencesEnableDebugLogging, false);
        }


        [MenuItem("Tools/TLP/UdonUtils/Log Assertion Errors/Enable", false, 2)]
        public static void LogAssertionErrorsEnable()
        {
            EditorPrefs.SetBool(EditorPreferencesEnableDebugLogging, true);
            InteractiveRefresh(true, $"Debug definition '{DebugCompileSymbol}'");
        }

        #endregion

        #region Disable

        [MenuItem("Tools/TLP/UdonUtils/Log Assertion Errors/Disable", true)]
        private static bool LogAssertionErrorsDisableValidation()
        {
            return EditorPrefs.GetBool(EditorPreferencesEnableDebugLogging, false);
        }


        [MenuItem("Tools/TLP/UdonUtils/Log Assertion Errors/Disable", false, 1)]
        public static void LogAssertionErrorsDisable()
        {
            if (EditorPrefs.HasKey(EditorPreferencesEnableDebugLogging))
            {
                EditorPrefs.DeleteKey(EditorPreferencesEnableDebugLogging);
            }

            InteractiveRefresh(false, $"Debug definition '{DebugCompileSymbol}'");
        }

        #endregion

        #endregion

        private static void InteractiveRefresh(bool enabled, string type)
        {
            UpdateSymbols();
            string newState = enabled ? "enabled" : "disabled";
            EditorUtility.DisplayDialog("Info", $"{type} {newState}.\nScripts will be recompiled now.", "Ok");
            AssetDatabase.Refresh();
        }
    }
}
#endif