using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Events
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ExampleEventListener), ExecutionOrder)]
    public class ExampleEventListener : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachine.StateMachineBehaviour.ExecutionOrder + 100;
        [SerializeField]
        private UdonEvent UdonEvent;

        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (UdonEvent._AddListenerVerified(this, nameof(_UdonEventFunctionName))) {
                return true;
            }

            _Error($"Failed to listen to {nameof(UdonEvent)}");
            return false;
        }

        public override void _OnEvent(string eventName) {
            switch (eventName) {
                case nameof(_UdonEventFunctionName):
                    _UdonEventFunctionName();
                    break;
                default:
                    base._OnEvent(eventName);
                    break;
            }
        }

        private void _UdonEventFunctionName() {
            // your code here, will be called whenever UdonEvent fires
        }
    }
}