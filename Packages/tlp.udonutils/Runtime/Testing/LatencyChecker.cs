using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources.Time;
using TLP.UdonUtils.Runtime.Sources.Time.Experimental;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Testing
{
    /// <summary>
    /// Small debug script to display the "true" latency between two players in game time.
    /// Should only be used with two players, otherwise it is not clear which two players are used.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(LatencyChecker), ExecutionOrder)]
    public class LatencyChecker : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpTime.ExecutionOrder + 1;

        public TextMeshProUGUI Text;
        private TlpNetworkTime _networkTime;

        #region Configuration
        [Header("Configuration")]
        [Tooltip("If set to true, manual sync is used instead of RPCs")]
        [SerializeField]
        private bool UseManualSync;

        [SerializeField]
        private float SendInterval = 1 / 8f;
        #endregion

        #region Synced
        [UdonSynced]
        private double _time;

        [UdonSynced]
        private double _requestTime;
        #endregion

        #region State
        private float _movingAverageDelta;
        #endregion

        #region RPCs
        [NetworkCallable(100)]
        public void RPC_Ping(double requestTime) {
            double localTime = _networkTime.TimeAsDouble();
            double delta2 = localTime - requestTime;
            UpdateMovingAverageDelta(delta2);
            Text.text =
                    $"local: {localTime:F6}s\nrequested: {requestTime:F6}\nreceived: {localTime:F6}s\ndelta requested:{delta2:F6}s\ndelta: {delta2:F6}s\nsmoothed delta: {_movingAverageDelta:F6}";
        }

        private void UpdateMovingAverageDelta(double delta2) {
            if (_movingAverageDelta == 0) _movingAverageDelta = (float)delta2;
            if (Mathf.Abs(_movingAverageDelta - (float)delta2) > 0.25f) {
                _movingAverageDelta = (float)delta2;
                return;
            }

            _movingAverageDelta = Mathf.Lerp(_movingAverageDelta, (float)delta2, 0.05f);
        }
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            _networkTime = TlpNetworkTime.GetInstance();
            if (!Utilities.IsValid(_networkTime)) {
                ErrorAndDisableGameObject($"{nameof(_networkTime)} is not set");
                return false;
            }

            SendCustomEventDelayedSeconds(nameof(Delayed_Refresh), SendInterval);

            return true;
        }

        public void Delayed_Refresh() {
            if (Networking.IsOwner(gameObject)) {
                if (UseManualSync) {
                    _requestTime = _networkTime.TimeAsDouble();
                    RequestSerialization();
                } else {
                    if (NetworkCalling.GetQueuedEvents((IUdonEventReceiver)this, nameof(RPC_Ping)) == 0) {
                        SendCustomNetworkEvent(
                                NetworkEventTarget.Others,
                                nameof(RPC_Ping),
                                _networkTime.TimeAsDouble());
                    }
                }
            }

            SendCustomEventDelayedSeconds(nameof(Delayed_Refresh), SendInterval);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();
            _time = _networkTime.TimeAsDouble();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            double delta = _networkTime.TimeAsDouble() - _time;
            double delta2 = _networkTime.TimeAsDouble() - _requestTime;
            UpdateMovingAverageDelta(delta);
            Text.text =
                    $"local: {_networkTime.TimeAsDouble():F6}s\nrequested: {_requestTime:F6}\nreceived: {_time:F6}s\ndelta requested:{delta2:F6}s\ndelta: {delta:F6}s\nsmoothed delta: {_movingAverageDelta:F6}";
        }
    }
}