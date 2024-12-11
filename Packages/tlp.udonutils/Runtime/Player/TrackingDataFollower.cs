using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TrackingDataFollower), ExecutionOrder)]
    public class TrackingDataFollower : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = WeaponsEvent.ExecutionOrder + 1;

        public VRCPlayerApi Player;

        [FormerlySerializedAs("trackingDataType")]
        public VRCPlayerApi.TrackingDataType TrackingDataType = VRCPlayerApi.TrackingDataType.Head;

        [FormerlySerializedAs("useLocalPlayerByDefault")]
        public bool UseLocalPlayerByDefault = true;

        protected Transform OwnTransform;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (UseLocalPlayerByDefault) {
                Player = Networking.LocalPlayer;
            }

            OwnTransform = transform;
            return true;
        }

        public override void PostLateUpdate() {
            if (!Utilities.IsValid(Player)) {
                return;
            }

            var trackingData = Player.GetTrackingData(TrackingDataType);
            if (trackingData.position.sqrMagnitude > 0.001f) {
                OwnTransform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
            } else {
                // fallback to player position/Rotation if non-humanoid
                OwnTransform.SetPositionAndRotation(Player.GetPosition(), Player.GetRotation());
            }
        }
    }
}