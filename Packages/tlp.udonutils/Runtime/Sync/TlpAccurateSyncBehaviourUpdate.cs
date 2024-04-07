using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Sync
{
    /// <summary>
    /// Variant that predicts movement based on <see cref="Update"/>
    /// </summary>
    public abstract class TlpAccurateSyncBehaviourUpdate : TlpAccurateSyncBehaviour
    {
        #region U# Lifecycle
        public void Update() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Update));
#endif
            #endregion

            if (Networking.IsOwner(gameObject)) {
                return;
            }

            PredictMovement(GetElapsed(), Time.deltaTime);
        }
        #endregion
    }
}