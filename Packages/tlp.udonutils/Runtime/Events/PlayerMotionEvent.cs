using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayerMotionEvent), ExecutionOrder)]
    public class PlayerMotionEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.PlayerMotionStart + 1;
    }
}