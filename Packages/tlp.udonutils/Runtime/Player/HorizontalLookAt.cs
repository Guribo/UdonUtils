using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// turns this gameobject around to always look at the local player, only rotates vertical rotation
    /// (pitch/roll are forced to 0)
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HorizontalLookAt : UdonSharpBehaviour
    {
        [Tooltip(
                "e.g. 0.5 makes it blend 50% between initial rotation and target rotation, 1.0 makes it look directly at the player"
        )]
        [Range(0, 1)]
        public float weight = 1f;

        private Quaternion _initialRotation;

        public void Start() {
            _initialRotation = transform.rotation;
        }

        public void Update() {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) {
                return;
            }

            var vectorToTarget = localPlayer.GetPosition() - transform.position;
            var targetRotation = Quaternion.LookRotation(vectorToTarget, Vector3.up);

            // blend from initial rotation to target rotation based on strength
            float yaw = Quaternion.Slerp(_initialRotation, targetRotation, weight).eulerAngles.y;

            // only apply yaw rotation part to this transform
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
    }
}