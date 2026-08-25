using JetBrains.Annotations;
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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpAccurateSyncBehaviour.ExecutionOrder + 1;

        #region U# Lifecycle
        public void FixedUpdate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(FixedUpdate));
#endif
            #endregion

            if (Networking.IsOwner(gameObject)) {
                return;
            }

            float predictionReduction = _GetTotalPredictionReduction();
            double networkTime = NetworkTime._TimeAsDouble();
            _PredictMovement(networkTime, 
                            networkTime - predictionReduction, 
                            networkTime - WorkingSendTime - predictionReduction, 
                            predictionReduction,
                            NetworkTime._FixedDeltaTime());
        }
        #endregion
    }
}