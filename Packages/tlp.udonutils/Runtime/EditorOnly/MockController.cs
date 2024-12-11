using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MockController), ExecutionOrder)]
    public class MockController : Controller
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder - 1;


        public bool InitResult = true;
        public bool DeInitResult = true;


        protected override bool InitializeInternal() {
            return InitResult;
        }

        protected override bool DeInitializeInternal() {
            return DeInitResult;
        }

        public void SetMockHasError(bool error) {
            CriticalError = error.ToString();
        }
    }
}