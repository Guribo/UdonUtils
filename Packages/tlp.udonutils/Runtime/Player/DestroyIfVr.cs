using JetBrains.Annotations;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Player
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DestroyIfVr : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpLogger.ExecutionOrder + 1;


        private void Start() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Start));
#endif
            #endregion

            if (Networking.LocalPlayer.IsUserInVR()) {
                DebugLog($"Destroying {name}");
                Destroy(gameObject);
            }
        }
    }
}