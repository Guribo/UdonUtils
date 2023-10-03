using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class PlayerFollower : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart;

        public VRCPlayerApi Player;
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
            if (Utilities.IsValid(Player))
            {
                OwnTransform.SetPositionAndRotation(Player.GetPosition(), Player.GetRotation());
            }
        }
    }
}