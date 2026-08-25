using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
#if UNITY_EDITOR
using TLP.UdonUtils.Editor.Core;
using TLP.UdonUtils.Runtime.Player;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Player
{
    [CustomEditor(typeof(PlayerCameraTrackerUi))]
    public class PlayerCameraTrackerUiEditor : TlpBehaviourEditor
    {
        protected override string _GetDescription() {
            return "Tracks the player camera or handheld camera and drones.";
        }
    }
}
#endif
namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerCameraTrackerUi), ExecutionOrder)]
    public class PlayerCameraTrackerUi : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = LookAtPlayerUI.ExecutionOrder + 1;
        #endregion

        #region Configuration
        [Header("Configuration")]
        [Tooltip("If false: automatically switches to Handheld cameras and drones")]
        public bool RestrictToMainCamera;

        [Tooltip("If false: camera rotation is used. If true: player root rotation is used instead in VR " +
                 "(only applies to ScreenCamera, not PhotoCamera/Drone).")]
        public bool UsePlayerBaseRotationInVr;
        #endregion

        #region State
        private Transform _ownTransform;
        #endregion

        #region Overrides
        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            _ownTransform = transform;
            return true;
        }

        public override void PostLateUpdate() {
            if (!HasStartedOk || !isActiveAndEnabled) {
                return;
            }

            VrcCameraSettingUtils.MoveToActivePlayerCamera(_ownTransform,
                    LocalPlayer,
                    RestrictToMainCamera,
                    UsePlayerBaseRotationInVr);
        }
        #endregion
    }
}