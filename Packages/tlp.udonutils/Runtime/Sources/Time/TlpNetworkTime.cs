using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/> that synchronizes network and game clocks by continuously sampling
    /// their difference, detecting drift, and applying compensation to keep them in sync over time as the scene runs.
    /// Sudden spikes or drift is also detected, causing the sampling to reset and start over
    /// to compensate for the drift.
    /// This usually happens when avatars are being loaded or the game has performance issues.
    ///
    /// In summary, it provides an averaged network time that is based on game time for smooth interpolations.
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

        #region Constants
        /// <summary>
        /// Upper limit of dynamic samples to prevent taking a long time to adjust for sudden drift.
        /// </summary>
        private const int MaxDynamicSamples = 1000;

        /// <summary>
        /// Lower limit of dynamic samples to prevent too few samples.
        /// </summary>
        private const int MinDynamicSamples = 10;

        /// <summary>
        /// correct drift at half the speed to prevent overshoot/offset oscillation
        /// </summary>
        private const double DriftCompensationRate = 0.5;

        /// <summary>
        /// If the delta time ever gets lower then this it means that GameTime is most certainly drifting relative
        /// to real time, making sampling worthless. Used to determine when we should use the RealNetworkTime instead.
        /// </summary>
        private const float MaxDeltaTime = 1f / 12f;
        #endregion

        #region Dependencies
        /// <summary>
        /// Source of local game time, e.g. based on <see cref="UnityEngine.Time.timeSinceLevelLoad"/>
        /// </summary>
        [SerializeField]
        [Tooltip("Source of local game time, e.g. based on Time.timeSinceLevelLoad")]
        internal TimeSource GameTime;

        /// <summary>
        /// Source of network time, e.g. based on VRChat network time
        /// </summary>
        [FormerlySerializedAs("NetworkTime")]
        [SerializeField]
        [Tooltip("Source of network time, e.g. based on VRChat network time")]
        internal TimeSource RealNetworkTime;

        /// <summary>
        /// Source of frame count, e.g. based on <see cref="Time.frameCount"/>
        /// </summary>
        [Tooltip("Source of frame count, e.g. based on Time.frameCount")]
        [SerializeField]
        internal FrameCountSource FrameCount;
        #endregion

        #region Settings
        /// <summary>
        /// For how many frames that <see cref="RealNetworkTime"/> shall be sampled before correcting for drift.
        /// Larger values increase time stability but decrease drift compensation speed.
        /// Is not used when AutoAdjustSampleCount is enabled.
        /// </summary>
        [Range(1, 1000)]
        [Tooltip(
                "For how many frames that RealNetworkTime shall be sampled before correcting for drift. "
                + "Larger values increase time stability but decrease drift compensation speed. "
                + "Is not used when AutoAdjustSampleCount is enabled."
        )]
        public int Samples = 256;

        /// <summary>
        /// When enabled the number of samples is derived from the current framerate and the <see cref="SamplingDuration"/> variable
        /// to achieve a near constant drift compensation speed.
        /// </summary>
        [Tooltip(
                "When enabled the number of samples is derived from "
                + "the current framerate and the SamplingDuration variable to "
                + "achieve a near constant drift compensation speed."
        )]
        public bool AutoAdjustSampleCount;

        /// <summary>
        /// Only used when <see cref="AutoAdjustSampleCount"/> is enabled.
        /// Dictates how many seconds the drift compensation needs on average to reduce the drift by the amount
        /// defined by <see cref="DriftCompensationRate"/>, independent of framerate.
        /// </summary>
        [SerializeField]
        [Range(1f, 10f)]
        [Tooltip(
                "Only used when AutoAdjustSampleCount is enabled. "
                + "Dictates how many seconds drift compensation needs on average to reduce the drift by half, independent of framerate."
        )]
        public float SamplingDuration = 2.5f;

        [SerializeField]
        [Tooltip(
                "If the difference between RealNetworkTime and TlpNetworkTime exceeds this value, "
                + "the TlpNetworkTime is forcefully updated to the RealNetworkTime instead of slowly drifting towards it. "
                + "This is useful to compensate for network drift caused by e.g. avatars being loaded.")]
        [Range(0f, 1f)]
        internal double DriftThreshold = 0.5f;
        #endregion

        #region State
        /// <summary>
        /// average difference between <see cref="RealNetworkTime"/> and <see cref="GameTime"/> that is currently
        /// being calculated, only ever contains the average when <see cref="Samples"/> samples have been collected/>
        /// </summary>
        private double _incompleteAverageOffset;

        /// <summary>
        /// latest completely calculated average difference between <see cref="RealNetworkTime"/> and
        /// <see cref="GameTime"/>
        /// </summary>
        internal double _averageOffset;

        /// <summary>
        /// previously completely calculated average difference between  <see cref="RealNetworkTime"/> and
        /// <see cref="GameTime"/>
        /// </summary>
        private double _previousAverageOffset;

        /// <summary>
        /// Delta to the <see cref="RealNetworkTime"/> that is used correct for drift over the
        /// next <see cref="Samples"/> frames
        /// </summary>
        public double AverageError { get; private set; }

        /// <summary>
        /// Number of  <see cref="RealNetworkTime"/> samples currently collected, resets every time
        /// <see cref="Samples"/> is reached.
        /// </summary>
        private int _offsetSampleCount;

        /// <summary>
        /// used to prevent calculating the time multiple times for the current frame
        /// </summary>
        private int _frameLastUpdated = -1;
        #endregion

        #region U# Lifecycle
        public void OnEnable() {
            if (DependenciesValid()) {
                ForceSynchronizeTime();
            }
        }

        public void Update() {
            UpdateTime();
        }
        #endregion

        #region Base Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!DependenciesValid()) {
                return false;
            }

            string existingInstance = Networking.LocalPlayer.GetPlayerTag(nameof(TlpNetworkTime));
            string idString = GetInstanceID().ToString();

            if (!string.IsNullOrEmpty(existingInstance) && existingInstance != idString) {
                ErrorAndDisableGameObject(
                        $"Another instance of {nameof(TlpNetworkTime)} already exists: {idString} (this) != {existingInstance} (other)");
                return false;
            }

            Networking.LocalPlayer.SetPlayerTag(nameof(TlpNetworkTime), idString);

            return true;
        }
        #endregion

        #region Public
        /// <returns>The <see cref="TlpNetworkTime"/> based on game
        /// time with an over multiple frames calculated offset and drift compensation</returns>
        public override float Time() {
            return (float)GetNetworkTime();
        }

        /// <returns>The <see cref="TlpNetworkTime"/> based on game
        /// time with an over multiple frames calculated offset and drift compensation</returns>
        public override double TimeAsDouble() {
            return GetNetworkTime();
        }

        /// <summary>
        /// Takes a time measured on the network, and converts it to the corresponding
        /// time that would be shown in the game.
        /// Assumes that the given network time value is based on the <see cref="RealNetworkTime"/>.
        /// </summary>
        /// <param name="networkTime">Network time in seconds</param>
        /// <returns><see cref="GameTime"/> corresponding to the server time</returns>
        [PublicAPI]
        public float NetworkTimeToGameTime(float networkTime) {
            return networkTime - (float)GetGameTimeOffset();
        }

        /// <summary>
        /// Takes a time measured on the network, and converts it to the corresponding
        /// time that would be shown in the game.
        /// Assumes that the given network time value is based on the <see cref="RealNetworkTime"/>.
        /// </summary>
        /// <param name="networkTime">Network time in seconds</param>
        /// <returns><see cref="GameTime"/> corresponding to the server time</returns>
        [PublicAPI]
        public double NetworkTimeToGameTimeAsDouble(double networkTime) {
            return networkTime - GetGameTimeOffset();
        }

        /// <summary>
        /// The difference between <see cref="RealNetworkTime"/> and <see cref="GameTime"/>
        /// </summary>
        /// <returns>Offset = RealNetworkTime - GameTime</returns>
        public double GetGameTimeOffset() {
            return _previousAverageOffset.LerpDouble(
                    _averageOffset,
                    DriftCompensationRate * _offsetSampleCount / Samples
            );
        }

        /// <summary>
        /// Current delta to the provided network time: (<see cref="RealNetworkTime"/> - <see cref="TlpNetworkTime"/>)
        /// </summary>
        public double ExactError { get; private set; }

        /// <summary>
        /// Exact server time used as the latest sample
        /// </summary>
        public double SampledRealServerTime { get; internal set; }

        /// <summary>
        /// Searches for the gameobject TLP_NetworkTime in the scene in order to get its TLPNetworkTime component
        /// </summary>
        /// <returns>the found component or null if not found</returns>
        public static TlpNetworkTime GetInstance() {
            var instance = GameObject.Find("TLP_NetworkTime");
            if (instance) {
                return instance.GetComponent<TlpNetworkTime>();
            }

            Debug.LogError("GameObject called 'TLP_NetworkTime' not found");
            return null;
        }
        #endregion

        #region Internal
        /// <returns>True if all dependencies are set</returns>
        private bool DependenciesValid() {
            if (!Utilities.IsValid(GameTime)) {
                Error($"{nameof(GameTime)} is not set");
                return false;
            }

            if (!Utilities.IsValid(RealNetworkTime)) {
                Error($"{nameof(RealNetworkTime)} is not set");
                return false;
            }

            if (!Utilities.IsValid(FrameCount)) {
                Error($"{nameof(FrameCount)} is not set");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ensures that the current delta between game time and network time is used and sampling is reset.
        /// </summary>
        internal void ForceSynchronizeTime() {
            SampledRealServerTime = RealNetworkTime.TimeAsDouble();
            _incompleteAverageOffset = GetNetworkGameTimeDelta();
            _previousAverageOffset = _incompleteAverageOffset;
            _averageOffset = _incompleteAverageOffset;
            ResetSampling();
        }

        /// <summary>
        /// samples the frame time deltas to calculate a running average time offset between the game and network
        /// <remarks>Do not call in FixedUpdate() if you are using dynamic sampling!</remarks>
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

            _frameLastUpdated = FrameCount.Frame();
            SampledRealServerTime = RealNetworkTime.TimeAsDouble();
            ExactError = SampledRealServerTime - (GameTime.TimeAsDouble() + GetGameTimeOffset());
            if (Math.Abs(ExactError) > DriftThreshold || GameTime.DeltaTime() >= MaxDeltaTime) {
                ForceSynchronizeTime();
                ExactError = 0f;
            }

            _incompleteAverageOffset += GetNetworkGameTimeDelta() / Samples;
            ++_offsetSampleCount;

            if (_offsetSampleCount < Samples) {
                return;
            }

            EvaluateError();
            ResetSampling();
        }

        ///<brief>
        /// ensure that every call to this method returns the same value throughout the entire frame
        /// </brief>
        /// <returns>The network time based on game
        /// time with an over time calculated offset and drift compensation</returns>
        private double GetNetworkTime() {
            if (UnityEngine.Time.inFixedTimeStep || UpdatedThisFrame()) {
                // don't update in FixedUpdate as it can be called
                // multiple times per frame and has a different delta time
                return GameTime.TimeAsDouble() + GetGameTimeOffset();
            }

            UpdateTime();
            return GameTime.TimeAsDouble() + GetGameTimeOffset();
        }

        /// <summary>
        /// Analyzes the accuracy of the running average by comparing to the previous average.
        /// The drift is calculated and stored to help compensate for drift in future updates.
        /// The incomplete average becomes the new running average.
        /// </summary>
        private void EvaluateError() {
            _previousAverageOffset = GetGameTimeOffset();
            AverageError = _incompleteAverageOffset - _averageOffset;
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

            float deltaTime = GameTime.SmoothDeltaTime();
            if (AutoAdjustSampleCount && deltaTime > 0) {
                Samples = Mathf.RoundToInt(
                        Mathf.Clamp(SamplingDuration / deltaTime, MinDynamicSamples, MaxDynamicSamples)
                );
            }
        }

        /// <returns>result of subtracting the game time from the network time</returns>
        private double GetNetworkGameTimeDelta() {
            return SampledRealServerTime - GameTime.TimeAsDouble();
        }

        /// <returns>true if the current frame has already been updated</returns>
        private bool UpdatedThisFrame() {
            return _frameLastUpdated == FrameCount.Frame();
        }
        #endregion
    }
}