using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Player
{
    /// <summary>
    /// Adjusts the volume of an AudioSource based on the configured curve and the distance to the player head.
    ///
    /// Note: Obsolete/Redundant, use curve editor of AudioSource component directly.
    /// </summary>
    [Obsolete("Use curve editor of AudioSource directly")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(CustomAudioFalloff), ExecutionOrder)]
    public class CustomAudioFalloff : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = AudioEvent.ExecutionOrder + 1;

        #region Dependencies
        [FormerlySerializedAs("audioSource")]
        public AudioSource AudioSource;
        #endregion

        #region Configuration
        [FormerlySerializedAs("customFallOff")]
        public AnimationCurve CustomFallOff = AnimationCurve.EaseInOut(0, 1, 25, 0);
        #endregion

        #region TLP Base Hooks
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (Utilities.IsValid(AudioSource)) {
                return true;
            }

            Error($"{nameof(AudioSource)} not set");
            return false;
        }
        #endregion

        #region Lifecycle
        public override void PostLateUpdate() {
            base.PostLateUpdate();
            UpdateVolume(Networking.LocalPlayer);
        }
        #endregion

        #region Internal
        internal void UpdateVolume(VRCPlayerApi player) {
            if (!Utilities.IsValid(player)) {
                return;
            }

            if (!Utilities.IsValid(AudioSource)) {
                return;
            }

            AudioSource.volume = CustomFallOff.Evaluate(
                    Vector3.Distance(
                            transform.position,
                            player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position));
        }
        #endregion
    }
}