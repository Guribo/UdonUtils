using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Testing
{
    /// <summary>
    /// Test that the NTP time is the same on two clients and differs at most by 3ms.
    /// Requires both players to be running on the same PC so that the UTC time is the same!!!
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NtpAccuracyTester), ExecutionOrder)]
    public class NtpAccuracyTester : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestGameTimeVsDeltaTime.ExecutionOrder + 1;

        public TimeSource TimeSource;
        public UtcTimeSource UtcTimeSource;

        [UdonSynced]
        public double TesterDelta;

        protected override void _InitializeTest() {
            if (!Utilities.IsValid(TimeSource)) {
                _Error("TimeSource is not set");
                TestController._TestInitialized(false);
                return;
            }

            if (!Utilities.IsValid(UtcTimeSource)) {
                _Error($"{nameof(UtcTimeSource)} is not set");
                TestController._TestInitialized(false);
                return;
            }

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                _Error("Two players required");
                TestController._TestInitialized(false);
                return;
            }

            TestController._TestInitialized(true);
        }

        [UdonSynced]
        private bool _pendingResponse;

        private double _workingTesterDelta;


        protected override void _RunTest() {
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = true;
            _Trigger(0.0);
            _MarkNetworkDirty();
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(_Delayed_Timeout), 5f);
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (TesterDelta <= 0) {
                return;
            }

            _workingTesterDelta = TesterDelta;


            if (_pendingResponse) {
                if (!Networking.IsOwner(gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }

                SendCustomEventDelayedSeconds(nameof(_Delayed_Response), 1f);
            } else if (Status == TestCaseStatus.Running) {
                // player who started the test
                _Info("Received response from other player");
                _Trigger(TesterDelta);
            }
        }

        public void _Delayed_Response() {
            // other player
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = false;
            _Trigger(_workingTesterDelta);
            _Info($"Received delta = {_workingTesterDelta:F9}s");
            _Info("Responding to tester");
            _MarkNetworkDirty();
            RequestSerialization();
        }

        public void _Delayed_Timeout() {
            if (Status == TestCaseStatus.Running) {
                _Error("Test timed out");
                TestController._TestCompleted(false);
            }
        }


        public void _Trigger(double testerDelta) {
            double serverTime = TimeSource._TimeAsDouble();
            double currentTimeUtc = UtcTimeSource._TimeAsDouble();

            TesterDelta = currentTimeUtc - serverTime;
            _Info($"currentTimeUtc: {currentTimeUtc:F9}s");
            _Info($"TimeSource time: {serverTime:F9}s");
            _Info($"Delta: {TesterDelta:F9}s");
            if (testerDelta > 0) {
                _Info($"Delta of player who started the test: {testerDelta:F9}");
                _Info($"Difference: {TesterDelta - testerDelta:F9}");

                if (!Utilities.IsValid(TestController)) {
                    return;
                }

                if (Status == TestCaseStatus.Running) {
                    if (Math.Abs(TesterDelta - testerDelta) > 3e-3) {
                        _Error("Delta between players was more than 3ms");
                        TestController._TestCompleted(false);
                    } else {
                        _Info("Delta was less than 3ms");
                        TestController._TestCompleted(true);
                    }
                }
            }
        }

        protected override void _CleanUpTest() {
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = false;
            TesterDelta = 0;
            _MarkNetworkDirty();
            RequestSerialization();
            TestController._TestCleanedUp(true);
        }
    }
}