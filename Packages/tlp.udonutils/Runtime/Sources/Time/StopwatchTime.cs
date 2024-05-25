using System;
using System.Diagnostics;


namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Uses a realtime based stopwatch that starts instantly as soon as this component is created.
    /// </summary>
    public class StopwatchTime : TimeSource
    {
        [NonSerialized]
        public Stopwatch Stopwatch = new Stopwatch();

        public void OnEnable() {
            Stopwatch.Start();
        }

        public override float Time() {
            return (float)Stopwatch.Elapsed.TotalSeconds;
        }

        public override double TimeAsDouble() {
            return Stopwatch.Elapsed.TotalSeconds;
        }
    }
}
