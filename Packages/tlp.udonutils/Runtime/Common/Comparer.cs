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
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.TimeSourcesStart + 1;


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

            bool noFallbackAvailable = !Utilities.IsValid(optionalFallback);
            return noFallbackAvailable || optionalFallback.Compare(first, second, out comparisonResult);
        }
        #endregion


        #region internal
        protected abstract bool ComparisonImplementation(
                UdonSharpBehaviour first,
                UdonSharpBehaviour second,
                out int comparisonResult
        );
        #endregion
    }
}