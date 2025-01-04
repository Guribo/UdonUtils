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
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpServer.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        public NtpClient NtpClient;
        public NtpServer NtpServer;
        #endregion

        #region Overrides
        #region Public
        /// <returns>Returns the <see cref="NtpClient"/> time,
        /// returns float.MinValue when initialization failed</returns>
        public override float Time() {
            if (Utilities.IsValid(NtpClient)) {
                return NtpClient.GetAdjustedLocalTime();
            }

            Warn($"{nameof(Time)}: {nameof(NtpClient)} not yet set (player objects not yet loaded)");
            return float.MinValue;
        }

        /// <returns><see cref="NtpTime"/> only provides float accuracy,
        /// returns float.MinValue when initialization failed</returns>
        public override double TimeAsDouble() {
            if (Utilities.IsValid(NtpClient)) {
                return NtpClient.GetAdjustedLocalTime();
            }

            Warn($"{nameof(TimeAsDouble)}: {nameof(NtpClient)} not yet set (player objects not yet loaded)");
            return float.MinValue;
        }
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(NtpServer)) {
                Error($"{nameof(NtpServer)} not set");
                return false;
            }

            if (!Utilities.IsValid(NtpClient)) {
                Error($"{nameof(NtpClient)} not set");
                return false;
            }

            return true;
        }
        #endregion
    }
}