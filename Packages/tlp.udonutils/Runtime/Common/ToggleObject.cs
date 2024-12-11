using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Recording;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ToggleObject), ExecutionOrder)]
    public class ToggleObject : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TransformBacklog.ExecutionOrder + 1;

        public AudioSource activationSound;
        public AudioSource deactivationSound;

        public KeyCode gameobjectToggle = KeyCode.G;

        public GameObject gameObjectToToggle;

        private void LateUpdate() {
            if (Input.GetKeyDown(gameobjectToggle)) {
                Toggle();
            }
        }

        internal void Toggle() {
            if (Utilities.IsValid(gameObjectToToggle)) {
                gameObjectToToggle.SetActive(!gameObjectToToggle.activeSelf);
                var sound = gameObjectToToggle.activeSelf ? activationSound : deactivationSound;
                if (Utilities.IsValid(sound)
                    && Utilities.IsValid(sound.clip)) {
                    sound.PlayOneShot(sound.clip);
                }
            }
        }

        public override void Interact() {
            Toggle();
        }
    }
}