using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(OwnerOnly), ExecutionOrder)]
    public class OwnerOnly : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DirectInputEvent.ExecutionOrder + 1;


        public GameObject[] gameObjects;
        public UdonSharpBehaviour[] UdonSharpBehaviours;

        public void OnEnable() {
            ToggleEnabled(Networking.IsOwner(gameObject));
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            ToggleEnabled(Networking.IsOwner(gameObject));
        }

        private void ToggleEnabled(bool enable) {
            if (gameObjects == null) {
                return;
            }

            foreach (var go in gameObjects) {
                if (!Utilities.IsValid(go)) {
                    continue;
                }

                if (go.activeSelf == enable) {
                    continue;
                }

                go.SetActive(enable);
            }

            if (UdonSharpBehaviours == null) {
                return;
            }

            foreach (var udonSharpBehaviour in UdonSharpBehaviours) {
                if (!Utilities.IsValid(udonSharpBehaviour)) {
                    continue;
                }

                udonSharpBehaviour.enabled = enable;
            }
        }
    }
}