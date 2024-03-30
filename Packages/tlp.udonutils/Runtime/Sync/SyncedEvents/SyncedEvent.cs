using JetBrains.Annotations;
using TLP.UdonUtils.Events;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedEvent : UdonEvent
    {
        #region Executionorder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;

        [Tooltip(
                "If enabled synchronisation is requested instantly instead of on the next frame, " +
                "off by default"
        )]
        public bool FastSync;
        #endregion

        [UdonSynced]
        public int Calls;

        /// <summary>
        /// Raises the event locally and remotely
        /// </summary>
        /// <param name="instigator"></param>
        /// <returns></returns>
        public override bool Raise(TlpBaseBehaviour instigator) {
            if (!MarkNetworkDirty()) {
                // as this is a networked event treat it as a failure when the player can not request sync
                return false;
            }

            if (!base.Raise(instigator)) {
                DropPendingSerializations();
                return false;
            }

            ++Calls; // can be changed here as the actual sync request is delayed by one frame
            if (FastSync) {
                // skip the default delay of 1 frame imposed by MarkNetworkDirty(), may lead to more network traffic
                RequestSerialization();
            }

            return true;
        }

        /// <summary>
        /// Raises the event on all non-local clients
        /// </summary>
        /// <param name="instigator"></param>
        /// <returns></returns>
        public bool RaiseRemoteOnly(TlpBaseBehaviour instigator) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(RaiseRemoteOnly));
#endif
            #endregion

            if (!MarkNetworkDirty()) {
                // as this is a networked event treat it as a failure when the player can not request sync
                return false;
            }

            ++Calls; // can be changed here as the actual sync request is delayed by one frame
            if (FastSync) {
                // skip the default delay of 1 frame imposed by MarkNetworkDirty(), may lead to more network traffic
                RequestSerialization();
            }

            return true;
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            // raise without requesting another serialization by using the base implementation
            base.Raise(this);
        }

        public override void OnPostSerialization(SerializationResult result) {
            base.OnPostSerialization(result);

            if (result.success) {
                Calls = 0;
            }
        }
    }
}