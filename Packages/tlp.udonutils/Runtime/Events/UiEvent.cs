using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(UiEvent), ExecutionOrder)]
    public class UiEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.UiStart + 1;

        public override void Interact() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(Interact));
#endif
            #endregion
            
            if (NetworkCalling.InNetworkCall) {
                _Warn($"{nameof(Interact)}: called during network call, skipping");
                return;
            }

            if (!HasStartedOk) {
                _Error($"{nameof(Interact)}: Not initialized");
                return;
            }

            if (!_Raise(this)) {
                _Error($"{nameof(Interact)}: Failed to raise event");
            }
        }
    }
}