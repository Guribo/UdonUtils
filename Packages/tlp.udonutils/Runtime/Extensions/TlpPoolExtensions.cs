using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class TlpPoolExtensions
    {
        /// <summary>
        /// Returns script back to the Pool that created it (if it was not yet returned already and actually comes from a Pool).
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns>true on success, false otherwise</returns>
        public static bool _TryReturnToPool(this TlpBaseBehaviour behaviour) {
            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"{nameof(_TryReturnToPool)}: {behaviour._GetScriptPathInScene()}", null);
#endif
            #endregion

            if (!Utilities.IsValid(behaviour)) {
                TlpLogger.StaticError($"{nameof(_TryReturnToPool)}: {nameof(behaviour)} invalid", null);
                return false;
            }

            var behaviourPool = (Pool.Pool)behaviour.Pool;
            if (behaviour.PoolableInUse && Utilities.IsValid(behaviourPool)) {
                behaviourPool._Return(behaviour.gameObject);
                return true;
            }

            TlpLogger.StaticWarning(
                    $"{nameof(_TryReturnToPool)}: {behaviour._GetScriptPathInScene()} was no pool-able in use",
                    null);

            return false;
        }

        /// <summary>
        /// Returns script back to the Pool that created it (if it was not yet returned already and actually comes from a Pool).
        /// If returning is not possible, the GameObject is destroyed.
        /// </summary>
        /// <param name="behaviour"></param>
        public static void _ReturnToPoolOrDestroy(this TlpBaseBehaviour behaviour) {
            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog($"{nameof(_ReturnToPoolOrDestroy)}: {behaviour._GetScriptPathInScene()}", null);
#endif
            #endregion

            if (!Utilities.IsValid(behaviour)) {
                TlpLogger.StaticError($"{nameof(_ReturnToPoolOrDestroy)}: {nameof(behaviour)} invalid", null);
                return;
            }

            var behaviourPool = (Pool.Pool)behaviour.Pool;
            if (behaviour.PoolableInUse && Utilities.IsValid(behaviourPool)) {
                behaviourPool._Return(behaviour.gameObject);
                return;
            }

            TlpLogger.StaticWarning(
                    $"{nameof(_ReturnToPoolOrDestroy)}: {behaviour._GetScriptPathInScene()} was no pool-able in use",
                    null);


#if UNITY_EDITOR && !COMPILER_UDONSHARP
            Object.DestroyImmediate(behaviour.gameObject);
#else
                Object.Destroy(behaviour.gameObject);
#endif
        }
    }
}