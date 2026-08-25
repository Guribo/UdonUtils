using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestMaxSendRate), ExecutionOrder)]
    public class TestMaxSendRate : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpAccuracyTester.ExecutionOrder + 1;

        #region Settings
        [Range(1, 100)]
        public int TargetSendRate = 30;

        [Range(1, 120)]
        public float SendDuration = 20f;

        public float AllowedDeviation = 0.05f;
        public bool AllowUnsorted;
        #endregion

        #region Dependencies
        public TestMaxSendRateSender[] TestMaxSendRateSender;
        #endregion

        #region TestState
        private VRCPlayerApi _otherPlayer;
        private float _nextSendTime = float.MinValue;
        private float _sendDeltaTime;
        private float _lastSendTime = float.MinValue;
        private int _senderIndex;
        private int _totalValidReceived;
        private int _totalOutOfOrderReceived;
        private double _lastReceived = double.MinValue;
        #endregion

        #region TestCase
        protected override void _InitializeTest() {
            if (TestMaxSendRateSender == null || TestMaxSendRateSender.Length < 1) {
                _Error("Test requires at least 1 sender");
                TestController._TestInitialized(false);
                return;
            }

            foreach (var testMaxSendRateSender in TestMaxSendRateSender) {
                if (!testMaxSendRateSender) {
                    _Error($"Invalid sender in {nameof(TestMaxSendRateSender)}");
                    TestController._TestInitialized(false);
                    return;
                }

                if (testMaxSendRateSender.transform.parent == transform) {
                    TestMaxSendRateSender[_senderIndex].SendTime = 0;
                    continue;
                }

                _Error(
                        $"Sender {testMaxSendRateSender._GetComponentPathInScene()} must be a child of {gameObject.transform._GetPathInScene()}");
                TestController._TestInitialized(false);
                return;
            }

            if (VRCPlayerApi.GetPlayerCount() != 2) {
                _Error("Test requires 2 players");
                TestController._TestInitialized(false);
                return;
            }

            var players = new VRCPlayerApi[2];
            VRCPlayerApi.GetPlayers(players);
            _otherPlayer = players[0].isLocal ? players[1] : players[0];

            Networking.SetOwner(_otherPlayer, gameObject);
            foreach (var testMaxSendRateSender in TestMaxSendRateSender) {
                Networking.SetOwner(_otherPlayer, testMaxSendRateSender.gameObject);
            }

            TestController._TestInitialized(true);
        }

        protected override void _RunTest() {
            _totalValidReceived = 0;
            _totalOutOfOrderReceived = 0;
            SendCustomEventDelayedSeconds(nameof(_Delayed_RequestStartSending), 3);
        }

        protected override void _CleanUpTest() {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            foreach (var testMaxSendRateSender in TestMaxSendRateSender) {
                Networking.SetOwner(Networking.LocalPlayer, testMaxSendRateSender.gameObject);
            }

            TestController._TestCleanedUp(true);
        }
        #endregion

        #region Delayed Events
        public void _Delayed_RequestStartSending() {
            if (Status != TestCaseStatus.Running) return;

            if (!Utilities.IsValid(TestController)) {
                Debug.LogError($"'{nameof(TestController)}' is no longer valid");
                return;
            }

            if (!Utilities.IsValid(gameObject)) {
                Debug.LogError("'GameObject' is no longer valid");
                TestController._TestCompleted(false);
                return;
            }

            if (Networking.IsOwner(gameObject)) {
                _Error("Local player should not be owner anymore");
                TestController._TestCompleted(false);
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_RPC_StartSending));
            SendCustomEventDelayedSeconds(nameof(_Delayed_ExpectTestFinished), SendDuration + 5f);
        }

        public void _Delayed_ExpectTestFinished() {
            if (Status != TestCaseStatus.Running) return;
            if (!Utilities.IsValid(TestController)) {
                Debug.LogError($"'{nameof(TestController)}' is no longer valid");
                return;
            }

            if (!Utilities.IsValid(gameObject)) {
                Debug.LogError("'GameObject' is no longer valid");
                TestController._TestInitialized(false);
                return;
            }

            _Info(
                    $"Received {_totalValidReceived}/{Mathf.RoundToInt(TargetSendRate * SendDuration)} updates, {_totalOutOfOrderReceived} out of expected order");

            if (Mathf.Abs(_totalValidReceived - Mathf.RoundToInt(TargetSendRate * SendDuration)) <
                AllowedDeviation * TargetSendRate * SendDuration) {
                TestController._TestCompleted(true);
                return;
            }

            _Error(
                    $"Expected {Mathf.RoundToInt(TargetSendRate * SendDuration)} updates, " +
                    $"got {_totalValidReceived} valid updates, " +
                    $"{_totalOutOfOrderReceived} updates were out of expected order " +
                    $"({100f - 100f * _totalValidReceived / (TargetSendRate * SendDuration):F3}% deviation, " +
                    $"allowed was {AllowedDeviation * 100:F3}%)");
            TestController._TestCompleted(false);
        }
        #endregion

        #region RPCs
        [NetworkCallable(1)]
        public void _RPC_StartSending() {
            _sendDeltaTime = 1f / TargetSendRate;
            _nextSendTime = Time.time + _sendDeltaTime;
            _lastSendTime = Time.time + SendDuration;
        }
        #endregion

        #region U# Lifecycle
        internal void Update() {
            if (Time.time > _lastSendTime || Time.time < _nextSendTime) return;

            TestMaxSendRateSender[_senderIndex].SendTime = _totalValidReceived;
            TestMaxSendRateSender[_senderIndex].RequestSerialization();
            _senderIndex._MoveIndexRightLooping(TestMaxSendRateSender.Length);
            _nextSendTime += _sendDeltaTime;
        }
        #endregion


        public void _ReceivedData(TestMaxSendRateSender sender) {
            if (Status != TestCaseStatus.Running) return;
            if (Networking.IsOwner(gameObject)) return;
            double sendTime = sender.SendTime;
            if (sendTime < _lastReceived) {
                _Warn(
                        $"Received outdated packet from {sender._GetComponentPathInScene()}: {sendTime:F6} < {_lastReceived:F6}");
                _totalOutOfOrderReceived++;
                if (!AllowUnsorted) return;
            }

            _lastReceived = sendTime;
            _totalValidReceived++;
        }
    }
}