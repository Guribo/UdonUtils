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

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (UdonEvent.AddListenerVerified(this, nameof(UdonEventFunctionName))) {
                return true;
            }

            Error($"Failed to listen to {nameof(UdonEvent)}");
            return false;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(UdonEventFunctionName):
                    UdonEventFunctionName();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void UdonEventFunctionName() {
            // your code here, will be called whenever UdonEvent fires
        }
    }
}