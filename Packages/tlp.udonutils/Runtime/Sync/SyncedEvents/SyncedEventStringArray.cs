using System;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedEventStringArray : SyncedEvent
    {
        [UdonSynced]
        internal string[] SyncedValues = new string[0];

        [NonSerialized]
        public string[] WorkingValues = new string[0];

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