using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Logger;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/> that synchronizes network and game clocks by continuously sampling
    /// their difference, detecting drift, and applying compensation to keep them in sync over time as the scene runs.
    ///
    /// In summary it provides an averaged network time that is based on game time for smooth interpolations.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TlpNetworkTime : TimeSource
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        /// <summary>
        /// Ensure that network time is determined as early as possible after the logger.
        /// </summary>
        [PublicAPI]
        public new const int ExecutionOrder = TlpLogger.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [SerializeField]
        [Tooltip("Source of local game time, e.g. based on Time.timeSinceLevelLoad")]
        internal TimeSource GameTime;

        [SerializeField]
        [Tooltip("Source of network time, e.g. based on Networking.ServerTimeInSeconds")]
        internal TimeSource NetworkTime;

        [SerializeField]
        internal FrameCountSource FrameCount;
        #endregion

        #region Settings
        [SerializeField]
        [Range(10, 1000)]
        [Tooltip(
                "For how many frames the VRChat Network time shall be sampled before correcting for drift. "
                + "Larger values increase time stability but decrease drift compensation speed. "
                + "Value is not used when AutoAdjustSampleCount is enabled."
        )]
        internal int Samples = 256;

        [SerializeField]
        [Tooltip(
                "When enabled the number of samples is derived from "
                + "the current framerate and the SamplingDuration variable to "
                + "achieve a near constant drift compensation speed."
        )]
        internal bool AutoAdjustSampleCount;

        [SerializeField]
        [Range(1f, 10f)]
        [Tooltip(
                "Only used when AutoAdjustSampleCount is enabled. "
                + "Dictates how long drift compensation shall take independent of framerate."
        )]
        internal float SamplingDuration = 2f;

        /// <summary>
        /// Upper limit of dynamic samples to prevent taking a long time to adjust for suddenly low frame rates.
        /// </summary>
        private const int MaxDynamicSamples = 1000;
        #endregion

        #region State
        /// <summary>
        /// average difference between NetworkTime and GameTime that is currently being calculated, only ever
        /// contains the average when <see cref="Samples"/> samples have been collected/>
        /// </summary>
        private double _incompleteAverageOffset;

        /// <summary>
        /// latest completely calculated average difference between NetworkTime and GameTime
        /// </summary>
        internal double _averageOffset;

        /// <summary>
        /// previously completely calculated average difference between NetworkTime and GameTime
        /// </summary>
        private double _previousAverageOffset;

        /// <summary>
        /// delta that is applied to the <see cref="_averageOffset"/> to correct for drift over the
        /// next <see cref="Samples"/> frames
        /// </summary>
        private double _offsetDriftCompensation;

        /// <summary>
        /// correct drift at half the speed to prevent overshoot in case of offset oscillation
        /// </summary>
        private const double DriftCompensationRate = 0.5;

        /// <summary>
        /// Number of network time samples currently collected, reset every time <see cref="Samples"/> is reached.
        /// </summary>
        private int _offsetSampleCount;

        /// <summary>
        /// to prevent calculating the time multiple times for the current frame
        /// </summary>
        private int _frameLastUpdated = -1;
        #endregion

        #region U# Lifecycle
        public void OnEnable() {
            Initialize();
        }

        public void Update() {
            UpdateTime();
        }
        #endregion

        #region Public
        /// <returns>The network time based on game
        /// time with an over time calculated offset and drift compensation"</returns>
        public override float Time() {
            return (float)GetNetworkTime();
        }

        /// <returns>The network time based on game
        /// time with an over time calculated offset and drift compensation"</returns>
        public override double TimeAsDouble() {
            return GetNetworkTime();
        }


        /// <summary>
        /// Takes a time measured by the network, and converts it to the corresponding
        /// time that would be shown in the game.
        /// </summary>
        /// <param name="networkTime">Network time in seconds</param>
        /// <returns>Game time corresponding to the server time</returns>
        [PublicAPI]
        public float NetworkTimeToGameTime(float networkTime) {
            return networkTime - (float)GetGameTimeOffset();
        }

        /// <summary>
        /// Takes a time measured by the network, and converts it to the corresponding
        /// time that would be shown in the game.
        /// </summary>
        /// <param name="networkTime">Network time in seconds</param>
        /// <returns>Game time corresponding to the server time</returns>
        [PublicAPI]
        public double NetworkTimeToGameTimeAsDouble(double networkTime) {
            return networkTime - GetGameTimeOffset();
        }

        /// <summary>
        /// The difference between NetworkTime and GameTime
        /// </summary>
        /// <returns>Offset = NetworkTime - GameTime</returns>
        public double GetGameTimeOffset() {
            return _previousAverageOffset.LerpDouble(
                    _averageOffset,
                    DriftCompensationRate * _offsetSampleCount / Samples
            );
        }

        /// <summary>
        /// Current delta to the provided network time: (real network time - TLP network time)
        /// </summary>
        public double ExactError { get; private set; }
        #endregion

        #region Internal
        /// <summary>
        /// Ensures that initially the current delta between game time and network time is used and sampling is reset.
        /// </summary>
        internal void Initialize() {
            _incompleteAverageOffset = GetNetworkGameTimeDelta();
            _previousAverageOffset = _incompleteAverageOffset;
            _averageOffset = _incompleteAverageOffset;
            ResetSampling();
        }

        /// <summary>
        /// samples the frame time deltas to calculate a running average time offset between the game and network
        /// </summary>
        internal void UpdateTime() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(UpdateTime));
