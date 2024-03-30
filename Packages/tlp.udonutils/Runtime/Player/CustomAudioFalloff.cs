using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CustomAudioFalloff : UdonSharpBehaviour
    {
        public AudioSource audioSource;
        public AnimationCurve customFallOff = AnimationCurve.EaseInOut(0, 1, 25, 0);

        internal void Start() {
            if (Utilities.IsValid(audioSource)) {
                return;
            }

            Debug.LogError($"{nameof(CustomAudioFalloff)}: AudioSource has not been set");
            enabled = false;
        }

        public override void PostLateUpdate() {
            UpdateVolume(Networking.LocalPlayer);
        }

        internal void UpdateVolume(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                return;
            }

            if (!Utilities.IsValid(audioSource)) {
                return;
            }

            audioSource.volume = customFallOff.Evaluate(
                    Vector3.Distance(
                            transform.position,
                            player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position));
        }
    }
}