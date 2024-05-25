using Cyan.PlayerObjectPool;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Adapters.Cyan
{
    /// <summary>
    /// Adapter that allows retrieving objects from the CyanPlayerObjectPool
    /// with having to reference it directly.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class CyanPoolAdapter : TlpBaseBehaviour
    {
        [PublicAPI]
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultEnd;

        [FormerlySerializedAs("cyanPlayerObjectAssigner")]
        public CyanPlayerObjectAssigner CyanPlayerObjectAssigner;

        public virtual Component[] PooledUdon() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(PooledUdon));
#endif
            #endregion

            if (Utilities.IsValid(CyanPlayerObjectAssigner)) {
                return CyanPlayerObjectAssigner.pooledUdon;
            }

            return new Component[0];
        }

        public virtual Component GetPlayerPooledUdon(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetPlayerPooledUdon)} for player {player.ToStringSafe()}");
#endif
            #endregion

            if (Utilities.IsValid(CyanPlayerObjectAssigner)) {
                return CyanPlayerObjectAssigner._GetPlayerPooledUdon(player);
            }

            return null;
        }

        public virtual GameObject GetPlayerPooledObject(VRCPlayerApi player) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetPlayerPooledObject)} for player {player.ToStringSafe()}");
#endif
            #endregion

            if (Utilities.IsValid(CyanPlayerObjectAssigner)) {
                return CyanPlayerObjectAssigner._GetPlayerPooledObject(player);
            }

            return null;
        }
    }
}