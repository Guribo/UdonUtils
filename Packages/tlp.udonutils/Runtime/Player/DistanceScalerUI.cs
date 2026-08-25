using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DistanceScalerUI), ExecutionOrder)]
    public class DistanceScalerUI : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = PlayerCameraTrackerUi.ExecutionOrder + 1;

        public VRCPlayerApi Player;

        [FormerlySerializedAs("useLocalPlayerByDefault")]
        public bool UseLocalPlayerByDefault = true;

        [FormerlySerializedAs("scale")]
        public float Scale = 0.1f;

        [FormerlySerializedAs("minSize")]
        public float MinSize = 0.00001f;

        [FormerlySerializedAs("maxSize")]
        public float MaxSize = 1000f;


        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (UseLocalPlayerByDefault) {
                Player = Networking.LocalPlayer;
            }

            return true;
        }

        public override void PostLateUpdate() {
            if (!Utilities.IsValid(Player)) {
                return;
            }

            var trackingData = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            float distance = Vector3.Distance(transform.position, trackingData.position);

            float clampedScale = Mathf.Clamp(Scale * distance, MinSize, MaxSize);

            transform.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
        }
    }
}