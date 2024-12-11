using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Comparer), ExecutionOrder)]
    public abstract class Comparer : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.TimeSourcesStart + 10;


        [Tooltip("Used when the result of this Comparer object indicates no difference")]
        public Comparer optionalFallback;

        #region Public API
        [PublicAPI]
        public bool Compare(UdonSharpBehaviour first, UdonSharpBehaviour second, out int comparisonResult) {
#if TLP_DEBUG
            DebugLog(nameof(Compare));
#endif

            if (!ComparisonImplementation(first, second, out comparisonResult)) {
                return false;
            }

            if (comparisonResult != 0) {
                // elements are not equal according to this comparison, no further checking needed
                return true;
            }

            if (Utilities.IsValid(optionalFallback)) {
                return optionalFallback.Compare(first, second, out comparisonResult);
            }

#if TLP_DEBUG
            DebugLog(
                    $"Entries '{first.GetScriptPathInScene()}' and '{second.GetScriptPathInScene()}' are equal and no fallback is available that could say otherwise");
#endif
            return true;
        }
        #endregion


        #region internal
        protected abstract bool ComparisonImplementation(
                UdonSharpBehaviour first,
                UdonSharpBehaviour second,
                out int comparisonResult
        );
        #endregion

        #region Pool
        public bool ReturnToPool() {
            if (PoolableInUse && Utilities.IsValid((Pool.Pool)Pool)) {
                ((Pool.Pool)Pool).Return(gameObject);
                return true;
            }

            return false;
        }
        #endregion
    }
}