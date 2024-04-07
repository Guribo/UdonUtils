using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Sources;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class TlpAccurateSyncBehaviour : TlpBaseBehaviour
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpBaseBehaviour.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        public TimeBacklog Backlog;
        public TimeSnapshot Snapshot;
        public TimeSource NetworkTime;
        #endregion

        #region NetworkState
        public double SendTime
        {
            get
            {
                DebugLog($"Get {nameof(SendTime)} = {SyncedSendTime}s");
                return SyncedSendTime;
            }
            set
            {
                DebugLog($"Set {nameof(SendTime)} = {SyncedSendTime}s to {value}s");
                SyncedSendTime = value;
                DebugLog($"{nameof(SendTime)} = {SyncedSendTime}s");
            }
        }

        [UdonSynced]
        public double SyncedSendTime = double.MinValue;

        #region Working Copy
        protected internal double WorkingSendTime;
        #endregion
        #endregion

        #region Settings
        public bool UseFixedUpdate;

        [Tooltip("0 [s] by default (full prediction), number of seconds that shall be removed from prediction")]
        [Range(0, 1)]
        public float PredictionReduction;
        #endregion

        #region Network Events
        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            DebugLog($"Latency VRC = {deserializationResult.Latency()} vs own {GetAge()}");

            if (ReceivedNetworkStateIsOutdated()) {
                return;
            }

            CreateWorkingCopyOfNetworkState();
            RecordSnapshot(
                    Snapshot,
                    (float)WorkingSendTime);
            Backlog.Add(Snapshot, 3f * PredictionReduction);

            // ensure we update as early as possible in the lifecycle of the UdonSharpBehaviour
            PredictMovement(GetElapsed(), 0f);
        }
        #endregion

        #region U# Lifecycle
        public virtual void Update() {
            if (UseFixedUpdate) {
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Update));
#endif
            #endregion

            if (Networking.IsOwner(gameObject)) {
                return;
            }

            PredictMovement(GetElapsed(), Time.deltaTime);
        }

        public virtual void FixedUpdate() {
            if (!UseFixedUpdate) {
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(FixedUpdate));
#endif
            #endregion

            if (Networking.IsOwner(gameObject)) {
                return;
            }

            PredictMovement(GetElapsed(), Time.fixedDeltaTime);
        }
        #endregion

        #region Hook Implementations
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) return false;
            if (!Utilities.IsValid(Backlog)) {
                Error($"{nameof(Backlog)} not set");
                return false;
            }

            if (!Utilities.IsValid(Snapshot)) {
                Error($"{nameof(Snapshot)} not set");
                return false;
            }

            return true;
        }
        #endregion

        #region Hooks
        protected virtual void RecordSnapshot(TimeSnapshot timeSnapshot, float mostRecentServerTime) {
            DebugLog($"{nameof(RecordSnapshot)}: {nameof(mostRecentServerTime)} = {mostRecentServerTime}s");
            timeSnapshot.ServerTime = mostRecentServerTime;
        }

        /// <summary>
        /// Hook that allows predicting movement based on elapsed time since the latest received network snapshot.
        /// </summary>
        /// <param name="receivedSnapshotAge">number of seconds that have passed relative to
        /// <see cref="TimeSource"/> time since the recording of the latest <see cref="SyncedSendTime"/></param>
        /// <param name="deltaTime">depending on <see cref="UseFixedUpdate"/> it is
        /// either <see cref="Time.deltaTime"/> or <see cref="Time.fixedDeltaTime"/></param>
        protected abstract void PredictMovement(float receivedSnapshotAge, float deltaTime);

        protected virtual void CreateWorkingCopyOfNetworkState() {
            DebugLog(nameof(CreateWorkingCopyOfNetworkState));
            WorkingSendTime = SyncedSendTime;
        }
        #endregion

        #region Internal
        internal float GetElapsed() {
            DebugLog(nameof(GetElapsed));
            return (float)(GetAge() - PredictionReduction);
        }

        private bool ReceivedNetworkStateIsOutdated() {
            DebugLog(nameof(ReceivedNetworkStateIsOutdated));
            if (SyncedSendTime > WorkingSendTime || SyncedSendTime == 0.0) {
                return false;
            }

            Warn($"Received outdated {nameof(SyncedSendTime)} = {SyncedSendTime}s");
            return true;
        }

        internal double GetAge() {
            DebugLog($"{nameof(GetAge)}: {NetworkTime.TimeAsDouble()} - {WorkingSendTime}");
            return NetworkTime.TimeAsDouble() - WorkingSendTime;
        }
        #endregion
    }
}