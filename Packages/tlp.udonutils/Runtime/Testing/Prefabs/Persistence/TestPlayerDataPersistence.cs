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

        protected override void InitializeTest() {
            if (!PlayerDataRestoredEvent.IsPlayerDataRestored(Networking.LocalPlayer)) {
                Error($"{nameof(InitializeTest)}: Player data was not restored yet");
                TestController.TestInitialized(false);
                return;
            }

            TestController.TestInitialized(true);
        }

        protected override void RunTest() {
            Info($"{nameof(RunTest)}: Restored value for key '{_dataKey}' is {_restoredValue}");
            if (_restoredValue <= 0) {
                Error(
                        "Expected to recover a value > 0 to be stored in the world (please rejoin the world and rerun the test, if it fails again file a bug report please)");
                TestController.TestCompleted(false);
                return;
            }

            TestController.TestCompleted(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            Info($"{nameof(OnPlayerRestored)}: {player.DisplayNameSafe()}({player.PlayerIdSafe()})");
            base.OnPlayerRestored(player);
            if (!player.IsLocalSafe()) return;

            bool readSucceeded = PlayerData.TryGetInt(Networking.LocalPlayer, _dataKey, out _restoredValue);
            PlayerData.SetInt(_dataKey, Mathf.Max(1, _restoredValue + 1));
            if (Status != TestCaseStatus.Running) {
                return;
            }

            if (!readSucceeded || _restoredValue <= 0) {
                Error($"Failed to retrieve persistent player data for key '{_dataKey}'");
            } else {
                Info($"{nameof(OnPlayerRestored)}: Restored value for for key '{_dataKey}' is {_restoredValue}");
            }

            TestController.TestCompleted(readSucceeded && _restoredValue > 0);
        }
    }
}