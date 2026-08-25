using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
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
    [RequireComponent(typeof(VRCPlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(NtpClient), ExecutionOrder)]
    public class NtpClient : TlpBaseBehaviour
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI] public new const int ExecutionOrder = UtcTimeSource.ExecutionOrder + 1;
        #endregion

        #region Dependencies
        public TimeSource TimeSource;
        public NtpServer Server;
        #endregion

        #region Settings
        [Header("Settings")] [Range(1, 60)] public float RequestInterval = 3f;

        [Range(1, 100)]
        [Tooltip("High values decrease the noise but update slower in case the network environment changes")]
        public int ClockOffsetSamples = 60;
        #endregion

        #region NetworkState
        [UdonSynced] public double RequestSendTime;
        #endregion

        #region State
        public double PingToMaster { get; internal set; }
        public double LatencyToMaster => 0.5f * PingToMaster;
        internal double ClockOffset;
        internal int CurrentClockOffsetSamples;
        internal double[] ClockOffsetHistory = new double[20];
        internal int ClockOffsetIndex;
        internal double WorkingRequestSendTime;
        internal double NextRequestTime = float.MinValue;
        #endregion

        #region Lifecycle
        internal void Update()
        {
            if (Networking.IsMaster) {
                return;
            }

            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                return;
            }

            if (TimeSource._Time() < NextRequestTime) {
                return;
            }

            _RequestNtpSynchronization();
            NextRequestTime = TimeSource._Time() + RequestInterval;
        }
        #endregion

        #region Network Events
        public override void OnPreSerialization()
        {
            base.OnPreSerialization();

            WorkingRequestSendTime = _GetRawTime();
#if TLP_DEBUG
            _DebugLog($"Client requesting: {nameof(WorkingRequestSendTime)}: {WorkingRequestSendTime:F9}");
#endif
            RequestSendTime = WorkingRequestSendTime;
        }

        public override void OnDeserialization(DeserializationResult deserializationResult)
        {
            base.OnDeserialization(deserializationResult);
            if (!HasStartedOk) {
                _Error($"{nameof(OnDeserialization)}: Not initialized");
                return;
            }

            if (!Networking.IsMaster) {
                return;
            }

            if (!Server.OwnNtpClient) {
                return;
            }

            double receiveTime = Server.OwnNtpClient._GetAdjustedLocalTime();
#if TLP_DEBUG
            _DebugLog($"Client request arrived: {nameof(RequestSendTime)}: {RequestSendTime:F9}, " +
                      $"receive time: {receiveTime:F9} ");
#endif
            if (!Server._AddRequest(this, receiveTime)) {
                _Error("Failed to add request to server");
            }
        }
        #endregion

        #region Public
        /// <returns>Returns the local time + offset if master
        /// and only the local time if not master</returns>
        public double _GetTime()
        {
            if (HasStartedOk) {
                return Networking.IsMaster ? _GetAdjustedLocalTime() : TimeSource._Time();
            }

            _Error($"{nameof(_GetTime)}: Not initialized");
            return float.MinValue;
        }

        /// <returns>the local time without offset</returns>
        public double _GetRawTime()
        {
            if (HasStartedOk) {
                return TimeSource._TimeAsDouble();
            }

            _Error($"{nameof(_GetRawTime)}: Not initialized");
            return float.MinValue;
        }

        /// <returns>the local time + offset</returns>
        public double _GetAdjustedLocalTime()
        {
            if (HasStartedOk) {
                return TimeSource._TimeAsDouble() + ClockOffset;
            }

            _Error($"{nameof(_GetAdjustedLocalTime)}: Not initialized");
            return float.MinValue;
        }

        public void _AdjustRequestTiming()
        {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_AdjustRequestTiming));
