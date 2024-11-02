using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// that returns the Unity time since the level was loaded./>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TimeSinceLevelLoad), ExecutionOrder)]
    public class TimeSinceLevelLoad : TimeSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = StopwatchTime.ExecutionOrder + 1;


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