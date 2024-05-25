using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Sync.SyncedEvents
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SyncedEventUrl : SyncedEvent
    {
        [PublicAPI]
        [UdonSynced]
        public VRCUrl Value;
    }
}