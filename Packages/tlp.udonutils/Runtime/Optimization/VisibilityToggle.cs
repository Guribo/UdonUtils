using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Rendering;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
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
        protected override string _GetDescription() {
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

        /// <summary>
        /// True when the renderer on this gameobject is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        private void OnBecameVisible() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnBecameVisible));
#endif
            #endregion

            IsVisible = true;

            int count = GameObjects._LengthSafe();
            for (int i = 0; i < count; ++i) {
                var go = GameObjects[i];
                if (!Utilities.IsValid(go)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Enabling GameObject {go.transform._GetPathInScene()}");
#endif
                #endregion

                go.SetActive(true);
            }

            count = Behaviours._LengthSafe();
            for (int i = 0; i < count; ++i) {
                var behaviour = Behaviours[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Enabling behaviour {behaviour._GetComponentPathInScene()}");
#endif
                #endregion

                behaviour.enabled = true;
            }

            count = UdonSharpBehaviour._LengthSafe();
            for (int i = 0; i < count; ++i) {
                var behaviour = UdonSharpBehaviour[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Enabling U# behaviour {behaviour._GetScriptPathInScene()}");
#endif
                #endregion

                behaviour.enabled = true;
            }
        }

        private void OnBecameInvisible() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnBecameInvisible));
#endif
            #endregion

            IsVisible = false;

            int count = UdonSharpBehaviour._LengthSafe();
            for (int i = count - 1; i > -1; --i) {
                var behaviour = UdonSharpBehaviour[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Disabling U# behaviour {behaviour._GetScriptPathInScene()}");
#endif
                #endregion

                behaviour.enabled = false;
            }

            count = Behaviours._LengthSafe();
            for (int i = count - 1; i > -1; --i) {
                var behaviour = Behaviours[i];
                if (!Utilities.IsValid(behaviour)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Disabling behaviour {behaviour._GetComponentPathInScene()}");
#endif
                #endregion

                behaviour.enabled = false;
            }

            count = GameObjects._LengthSafe();
            for (int i = count - 1; i > -1; --i) {
                var go = GameObjects[i];
                if (!Utilities.IsValid(go)) {
                    continue;
                }

                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Disabling GameObject {go.transform._GetPathInScene()}");
#endif
                #endregion

                go.SetActive(false);
            }
        }

        /// <summary>
        /// Adds the behaviour if it is valid. Does NOT check for duplicates.
        /// Does NOT change the scripts enabled state. Use <see cref="IsVisible"/> to control the enabled state.
        /// </summary>
        /// <param name="udonSharpBehaviour"></param>
        public void _AddUdonSharpBehaviour(UdonSharpBehaviour udonSharpBehaviour) {
            if (!Utilities.IsValid(udonSharpBehaviour)) {
                return;
            }

            UdonSharpBehaviour = UdonSharpBehaviour._Append(udonSharpBehaviour);
        }

        /// <summary>
        /// Tries to removed the first entry of the behaviour if it is valid/exists. Does NOT remove duplicates.
        /// Does NOT change the scripts enabled state. Use <see cref="IsVisible"/> to control the enabled state.
        /// </summary>
        /// <param name="udonSharpBehaviour"></param>
        public void _RemoveUdonSharpBehaviour(UdonSharpBehaviour udonSharpBehaviour) {
            if (!Utilities.IsValid(udonSharpBehaviour)) {
                return;
            }

            UdonSharpBehaviour = UdonSharpBehaviour._Delete(udonSharpBehaviour);
        }
    }
}