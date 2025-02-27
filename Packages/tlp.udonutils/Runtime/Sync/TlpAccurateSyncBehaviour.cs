using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Physics;
using TLP.UdonUtils.Runtime.Recording;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Base Sync Behaviour for movement prediction based on received values from the current owner
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TlpAccurateSyncBehaviour), ExecutionOrder)]
    public abstract class TlpAccurateSyncBehaviour : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TransformRecordingPlayer.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [Tooltip("Container for received snapshot data for inter-/extrapolation")]
        public TimeBacklog Backlog;

        [Tooltip("Container for a received network snapshot")]
        public TimeSnapshot Snapshot;

        [Tooltip("The network time as provided by e.g. TlpNetworkTime")]
        public TimeSource NetworkTime;

        [Tooltip("The game time as provided by e.g. Time.timeSinceLevelLoad.")]
        public TimeSource GameTime;

        public bool ShowDebugTrails = true;
        public GameObject DebugTrailReceived;
        public GameObject DebugTrailSmoothPrediction;
        public GameObject DebugTrailRawPrediction;
        #endregion

        #region NetworkState
        /// <summary>
        /// Compressed SendTime relative to the latest <see cref="TlpNetworkTime.ReferenceTime"/> snapshot.
        /// </summary>
        [UdonSynced]
        private float _syncedSendTime;

        /// <summary>
        /// snapshot of the <see cref="TlpNetworkTime"/> for delta-compression.
        /// </summary>
        private double _referenceTime;

        /// <summary>
        /// Get: SendTime decompressed using the latest <see cref="TlpNetworkTime.ReferenceTime"/> snapshot.
        /// Set: Compresses SendTime using the latest <see cref="TlpNetworkTime.ReferenceTime"/> snapshot.
        /// </summary>
        public double SyncedSendTime
        {
            get => _syncedSendTime + _referenceTime;
            set => _syncedSendTime = (float)(value - _referenceTime);
        }

        #region Working Copy
        /// <summary>
        /// Time when the last update was sent in seconds of <see cref="NetworkTime"/>.
        /// </summary>
        [NonSerialized]
        public double WorkingSendTime = double.MinValue;

        /// <summary>
        /// Time when the last update was received in seconds of <see cref="NetworkTime"/>.
        /// </summary>
        [NonSerialized]
        public double ReceiveTime = double.MinValue;

        /// <summary>
        /// In seconds, time between <see cref="ReceiveTime"/> and <see cref="WorkingSendTime"/>.
        /// </summary>
        [NonSerialized]
        public double Latency;
        #endregion
        #endregion

        #region Settings
        [Tooltip("0 [s] by default (full prediction), number of seconds that shall be removed from prediction")]
        [Range(0, 1)]
        public float PredictionReduction;
        #endregion

        #region Network Events
        /// <summary>
        /// Copy the working state to the network state
        /// </summary>
        public override void OnPreSerialization() {
            base.OnPreSerialization();
            CreateNetworkStateFromWorkingState();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            if (!HasStartedOk) {
                Error($"{nameof(OnDeserialization)}: Not initialized");
                return;
            }

            if (!ReceivedNetworkStateIsNewer()) {
                return;
            }

            CreateWorkingCopyOfNetworkState();
            RecordSnapshot(
                    Snapshot,
                    WorkingSendTime);
            Backlog.Add(Snapshot, 3 * PredictionReduction);

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetAge)}: {NetworkTime.TimeAsDouble()} - {WorkingSendTime}");

#endif
            #endregion

            Latency = ReceiveTime - WorkingSendTime;
#if TLP_DEBUG
            DebugLog($"Latency VRC = {deserializationResult.Latency():F6} vs own {Latency:F6}");
#endif

            // ensure we update as early as possible in the lifecycle of the UdonSharpBehaviour
            PredictMovement(GetElapsed(), 0f);
        }
        #endregion

        #region Hook Implementations
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) return false;

            if (!Utilities.IsValid(GameTime)) {
                Error($"{nameof(GameTime)} not set");
                return false;
            }

            if (!Utilities.IsValid(NetworkTime)) {
                Error($"{nameof(NetworkTime)} not set");
                return false;
            }

            if (NetworkTime.GetUdonTypeID() == GetUdonTypeID<TlpNetworkTime>()) {
                var tlpTimeSource = (TlpNetworkTime)NetworkTime;
                if (Utilities.IsValid(tlpTimeSource)) {
                    _referenceTime = tlpTimeSource.ReferenceTime;
                    if (Utilities.IsValid(tlpTimeSource.OnReferenceTimeUpdated)
                        && !tlpTimeSource.OnReferenceTimeUpdated.AddListenerVerified(
                                this,
                                nameof(OnNetworkTimeShifted))) {
                        Error(
                                $"{nameof(SetupAndValidate)}: Failed to add listener to {nameof(tlpTimeSource.OnReferenceTimeUpdated)}");
                        return false;
                    }
                }
            }

            if (!Utilities.IsValid(Backlog)) {
                Error($"{nameof(Backlog)} not set");
                return false;
            }

            if (Utilities.IsValid(Snapshot)) {
                return true;
            }

            Error($"{nameof(Snapshot)} not set");
            return false;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(OnNetworkTimeShifted):
