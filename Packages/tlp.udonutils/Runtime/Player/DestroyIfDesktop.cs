using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Examples;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DestroyIfDesktop), ExecutionOrder)]
    public class DestroyIfDesktop : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = WorldVersionEventListener.ExecutionOrder + 1;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (Networking.LocalPlayer.IsUserInVR()) {
                return true;
            }

            DebugLog($"Destroying {transform.GetPathInScene()}");
            Destroy(gameObject);
            return true;
        }
    }
}