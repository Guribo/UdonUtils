using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
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

        public VRCPlayerApi player;
        public bool useLocalPlayerByDefault = true;


        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (useLocalPlayerByDefault) {
                player = Networking.LocalPlayer;
            }

            return true;
        }

        public override void PostLateUpdate() {
            if (!Utilities.IsValid(player)) {
                return;
            }

            var trackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.LookAt(trackingData.position);
        }
    }
}