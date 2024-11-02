using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ChairProxy), ExecutionOrder)]
    public class ChairProxy : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpAccurateSyncBehaviour.ExecutionOrder + 100;

        [FormerlySerializedAs("actualChair")]
        [SerializeField]
        protected Chair ActualChair;

        [PublicAPI]
        public virtual bool OnLocalPlayerEntered() {
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerEntered));
#endif
            if (!Utilities.IsValid(ActualChair)) {
                Error($"{nameof(OnLocalPlayerEntered)}.{nameof(ActualChair)} not set");
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer.TransferOwnershipFrom(go, go, Networking.LocalPlayer, true);
        }

        [PublicAPI]
        public virtual bool OnLocalPlayerExited() {
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerExited));
#endif
            if (!Utilities.IsValid(ActualChair)) {
                Error($"{nameof(OnLocalPlayerExited)}.{nameof(ActualChair)} not set");
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer.TransferOwnershipFrom(go, go, Networking.GetOwner(ActualChair.gameObject), true);
        }

        [PublicAPI]
        public virtual bool OnRemotePlayerEntered(VRCPlayerApi remotePlayer) {
#if TLP_DEBUG
            DebugLog(nameof(OnRemotePlayerEntered));
#endif
            return true;
        }

        [PublicAPI]
        public virtual bool OnRemotePlayerExited(VRCPlayerApi remotePlayer) {
#if TLP_DEBUG
            DebugLog(nameof(OnRemotePlayerExited));
#endif
            return true;
        }

        [PublicAPI]
        public VRCPlayerApi GetSeatedPlayer() {
            return Utilities.IsValid(ActualChair) ? ActualChair.GetSeatedPlayer() : null;
        }
    }
}