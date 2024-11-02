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
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TrackingDataFollowerUI.ExecutionOrder + 1;

        public VRCPlayerApi player;
        public bool useLocalPlayerByDefault = true;

        public override void Start() {
            base.Start();
            if (useLocalPlayerByDefault) {
                player = Networking.LocalPlayer;
            }
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