#endif
            #endregion

            NextRequestTime += 0.1f * RequestInterval;
        }

        public bool _UpdateOffset(double requestReceiveTime, double responseSendTime, double responseReceiveTime)
        {
            #region TLP_DEBUG
#if TLP_DEBUG
            _Info(
                    $"{nameof(WorkingRequestSendTime)}: {WorkingRequestSendTime:F5}; " +
                    $"{nameof(requestReceiveTime)}: {requestReceiveTime:F5}; " +
                    $"{nameof(responseSendTime)}: {responseSendTime:F5}; " +
                    $"{nameof(responseReceiveTime)}: {responseReceiveTime:F5}");
#endif
            #endregion

            if (!HasStartedOk) {
                _Error($"{nameof(_UpdateOffset)}: Not initialized");
                return false;
            }

            double ping = _GetDelta(WorkingRequestSendTime, requestReceiveTime, responseSendTime, responseReceiveTime);
            if (ping < 0f) {
                _AdjustRequestTiming();
                _Warn($"Ping {ping} is negative, skipping offset update and adjusting request timing.");
                return false;
            }

            PingToMaster = ping;

            // failure cases already covered by GetDelta above
            _GetClockOffset(
                    WorkingRequestSendTime,
                    requestReceiveTime,
                    responseSendTime,
                    responseReceiveTime,
                    out double offsetSample);

            _SaveOffsetSample(offsetSample);
            double newAverageOffset = _GetNewAverageClockOffset();

#if TLP_DEBUG
            _DebugLog($"New client time offset: {newAverageOffset:F9} change: " +
                      $"{1000f * (newAverageOffset - ClockOffset):F3} ms; Ping to master: " +
                      $"{1000f * PingToMaster:F3}ms; Latency to master: {1000f * LatencyToMaster:F3}ms");
#endif
            ClockOffset = newAverageOffset;
            return true;
        }

        #region Static
        /// <summary>
        /// Round-Trip Delay: The total time taken for a message to travel from the client to the server and back,
        /// without the duration it takes the server to send its response.
        /// </summary>
        /// <param name="requestSendTime"></param>
        /// <param name="requestReceiveTime"></param>
        /// <param name="responseSendTime"></param>
        /// <param name="responseReceiveTime"></param>
        /// <returns>-1 if requestSendTime > responseReceiveTime or requestReceiveTime > responseSendTime</returns>
        public static double _GetDelta(
                double requestSendTime,
                double requestReceiveTime,
                double responseSendTime,
                double responseReceiveTime
        )
        {
            if (requestSendTime > responseReceiveTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(_GetDelta)}: {nameof(requestSendTime)} must be <= {nameof(responseReceiveTime)}");
                return -1f;
            }

            if (requestReceiveTime > responseSendTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(_GetDelta)}: {nameof(requestReceiveTime)} must be <= {nameof(responseSendTime)}");
                return -1f;
            }


            double clientWaitingTime = responseReceiveTime - requestSendTime;
            double serverResponseDelay = responseSendTime - requestReceiveTime;
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
        public static bool _GetClockOffset(
                double requestSendTime,
                double requestReceiveTime,
                double responseSendTime,
                double responseReceiveTime,
                out double offset
        )
        {
            if (requestSendTime > responseReceiveTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(_GetDelta)}: {nameof(requestSendTime)} must be <= {nameof(responseReceiveTime)}");
                offset = 0f;
                return false;
            }

            if (requestReceiveTime > responseSendTime) {
                Debug.LogError(
                        $"{nameof(NtpClient)}.{nameof(_GetDelta)}: {nameof(requestReceiveTime)} must be <= {nameof(responseSendTime)}");
                offset = 0f;
                return false;
            }

            double requestSendDuration = requestReceiveTime - requestSendTime;
            double responseSendDurationFlipped = responseSendTime - responseReceiveTime;
            offset = 0.5f * (requestSendDuration + responseSendDurationFlipped);
            return true;
        }
        #endregion
        #endregion

        #region TlpBase Overrides
        protected override bool _SetupAndValidate()
        {
            if (!base._SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(TimeSource)) {
                _Error($"{nameof(_SetupAndValidate)}: {nameof(TimeSource)} not set");
                return false;
            }

            if (!Utilities.IsValid(Server)) {
                _Error($"{nameof(_SetupAndValidate)}: {nameof(Server)} not set");
                return false;
            }

            if (!Utilities.IsValid(Server.NtpTime)) {
                _Error($"{nameof(_SetupAndValidate)}: {nameof(Server)}.{nameof(Server.NtpTime)} not set");
                return false;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (ClockOffsetSamples < 1) {
                _Error($"{nameof(ClockOffsetSamples)} must be >= 1");
                return false;
            }

            ClockOffsetHistory = new double[ClockOffsetSamples];
            Server.OwnNtpClient = Networking.IsOwner(gameObject) ? this : Server.OwnNtpClient;
            Server.NtpTime.NtpClient = Server.OwnNtpClient;
            Server.enabled = true;
            Server.NtpTime.enabled = true;

            return true;
        }
        #endregion

        #region Internal
        private double _GetNewAverageClockOffset()
        {
            double newAverageOffset = 0f;
            int maxSamples = ClockOffsetHistory._LengthSafe();
            int startIndex = ClockOffsetIndex;
            for (int i = CurrentClockOffsetSamples - 1; i >= 0; i--) {
                newAverageOffset += ClockOffsetHistory[startIndex] / CurrentClockOffsetSamples;
                startIndex._MoveIndexLeftLooping(maxSamples);
            }

            return newAverageOffset;
        }

        private void _SaveOffsetSample(double offset)
        {
            int maxSamples = ClockOffsetHistory._LengthSafe();
            ClockOffsetIndex._MoveIndexRightLooping(maxSamples);
            ClockOffsetHistory[ClockOffsetIndex] = offset;
            CurrentClockOffsetSamples = Mathf.Min(maxSamples, CurrentClockOffsetSamples + 1);
        }

        internal bool _RequestNtpSynchronization()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                _Error("Not Owner");
                return false;
            }

            _MarkNetworkDirty();
            RequestSerialization();
            return true;
        }
        #endregion
    }
}