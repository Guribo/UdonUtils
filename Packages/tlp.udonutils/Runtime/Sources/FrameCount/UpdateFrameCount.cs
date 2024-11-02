using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.FrameCount
{
    /// <summary>
    /// Implementation of <see cref="FrameCountSource"/>
    /// that returns the current frame count of Unity.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(UpdateFrameCount), ExecutionOrder)]
    public class UpdateFrameCount : FrameCountSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ConstantFrameCount.ExecutionOrder + 1;

        /// <returns><see cref="Time.frameCount"/></returns>
        public override int Frame() {
            return UnityEngine.Time.frameCount;
        }
    }
}