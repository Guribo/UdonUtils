namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// that returns the Unity time since the level was loaded./>
    /// </summary>
    public class TimeSinceLevelLoad : TimeSource
    {
        /// <returns><see cref="UnityEngine.Time.timeSinceLevelLoad"/></returns>
        public override float Time() {
            return UnityEngine.Time.timeSinceLevelLoad;
        }

        /// <returns><see cref="UnityEngine.Time.timeSinceLevelLoadAsDouble"/></returns>
        public override double TimeAsDouble() {
            return UnityEngine.Time.timeSinceLevelLoadAsDouble;
        }
    }
}