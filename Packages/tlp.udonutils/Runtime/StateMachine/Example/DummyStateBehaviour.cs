using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.StateMachine.Example
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DummyStateBehaviour), ExecutionOrder)]
    public class DummyStateBehaviour : StateMachineBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachineBehaviour.ExecutionOrder + 1;

        public GameObject ThingToToggle;

        public override void OnStateEntered() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnStateEntered));
#endif
            #endregion

            Info($"Hello from {this.GetScriptPathInScene()}");
            ThingToToggle.SetActive(true);
        }

        public override void OnStateExited() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnStateExited));
#endif
            #endregion

            Info($"Goodbye from {this.GetScriptPathInScene()}");
            ThingToToggle.SetActive(false);
        }
    }
}