using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Player;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(WeaponsEvent), ExecutionOrder)]
    public class WeaponsEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = HorizontalLookAt.ExecutionOrder + 1;
    }
}