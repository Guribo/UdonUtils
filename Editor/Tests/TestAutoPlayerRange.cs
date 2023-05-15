using NUnit.Framework;
using TLP.UdonUtils.Runtime.Rendering;
using TLP.UdonUtils.Tests.Runtime.Utils;
using TLP.UdonVoiceUtils.Runtime.Examples;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestAutoPlayerRange
    {
        private AutoPlayerRange _autoPlayerRange;
        private AnimationCurve _playerRangeMapping;
        private const float DefaultRange = 25f;
        private UdonTestUtils.UdonTestEnvironment _udonTestEnvironment;


        [SetUp]
        public void Prepare()
        {
            _autoPlayerRange = new GameObject().AddComponent<AutoPlayerRange>();
            _playerRangeMapping = AnimationCurve.Linear(1, 25, 80, 10);
            _udonTestEnvironment = new UdonTestUtils.UdonTestEnvironment();
        }

        [TearDown]
        public void Cleanup()
        {
            _udonTestEnvironment.Deconstruct();
            _udonTestEnvironment = null;
        }

        #region OnEnable

        private static bool UpdatePlayerVoiceRangeInvoked;
        private static bool UpdatePlayerVoiceRangeParameter;

        public static bool UpdatePlayerVoiceRangeCheck(bool reset)
        {
            UpdatePlayerVoiceRangeInvoked = true;
            UpdatePlayerVoiceRangeParameter = reset;
            return false;
        }

        [Test]
        public void OnEnable_UpdatePlayerVoiceRanges()
        {
            UpdatePlayerVoiceRangeInvoked = false;
            UpdatePlayerVoiceRangeParameter = false;
            ReflectionUtils.PatchMethod(
                typeof(AutoPlayerRange),
                nameof(AutoPlayerRange.UpdatePlayerVoiceRange),
                GetType(),
                nameof(UpdatePlayerVoiceRangeCheck),
                (harmony) => { _autoPlayerRange.OnEnable(); }
            );
            Assert.True(UpdatePlayerVoiceRangeInvoked);
            Assert.False(UpdatePlayerVoiceRangeParameter);
        }

        #endregion


        #region OnDisable

        [Test]
        public void OnDisable_ResetsAllPlayerVoiceRanges()
        {
            UpdatePlayerVoiceRangeInvoked = false;
            UpdatePlayerVoiceRangeParameter = false;
            ReflectionUtils.PatchMethod(
                typeof(AutoPlayerRange),
                nameof(AutoPlayerRange.UpdatePlayerVoiceRange),
                GetType(),
                nameof(UpdatePlayerVoiceRangeCheck),
                (harmony) => { _autoPlayerRange.OnDisable(); }
            );
            Assert.True(UpdatePlayerVoiceRangeInvoked);
            Assert.True(UpdatePlayerVoiceRangeParameter);
        }

        [Test]
        public void Disable_ResetsRange()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            _autoPlayerRange.OnPlayerJoined(_udonTestEnvironment.CreatePlayer());

            VerifyVoiceRange();

            _autoPlayerRange.OnDisable();

            VerifyVoiceRange(true);

            _autoPlayerRange.OnEnable();
            VerifyVoiceRange();
        }

        #endregion

        #region OnPlayerJoined

        [Test]
        public void OnPlayerJoined_invalid()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerJoined(null));

            VerifyVoiceRange();
        }

        [Test]
        public void OnPlayerJoined_ValidPlayer()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerJoined(_udonTestEnvironment.CreatePlayer()));

            VerifyVoiceRange();
        }

        #endregion

        #region OnPlayerLeft

        [Test]
        public void OnPlayerLeft_invalid()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerLeft(null));

            VerifyVoiceRange();
        }

        [Test]
        public void OnPlayerLeft_ValidPlayer()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            Assert.DoesNotThrow(() => _autoPlayerRange.OnPlayerLeft(new VRCPlayerApi()));

            VerifyVoiceRange();
        }

        #endregion

        #region UpdatePlayerVoiceRange

        [Test]
        public void UpdatePlayerVoiceRange()
        {
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();
            _udonTestEnvironment.CreatePlayer();

            _autoPlayerRange.UpdatePlayerVoiceRange(false);

            VerifyVoiceRange();
        }

        #endregion

        #region GetRange

        [Test]
        public void GetRange_InvalidCurveInvalidCount()
        {
            Assert.AreEqual(DefaultRange, _autoPlayerRange.GetRange(null, 0));
        }

        [Test]
        public void GetRange_ValidCurveInvalidCount()
        {
            Assert.AreEqual(_playerRangeMapping.Evaluate(1), _autoPlayerRange.GetRange(_playerRangeMapping, 0));
            Assert.AreEqual(_playerRangeMapping.Evaluate(80), _autoPlayerRange.GetRange(_playerRangeMapping, 81));
        }

        [Test]
        public void GetRange_ValidCurveValidCount()
        {
            Assert.AreEqual(_playerRangeMapping.Evaluate(40), _autoPlayerRange.GetRange(_playerRangeMapping, 40));
        }

        #endregion

        #region UpdateVoiceRange

        [Test]
        public void UpdateVoiceRange_invalidPlayers()
        {
            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(null, 0));
        }

        [Test]
        public void UpdateVoiceRange_ValidPlayers()
        {
            var players = new[]
            {
                _udonTestEnvironment.CreatePlayer(),
                _udonTestEnvironment.CreatePlayer(),
                _udonTestEnvironment.CreatePlayer()
            };

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 0));
            foreach (var playerApi in players)
            {
                Assert.AreEqual(0, _udonTestEnvironment.GetPlayerData(playerApi).VoiceRangeFar);
            }

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 7));
            foreach (var playerApi in players)
            {
                Assert.AreEqual(7, _udonTestEnvironment.GetPlayerData(playerApi).VoiceRangeFar);
            }
        }

        [Test]
        public void UpdateVoiceRange_PartiallyValidPlayers()
        {
            var players = new[]
            {
                _udonTestEnvironment.CreatePlayer(),
                null,
                _udonTestEnvironment.CreatePlayer()
            };

            Assert.DoesNotThrow(() => _autoPlayerRange.UpdateVoiceRange(players, 5));
            Assert.AreEqual(5, _udonTestEnvironment.GetPlayerData(players[0]).VoiceRangeFar);
            Assert.Null(players[1]);
            Assert.AreEqual(5, _udonTestEnvironment.GetPlayerData(players[2]).VoiceRangeFar);
        }

        #endregion

        private void VerifyVoiceRange(bool expectDefault = false)
        {
            foreach (var vrcPlayerApi in VRCPlayerApi.AllPlayers)
            {
                if (expectDefault)
                {
                    Assert.AreEqual(DefaultRange, _udonTestEnvironment.GetPlayerData(vrcPlayerApi).VoiceRangeFar);
                    continue;
                }

                Assert.AreNotEqual(DefaultRange, _udonTestEnvironment.GetPlayerData(vrcPlayerApi).VoiceRangeFar);
                Assert.AreEqual(
                    _playerRangeMapping.Evaluate(VRCPlayerApi.AllPlayers.Count),
                    _udonTestEnvironment.GetPlayerData(vrcPlayerApi).VoiceRangeFar
                );
            }
        }
    }
}