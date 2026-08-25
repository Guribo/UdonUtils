using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sources.FrameCount;
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
        public new const int ExecutionOrder = UpdateFrameCount.ExecutionOrder + 1;

        /// <summary>
        /// Current time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public abstract float _Time();

        /// <summary>
        /// Same as <see cref="_Time"/> with higher accuracy if available.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public abstract double _TimeAsDouble();

        /// <summary>
        /// Fixed delta time of this source for physics calculation.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float _FixedDeltaTime() {
            return UnityEngine.Time.fixedDeltaTime;
        }

        /// <summary>
        /// Smooth delta time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float _SmoothDeltaTime() {
            return UnityEngine.Time.smoothDeltaTime;
        }

        /// <summary>
        /// Delta time of this source.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float _DeltaTime() {
            return UnityEngine.Time.deltaTime;
        }

        /// <summary>
        /// Delta time of this source, not clamped or scaled.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public virtual float _UnscaledDeltaTime() {
            return UnityEngine.Time.unscaledDeltaTime;
        }
    }
}