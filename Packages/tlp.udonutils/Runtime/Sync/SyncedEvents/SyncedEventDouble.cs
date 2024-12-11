using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventDouble), ExecutionOrder)]
    public class SyncedEventDouble : SyncedEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventBool.ExecutionOrder + 1;

        [UdonSynced]
        internal double SyncedValue;

        [NonSerialized]
        public double WorkingValue;

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