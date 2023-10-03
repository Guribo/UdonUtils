using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class TrackingDataFollower : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart;

        public VRCPlayerApi Player;
        public VRCPlayerApi.TrackingDataType trackingDataType = VRCPlayerApi.TrackingDataType.Head;
        public bool useLocalPlayerByDefault = true;
        protected Transform OwnTransform;

        public void Start()
        {
            if (useLocalPlayerByDefault)
            {
                Player = Networking.LocalPlayer;
            }

            OwnTransform = transform;
        }

        public override void PostLateUpdate()
        {
            if (Utilities.IsValid(Player))
            {
                var trackingData = Player.GetTrackingData(trackingDataType);
                OwnTransform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
            }
        }
    }
}