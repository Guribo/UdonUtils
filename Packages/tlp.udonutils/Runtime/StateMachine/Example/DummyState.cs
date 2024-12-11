using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.StateMachine.Example
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DummyState), ExecutionOrder)]
    public class DummyState : StateMachineState
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachineState.ExecutionOrder + 1;

        public StateMachineState Next;
        public float Delay = 3f;

        protected override void OnStateEntered() {
            base.OnStateEntered();
            if (Networking.IsOwner(StateMachine.gameObject)) {
                SendCustomEventDelayedSeconds(nameof(Delayed_TransitionToNext), Delay);
            }
        }

        public void Delayed_TransitionToNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Delayed_TransitionToNext));
#endif
            #endregion

            if (!Utilities.IsValid(Next) || !enabled) {
                return;
            }

            if (!TransitionTo(Next)) {
                Error($"Failed to transition to {Next.transform.GetPathInScene()}");
            }
        }

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Next)) {
                Error($"{nameof(Next)} not set");
                return false;
            }

            return true;
        }
    }
}