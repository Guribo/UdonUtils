using JetBrains.Annotations;
using UnityEngine;

namespace TLP.UdonUtils.Events
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public class AudioEvent : UdonEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.AudioStart;
    }
}