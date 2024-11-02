using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    /// <summary>
    /// <see cref="TimeSource"/> that uses the local UTC time to provide
    /// the seconds since 1970/01/01 00:00.000000
    /// <remarks>Only offers a double based time value, float based is zero due to lack of accuracy!</remarks>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(UtcTimeSource), ExecutionOrder)]
    public class UtcTimeSource : TimeSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpNetworkTime.ExecutionOrder + 1;

        private DateTime _referenceTimeUtc;
        private bool _referenceTimeSet;

        /// <summary>
        /// Do not use as float is not accurate enough!
        /// <remarks>Use <see cref="TimeAsDouble"/> instead or enable <see cref="InReferenceToLevelLoad"/>.</remarks>
        /// </summary>
        /// <returns>returns 0.0f</returns>
        public override float Time() {
            Error($"{nameof(UtcTimeSource)} does not support float, use TimeAsDouble() instead");
            return 0f;
        }

        /// <returns>Utc time in reference to 1970/01/01 00:00.000000 </returns>
        public override double TimeAsDouble() {
            if (!_referenceTimeSet) {
                _referenceTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                _referenceTimeSet = true;
            }

            return (DateTime.UtcNow - _referenceTimeUtc).TotalSeconds;
        }
    }
}