using UdonSharp;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Sync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TimeSnapshot : TlpBaseBehaviour
    {
        [FormerlySerializedAs("Time")]
        public float ServerTime;
    }
}