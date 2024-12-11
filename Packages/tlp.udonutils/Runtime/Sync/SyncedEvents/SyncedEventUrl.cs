using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventUrl), ExecutionOrder)]
    public class SyncedEventUrl : SyncedEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventStringArray.ExecutionOrder + 1;

        [UdonSynced]
        internal VRCUrl SyncedValue;

        [NonSerialized]
        public VRCUrl WorkingValue;

        public override void OnPreSerialization() {
            SyncedValue = WorkingValue;
            base.OnPreSerialization();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            WorkingValue = SyncedValue;
            base.OnDeserialization(deserializationResult);
        }
    }
}