using Cyan.PlayerObjectPool;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Adapters.Cyan
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class CyanPoolAdapter : TlpBaseBehaviour
    {
        [PublicAPI]
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultEnd;

        public CyanPlayerObjectAssigner cyanPlayerObjectAssigner;

        public virtual Component[] PooledUdon()
        {
            DebugLog(nameof(PooledUdon));
            if (Utilities.IsValid(cyanPlayerObjectAssigner))
            {
                return cyanPlayerObjectAssigner.pooledUdon;
            }

            return new Component[0];
        }

        public virtual Component GetPlayerPooledUdon(VRCPlayerApi player)
        {
            DebugLog($"{nameof(GetPlayerPooledUdon)} ({player.displayName})");
            if (Utilities.IsValid(cyanPlayerObjectAssigner))
            {
                return cyanPlayerObjectAssigner._GetPlayerPooledUdon(player);
            }

            return null;
        }

        public virtual GameObject GetPlayerPooledObject(VRCPlayerApi player)
        {
            DebugLog($"{nameof(GetPlayerPooledObject)} ({player.displayName})");
            if (Utilities.IsValid(cyanPlayerObjectAssigner))
            {
                return cyanPlayerObjectAssigner._GetPlayerPooledObject(player);
            }

            return null;
        }
    }
}