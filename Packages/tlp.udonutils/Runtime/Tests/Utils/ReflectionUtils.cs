#if UNITY_EDITOR
using System;
using HarmonyLib;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Tests.Utils
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Replaces/Patches a method of a given class.
        /// </summary>
        /// <param name="targetClass">Class which shall have a method replaced/patched</param>
        /// <param name="targetMethod">Name of the method to replace/patch</param>
        /// <param name="injectedClass">Class which has the replacement method</param>
        /// <param name="injectMethod">static method that can run before/instead of the original method. Must return a boolean (true will run it before the original code, false will run it instead of the original code)</param>
        /// <param name="actionUsingPatch">Code that shall run with the patched method. Afterwards the patch is removed again.</param>
        /// <exception cref="Exception">should patching or anything else fail</exception>
        public static void PatchMethod(
                Type targetClass,
                string targetMethod,
                Type injectedClass,
                string injectMethod,
                Action<Harmony> actionUsingPatch,
                Harmony harmony = null
        ) {
            const string harmonyId = "TLP.UdonUtils.Tests.Editor";
            var activeHarmony = harmony ?? new Harmony(harmonyId);

            var origInfo = AccessTools.Method(targetClass, targetMethod);
            bool success = false;
            try {
                activeHarmony.Patch(
                        origInfo,
                        new HarmonyMethod(injectedClass, injectMethod)
                );
                success = true;
                actionUsingPatch.Invoke(activeHarmony);
            }
            finally {
                if (success) {
                    activeHarmony.Unpatch(origInfo, HarmonyPatchType.Prefix, harmonyId);
                }
            }
        }

        /// <summary>
        /// Replaces/Patches a method of a given class.
        /// </summary>
        /// <param name="targetClass">Class which shall have a method replaced/patched</param>
        /// <param name="targetMethod">Name of the method to replace/patch</param>
        /// <param name="injectedClass">Class which has the replacement method</param>
        /// <param name="injectMethod">static method that can run before/instead of the original method. Must return a boolean (true will run it before the original code, false will run it instead of the original code)</param>
        /// <param name="actionUsingPatch">Code that shall run with the patched method. Afterwards the patch is removed again.</param>
        /// <exception cref="Exception">should patching or anything else fail</exception>
        public static Harmony PatchMethodWithoutCleanup(
                Type targetClass,
                string targetMethod,
                Type injectedClass,
                string injectMethod,
                Harmony harmony = null
        ) {
            const string harmonyId = "TLP.UdonUtils.Tests.Editor";
            var activeHarmony = harmony ?? new Harmony(harmonyId);

            var origInfo = AccessTools.Method(targetClass, targetMethod);
            activeHarmony.Patch(
                    origInfo,
                    new HarmonyMethod(injectedClass, injectMethod)
            );
            Debug.Log($"Patched {targetMethod} with {injectMethod}");

            return activeHarmony;
        }
    }
}
#endif