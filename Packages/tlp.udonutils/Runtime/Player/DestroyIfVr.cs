using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DestroyIfVr), ExecutionOrder)]
    public class DestroyIfVr : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DestroyIfDesktop.ExecutionOrder + 1;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Networking.LocalPlayer.IsUserInVR()) {
                return true;
            }

            _DebugLog($"Destroying {transform._GetPathInScene()}");
            Destroy(gameObject);
            return true;
        }
    }
}