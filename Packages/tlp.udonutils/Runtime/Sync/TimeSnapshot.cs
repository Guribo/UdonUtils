using UdonSharp;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Sync
{
    /// <summary>
    /// Container for a received network snapshot
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TimeSnapshot : TlpBaseBehaviour
    {
        [FormerlySerializedAs("Time")]
        public double ServerTime;
    }
}