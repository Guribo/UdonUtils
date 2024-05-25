using JetBrains.Annotations;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public class AudioEvent : UdonEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.AudioStart;
    }
}