#endif
            #endregion

            if (UpdatedThisFrame()) {
                return;
            }

            ExactError = NetworkTime.TimeAsDouble() - (GameTime.TimeAsDouble() + GetGameTimeOffset());
            _frameLastUpdated = FrameCount.Frame();

#if TLP_DEBUG
            PrintDebugInfo();
#endif

            _incompleteAverageOffset += GetNetworkGameTimeDelta() / Samples;
            ++_offsetSampleCount;

            if (_offsetSampleCount != Samples) {
                return;
            }

            EvaluateError();
            ResetSampling();
        }

        /// <returns>The network time based on game
        /// time with an over time calculated offset and drift compensation"</returns>
        private double GetNetworkTime() {
            // ensure that every call to this method returns the same value throughout the entire frame
            if (!UpdatedThisFrame()) {
                UpdateTime();
            }

            return GameTime.TimeAsDouble() + GetGameTimeOffset();
        }

        /// <summary>
        /// Analyzes the accuracy of the running average by comparing to the previous average.
        /// The drift is calculated and stored to help compensate for drift in future updates.
        /// The incomplete average becomes the new running average.
        /// </summary>
        private void EvaluateError() {
            _previousAverageOffset = GetGameTimeOffset();
            _offsetDriftCompensation = _incompleteAverageOffset - _averageOffset;
            _averageOffset = _incompleteAverageOffset;
        }

        /// <summary>
        /// Resets the variables to begin accumulating a new time offset average from scratch.
        /// If enabled, it also dynamically sets the sample count based on frame rate to ensure
        /// accurate sampling over a set duration.
        /// </summary>
        private void ResetSampling() {
            _incompleteAverageOffset = 0;
            _offsetSampleCount = 0;

            if (AutoAdjustSampleCount && GameTime.SmoothDeltaTime() > 0) {
                Samples = Mathf.CeilToInt(
                        Mathf.Min(MaxDynamicSamples, SamplingDuration / GameTime.SmoothDeltaTime())
                );
            }
        }

        private void PrintDebugInfo() {
            Info(
                    $"Server time vrc = {NetworkTime.TimeAsDouble():F6}; "
                    + $"Tlp server time = {GetNetworkTime():F6}; "
                    + $"Error = {(Samples - _offsetSampleCount) * _offsetDriftCompensation / Samples:F6}; "
                    + $" Corrective drift = {_offsetDriftCompensation / Samples:F6}"
            );
        }

        /// <returns>result of subtracting the game time from the network time</returns>
        private double GetNetworkGameTimeDelta() {
            return NetworkTime.TimeAsDouble() - GameTime.TimeAsDouble();
        }

        /// <returns>true if the current frame has already been updated</returns>
        private bool UpdatedThisFrame() {
            return _frameLastUpdated == FrameCount.Frame();
        }
        #endregion
    }
}