using UdonSharp;

namespace TLP.UdonUtils.Runtime.Sources
{
    /// <summary>
    /// Base class for frame counts that allows being independent of Unity.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FrameCountSource : TlpBaseBehaviour
    {
        /// <summary>
        /// Implementation dependent number of frames.
        /// </summary>
        /// <returns>Number of frames</returns>
        public abstract int Frame();
    }
}