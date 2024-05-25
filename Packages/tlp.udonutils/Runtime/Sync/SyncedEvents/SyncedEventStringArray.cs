using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedEventStringArray : SyncedEvent
    {
        [PublicAPI]
        [UdonSynced]
        public string[] Values = new string[0];
    }
}