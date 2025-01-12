using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// <see cref="TimeSource"/> that always returns the same value.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ConstantTime), ExecutionOrder)]
    public class ConstantTime : TimeSource
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TimeSource.ExecutionOrder + 1;
        #region Settings
        [Tooltip("Constant time returned by Time and TimeAsDouble")]
        [FormerlySerializedAs("Value")]
        public double Seconds;

        [Tooltip("Constant time returned by SmoothDeltaTime")]
        public float SmoothDeltaTimeSeconds;

        [Tooltip("Constant time returned by FixedDeltaTime")]
        public float FixedDeltaTimeSeconds;

        [Tooltip("Constant time returned by DeltaTime")]
        public float DeltaTimeSeconds;

        [Tooltip("Constant time returned by UnscaledDeltaTime")]
        public float UnscaledDeltaTimeSeconds;
        #endregion

        #region TimeSource Overrides
        /// <summary>
        /// Returns the constant <see cref="Seconds"/>.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public override float Time() {
            return (float)Seconds;
        }

        /// <returns>the constant <see cref="Seconds"/></returns>
        public override double TimeAsDouble() {
            return Seconds;
        }

        /// <returns>the constant <see cref="FixedDeltaTimeSeconds"/></returns>
        public override float FixedDeltaTime() {
            return FixedDeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="SmoothDeltaTimeSeconds"/></returns>
        public override float SmoothDeltaTime() {
            return SmoothDeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="DeltaTimeSeconds"/></returns>
        public override float DeltaTime() {
            return DeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="UnscaledDeltaTime"/></returns>
        public override float UnscaledDeltaTime() {
            return UnscaledDeltaTimeSeconds;
        }
        #endregion
    }
}