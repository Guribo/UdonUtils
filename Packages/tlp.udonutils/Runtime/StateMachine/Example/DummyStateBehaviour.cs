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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StateMachineBehaviour.ExecutionOrder + 1;

        public GameObject ThingToToggle;

        public override void _OnStateEntered() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnStateEntered));
#endif
            #endregion

            _Info($"Hello from {this._GetScriptPathInScene()}");
            ThingToToggle.SetActive(true);
        }

        public override void _OnStateExited() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_OnStateExited));
#endif
            #endregion

            _Info($"Goodbye from {this._GetScriptPathInScene()}");
            ThingToToggle.SetActive(false);
        }
    }
}