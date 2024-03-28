using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Sources.Time
{
    /// <summary>
    /// <see cref="TimeSource"/> that always returns the same value.
    /// </summary>
    public class ConstantTime : TimeSource
    {
        #region Settings

        [Tooltip("Constant time returned by Time and TimeAsDouble")]
        [FormerlySerializedAs("Value")]
        public double Seconds;

        [Tooltip("Constant time returned by SmoothDeltaTime")]
        public float SmoothDeltaTimeSeconds;

        [Tooltip("Constant time returned by FixedDeltaTime")]
        public float FixedDeltaTimeSeconds;

        #endregion

        #region TimeSource Overrides

        /// <summary>
        /// Returns the constant <see cref="Seconds"/>.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public override float Time()
        {
            return (float)Seconds;
        }

        /// <returns>the constant <see cref="Seconds"/></returns>
        public override double TimeAsDouble()
        {
            return Seconds;
        }

        /// <returns>the constant <see cref="FixedDeltaTimeSeconds"/></returns>
        public override float FixedDeltaTime()
        {
            return FixedDeltaTimeSeconds;
        }

        /// <returns>the constant <see cref="SmoothDeltaTimeSeconds"/></returns>
        public override float SmoothDeltaTime()
        {
            return SmoothDeltaTimeSeconds;
        }

        #endregion
    }
}