using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;


namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Uses a realtime based stopwatch that starts instantly as soon as this component is created.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(StopwatchTime), ExecutionOrder)]
    public class StopwatchTime : TimeSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ConstantTime.ExecutionOrder + 1;

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
