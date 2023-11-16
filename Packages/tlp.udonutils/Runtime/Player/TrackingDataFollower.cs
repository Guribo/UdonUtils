﻿using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
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

        [FormerlySerializedAs("trackingDataType")]
        public VRCPlayerApi.TrackingDataType TrackingDataType = VRCPlayerApi.TrackingDataType.Head;

        [FormerlySerializedAs("useLocalPlayerByDefault")]
        public bool UseLocalPlayerByDefault = true;

        protected Transform OwnTransform;

        public void Start()
        {
            if (UseLocalPlayerByDefault)
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

            var trackingData = Player.GetTrackingData(TrackingDataType);
            if (trackingData.position.sqrMagnitude > 0.001f)
            {
                OwnTransform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
            }
            else
            {
                // fallback to player position/Rotation if non-humanoid
                OwnTransform.SetPositionAndRotation(Player.GetPosition(), Player.GetRotation());
            }
        }
    }
}