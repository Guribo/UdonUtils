using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventString), ExecutionOrder)]
    public class SyncedEventString : SyncedEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventIntArray.ExecutionOrder + 1;

        [UdonSynced]
        internal string SyncedValue;

        [NonSerialized]
        public string WorkingValue;

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