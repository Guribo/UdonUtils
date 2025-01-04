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
    [TlpDefaultExecutionOrder(typeof(SyncedEventByteArray), ExecutionOrder)]
    public class SyncedEventByteArray : SyncedEvent
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = SyncedEventBool.ExecutionOrder + 1;

        [UdonSynced]
        internal byte[] SyncedValues = new byte[0];

        [NonSerialized]
        public byte[] WorkingValues = new byte[0];

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