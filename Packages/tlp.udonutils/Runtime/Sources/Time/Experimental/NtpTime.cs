using TLP.UdonUtils.Runtime.Sync.Experimental;
using TLP.UdonUtils.Runtime.Sources;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sources.Time.Experimental
{
    /// <summary>
    /// <see cref="TimeSource"/> that synchronizes its time with the current instance master
    /// using the NTP algorithm
    /// </summary>
    public class NtpTime : TimeSource
    {
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