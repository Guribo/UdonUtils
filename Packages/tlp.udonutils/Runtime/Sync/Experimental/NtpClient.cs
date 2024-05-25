using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Adapters.Cyan;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time.Experimental;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.Experimental
{
    /// <summary>
    /// EXPERIMENTAL time sync that can replace Networking.ServerTimeInSeconds
    ///
    /// Client used for calculating custom network time. Sends a timestamp to the master player
    /// every few seconds and calculates its own time offset to the master based on the masters response.
    /// This offset is used to correct the local time to be in sync with the master time.
    /// Also provides the latency/ping to the master.
    ///
    /// Known issues:
    ///  - the time offset assumes symmetric time for requesting and receiving responses which in practice is not guaranteed
    ///  - differences in framerate on the client and the master player shift the offset due to asymmetry in travel times
    ///    Example: if the client has 10fps and the master 100fps the offset is off by 40-45 milliseconds
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class NtpClient : CyanPooledObject
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpTime.ExecutionOrder - 2;
        #endregion

        #region Dependencies
        public TimeSource TimeSource;
        public NtpServer Server;
        #endregion

        #region Settings
        [Header("Settings")]
        [Range(1, 60)]
        public float RequestInterval = 3f;

        [Range(1, 100)]
        [Tooltip("High values decrease the noise but update slower in case the network environment changes")]
        public int ClockOffsetSamples = 60;
        #endregion

        #region NetworkState
        [UdonSynced]
        public float RequestSendTime;
        #endregion

        #region State
        public float PingToMaster { get; internal set; }
        public float LatencyToMaster => 0.5f * PingToMaster;
        internal float ClockOffset;
        internal int CurrentClockOffsetSamples;
        internal float[] ClockOffsetHistory = new float[20];
        internal int ClockOffsetIndex;
        internal float WorkingRequestSendTime;
        internal float NextRequestTime = float.MinValue;
        #endregion

        #region Lifecycle
        public void Update() {
            if (Networking.IsMaster) {
                return;
            }

            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                return;
            }

            if (TimeSource.Time() < NextRequestTime) {
                return;
            }

            RequestNtpSynchronization();
            NextRequestTime = TimeSource.Time() + RequestInterval;
        }
        #endregion

        #region Network Events
        public override void OnPreSerialization() {
            base.OnPreSerialization();

            WorkingRequestSendTime = TimeSource.Time();
#if TLP_DEBUG
            Warn($"Client requesting: {nameof(WorkingRequestSendTime)}: {WorkingRequestSendTime:F9}");
#endif
            RequestSendTime = WorkingRequestSendTime;
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
#if TLP_DEBUG
            Warn(
                    $"Client request arrived: {nameof(RequestSendTime)}: {RequestSendTime:F9}, receive time: {TimeSource.Time():F9} ");
#endif
            if (!Server.AddRequest(this)) {
                Error("Failed to add request to server");
            }
        }
        #endregion

        #region Public
        public float GetAdjustedLocalTime() {
            return TimeSource.Time() + ClockOffset;
        }

        public void AdjustRequestTiming() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(AdjustRequestTiming));
