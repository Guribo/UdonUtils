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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpAccurateSyncBehaviour.ExecutionOrder + 100;

        [FormerlySerializedAs("actualChair")]
        [SerializeField]
        protected Chair ActualChair;

        [PublicAPI]
        public virtual bool _OnLocalPlayerEntered() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnLocalPlayerEntered));
#endif
            if (!Utilities.IsValid(ActualChair)) {
                _Error($"{nameof(_OnLocalPlayerEntered)}.{nameof(ActualChair)} not set");
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer._TransferOwnershipFrom(go, go, Networking.LocalPlayer, true);
        }

        [PublicAPI]
        public virtual bool _OnLocalPlayerExited() {
#if TLP_DEBUG
            _DebugLog(nameof(_OnLocalPlayerExited));
#endif
            if (!Utilities.IsValid(ActualChair)) {
                _Error($"{nameof(_OnLocalPlayerExited)}.{nameof(ActualChair)} not set");
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer._TransferOwnershipFrom(go, go, Networking.GetOwner(ActualChair.gameObject), true);
        }

        [PublicAPI]
        public virtual bool _OnRemotePlayerEntered(VRCPlayerApi remotePlayer) {
#if TLP_DEBUG
            _DebugLog(nameof(_OnRemotePlayerEntered));
#endif
            return true;
        }

        [PublicAPI]
        public virtual bool _OnRemotePlayerExited(VRCPlayerApi remotePlayer) {
#if TLP_DEBUG
            _DebugLog(nameof(_OnRemotePlayerExited));
#endif
            return true;
        }

        [PublicAPI]
        public VRCPlayerApi _GetSeatedPlayer() {
            return Utilities.IsValid(ActualChair) ? ActualChair._GetSeatedPlayer() : null;
        }
    }
}