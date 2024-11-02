using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources.Time.Experimental;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// using <see cref="Networking.GetServerTimeInSeconds"/>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VrcNetworkTime), ExecutionOrder)]
    public class VrcNetworkTime : TimeSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TimeSinceLevelLoad.ExecutionOrder + 1;


        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            string existingInstance = Networking.LocalPlayer.GetPlayerTag(nameof(VrcNetworkTime));
            string idString = GetInstanceID().ToString();

            if (!string.IsNullOrEmpty(existingInstance) && existingInstance != idString) {
                ErrorAndDisableGameObject($"Another instance of {nameof(VrcNetworkTime)} already exists: {idString} (this) != {existingInstance} (other)");
                return false;
            }
            Networking.LocalPlayer.SetPlayerTag(nameof(VrcNetworkTime), idString);
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