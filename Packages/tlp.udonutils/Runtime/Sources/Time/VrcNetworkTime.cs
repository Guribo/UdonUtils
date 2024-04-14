using TLP.UdonUtils.Common;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// using <see cref="Networking.GetServerTimeInSeconds"/>
    /// </summary>
    public class VrcNetworkTime : TimeSource
    {
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            string existingInstance = Networking.LocalPlayer.GetPlayerTag(nameof(VrcNetworkTime));
            if (!string.IsNullOrEmpty(existingInstance) && existingInstance != this.GetScriptPathInScene()) {
                ErrorAndDisableGameObject($"Another instance of {nameof(VrcNetworkTime)} already exists: {existingInstance}");
                return false;
            }
            Networking.LocalPlayer.SetPlayerTag(nameof(VrcNetworkTime), this.GetScriptPathInScene());
            return true;
        }

        private double LatestTime
        {
            get
            {
                if (_lastUpdated == UnityEngine.Time.frameCount) return _latestTime;
                _lastUpdated = UnityEngine.Time.frameCount;
                _latestTime = Networking.GetServerTimeInSeconds();
                return _latestTime;
            }
        }

        private int _lastUpdated;
        private double _latestTime;

        /// <returns><see cref="Networking.GetServerTimeInSeconds"/></returns>
        public override float Time() {
            return (float)LatestTime;
        }

        /// <returns><see cref="Networking.GetServerTimeInSeconds"/></returns>
        public override double TimeAsDouble() {
            return LatestTime;
        }
    }
}