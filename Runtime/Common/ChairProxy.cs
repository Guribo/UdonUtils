using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    public class ChairProxy : TlpBaseBehaviour
    {
        [SerializeField]
        protected Chair actualChair;

        [PublicAPI]
        public virtual bool OnLocalPlayerEntered()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerEntered));
#endif

            if (!Assert(Utilities.IsValid(actualChair), "actualChair invalid", this))
            {
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer.TransferOwnershipFrom(go, go, Networking.LocalPlayer, true);
        }

        [PublicAPI]
        public virtual bool OnLocalPlayerExited()
        {
#if TLP_DEBUG
            DebugLog(nameof(OnLocalPlayerExited));
#endif
            if (!Assert(Utilities.IsValid(actualChair), "actualChair invalid", this))
            {
                return false;
            }

            var go = gameObject;
            return OwnershipTransfer.TransferOwnershipFrom(go, go, Networking.GetOwner(actualChair.gameObject), true);
        }

        [PublicAPI]
        public virtual bool OnRemotePlayerEntered(VRCPlayerApi remotePlayer)
        {
#if TLP_DEBUG
            DebugLog(nameof(OnRemotePlayerEntered));
#endif
            return true;
        }

        [PublicAPI]
        public virtual bool OnRemotePlayerExited(VRCPlayerApi remotePlayer)
        {
#if TLP_DEBUG
            DebugLog(nameof(OnRemotePlayerExited));
#endif
            return true;
        }

        [PublicAPI]
        public VRCPlayerApi GetSeatedPlayer()
        {
            return Utilities.IsValid(actualChair) ? actualChair.GetSeatedPlayer() : null;
        }
    }
}