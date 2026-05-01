using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Recording;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VehicleMotionEvent), ExecutionOrder)]
    public class VehicleMotionEvent : UdonEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TransformRecordingPlayer.ExecutionOrder + 1;
    }
}