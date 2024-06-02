using TLP.UdonUtils.Runtime.Common;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.StateMachine.Example
{
    public class DummyStateBehaviour : StateMachineBehaviour
    {
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