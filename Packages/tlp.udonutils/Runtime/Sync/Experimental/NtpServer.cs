using System;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Adapters.Cyan;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sources.Time.Experimental;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.Experimental
{
    /// <summary>
    /// EXPERIMENTAL time sync that can replace Networking.ServerTimeInSeconds
    ///
    /// Server used by the master player for allowing other clients calculating a custom network time.
    /// Collects player request times and when these requests were received.
    /// In a fixed interval it sends all request receive times (with player info who each belongs to) and a response
    /// time to all clients.
    /// Clients can then calculate the time offset, ping and latency to the master.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class NtpServer : CyanPoolEventListener
    {
        #region ExecutionOrder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = NtpTime.ExecutionOrder - 1;
        #endregion

        #region Dependencies
        public NtpTime NtpTime;

        /// <summary>
        /// Set automatically when the CyanPlayerObjectPool assigns the locally owned <see cref="NtpClient"/>
        /// </summary>
        [HideInInspector]
        public NtpClient OwnNtpClient;
        #endregion

        #region Network State
        /// <summary>
        /// Timestamps in local NtpClient time when each client sync request was received.
        /// </summary>
        [HideInInspector]
        [UdonSynced]
        public float[] RequestReceiveTimes = new float[1];

        /// <summary>
        /// Player IDs of the clients who sent the sync request.
        /// Clients that didn't request anything yet again are set to 0.
        /// </summary>
        [HideInInspector]
        [UdonSynced]
        public int[] ClientOwners = new int[1];

        /// <summary>
        /// Timestamps in local NtpClient time when the server sent the response to all clients.
        /// </summary>
        [HideInInspector]
        [UdonSynced]
        public float ResponseSendTime;
        #endregion

        #region State
        internal DataDictionary ClientsRequestTimes = new DataDictionary();
        internal DataDictionary ClientIndices = new DataDictionary();
        internal DataList Clients = new DataList();

        private float _nextRequestTime = float.MinValue;
        internal int[] WorkingClientOwners = new int[1];
        internal float[] WorkingRequestReceiveTimes = new float[1];
        internal float WorkingResponseSentTime;
        #endregion

        #region Public
        public bool GetLatestClientRequest(NtpClient client, out float requestTime, out float receiveTime) {
            if (!ClientsRequestTimes.ContainsKey(client)) {
                requestTime = 0f;
                receiveTime = 0f;
                return false;
            }

            requestTime = ClientsRequestTimes[client].Float;
            receiveTime = WorkingRequestReceiveTimes[ClientIndices[client].Int];
            return true;
        }

        public bool AddRequest(NtpClient client, float receiveTime) {
            int clientIndex;
            if (!Utilities.IsValid(client)) {
                return false;
            }

            if (!ClientsRequestTimes.ContainsKey(client)) {
                ClientsRequestTimes.Add(client, client.RequestSendTime);

                Clients.Add(client);
                int clients = Clients.Count;
                clientIndex = clients - 1;
                ClientIndices.Add(client, clientIndex);

                WorkingRequestReceiveTimes = WorkingRequestReceiveTimes.ResizeOrCreate(clients);
                WorkingClientOwners = WorkingClientOwners.ResizeOrCreate(clients);

                WorkingClientOwners[clientIndex] = Networking.GetOwner(client.gameObject).PlayerIdSafe();
                WorkingRequestReceiveTimes[clientIndex] = receiveTime;
                return true;
            }

            ClientsRequestTimes[client] = client.RequestSendTime;
            clientIndex = ClientIndices[client].Int;
            if (clientIndex < 0 || clientIndex >= WorkingClientOwners.Length) {
                Error(
                        $"Index {clientIndex} is out of range of " +
                        $"{WorkingClientOwners}({WorkingClientOwners.Length} entries)");
                return false;
            }

            WorkingClientOwners[clientIndex] = Networking.GetOwner(client.gameObject).PlayerIdSafe();

            WorkingRequestReceiveTimes[clientIndex] = receiveTime;
            if (clientIndex >= WorkingRequestReceiveTimes.Length) {
                Error(
                        $"Index {clientIndex} is out of range of " +
                        $"{WorkingRequestReceiveTimes}({WorkingRequestReceiveTimes.Length} entries)");
                return false;
            }

            return true;
        }
        #endregion

        #region Lifecycle
        public override void OnPlayerLeft(VRCPlayerApi player) {
            base.OnPlayerLeft(player);

            RequestReceiveTimes = new float[1];
            ClientOwners = new int[1];
            ResponseSendTime = 0;
            ClientIndices.Clear();
            Clients.Clear();
            WorkingClientOwners = new int[1];
            WorkingRequestReceiveTimes = new float[1];
            WorkingResponseSentTime = 0;
        }

        public void Update() {
            var localPlayer = Networking.LocalPlayer;
            if (!localPlayer.IsMasterSafe()) {
                return;
            }

            if (!Utilities.IsValid(OwnNtpClient)) {
                return;
            }

            if (!Networking.IsOwner(localPlayer, gameObject)) {
                Networking.SetOwner(localPlayer, gameObject);
            }

            if (OwnNtpClient.GetRawTime() < _nextRequestTime) {
                return;
            }

            MarkNetworkDirty();
            RequestSerialization();
            _nextRequestTime = OwnNtpClient.GetRawTime() + OwnNtpClient.RequestInterval;
        }
        #endregion

        #region Network Events
        public override void OnPreSerialization() {
            base.OnPreSerialization();
            if (!Utilities.IsValid(OwnNtpClient)) {
                return;
            }

            RequestReceiveTimes = RequestReceiveTimes.ResizeOrCreate(WorkingRequestReceiveTimes.Length);
            ClientOwners = ClientOwners.ResizeOrCreate(WorkingClientOwners.Length);
            Array.Copy(WorkingRequestReceiveTimes, RequestReceiveTimes, WorkingRequestReceiveTimes.Length);
            Array.Copy(WorkingClientOwners, ClientOwners, WorkingClientOwners.Length);
            ResponseSendTime = OwnNtpClient.GetAdjustedLocalTime();
            WorkingResponseSentTime = ResponseSendTime;

            #region TLP_DEBUG
#if TLP_DEBUG
            string log = "";
            foreach (float receiveTime in RequestReceiveTimes) {
                log += $"{receiveTime:F4}, ";
            }

            Warn($"Response send time: {WorkingResponseSentTime:F9}");
            Warn($"Sending: {nameof(RequestReceiveTimes)}: {RequestReceiveTimes.LengthSafe()} elements; [{log}]");

            log = "";
            foreach (int clientId in ClientOwners) {
                log += $"{clientId}, ";
            }

            Warn($"Sending: {nameof(ClientOwners)}: {ClientOwners.LengthSafe()} elements; [{log}]");
#endif
            #endregion
        }

        public override void OnPostSerialization(SerializationResult result) {
            base.OnPostSerialization(result);
            if (result.success) {
                Array.Clear(WorkingRequestReceiveTimes, 0, WorkingRequestReceiveTimes.Length);
                Array.Clear(WorkingClientOwners, 0, WorkingClientOwners.Length);
            }
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);
            WorkingResponseSentTime = ResponseSendTime;
            if (!OwnNtpClient) {
                return;
            }

            float responseReceiveTime = OwnNtpClient.GetRawTime();

            #region TLP_DEBUG
#if TLP_DEBUG
            string log = "";
            foreach (float receiveTime in RequestReceiveTimes) {
                log += $"{receiveTime:F4}, ";
            }

            Warn($"Response receive time: {responseReceiveTime:F9}");
            Warn($"Received: {nameof(RequestReceiveTimes)}: {RequestReceiveTimes.LengthSafe()} elements; [{log}]");

            log = "";
            foreach (int clientId in ClientOwners) {
                log += $"{clientId}, ";
            }

            Warn($"Received: {nameof(ClientOwners)}: {ClientOwners.LengthSafe()} elements; [{log}]");
#endif
            #endregion

            int index = RetrieveLocalPlayerResponseData(out float requestReceiveTime);
            if (!Networking.IsOwner(Networking.LocalPlayer, OwnNtpClient.gameObject)) {
                Error($"Not owning {nameof(OwnNtpClient)}");
                return;
            }

            if (index == -1) {
                Warn("Player has not requested yet");
                OwnNtpClient.AdjustRequestTiming();
                return;
            }

            if (!OwnNtpClient.UpdateOffset(
                        requestReceiveTime,
                        ResponseSendTime,
                        responseReceiveTime)) {
                Warn($"Failed to update clock offset of own client '{OwnNtpClient.GetScriptPathInScene()}'");
            }
        }
        #endregion

        #region CyanPoolEventListener Overrides
        public override void _OnLocalPlayerAssigned() {
            base._OnLocalPlayerAssigned();
            if (!Utilities.IsValid(playerAssignedPoolObject)) {
                Error($"CyanPlayerObjectPool did not provide a valid {nameof(playerAssignedPoolObject)}");
                return;
            }

            var instance = playerAssignedPoolObject.gameObject;
            var ownNtpClient = instance.GetComponent<NtpClient>();
            if (!Utilities.IsValid(ownNtpClient)) {
                Error(
                        $"Failed to get {nameof(NtpClient)} from {nameof(playerAssignedPoolObject)} {instance.transform.GetPathInScene()}");
                return;
            }

            if (!Networking.IsOwner(Networking.LocalPlayer, ownNtpClient.gameObject)) {
                Error($"Assigned {nameof(NtpClient)} is not owned by local player");
                return;
            }

            OwnNtpClient = ownNtpClient;
            if (!Utilities.IsValid(NtpTime)) {
                Error($"{nameof(NtpTime)} not set");
                return;
            }

            NtpTime.NtpClient = OwnNtpClient;
        }
        #endregion


        #region Internal
        private int RetrieveLocalPlayerResponseData(out float requestReceiveTime) {
            int lengthSafe = ClientOwners.LengthSafe();
            int playerId = Networking.LocalPlayer.PlayerIdSafe();
            int index = -1;
            int requestTimes = RequestReceiveTimes.LengthSafe();
            requestReceiveTime = 0f;
            for (int i = 0; i < lengthSafe && i < requestTimes; i++) {
                if (ClientOwners[i] == playerId) {
                    requestReceiveTime = RequestReceiveTimes[i];
                    if (requestReceiveTime == 0f) {
                        continue;
                    }

                    index = i;
                    break;
                }
            }

            return index;
        }
        #endregion
    }
}