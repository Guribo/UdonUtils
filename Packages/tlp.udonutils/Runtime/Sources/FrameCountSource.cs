using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Sources.Time;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources
{
    /// <summary>
    /// Base class for frame counts that allows being independent of Unity.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(FrameCountSource), ExecutionOrder)]
    public abstract class FrameCountSource : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Comparer.ExecutionOrder + 100;

        /// <summary>
        /// Implementation dependent number of frames.
        /// </summary>
        /// <returns>Number of frames</returns>
        public abstract int Frame();
    }
}