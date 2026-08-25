using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Variant that predicts movement based on <see cref="Update"/>
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpAccurateSyncBehaviourUpdate),
                              ExecutionOrder)]
    public abstract class TlpAccurateSyncBehaviourUpdate : TlpAccurateSyncBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpAccurateSyncBehaviourFixedUpdate.ExecutionOrder + 1;

        #region U# Lifecycle
        internal void Update() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(Update));
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
                            NetworkTime._UnscaledDeltaTime());
        }
        #endregion
    }
}