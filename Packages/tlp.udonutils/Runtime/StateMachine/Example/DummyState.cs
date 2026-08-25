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

        protected override void _OnStateEntered() {
            base._OnStateEntered();
            if (Networking.IsOwner(StateMachine.gameObject)) {
                SendCustomEventDelayedSeconds(nameof(_Delayed_TransitionToNext), Delay);
            }
        }

        public void _Delayed_TransitionToNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_Delayed_TransitionToNext));
#endif
            #endregion

            if (!Utilities.IsValid(Next) || !enabled) {
                return;
            }

            if (!_TransitionTo(Next)) {
                _Error($"Failed to transition to {Next.transform._GetPathInScene()}");
            }
        }

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(Next)) {
                _Error($"{nameof(Next)} not set");
                return false;
            }

            return true;
        }
    }
}