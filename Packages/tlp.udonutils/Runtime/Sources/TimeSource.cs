using UdonSharp;

namespace TLP.UdonUtils.Sources
{
    /// <summary>
    /// Base class for time sources.
    /// Used to be more independent from Unity.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TimeSource : TlpBaseBehaviour
    {
        /// <summary>
        /// Current time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public abstract float Time();

        /// <summary>
        /// Same as <see cref="Time"/> with higher accuracy if available.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public abstract double TimeAsDouble();

        /// <summary>
        /// Fixed delta time of this source for physics calculation.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float FixedDeltaTime() {
            return UnityEngine.Time.fixedDeltaTime;
        }

        /// <summary>
        /// Smooth delta time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float SmoothDeltaTime()
        {
            return UnityEngine.Time.smoothDeltaTime;
        }
    }
}
