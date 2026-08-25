using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// Utils for the <see cref="VRCCameraSettings"/> class.
    /// </summary>
    public static class VrcCameraSettingUtils
    {
        /// <summary>
        /// Tries to move a given transform to the active view camera of the local player.
        /// </summary>
        /// <param name="transformToMove">Transform to move to the active view camera location/rotation.</param>
        /// <param name="localPlayer">Must be valid.</param>
        /// <param name="usePlayerBaseRotationInVr">When true, will ignore the players camera rotation in first person
        ///     mode when in vr. <see cref="VRCPlayerApi.GetRotation"/> will be used instead.</param>
        /// <returns>False if player camera is invalid or not active.</returns>
        public static bool TryMoveToScreenCamera(
                Transform transformToMove,
                VRCPlayerApi localPlayer,
                bool usePlayerBaseRotationInVr = false
        ) {
            var screenCameraSettings = VRCCameraSettings.ScreenCamera;
            if (!Utilities.IsValid(screenCameraSettings) || !screenCameraSettings.Active) {
                return false;
            }

            // See https://creators.vrchat.com/worlds/udon/vrc-graphics/vrc-camera-settings/
            switch (screenCameraSettings.CameraMode) {
                case VRCCameraMode.Screen: // The default mode for rendering the user's current view.
                case VRCCameraMode.FocusView: // Active when the user is in Focus View on mobile devices.
                    transformToMove.SetPositionAndRotation(
                            screenCameraSettings.Position,
                            usePlayerBaseRotationInVr && localPlayer.IsUserInVR()
                                    ? localPlayer.GetRotation()
                                    : screenCameraSettings.Rotation);
                    return true;
                case VRCCameraMode.ThirdPersonView: // The user is in Third Person View mode (desktop only).
                    transformToMove.SetPositionAndRotation(
                            screenCameraSettings.Position,
                            screenCameraSettings.Rotation);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Tries to get the position of the active screen camera.
        /// </summary>
        /// <param name="position">Resulting position or <see cref="Vector3.zero"/></param>
        /// <returns>True on success, false otherwise</returns>
        public static bool TryGetScreenCameraPosition(out Vector3 position) {
            var screenCameraSettings = VRCCameraSettings.ScreenCamera;
            if (!Utilities.IsValid(screenCameraSettings) || !screenCameraSettings.Active) {
                position = Vector3.zero;
                return false;
            }

            position = screenCameraSettings.Position;
            return true;
        }

        /// <summary>
        /// Tries to move a given transform to the active drone/handheld camera of the local player.
        /// </summary>
        /// <param name="transformToMove">Transform to move to the active drone/camera location/rotation.</param>
        /// <returns>false if no drone/handheld camera is active or in clientsim.</returns>
        public static bool TryMoveToPhotoOrDroneCamera(Transform transformToMove) {
            var photoCameraSettings = VRCCameraSettings.PhotoCamera;
            if (!Utilities.IsValid(photoCameraSettings) || !photoCameraSettings.Active) {
                return false;
            }

            // Reference for future changes: https://creators.vrchat.com/worlds/udon/vrc-graphics/vrc-camera-settings/
            // For now ignoring all photoCameraSettings.CameraModes, treat all the same
            transformToMove.SetPositionAndRotation(photoCameraSettings.Position, photoCameraSettings.Rotation);
            return true;
        }

        /// <summary>
        /// Moves a given transform to the active camera of the local player.
        /// Prioritizes handheld cameras/drones by default.
        /// </summary>
        /// <param name="transformToMove">Transform to move to the determined camera location/rotation</param>
        /// <param name="localPlayer"></param>
        /// <param name="restrictToMainCamera">Ignore active drone cameras etc.</param>
        /// <param name="usePlayerBaseRotationInVr">When true, will ignore the players camera rotation in first person
        ///     mode when in vr. <see cref="VRCPlayerApi.GetRotation"/> will be used instead.</param>
        public static void MoveToActivePlayerCamera(
                Transform transformToMove,
                VRCPlayerApi localPlayer,
                bool restrictToMainCamera = false,
                bool usePlayerBaseRotationInVr = false
        ) {
            if (restrictToMainCamera && TryMoveToScreenCamera(
                        transformToMove,
                        localPlayer,
                        usePlayerBaseRotationInVr)) {
                return;
            }

            if (TryMoveToPhotoOrDroneCamera(transformToMove)) {
                return;
            }

            TryMoveToScreenCamera(
                    transformToMove,
                    localPlayer,
                    usePlayerBaseRotationInVr);
        }
    }
}