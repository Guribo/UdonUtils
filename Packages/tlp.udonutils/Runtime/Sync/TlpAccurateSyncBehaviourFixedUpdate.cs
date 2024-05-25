using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Variant that predicts movement based on <see cref="FixedUpdate"/>
    /// </summary>
    public abstract class TlpAccurateSyncBehaviourFixedUpdate : TlpAccurateSyncBehaviour
    {
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