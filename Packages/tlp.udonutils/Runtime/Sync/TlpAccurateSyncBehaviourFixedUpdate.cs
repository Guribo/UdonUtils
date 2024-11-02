using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Variant that predicts movement based on <see cref="FixedUpdate"/>
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpAccurateSyncBehaviourFixedUpdate), ExecutionOrder)]
    public abstract class TlpAccurateSyncBehaviourFixedUpdate : TlpAccurateSyncBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpAccurateSyncBehaviour.ExecutionOrder + 1;

        #region U# Lifecycle
        public void FixedUpdate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(FixedUpdate));
#endif
            #endregion

            if (Networking.IsOwner(gameObject)) {
                return;
            }

            PredictMovement(GetElapsed(), GameTime.FixedDeltaTime());
        }
        #endregion
    }
}