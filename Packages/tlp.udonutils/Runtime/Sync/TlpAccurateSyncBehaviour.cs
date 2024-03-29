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
        public bool Testing;

        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpBaseBehaviour.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        [SerializeField]
        protected internal TimeBacklog Backlog;

        [SerializeField]
        internal TimeSnapshot Snapshot;

        [SerializeField]
        protected internal TimeSource NetworkTime;
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

            if (IsReceivedNetworkStateOutdated()) {
                return;
            }

            CreateWorkingCopyOfNetworkState();
            RecordSnapshot(
                    Snapshot,
                    (float)WorkingSendTime);
            Backlog.Add(Snapshot, 3f * PredictionReduction);
            PredictMovement(GetElapsed(), 0f);
        }
        #endregion

        #region U# Lifecycle
        public void Start() {
            if (!Utilities.IsValid(Backlog)) ErrorAndDisableGameObject($"{nameof(Backlog)} not set");
            if (!Utilities.IsValid(Snapshot)) ErrorAndDisableGameObject($"{nameof(Snapshot)} not set");
        }

        public virtual void Update() {
            if (UseFixedUpdate) {
                return;
            }

            DebugLog(nameof(Update));

            if (!Testing) {
                if (Networking.IsOwner(gameObject)) {
                    return;
                }
            }

            PredictMovement(GetElapsed(), Time.deltaTime);
        }

        public virtual void FixedUpdate() {
            if (!UseFixedUpdate) {
                return;
            }

            DebugLog(nameof(FixedUpdate));

            if (!Testing) {
                if (Networking.IsOwner(gameObject)) {
                    return;
                }
            }

            PredictMovement(GetElapsed(), Time.fixedDeltaTime);
        }
        #endregion

        #region Hooks
        protected virtual void RecordSnapshot(TimeSnapshot timeSnapshot, float mostRecentServerTime) {
            DebugLog($"{nameof(RecordSnapshot)}: {nameof(mostRecentServerTime)} = {mostRecentServerTime}s");
            timeSnapshot.ServerTime = mostRecentServerTime;
        }

        protected abstract void PredictMovement(float elapsedSinceSent, float deltaTime);

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

        private bool IsReceivedNetworkStateOutdated() {
            DebugLog(nameof(IsReceivedNetworkStateOutdated));
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