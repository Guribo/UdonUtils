using UdonSharp;

namespace TLP.UdonUtils.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class SnapshotHistory : UdonSharpBehaviour
    {
        public abstract bool _AddFromSnapshot(Snapshot snapshot);
    }
}