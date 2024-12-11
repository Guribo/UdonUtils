using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Sources.Time
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RealTimeAtFrameStart), ExecutionOrder)]
    public class RealTimeAtFrameStart : TimeSource
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Min + 3;
        #endregion

        private double _realTime;
        private int _frameOfUpdate;

        private void Update() {
            if (_frameOfUpdate == UnityEngine.Time.frameCount) return;
            _realTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _frameOfUpdate = UnityEngine.Time.frameCount;
        }

        public override float Time() {
            if (_frameOfUpdate != UnityEngine.Time.frameCount) {
                Update();
            }

            return (float)_realTime;
        }

        public override double TimeAsDouble() {
            if (_frameOfUpdate != UnityEngine.Time.frameCount) {
                Update();
            }

            return _realTime;
        }
    }
}