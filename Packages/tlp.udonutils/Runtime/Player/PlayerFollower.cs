using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerFollower), ExecutionOrder)]
    public class PlayerFollower : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.PlayerMotionStart + 500;

        public VRCPlayerApi Player;
        public bool UseLocalPlayerByDefault = true;
        protected Transform OwnTransform;

        [SerializeField]
        [Range(0, 1)]
        private float SmoothTime;

        private Vector3 _smoothingVelocity;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            OwnTransform = transform;
            return true;
        }

        public override void PostLateUpdate() {
            if (UseLocalPlayerByDefault) {
                Player = Networking.LocalPlayer;
            }

            if (!Utilities.IsValid(Player)) {
                Warn("Player is not valid");
                return;
            }

            if (SmoothTime <= 0f) {
                OwnTransform.SetPositionAndRotation(Player.GetPosition(), Player.GetRotation());
            } else {
                var newPosition = Vector3.SmoothDamp(
                        OwnTransform.position,
                        Player.GetPosition(),
                        ref _smoothingVelocity,
                        SmoothTime
                );
                OwnTransform.SetPositionAndRotation(
                        newPosition,
                        Player.GetRotation()
                );
            }
        }
    }
}