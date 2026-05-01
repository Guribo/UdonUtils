using UdonSharp;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    /// <summary>
    /// Empty base class for snapshot data containers.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class Snapshot : UdonSharpBehaviour // intentionally not TlpBaseBehaviour as it is only data
    {
        public double Time = double.MinValue;
        
        /// <summary>
        /// Copies all physics state values from another instance.
        /// </summary>
        /// <param name="other">The source physics state to copy from.</param>
        public virtual bool CopyFrom(Snapshot other) {
            if (!Utilities.IsValid(other)) {
                return false;
            }
            Time = other.Time;
            return true;
        }
    }
}