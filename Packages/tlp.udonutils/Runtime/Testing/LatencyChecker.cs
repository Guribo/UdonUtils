using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources.Time;
using TLP.UdonUtils.Runtime.Sources.Time.Experimental;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

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

        [UdonSynced]
        private double _time;

        [UdonSynced]
        private double _requestTime;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            _networkTime = TlpNetworkTime.GetInstance();
            if (!Utilities.IsValid(_networkTime)) {
                ErrorAndDisableGameObject($"{nameof(_networkTime)} is not set");
                return false;
            }

            SendCustomEventDelayedSeconds(nameof(Refresh), 1);

            return true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (Networking.IsOwner(gameObject))
                SendCustomEventDelayedSeconds(nameof(Refresh), 1);
        }

        public void Refresh() {
            if (!Networking.IsOwner(gameObject)) return;

            _requestTime = _networkTime.TimeAsDouble();
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(Refresh), 1);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();
            _time = _networkTime.TimeAsDouble();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            double delta = _networkTime.TimeAsDouble() - _time;
            double delta2 = _networkTime.TimeAsDouble() - _time;
            Text.text =
                    $"local: {_networkTime.TimeAsDouble():F6}s\nrequested: {_requestTime:F6}\nreceived: {_time:F6}s\ndelta requested:{delta2:F6}s\ndelta: {delta:F6}s";
        }
    }
}