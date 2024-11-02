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
    /// without having to reference it directly.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(CyanPoolAdapter), ExecutionOrder)]
    public class CyanPoolAdapter : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = CyanPoolEventListener.ExecutionOrder + 1;

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