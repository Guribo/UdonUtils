using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MockModel), ExecutionOrder)]
    public class MockModel : Model
    {

        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Controller.ExecutionOrder -1;


        public bool InitResult = true;
        public bool DeInitResult = true;


        protected override bool _InitializeInternal() {
            return InitResult;
        }

        protected override bool _DeInitializeInternal() {
            return DeInitResult;
        }

        public void _SetMockHasError(bool error) {
            CriticalError = error.ToString();
        }
    }
}