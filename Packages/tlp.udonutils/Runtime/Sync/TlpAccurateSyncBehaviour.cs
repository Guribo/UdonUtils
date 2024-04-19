using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Sources;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync
{
    /// <summary>
    /// Base Sync Behaviour for movement prediction based on received values from the current owner
    /// </summary>
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
        [Tooltip("Container for received snapshot data for inter-/extrapolation")]
        public TimeBacklog Backlog;

        [Tooltip("Container for a received network snapshot")]
        public TimeSnapshot Snapshot;

        [Tooltip("The network time as provided by e.g. TlpNetworkTime")]
        public TimeSource NetworkTime;

        [Tooltip("The game time as provided by e.g. Time.timeSinceLevelLoad.")]
        public TimeSource GameTime;
        #endregion

        #region NetworkState
        [UdonSynced]
        [SerializeField]
        private double SyncedSendTime = double.MinValue;

        #region Working Copy
        public double WorkingSendTime = double.MinValue;
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
            DebugLog($"Latency VRC = {deserializationResult.Latency()} vs own {GetAge()}");

            if (ReceivedNetworkStateIsOutdated()) {
                return;
            }

            CreateWorkingCopyOfNetworkState();
            RecordSnapshot(
                    Snapshot,
                    WorkingSendTime);
            Backlog.Add(Snapshot, 3 * PredictionReduction);

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
        #endregion

        #region Hooks
        protected virtual void RecordSnapshot(TimeSnapshot timeSnapshot, double mostRecentServerTime) {
            DebugLog($"{nameof(RecordSnapshot)}: {nameof(mostRecentServerTime)} = {mostRecentServerTime}s");
            timeSnapshot.ServerTime = mostRecentServerTime;
        }

        /// <summary>
        /// Hook that allows predicting movement based on elapsed time since the latest received network snapshot.
        /// </summary>
        /// <param name="receivedSnapshotAge">number of seconds that have passed relative to
        /// <see cref="TimeSource"/> time since the recording of the latest <see cref="SyncedSendTime"/></param>
        /// <param name="deltaTime">time since previous update</param>
        protected abstract void PredictMovement(float receivedSnapshotAge, float deltaTime);

        protected virtual void CreateWorkingCopyOfNetworkState() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateWorkingCopyOfNetworkState));
#endif
            #endregion

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
        internal float GetElapsed() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(GetElapsed));
#endif
            #endregion

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
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(GetAge)}: {NetworkTime.TimeAsDouble()} - {WorkingSendTime}");

#endif
            #endregion

            return NetworkTime.TimeAsDouble() - WorkingSendTime;
        }
        #endregion
    }
}