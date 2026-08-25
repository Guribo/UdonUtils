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
        public bool _Compare(UdonSharpBehaviour first, UdonSharpBehaviour second, out int comparisonResult) {
#if TLP_DEBUG
            _DebugLog(nameof(_Compare));
#endif

            if (!_ComparisonImplementation(first, second, out comparisonResult)) {
                return false;
            }

            if (comparisonResult != 0) {
                // elements are not equal according to this comparison, no further checking needed
                return true;
            }

            if (Utilities.IsValid(optionalFallback)) {
                return optionalFallback._Compare(first, second, out comparisonResult);
            }

#if TLP_DEBUG
            _DebugLog(
                    $"Entries '{first._GetScriptPathInScene()}' and '{second._GetScriptPathInScene()}' are equal and no fallback is available that could say otherwise");
#endif
            return true;
        }
        #endregion


        #region internal
        protected abstract bool _ComparisonImplementation(
                UdonSharpBehaviour first,
                UdonSharpBehaviour second,
                out int comparisonResult
        );
        #endregion
    }
}