#if TLP_DEBUG
DebugLog_OnEvent(eventName);
#endif
                    OnNetworkTimeShifted();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Hooks
        protected virtual void RecordSnapshot(TimeSnapshot timeSnapshot, double mostRecentServerTime) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(RecordSnapshot)}: {nameof(mostRecentServerTime)} = {mostRecentServerTime}s");
#endif
            #endregion

            timeSnapshot.ServerTime = mostRecentServerTime;
        }

        /// <summary>
        /// Hook that allows predicting movement based on elapsed time since the latest received network snapshot.
        /// </summary>
        /// <param name="receivedSnapshotAge">number of seconds that have passed relative to
        /// <see cref="TimeSource"/> time since the recording of the latest <see cref="SyncedSendTime"/></param>
        /// <param name="deltaTime">time since previous update</param>
        protected abstract void PredictMovement(double receivedSnapshotAge, float deltaTime);

        protected virtual void CreateWorkingCopyOfNetworkState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateWorkingCopyOfNetworkState));
#endif
            #endregion

            ReceiveTime = NetworkTime.TimeAsDouble();
            WorkingSendTime = SyncedSendTime;
        }

        protected virtual void CreateNetworkStateFromWorkingState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateNetworkStateFromWorkingState));
#endif
            #endregion

            SyncedSendTime = WorkingSendTime;
        }
        #endregion

        #region Internal
        internal double GetElapsed() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(GetElapsed));
#endif
            #endregion

            return GetAge() - PredictionReduction;
        }

        private bool ReceivedNetworkStateIsNewer() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ReceivedNetworkStateIsNewer));
#endif
            #endregion

            if (SyncedSendTime < WorkingSendTime) {
                Warn(
                        $"{nameof(ReceivedNetworkStateIsNewer)}: Received late {nameof(SyncedSendTime)}={SyncedSendTime:F6}s; previous {nameof(WorkingSendTime)}={WorkingSendTime:F6}s");
                return false;
            }

            if (SyncedSendTime > NetworkTime.TimeAsDouble()) {
                Warn(
                        $"{nameof(ReceivedNetworkStateIsNewer)}: Received {nameof(SyncedSendTime)}={SyncedSendTime:F6}s from the future (current time={NetworkTime.TimeAsDouble():F6}s)");
                return false;
            }
            #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{nameof(ReceivedNetworkStateIsNewer)}: Valid {nameof(SyncedSendTime)} received: {SyncedSendTime:F6} > {WorkingSendTime:F6}");
#endif
            #endregion

            return true;
        }

        internal double GetAge() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetAge)}: {NetworkTime.TimeAsDouble()} - {WorkingSendTime}");

#endif
            #endregion

            return NetworkTime.TimeAsDouble() - WorkingSendTime;
        }

        private void OnNetworkTimeShifted() {
            #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog(nameof(OnNetworkTimeShifted));
#endif
            #endregion

            var tlpTimeSource = (TlpNetworkTime)NetworkTime;
            if (Utilities.IsValid(tlpTimeSource)) {
                _referenceTime = tlpTimeSource.ReferenceTime;
            }

            if (Networking.IsOwner(gameObject)) {
                MarkNetworkDirty();
            }
        }
        #endregion

        #region Public
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="acceleration"></param>
        /// <param name="angularVelocityRad"></param>
        /// <param name="rotation"></param>
        /// <param name="circleAngularVelocityDegrees"></param>
        /// <param name="circleThreshold">threshold in degrees, if circleAngularVelocityDegrees is greater prediction occurs on a circle</param>
        /// <param name="elapsed"></param>
        /// <param name="newPosition"></param>
        /// <param name="newVelocity"></param>
        /// <param name="newRotation"></param>
        public static void PredictState(
                Vector3 position,
                Vector3 velocity,
                Vector3 acceleration,
                Vector3 angularVelocityRad,
                Quaternion rotation,
                float circleAngularVelocityDegrees,
                float circleThreshold,
                double elapsed,
                out Vector3 newPosition,
                out Vector3 newVelocity,
                out Quaternion newRotation
        ) {
            if (circleAngularVelocityDegrees > circleThreshold) {
                newPosition = ConstantCircularVelocity.PositionOnCircle(
                        position,
                        velocity,
                        acceleration,
                        circleAngularVelocityDegrees,
                        (float)elapsed,
                        out newVelocity,
                        out var rotationDelta);
            } else {
                newPosition = ConstantLinearAcceleration.Position(
                        position,
                        velocity,
                        acceleration,
                        (float)elapsed);
                newVelocity = ConstantLinearAcceleration.Velocity(
                        velocity,
                        acceleration,
                        (float)elapsed);
            }

            Quaternion.Euler(angularVelocityRad).ToAngleAxis(
                    out float syncedTurnRateRadians,
                    out var syncedTurnAxis
            );

            // conversion to degrees is done AFTER the axis is created, otherwise huge errors are introduced from euler angles
            float predictedTurnDelta = (float)(syncedTurnRateRadians * elapsed * Mathf.Rad2Deg);
            var rawDeltaRotation = Quaternion.AngleAxis(predictedTurnDelta, syncedTurnAxis);

            // apply deltaRotation in world space
            newRotation = rawDeltaRotation * rotation.normalized;
        }
        #endregion
    }
}