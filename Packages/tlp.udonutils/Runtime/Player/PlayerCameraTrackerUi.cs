using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Runtime.Player;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Player
{
    [CustomEditor(typeof(PlayerCameraTrackerUi))]
    public class PlayerCameraTrackerUiEditor : UnityEditor.Editor
    {
        private const string Description = "Tracks the player camera or handheld camera and drones";

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif
namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TrackingDataFollower), ExecutionOrder)]
    public class PlayerCameraTrackerUi : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TrackingDataFollowerUI.ExecutionOrder + 1;
        #endregion


        [Tooltip("If false allowing to switch to Handheld cameras and drones")]
        public bool RestrictToMainCamera;

        [Tooltip(
                "If false the camera rotation is used, if true the player root rotation is used instead in VR " +
                "(only applies to ScreenCamera, not PhotoCamera/Drone).")]
        public bool UsePlayerBaseRotationInVr;

        #region State
        private Transform _ownTransform;
        #endregion

        #region Lifecycle
        public override void PostLateUpdate() {
            if (!HasStartedOk || !enabled) {
                return;
            }

            if (RestrictToMainCamera) {
                MoveToScreenCamera();
                return;
            }

            var photoCam = VRCCameraSettings.PhotoCamera;
            if (Utilities.IsValid(photoCam) && photoCam.Active) {
                _ownTransform.SetPositionAndRotation(photoCam.Position, photoCam.Rotation);
                return;
            }

            MoveToScreenCamera();
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            _ownTransform = transform;
            return true;
        }
        #endregion

        #region Internal
        private void MoveToScreenCamera() {
            var screenCam = VRCCameraSettings.ScreenCamera;
            if (!Utilities.IsValid(screenCam)) {
                return;
            }

            _ownTransform.SetPositionAndRotation(screenCam.Position, GetRotation(screenCam));
        }

        private Quaternion GetRotation(VRCCameraSettings cam) {
            if (UsePlayerBaseRotationInVr && LocalPlayer.IsUserInVR()) {
                return LocalPlayer.GetRotation();
            }

            return cam.Rotation;
        }
        #endregion
    }
}