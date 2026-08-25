using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(LookAtPlayerUI), ExecutionOrder)]
    public class LookAtPlayerUI : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TrackingDataFollowerUI.ExecutionOrder + 1;

        public VRCPlayerApi Player;

        [FormerlySerializedAs("useLocalPlayerByDefault")]
        public bool UseLocalPlayerByDefault = true;


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
            transform.LookAt(trackingData.position);
        }
    }
}