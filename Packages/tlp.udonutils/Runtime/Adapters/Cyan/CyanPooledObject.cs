using JetBrains.Annotations;
using VRC.SDKBase;

namespace TLP.UdonUtils.Adapters.Cyan
{
    public class CyanPooledObject : TlpBaseBehaviour
    {
        /// <summary>
        /// Who is the current owner of this object. Null if object is not currently in use. 
        /// </summary>
        [PublicAPI]
        public VRCPlayerApi Owner;

        /// <summary>
        /// This method will be called on all clients when the object is enabled and the Owner has been assigned.
        /// Initialize the object here
        /// </summary>
        [PublicAPI]
        public virtual void _OnOwnerSet() {
#if TLP_DEBUG
            DebugLog(nameof(_OnOwnerSet));
#endif
        }

        /// <summary>
        /// This method will be called on all clients when the original owner has
        /// left and the object is about to be disabled.
        /// Cleanup the object here.
        /// </summary>
        [PublicAPI]
        public virtual void _OnCleanup() {
#if TLP_DEBUG
            DebugLog(nameof(_OnCleanup));
#endif
        }

        public CyanPoolAdapter cyanPoolAdapter;
    }
}