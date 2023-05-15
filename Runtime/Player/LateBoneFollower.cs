using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(50_000 + 100000)]
    public class LateBoneFollower : UdonSharpBehaviour
    {
        public HumanBodyBones humanBodyBone;

        public override void PostLateUpdate()
        {
            var localPlayer = Networking.LocalPlayer;
            if (Utilities.IsValid(localPlayer))
            {
                transform.SetPositionAndRotation(
                    localPlayer.GetBonePosition(humanBodyBone),
                    localPlayer.GetBoneRotation(humanBodyBone)
                );
            }
        }
    }
}