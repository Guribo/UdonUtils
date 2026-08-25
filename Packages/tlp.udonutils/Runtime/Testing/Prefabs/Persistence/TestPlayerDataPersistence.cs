using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Testing
{
    /// <summary>
    /// Tests that PlayerData is restored successfully and not empty/initialized to zero.
    ///
    /// <remarks>May fail if run on the very first test as it hasn't written anything to PlayerData yet.
    /// Rejoin the world and try again.</remarks>
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestPlayerDataPersistence), ExecutionOrder)]
    public class TestPlayerDataPersistence : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestMaxSendRateSender.ExecutionOrder + 1;

        #region PlayerDataKeys
        private readonly string _dataKey = $"TLP/PersistenceTest/counter";
        #endregion

        #region State
        private int _restoredValue;
        #endregion

        protected override void _InitializeTest() {
            if (!PlayerDataRestoredEvent._IsPlayerDataRestored(Networking.LocalPlayer)) {
                _Error($"{nameof(_InitializeTest)}: Player data was not restored yet");
                TestController._TestInitialized(false);
                return;
            }

            TestController._TestInitialized(true);
        }

        protected override void _RunTest() {
            _Info($"{nameof(_RunTest)}: Restored value for key '{_dataKey}' is {_restoredValue}");
            if (_restoredValue <= 0) {
                _Error(
                        "Expected to recover a value > 0 to be stored in the world (please rejoin the world and rerun the test, if it fails again file a bug report please)");
                TestController._TestCompleted(false);
                return;
            }

            TestController._TestCompleted(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            _Info($"{nameof(OnPlayerRestored)}: {player._DisplayNameSafe()}({player._PlayerIdSafe()})");
            base.OnPlayerRestored(player);
            if (!player._IsLocalSafe()) return;

            bool readSucceeded = PlayerData.TryGetInt(Networking.LocalPlayer, _dataKey, out _restoredValue);
            PlayerData.SetInt(_dataKey, Mathf.Max(1, _restoredValue + 1));
            if (Status != TestCaseStatus.Running) {
                return;
            }

            if (!readSucceeded || _restoredValue <= 0) {
                _Error($"Failed to retrieve persistent player data for key '{_dataKey}'");
            } else {
                _Info($"{nameof(OnPlayerRestored)}: Restored value for for key '{_dataKey}' is {_restoredValue}");
            }

            TestController._TestCompleted(readSucceeded && _restoredValue > 0);
        }
    }
}