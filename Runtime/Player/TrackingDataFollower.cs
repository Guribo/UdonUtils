using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(50_000 + 3750)]
    public class TrackingDataFollower : UdonSharpBehaviour
    {
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
            if (!Utilities.IsValid(Player))
            {
                return;
            }

            var trackingData = Player.GetTrackingData(trackingDataType);
            OwnTransform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
        }
    }
}