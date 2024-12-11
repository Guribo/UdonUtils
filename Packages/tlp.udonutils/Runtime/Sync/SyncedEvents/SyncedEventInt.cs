using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventInt), ExecutionOrder)]
    public class SyncedEventInt : SyncedEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventFloat.ExecutionOrder + 1;

        [UdonSynced]
        internal int SyncedValue;

        [NonSerialized]
        public int WorkingValue;

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