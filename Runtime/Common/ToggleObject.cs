using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Common
{
    public class ToggleObject : UdonSharpBehaviour
    {
        public AudioSource activationSound;
        public AudioSource deactivationSound;

        public KeyCode gameobjectToggle = KeyCode.G;

        public GameObject gameObjectToToggle;

        private void LateUpdate()
        {
            if (Input.GetKeyDown(gameobjectToggle))
            {
                Toggle();
            }
        }

        internal void Toggle()
        {
            if (Utilities.IsValid(gameObjectToToggle))
            {
                gameObjectToToggle.SetActive(!gameObjectToToggle.activeSelf);
                var sound = (gameObjectToToggle.activeSelf ? activationSound : deactivationSound);
                if (Utilities.IsValid(sound)
                    && Utilities.IsValid(sound.clip))
                {
                    sound.PlayOneShot(sound.clip);
                }
            }
        }

        public override void Interact()
        {
            Toggle();
        }
    }
}