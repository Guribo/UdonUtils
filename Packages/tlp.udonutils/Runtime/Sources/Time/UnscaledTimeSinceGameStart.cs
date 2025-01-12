using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// Implementation of <see cref="TimeSource"/>
    /// that returns the Unity time since the game was started./>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(UnscaledTimeSinceGameStart), ExecutionOrder)]
    public class UnscaledTimeSinceGameStart : TimeSource
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TimeSinceLevelLoad.ExecutionOrder + 1;

        /// <returns><see cref="UnityEngine.Time.unscaledTime"/></returns>
        public override float Time() {
            return UnityEngine.Time.unscaledTime;
        }

        /// <returns><see cref="UnityEngine.Time.unscaledTimeAsDouble"/></returns>
        public override double TimeAsDouble() {
            return UnityEngine.Time.unscaledTimeAsDouble;
        }
    }
}