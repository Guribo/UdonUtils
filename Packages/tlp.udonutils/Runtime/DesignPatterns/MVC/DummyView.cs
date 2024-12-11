using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.DesignPatterns.MVC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DummyView), ExecutionOrder)]
    public class DummyView : View
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = View.ExecutionOrder + 1;

        public override void OnModelChanged() {
        }
    }
}