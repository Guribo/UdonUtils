using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Testing
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestPlayerData), ExecutionOrder)]
    public class TestPlayerData : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestPlayerDataPersistence.ExecutionOrder + 1;


        #region PlayerDataKeys
        private readonly string _dataKey = $"TLP/PersistenceTest/data";
        private int _bytes;
        private const int VrcLimitBytes = 100_000;
        #endregion

        protected override void InitializeTest() {
            if (!PlayerDataRestoredEvent.IsPlayerDataRestored(Networking.LocalPlayer)) {
                Error($"{nameof(InitializeTest)}: Player data was not restored yet");
                TestController.TestInitialized(false);
                return;
            }

            if (PlayerData.TryGetBytes(Networking.LocalPlayer, _dataKey, out var data)) {
                DebugLog($"Read {data.LengthSafe()} bytes of persistent player data");
            }

            PlayerData.SetBytes(_dataKey, new byte[0]);
            TestController.TestInitialized(true);
        }


        protected override void RunTest() {
            for (_bytes = 0; _bytes <= VrcLimitBytes; _bytes += 1_000) {
                PlayerData.SetBytes(_dataKey, new byte[_bytes]);
                var data = PlayerData.GetBytes(Networking.LocalPlayer, _dataKey);

                // verify saving the player data succeeded up to the limit
                if (data.LengthSafe() != _bytes) {
                    Error(
                            $"{nameof(RunTest)}: Failed to store {_bytes} bytes of persistent player data, was {data.LengthSafe()} bytes");
                    TestController.TestCompleted(false);
                    return;
                }
            }
        }

        protected override void CleanUpTest() {
            PlayerData.SetBytes(_dataKey, new byte[0]);
            base.CleanUpTest();
        }

        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos) {
            Info(
                    $"{nameof(OnPlayerDataUpdated)}: {player.DisplayNameSafe()}({player.PlayerIdSafe()}) : {infos.LengthSafe()} infos received");
            base.OnPlayerDataUpdated(player, infos);

            if (Status != TestCaseStatus.Running || _bytes < VrcLimitBytes) {
                return;
            }

            if (!PlayerData.TryGetBytes(Networking.LocalPlayer, _dataKey, out var data)) {
                Error($"Failed to retrieve persistent player data");
                TestController.TestCompleted(false);
                return;
            }

            if (data.LengthSafe() != VrcLimitBytes) {
                Error(
                        $"{nameof(OnPlayerDataUpdated)}: Failed to store {VrcLimitBytes} bytes of persistent player data, was {data.LengthSafe()} bytes");
                TestController.TestCompleted(false);
                return;
            }

            Info($"{data.LengthSafe()} of persistent player data was successfully saved globally");
            TestController.TestCompleted(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            Info($"{nameof(OnPlayerRestored)}: {player.DisplayNameSafe()}({player.PlayerIdSafe()})");
            base.OnPlayerRestored(player);

            if (!player.IsLocalSafe()) return;
        }
    }
}