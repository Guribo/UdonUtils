using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sync.Experimental;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sources.Time.Experimental
{
    /// <summary>
    /// <see cref="TimeSource"/> that synchronizes its time with the current instance master
    /// using the NTP algorithm
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NtpTime), ExecutionOrder)]
    public class NtpTime : TimeSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpServer.ExecutionOrder + 1;

        public NtpClient NtpClient;
        public NtpServer NtpServer;

        public override float Time() {
            return (float) TimeAsDouble();
        }

        /// <returns><see cref="NtpTime"/> only provides float accuracy</returns>
        public override double TimeAsDouble() {
            if (!Utilities.IsValid(NtpClient)) {
                return float.MinValue;
            }

            return NtpClient.GetAdjustedLocalTime();
        }

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (Utilities.IsValid(NtpServer)) {
                return true;
            }

            Error($"{nameof(NtpServer)} not set");
            return false;
        }
    }
}