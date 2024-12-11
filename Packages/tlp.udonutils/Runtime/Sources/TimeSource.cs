using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources
{
    /// <summary>
    /// Base class for time sources.
    /// Used to be more independent from Unity.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TimeSource), ExecutionOrder)]
    public abstract class TimeSource : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TimeBacklog.ExecutionOrder + 1;

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
        public virtual float SmoothDeltaTime() {
            return UnityEngine.Time.smoothDeltaTime;
        }

        /// <summary>
        /// Delta time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float DeltaTime() {
            return UnityEngine.Time.deltaTime;
        }
    }
}