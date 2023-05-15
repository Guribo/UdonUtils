using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(50_000 + 10100)]
    public class TrackingDataFollowerUI : UdonSharpBehaviour
    {
        public VRCPlayerApi player;
        public VRCPlayerApi.TrackingDataType trackingDataType = VRCPlayerApi.TrackingDataType.Head;
        public bool useLocalPlayerByDefault = true;

        public void Start()
        {
            if (useLocalPlayerByDefault)
            {
                player = Networking.LocalPlayer;
            }
        }

        public override void PostLateUpdate()
        {
            if (!Utilities.IsValid(player))
            {
                return;
            }

            var trackingData = player.GetTrackingData(trackingDataType);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
        }
    }
}