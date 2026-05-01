using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Rendering;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Runtime.Optimization;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Core
{
    [CustomEditor(typeof(VisibilityToggle))]
    public class VisibilityToggleEditor : TlpBehaviourEditor
    {
        protected override string GetDescription() {
            return
                    "Disables scripts/Behaviours/GameObjects when the associated renderer is invisible, enables them when visible.\n" +
                    "Disabling occurs in reverse order of their appearance in the inspector.\n" +
                    "Enabling occurs in the same order as they appear in the inspector.\n" +
                    "GameObjects are enabled first, followed by Behaviours, followed by scripts.\n" +
                    "Disabling occurs in order scripts, followed by Behaviours, followed by GameObjects.";
        }
    }
}
#endif
namespace TLP.UdonUtils.Runtime.Optimization
{
    /// <summary>
    /// Disables scripts/Behaviours/GameObjects when the associated renderer is invisible, enables them when visible.
    /// Disabling occurs in reverse order of their appearance in the inspector.
    /// Enabling occurs in the same order as they appear in the inspector.
    /// GameObjects are enabled first, followed by Behaviours, followed by scripts.
    /// Disabling occurs in order scripts, followed by Behaviours, followed by GameObjects.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VisibilityToggle), ExecutionOrder)]
    [RequireComponent(typeof(Renderer))]
    public class VisibilityToggle : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ReflectionProbeController.ExecutionOrder + 1;
        #endregion

        /// <summary>
        /// GameObjects to be toggled based on renderer visibility.
        /// Enabled in order (index 0 first), disabled in reverse order (last index first).
        /// GameObjects are always enabled before scripts and disabled after scripts.
        /// </summary>
        public GameObject[] GameObjects;

        /// <summary>
        /// Unity <see cref="Behaviour"/> components to be toggled based on renderer visibility.
        /// Enabled in order (index 0 first), disabled in reverse order (last index first).
        /// </summary>
        public Behaviour[] Behaviours;

        /// <summary>
        /// UdonSharp behaviours to be toggled based on renderer visibility.
        /// Enabled in order (index 0 first), disabled in reverse order (last index first).
        /// UdonSharp behaviours are always disabled before other behaviours and enabled after them.
        /// </summary>
        public UdonSharpBehaviour[] UdonSharpBehaviour;

        private void OnBecameVisible() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnBecameVisible));
#endif
            #endregion

            int count = GameObjects.LengthSafe();
            for (int i = 0; i < count; ++i) {
                var go = GameObjects[i];
                if (!Utilities.IsValid(go)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Enabling GameObject {go.transform.GetPathInScene()}");
#endif
                #endregion

                go.SetActive(true);
            }

            count = Behaviours.LengthSafe();
            for (int i = 0; i < count; ++i) {
                var behaviour = Behaviours[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Enabling behaviour {behaviour.GetComponentPathInScene()}");
#endif
                #endregion

                behaviour.enabled = true;
            }

            count = UdonSharpBehaviour.LengthSafe();
            for (int i = 0; i < count; ++i) {
                var behaviour = UdonSharpBehaviour[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Enabling U# behaviour {behaviour.GetScriptPathInScene()}");
#endif
                #endregion

                behaviour.enabled = true;
            }
        }

        private void OnBecameInvisible() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnBecameInvisible));
#endif
            #endregion

            int count = UdonSharpBehaviour.LengthSafe();
            for (int i = count -1; i > -1; --i) {
                var behaviour = UdonSharpBehaviour[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Disabling U# behaviour {behaviour.GetScriptPathInScene()}");
#endif
                #endregion

                behaviour.enabled = false;
            }

            count = Behaviours.LengthSafe();
            for (int i = count -1; i > -1; --i) {
                var behaviour = Behaviours[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Disabling behaviour {behaviour.GetComponentPathInScene()}");
#endif
                #endregion

                behaviour.enabled = false;
            }

            count = GameObjects.LengthSafe();
            for (int i = count -1; i > -1; --i) {
                var go = GameObjects[i];
                if (!Utilities.IsValid(go)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Disabling GameObject {go.transform.GetPathInScene()}");
#endif
                #endregion

                go.SetActive(false);
            }
        }
    }
}