#endif
            #endregion

            NextRequestTime += 0.5f * RequestInterval;
        }

        public bool UpdateOffset(float requestReceiveTime, float responseSendTime, float responseReceiveTime) {
            float ping = GetDelta(WorkingRequestSendTime, requestReceiveTime, responseSendTime, responseReceiveTime);
            if (ping < 0f) {
                AdjustRequestTiming();
                Warn($"Ping {ping} is negative, skipping offset update and adjusting request timing.");
                return false;
            }

            PingToMaster = ping;

            // failure cases already covered by GetDelta above
            GetClockOffset(
                    WorkingRequestSendTime,
                    requestReceiveTime,
                    responseSendTime,
                    responseReceiveTime,
                    out float offsetSample);

            SaveOffsetSample(offsetSample);
            float newAverageOffset = GetNewAverageClockOffset();

#if TLP_DEBUG
            Warn(
                    $"New client time offset: {newAverageOffset:F9} change: {1000f * (newAverageOffset - ClockOffset):F3} ms; Ping to master: {1000f * PingToMaster:F3}ms; Latency to master: {1000f * LatencyToMaster:F3}ms");
#endif
            ClockOffset = newAverageOffset;
            return true;
        }

        private float GetNewAverageClockOffset() {
            float newAverageOffset = 0f;
            int maxSamples = ClockOffsetHistory.LengthSafe();
            int startIndex = ClockOffsetIndex;
            for (int i = CurrentClockOffsetSamples - 1; i >= 0; i--) {
                newAverageOffset += ClockOffsetHistory[startIndex] / CurrentClockOffsetSamples;
                startIndex.MoveIndexLeftLooping(maxSamples);
            }

            return newAverageOffset;
        }

        private void SaveOffsetSample(float offset) {
            int maxSamples = ClockOffsetHistory.LengthSafe();
            ClockOffsetIndex.MoveIndexRightLooping(maxSamples);
            ClockOffsetHistory[ClockOffsetIndex] = offset;
            CurrentClockOffsetSamples = Mathf.Min(maxSamples, CurrentClockOffsetSamples + 1);
        }
        #endregion

        #region TlpBase Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(TimeSource)) {
                Error($"{nameof(TimeSource)} not set");
                return false;
            }

            if (!Utilities.IsValid(Server)) {
                Error($"{nameof(Server)} not set");
                return false;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (ClockOffsetSamples < 1) {
                Error($"{nameof(ClockOffsetSamples)} must be >= 1");
                return false;
            }

            ClockOffsetHistory = new float[ClockOffsetSamples];

            return true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (Networking.LocalPlayer.IsOwner(gameObject)
                && (!Utilities.IsValid(Server.OwnNtpClient)
                    || !Networking.LocalPlayer.IsOwner(Server.OwnNtpClient.gameObject))) {
                Server.OwnNtpClient = this;
            }

            PingToMaster = 0f;
            CurrentClockOffsetSamples = 0;
            ClockOffsetIndex = 0;
        }
        #endregion

        #region Internal
        internal bool RequestNtpSynchronization() {
            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                Error("Not Owner");
                return false;
            }

            MarkNetworkDirty();
            RequestSerialization();
            return true;
        }

        /// <summary>
        /// Round-Trip Delay: The total time taken for a message to travel from the client to the server and back,
        /// without the duration it takes the server to send its response.
        /// </summary>
        /// <param name="requestSendTime"></param>
        /// <param name="requestReceiveTime"></param>
        /// <param name="responseSendTime"></param>
        /// <param name="responseReceiveTime"></param>
        /// <returns>-1 if requestSendTime > responseReceiveTime or requestReceiveTime > responseSendTime</returns>
        public static float GetDelta(
                float requestSendTime,
                float requestReceiveTime,
                float responseSendTime,
                float responseReceiveTime
        ) {
            if (requestSendTime > responseReceiveTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(GetDelta)}: {nameof(requestSendTime)} must be <= {nameof(responseReceiveTime)}");
                return -1f;
            }

            if (requestReceiveTime > responseSendTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(GetDelta)}: {nameof(requestReceiveTime)} must be <= {nameof(responseSendTime)}");
                return -1f;
            }


            float clientWaitingTime = responseReceiveTime - requestSendTime;
            float serverResponseDelay = responseSendTime - requestReceiveTime;
            return clientWaitingTime - serverResponseDelay;
        }

        /// <summary>
        /// Clock Offset: The difference between the client's clock and the server's clock.
        /// </summary>
        /// <param name="requestSendTime"></param>
        /// <param name="requestReceiveTime"></param>
        /// <param name="responseSendTime"></param>
        /// <param name="responseReceiveTime"></param>
        /// <param name="offset"></param>
        /// <returns>false if requestSendTime > responseReceiveTime or requestReceiveTime > responseSendTime</returns>
        public static bool GetClockOffset(
                float requestSendTime,
                float requestReceiveTime,
                float responseSendTime,
                float responseReceiveTime,
                out float offset
        ) {
            if (requestSendTime > responseReceiveTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(GetDelta)}: {nameof(requestSendTime)} must be <= {nameof(responseReceiveTime)}");
                offset = 0f;
                return false;
            }

            if (requestReceiveTime > responseSendTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(GetDelta)}: {nameof(requestReceiveTime)} must be <= {nameof(responseSendTime)}");
                offset = 0f;
                return false;
            }

            float requestSendDuration = requestReceiveTime - requestSendTime;
            float responseSendDurationFlipped = responseSendTime - responseReceiveTime;
            offset = 0.5f * (requestSendDuration + responseSendDurationFlipped);
            return true;
        }
        #endregion
    }
}