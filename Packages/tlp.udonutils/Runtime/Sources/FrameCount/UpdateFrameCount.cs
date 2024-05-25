using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.FrameCount
{
    /// <summary>
    /// Implementation of <see cref="FrameCountSource"/>
    /// that returns the current frame count of Unity.
    /// </summary>
    public class UpdateFrameCount : FrameCountSource
    {
        /// <returns><see cref="Time.frameCount"/></returns>
        public override int Frame() {
            return UnityEngine.Time.frameCount;
        }
    }
}