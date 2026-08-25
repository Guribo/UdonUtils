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

        protected override void _InitializeTest() {
            if (!PlayerDataRestoredEvent._IsPlayerDataRestored(Networking.LocalPlayer)) {
                _Error($"{nameof(_InitializeTest)}: Player data was not restored yet");
                TestController._TestInitialized(false);
                return;
            }

            if (PlayerData.TryGetBytes(Networking.LocalPlayer, _dataKey, out var data)) {
                _DebugLog($"Read {data._LengthSafe()} bytes of persistent player data");
            }

            PlayerData.SetBytes(_dataKey, new byte[0]);
            TestController._TestInitialized(true);
        }


        protected override void _RunTest() {
            for (_bytes = 0; _bytes <= VrcLimitBytes; _bytes += 1_000) {
                PlayerData.SetBytes(_dataKey, new byte[_bytes]);
                var data = PlayerData.GetBytes(Networking.LocalPlayer, _dataKey);

                // verify saving the player data succeeded up to the limit
                if (data._LengthSafe() != _bytes) {
                    _Error(
                            $"{nameof(_RunTest)}: Failed to store {_bytes} bytes of persistent player data, was {data._LengthSafe()} bytes");
                    TestController._TestCompleted(false);
                    return;
                }
            }
        }

        protected override void _CleanUpTest() {
            PlayerData.SetBytes(_dataKey, new byte[0]);
            base._CleanUpTest();
        }

        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos) {
            _Info(
                    $"{nameof(OnPlayerDataUpdated)}: {player._DisplayNameSafe()}({player._PlayerIdSafe()}) : {infos._LengthSafe()} infos received");
            base.OnPlayerDataUpdated(player, infos);

            if (Status != TestCaseStatus.Running || _bytes < VrcLimitBytes) {
                return;
            }

            if (!PlayerData.TryGetBytes(Networking.LocalPlayer, _dataKey, out var data)) {
                _Error($"Failed to retrieve persistent player data");
                TestController._TestCompleted(false);
                return;
            }

            if (data._LengthSafe() != VrcLimitBytes) {
                _Error(
                        $"{nameof(OnPlayerDataUpdated)}: Failed to store {VrcLimitBytes} bytes of persistent player data, was {data._LengthSafe()} bytes");
                TestController._TestCompleted(false);
                return;
            }

            _Info($"{data._LengthSafe()} of persistent player data was successfully saved globally");
            TestController._TestCompleted(true);
        }

        public override void OnPlayerRestored(VRCPlayerApi player) {
            _Info($"{nameof(OnPlayerRestored)}: {player._DisplayNameSafe()}({player._PlayerIdSafe()})");
            base.OnPlayerRestored(player);

            if (!player._IsLocalSafe()) return;
        }
    }
}