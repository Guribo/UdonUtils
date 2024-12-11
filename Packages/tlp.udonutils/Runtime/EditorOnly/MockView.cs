using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(MockView), ExecutionOrder)]
    public class MockView : View
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder + 99;

        public bool InitResult = true;
        public bool DeInitResult = true;
        public int ModelChangedInvocations;

        protected override bool InitializeInternal() {
            return InitResult;
        }

        protected override bool DeInitializeInternal() {
            return DeInitResult;
        }

        public override void OnModelChanged() {
            ++ModelChangedInvocations;
        }

        public void SetMockHasError(bool error) {
            CriticalError = error.ToString();
        }
    }
}