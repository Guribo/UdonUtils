using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(SyncedEventIntArray), ExecutionOrder)]
    public class SyncedEventIntArray : SyncedEvent
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventInt.ExecutionOrder + 1;

        [UdonSynced]
        internal int[] SyncedValues = new int[0];

        [NonSerialized]
        public int[] WorkingValues = new int[0];

        public override void OnPreSerialization() {
            WorkingValues.CreateCopy(ref SyncedValues);
            base.OnPreSerialization();
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            SyncedValues.CreateCopy(ref WorkingValues);
            base.OnDeserialization(deserializationResult);
        }
    }
}