using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DestroyIfDesktop : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLogger.ExecutionOrder + 1;

        public override void Start() {
            base.Start();

            if (Networking.LocalPlayer.IsUserInVR()) {
                return;
            }

            DebugLog($"Destroying {transform.GetPathInScene()}");
            Destroy(gameObject);
        }
    }
}