using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Testing
{
    /// <summary>
    /// Empty test that should always pass.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestSanity), ExecutionOrder)]
    public class TestSanity : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestCase.ExecutionOrder + 2;
    }
}