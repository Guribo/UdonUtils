using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(LateBoneFollower), ExecutionOrder)]
    public class LateBoneFollower : TlpBaseBehaviour
    {

        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TrackingDataFollower.ExecutionOrder + 1;


        public HumanBodyBones humanBodyBone;

        public override void PostLateUpdate() {
            var localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer)) {
                transform.SetPositionAndRotation(
                        localPlayer.GetBonePosition(humanBodyBone),
                        localPlayer.GetBoneRotation(humanBodyBone)
                );
            }
        }
    }
}