using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Sync;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Adapters.Cyan
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(CyanPooledObject), ExecutionOrder)]
    public abstract class CyanPooledObject : TlpBaseBehaviour
    {

        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VehicleMotionEvent.ExecutionOrder + 1;


        [FormerlySerializedAs("cyanPoolAdapter")]
        public CyanPoolAdapter CyanPoolAdapter;

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
    }
}