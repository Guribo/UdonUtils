using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(50_000 + 10101)]
    public class LookAtPlayerUI : UdonSharpBehaviour
    {
        public VRCPlayerApi player;
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

            var trackingData = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.LookAt(trackingData.position);
        }
    }
}