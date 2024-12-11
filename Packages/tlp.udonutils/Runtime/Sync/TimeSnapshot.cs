using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Sources;
using TLP.UdonUtils.Runtime.Sources.Time;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.Sync
{
    /// <summary>
    /// Container for a received network snapshot
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TimeSnapshot), ExecutionOrder)]
    public abstract class TimeSnapshot : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = FrameCountSource.ExecutionOrder + 100;

        [FormerlySerializedAs("Time")]
        public double ServerTime;
    }
}