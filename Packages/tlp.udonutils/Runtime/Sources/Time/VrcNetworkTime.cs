using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
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
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = UnscaledTimeSinceGameStart.ExecutionOrder + 1;


        protected override bool _SetupAndValidate() {
            if (!base._SetupAndValidate()) {
                return false;
            }

            string existingInstance = Networking.LocalPlayer._GetPlayerTagSafe(nameof(VrcNetworkTime));
            string idString = GetInstanceID().ToString();

            if (!string.IsNullOrEmpty(existingInstance) && existingInstance != idString) {
                _ErrorAndDisableGameObject(
                        $"Another instance of {nameof(VrcNetworkTime)} already exists: {idString} (this) != {existingInstance} (other)");
                return false;
            }

            Networking.LocalPlayer._SetPlayerTagSafe(nameof(VrcNetworkTime), idString);
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
        public override float _Time() {
            return (float)LatestTime;
        }

        /// <returns><see cref="Networking.GetServerTimeInSeconds"/></returns>
        public override double _TimeAsDouble() {
            return LatestTime;
        }
    }
}