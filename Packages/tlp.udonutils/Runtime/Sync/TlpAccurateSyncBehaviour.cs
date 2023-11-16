using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class TlpAccurateSyncBehaviour : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpBaseBehaviour.ExecutionOrder + 1;

        [UdonSynced]
        public float SyncedGameTime;

        [UdonSynced]
        public double SyncedServerSendTime;

        [UdonSynced]
        public float SyncedSnapshotGameTime;

        [SerializeField]
        protected NetworkTime NetworkTime;

        private const float ElapsedCompensation = 0.007f;

        private float _gameTimeDifference;
        private float _previousGameTimeDifference;
        private float _smoothingSpeedGameTimeDifference;

        public bool UseFixedUpdate;

        protected readonly DataList TimeStamps = new DataList();

        [Tooltip("0 [s] by default (full prediction), number of seconds that shall be removed from prediction")]
        [Range(0, 1)]
        public float PredictionReduction;

        public override void OnPreSerialization()
        {
            base.OnPreSerialization();
            SyncedGameTime = Time.timeSinceLevelLoad - Time.smoothDeltaTime;
            SyncedServerSendTime = NetworkTime.GetTimeForCurrentFrame();
        }

        public Text Text;

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
            base.OnDeserialization(deserializationResult);

            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(
                $"Latency VRC = {deserializationResult.Latency()} vs own {NetworkTime.GetTimeForCurrentFrame() - SyncedServerSendTime}"
            );
#endif

            #endregion

            _UpdateGameTimeDeltaToSender((float)(NetworkTime.GetTimeForCurrentFrame() - SyncedServerSendTime));

            float elapsed = _GetElapsed();

            float mostRecentTimeStamp = Time.timeSinceLevelLoad - elapsed;
            TimeStamps.Add(mostRecentTimeStamp);
            while (TimeStamps.Count > 0 && mostRecentTimeStamp - TimeStamps[0].Float > 3f * PredictionReduction)
            {
                TimeStamps.RemoveAt(0);
            }

            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog($"Backlog of timestamps: ${TimeStamps.Count}");
#endif

            #endregion

            PredictMovement(elapsed, 0f);
            if (Text)
            {
                Text.text = $"_gameTimeDifference = {_gameTimeDifference}\nelapsed = {elapsed}";
            }
        }

        public virtual void Update()
        {
            if (UseFixedUpdate)
            {
                return;
            }

            if (Networking.IsOwner(gameObject))
            {
                return;
            }

            PredictMovement(_GetElapsed(), Time.deltaTime);
        }

        public virtual void FixedUpdate()
        {
            if (!UseFixedUpdate)
            {
                return;
            }

            if (Networking.IsOwner(gameObject))
            {
                return;
            }

            PredictMovement(_GetElapsed(), Time.fixedDeltaTime);
        }

        protected abstract void PredictMovement(float elapsed, float deltaTime);

        private void _UpdateGameTimeDeltaToSender(float latency)
        {
            float gameTimeDifference = GameTimeDifference(
                SyncedGameTime,
                latency,
                Time.timeSinceLevelLoad
            );

            if (Mathf.Abs(_gameTimeDifference - gameTimeDifference) > 1.0)
            {
                _gameTimeDifference = gameTimeDifference;
            }

            UpdateAverageGameTimeDifference(
                ref _gameTimeDifference,
                ref _previousGameTimeDifference,
                gameTimeDifference - Time.smoothDeltaTime
            );
        }

        internal float _GetElapsed()
        {
            return GetElapsedTime(
                SyncedSnapshotGameTime,
                SyncedGameTime,
                Time.timeSinceLevelLoad,
                _gameTimeDifference
            ) - ElapsedCompensation - PredictionReduction;
        }

        public static float GetElapsedTime(
            float gameTimeSenderOnSnapshot,
            float gameTimeSenderOnSend,
            float gameTimeReceiverOnUpdate,
            float estimatedGameTimeDifference
        )
        {
            return gameTimeReceiverOnUpdate - (gameTimeSenderOnSend + estimatedGameTimeDifference -
                                               (gameTimeSenderOnSend - gameTimeSenderOnSnapshot));
        }

        public static float GameTimeDifference(
            float gameTimeOnSend,
            float transmitDuration,
            float gameTimeOnReceive
        )
        {
            return gameTimeOnReceive - (gameTimeOnSend + transmitDuration);
        }

        public static void UpdateAverageGameTimeDifference(
            ref float averageGameTimeDifference,
            ref float previousAverageGameTimeDifference,
            float gameTimeDifference
        )
        {
            float change = Mathf.Abs(averageGameTimeDifference - previousAverageGameTimeDifference);
            previousAverageGameTimeDifference = averageGameTimeDifference;
            if (change > 0.1f * 0.1f)
            {
                averageGameTimeDifference = (float)(0.9 *
                    averageGameTimeDifference + 0.1 * gameTimeDifference);
            }
            else if (change > 0.01f * 0.01f)
            {
                averageGameTimeDifference = (float)(0.99 *
                    averageGameTimeDifference + 0.01 * gameTimeDifference);
            }
            else if (change > 0.001f * 0.001f)
            {
                averageGameTimeDifference = (float)(0.999 *
                    averageGameTimeDifference + 0.001 * gameTimeDifference);
            }
            else
            {
                averageGameTimeDifference = (float)(0.99995 *
                    averageGameTimeDifference + 0.00005 * gameTimeDifference);
            }
        }
    }
}