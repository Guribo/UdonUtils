using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class Comparer : TlpBaseBehaviour
    {
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