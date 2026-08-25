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
        public override float _Time() {
            return (float)Seconds;
        }

        /// <returns>the constant <see cref="Seconds"/></returns>
        public override double _TimeAsDouble() {
            return Seconds;
        }

        /// <returns>the constant <see cref="FixedDeltaTimeSeconds"/></returns>
        public override float _FixedDeltaTime() {
            return FixedDeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="SmoothDeltaTimeSeconds"/></returns>
        public override float _SmoothDeltaTime() {
            return SmoothDeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="DeltaTimeSeconds"/></returns>
        public override float _DeltaTime() {
            return DeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="_UnscaledDeltaTime"/></returns>
        public override float _UnscaledDeltaTime() {
            return UnscaledDeltaTimeSeconds;
        }
        #endregion
    }
}