#if !COMPILER_UDONSHARP && UNITY_EDITOR
using TLP.UdonUtils.Runtime.Events;
using UnityEngine.Serialization;
using VRC.Udon.Common.Enums;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    public class MockEvent : UdonEvent
    {
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