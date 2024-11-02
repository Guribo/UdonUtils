using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventFloat), ExecutionOrder)]
    public class SyncedEventFloat : SyncedEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventDouble.ExecutionOrder + 1;

        [UdonSynced]
        internal float SyncedValue;

        [NonSerialized]
        public float WorkingValue;

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