#if !COMPILER_UDONSHARP && UNITY_EDITOR
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.Udon.Common.Enums;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MockEvent), ExecutionOrder)]
    public class MockEvent : UdonEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = UdonEvent.ExecutionOrder + 99;

        public TlpBaseBehaviour Caller;
        public TlpBaseBehaviour RaiseOnIdleInstigator { get; private set; }
        public int RaiseOnIdleIdleFrames { get; private set; }
        public EventTiming RaiseOnIdleEventTiming { get; private set; }

        [FormerlySerializedAs("Raises")]
        public int Invocations;

        public override bool Raise(TlpBaseBehaviour instigator) {
            DebugLog(nameof(Raise));
            ++Invocations;
            Caller = instigator;
            return true;
        }

        public override bool RaiseOnIdle(
                TlpBaseBehaviour instigator,
                int idleFrames = 1,
                EventTiming eventTiming = EventTiming.Update
        ) {
            DebugLog(nameof(RaiseOnIdle));
            RaiseOnIdleEventTiming = eventTiming;
            RaiseOnIdleIdleFrames = idleFrames;
            RaiseOnIdleInstigator = instigator;

            return true;
        }
    }
}
#endif