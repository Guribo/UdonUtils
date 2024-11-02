using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.Sources.FrameCount
{
    /// <summary>
    /// Implementation of the <see cref="FrameCountSource"/>
    /// that returns a constant value.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ConstantFrameCount), ExecutionOrder)]
    public class ConstantFrameCount : FrameCountSource
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = FrameCountSource.ExecutionOrder + 1;

        /// <summary>
        /// Constant value returned by the method <see cref="Frame"/>
        /// </summary>
        [Tooltip("Constant value returned by the method Frame")]
        [FormerlySerializedAs("Value")]
        public int Frames;

        /// <summary>
        ///
        /// </summary>
        /// <returns>Value of <see cref="Frames"/></returns>
        public override int Frame() {
            return Frames;
        }
    }
}