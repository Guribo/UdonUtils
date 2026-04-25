using System;
using System.Collections.Generic;
using System.Linq;
using TLP.UdonUtils.Runtime.Logger;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Editor;

namespace TLP.UdonUtils.Editor
{
    public static class CustomDefinitionUtils
    {
        /// <summary>
        /// Ensures that the given defines exist in the PlayerSettings.
        /// Additionally, enables the given defines for shaders.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="newDefines"></param>
        public static void EnsureDefinitionsExist(Type type, params string[] newDefines) {
            AddDefinesIfMissing(
                    type,
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    newDefines);
            foreach (string newDefine in newDefines) {
                Shader.EnableKeyword(newDefine);
                TlpLogger.StaticInfo(
                        $"{nameof(EnsureDefinitionsExist)}: Enabled shader keyword '{newDefine}'.",
                        type);
            }
        }

        #region Internal
        private static void AddDefinesIfMissing(
                Type type,
                BuildTargetGroup buildGroup,
                params string[] newDefines
        ) {
            bool definesChanged = false;
            string[] defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup).Split(';');
            var defineSet = new HashSet<string>(defines);

            foreach (string newDefine in newDefines) {
                definesChanged |= defineSet.Add(newDefine);
            }

            if (!definesChanged) {
                #region TLP_DEBUG
#if TLP_DEBUG
                TlpLogger.StaticDebugLog(
                        $"{nameof(AddDefinesIfMissing)}: No changes to define symbols, skipping",
                        type);
#endif
                #endregion

                return;
            }

            string finalDefineString = string.Join(";", defineSet.ToArray());
            string newDefinesString = string.Join(";", newDefines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, finalDefineString);
            TlpLogger.StaticInfo(
                    $"{nameof(AddDefinesIfMissing)}: Added '{newDefinesString}' to scripting define " +
                    $"symbols group '{buildGroup}': {finalDefineString}",
                    type);
            AssetDatabase.Refresh();
            TlpLogger.StaticInfo(
                    $"{nameof(AddDefinesIfMissing)}: Re-Compiling all Udon sources...",
                    type);
            UdonEditorManager.RecompileAllProgramSources();
            TlpLogger.StaticInfo($"{nameof(AddDefinesIfMissing)}: Done.", type);
        }
        #endregion
    }
}