using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
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


        [FormerlySerializedAs("humanBodyBone")]
        public HumanBodyBones HumanBodyBone;

        public override void PostLateUpdate() {
            if (!HasStartedOk || !enabled) {
                return;
            }

            transform.SetPositionAndRotation(
                    LocalPlayer.GetBonePosition(HumanBodyBone),
                    LocalPlayer.GetBoneRotation(HumanBodyBone)
            );
        }
    }
}