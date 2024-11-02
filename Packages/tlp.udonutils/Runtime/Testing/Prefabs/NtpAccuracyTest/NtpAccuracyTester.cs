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
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestGameTimeVsDeltaTime.ExecutionOrder + 1;

        public TimeSource TimeSource;
        public UtcTimeSource UtcTimeSource;

        [UdonSynced]
        public double TesterDelta;

        protected override void InitializeTest() {
            if (!Utilities.IsValid(TimeSource)) {
                Error("TimeSource is not set");
                TestController.TestInitialized(false);
                return;
            }

            if (!Utilities.IsValid(UtcTimeSource)) {
                Error($"{nameof(UtcTimeSource)} is not set");
                TestController.TestInitialized(false);
                return;
            }

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                Error("Two players required");
                TestController.TestInitialized(false);
                return;
            }

            TestController.TestInitialized(true);
        }

        [UdonSynced]
        private bool _pendingResponse;

        private double _workingTesterDelta;


        protected override void RunTest() {
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = true;
            Trigger(0.0);
            MarkNetworkDirty();
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(Delayed_Timeout), 5f);
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

                SendCustomEventDelayedSeconds(nameof(Delayed_Response), 1f);
            } else if (Status == TestCaseStatus.Running) {
                // player who started the test
                Info("Received response from other player");
                Trigger(TesterDelta);
            }
        }

        public void Delayed_Response() {
            // other player
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = false;
            Trigger(_workingTesterDelta);
            Info($"Received delta = {_workingTesterDelta:F9}s");
            Info("Responding to tester");
            MarkNetworkDirty();
            RequestSerialization();
        }

        public void Delayed_Timeout() {
            if (Status == TestCaseStatus.Running) {
                Error("Test timed out");
                TestController.TestCompleted(false);
            }
        }


        public void Trigger(double testerDelta) {
            double serverTime = TimeSource.TimeAsDouble();
            double currentTimeUtc = UtcTimeSource.TimeAsDouble();

            TesterDelta = currentTimeUtc - serverTime;
            Info($"currentTimeUtc: {currentTimeUtc:F9}s");
            Info($"TimeSource time: {serverTime:F9}s");
            Info($"Delta: {TesterDelta:F9}s");
            if (testerDelta > 0) {
                Info($"Delta of player who started the test: {testerDelta:F9}");
                Info($"Difference: {TesterDelta - testerDelta:F9}");

                if (!Utilities.IsValid(TestController)) {
                    return;
                }

                if (Status == TestCaseStatus.Running) {
                    if (Math.Abs(TesterDelta - testerDelta) > 3e-3) {
                        Error("Delta between players was more than 3ms");
                        TestController.TestCompleted(false);
                    } else {
                        Info("Delta was less than 3ms");
                        TestController.TestCompleted(true);
                    }
                }
            }
        }

        protected override void CleanUpTest() {
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _pendingResponse = false;
            TesterDelta = 0;
            MarkNetworkDirty();
            RequestSerialization();
            TestController.TestCleanedUp(true);
        }
    }
}