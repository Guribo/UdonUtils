using NUnit.Framework;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Tests.Runtime.Utils;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestCustomAudioFalloff
    {
        private TlpLogger _tlpLogger;

        [SetUp]
        public void Setup()
        {
            if (!_tlpLogger)
            {
                _tlpLogger = new GameObject("TLPLogger").AddComponent<TlpLogger>();
                _tlpLogger.Severity = ELogLevel.Debug;
            }

            UdonTestUtils.UdonTestEnvironment.ResetApiBindings();
        }

        #region Start

        [Test]
        public void Start_DisablesComponentIfAudioInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();

            Assert.Null(customAudioFallOff.audioSource);

            LogAssert.Expect(LogType.Error, "CustomAudioFalloff: AudioSource has not been set");
            customAudioFallOff.Start();

            Assert.False(customAudioFallOff.enabled);
        }

        [Test]
        public void Start_EnabledIfAudioValid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            LogAssert.NoUnexpectedReceived();
            customAudioFallOff.Start();

            Assert.NotNull(customAudioFallOff.audioSource);
            Assert.True(customAudioFallOff.enabled);
        }

        #endregion

        #region UpdateVolume

        [Test]
        public void UpdateVolume_SetsVolumeToCurveValue()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            var ute = new UdonTestUtils.UdonTestEnvironment();
            var player = ute.CreatePlayer();

            player.gameObject.transform.position = Vector3.forward * 12.5f;

            customAudioFallOff.UpdateVolume(player);

            var headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            float distance = Vector3.Distance(customAudioFallOff.transform.position, headPosition);
            Assert.True(
                Mathf.Abs(customAudioFallOff.customFallOff.Evaluate(distance) - customAudioFallOff.audioSource.volume) <
                0.001f
            );
        }

        [Test]
        public void UpdateVolume_DoesNothingWhenPlayerInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            Assert.DoesNotThrow(() => customAudioFallOff.UpdateVolume(null));
        }

        #endregion

        #region PostLateUpdate

        [Test]
        public void PostLateUpdate_SetsVolumeToCurveValue()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            var ute = new UdonTestUtils.UdonTestEnvironment();
            var _ = ute.CreatePlayer();

            Assert.DoesNotThrow(() => customAudioFallOff.PostLateUpdate());
        }

        [Test]
        public void PostLateUpdate_DoesNothingWhenPlayerInvalid()
        {
            var customAudioFallOff = new GameObject().AddComponent<CustomAudioFalloff>();
            customAudioFallOff.audioSource = customAudioFallOff.gameObject.AddComponent<AudioSource>();

            UdonTestUtils.UdonTestEnvironment.ResetApiBindings();

            Assert.False(Utilities.IsValid(Networking.LocalPlayer));
            Assert.DoesNotThrow(() => customAudioFallOff.PostLateUpdate());
        }

        #endregion
    }
}