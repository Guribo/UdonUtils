using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

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
            DebugLog(nameof(Interact));
#endif
            #endregion

            if (!HasStartedOk) {
                Error($"{nameof(Interact)}: Not initialized");
                return;
            }

            if (!Raise(this)) {
                Error($"{nameof(Interact)}: Failed to raise event");
            }
        }
    }
}