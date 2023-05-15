using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Adapters.Cyan
{
    public class CyanPoolEventListener : TlpBaseBehaviour
    {
        /// <summary>
        /// This event is called when the local player's pool object has been assigned.
        /// </summary>
        [PublicAPI]
        public virtual void _OnLocalPlayerAssigned()
        {
#if TLP_DEBUG
            DebugLog(nameof(_OnLocalPlayerAssigned));
#endif
        }

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerAssigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public VRCPlayerApi playerAssignedPlayer;

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerAssigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public int playerAssignedIndex;

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerAssigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public UdonBehaviour playerAssignedPoolObject;

        /// <summary>
        /// This event is called when any player is assigned a pool object.
        /// Assigned player is assigned to <see cref="playerAssignedPlayer"/>.
        /// Assigned player index is stored in <see cref="playerAssignedIndex"/>.
        /// Assigned pool is stored in <see cref="playerAssignedPoolObject"/>.
        /// </summary>
        [PublicAPI]
        public virtual void _OnPlayerAssigned()
        {
#if TLP_DEBUG
            DebugLog(nameof(_OnPlayerAssigned));
#endif
        }

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerUnassigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public VRCPlayerApi playerUnassignedPlayer;

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerUnassigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public int playerUnassignedIndex;

        /// <summary>
        /// The variable will be set before the event <see cref="_OnPlayerUnassigned"/> is called.
        /// </summary>
        [PublicAPI]
        [HideInInspector]
        public UdonBehaviour playerUnassignedPoolObject;

        /// <summary>
        /// This event is called when any player's object has been unassigned.
        /// Unassigned player is assigned to <see cref="playerUnassignedPlayer"/>.
        /// Unassigned player index is stored in <see cref="playerUnassignedIndex"/>.
        /// Unassigned pool is stored in <see cref="playerUnassignedPoolObject"/>.
        /// </summary>
        [PublicAPI]
        public virtual void _OnPlayerUnassigned()
        {
#if TLP_DEBUG
            DebugLog(nameof(_OnPlayerUnassigned));
#endif
        }
    }
}