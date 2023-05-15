using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(50_000 + 10102)]
    public class DistanceScalerUI : UdonSharpBehaviour
    {
        public VRCPlayerApi player;
        public bool useLocalPlayerByDefault = true;
        public float scale = 0.1f;
        public float minSize = 0.00001f;
        public float maxSize = 1000f;

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
            float distance = Vector3.Distance(transform.position, trackingData.position);

            float clampedScale = Mathf.Clamp(scale * distance, minSize, maxSize);

            transform.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
        }
    }
}