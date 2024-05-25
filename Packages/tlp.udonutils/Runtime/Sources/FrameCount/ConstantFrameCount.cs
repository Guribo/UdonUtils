using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.Sources.FrameCount
{
    /// <summary>
    /// Implementation of the <see cref="FrameCountSource"/>
    /// that returns a constant value.
    /// </summary>
    public class ConstantFrameCount : FrameCountSource
    {
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