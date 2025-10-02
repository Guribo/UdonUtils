using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
#if UNITY_EDITOR
using UnityEditor;
using TLP.UdonUtils.Runtime.Sync;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Sync
{
    [CustomEditor(typeof(OwnerOnly))]
    public class OwnerOnlyEditor : UnityEditor.Editor
    {
        private const string Description = "Controls the activation state of GameObjects and UdonSharpBehaviours " +
                                           "based on object ownership. When the local player owns this object, the " +
                                           "specified GameObjects will be activated and UdonSharpBehaviours will " +
                                           "be enabled. When ownership is transferred to another player, these " +
                                           "elements will be deactivated/disabled for the local player.";

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif
namespace TLP.UdonUtils.Runtime.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(OwnerOnly), ExecutionOrder)]
    public class OwnerOnly : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = DirectInputEvent.ExecutionOrder + 1;

        [FormerlySerializedAs("gameObjects")]
        public GameObject[] GameObjects;

        public UdonSharpBehaviour[] UdonSharpBehaviours;
        // public Behaviour[] Behaviours;

        public void OnEnable() {
            ToggleEnabled(Networking.IsOwner(gameObject));
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            ToggleEnabled(Networking.IsOwner(gameObject));
        }

        private void ToggleEnabled(bool enable) {
            ToggleGameObjectsActiveState(enable);
            ToggleUdonSharpBehaviours(enable);
            ToggleComponentsEnabled(enable);
        }

        private void ToggleComponentsEnabled(bool enable) {
            // if (Behaviours != null) {
            //     foreach (var component in Behaviours) {
            //         if (!Utilities.IsValid(component)) {
            //             continue;
            //         }
            //         // component.enabled = enable; // not exposed :(
            //     }
            // }
        }

        private void ToggleUdonSharpBehaviours(bool enable) {
            if (UdonSharpBehaviours != null) {
                foreach (var udonSharpBehaviour in UdonSharpBehaviours) {
                    if (!Utilities.IsValid(udonSharpBehaviour)) {
                        continue;
                    }

                    udonSharpBehaviour.enabled = enable;
                }
            }
        }

        private void ToggleGameObjectsActiveState(bool enable) {
            if (GameObjects != null) {
                foreach (var go in GameObjects) {
                    if (!Utilities.IsValid(go)) {
                        continue;
                    }

                    if (go.activeSelf == enable) {
                        continue;
                    }

                    go.SetActive(enable);
                }
            }
        }
    }
}