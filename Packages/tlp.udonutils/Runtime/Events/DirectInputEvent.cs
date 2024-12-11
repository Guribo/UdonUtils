using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DirectInputEvent), ExecutionOrder)]
    public class DirectInputEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DirectInputStart + 1;
    }
}