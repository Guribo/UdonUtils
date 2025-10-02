using System.Diagnostics;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Testing
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(FrameRateRandomizer), ExecutionOrder)]
    public class FrameRateRandomizer : TlpBaseBehaviour
    {
        #region ExecutionOrder
        [PublicAPI]
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;
        #endregion

        [Tooltip("If set to 0 it defaults to the fixed update rate")]
        public int UpperFpsLimit;

        [Tooltip("If set to 0 it defaults to 10")]
        public int LowerFpsLimit = 10;

        #region State
        private readonly Stopwatch _stopWatch = new Stopwatch();
        #endregion

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            UpperFpsLimit = UpperFpsLimit == 0 ? Mathf.RoundToInt(1f / Time.fixedDeltaTime) : UpperFpsLimit;
            LowerFpsLimit = LowerFpsLimit == 0 ? 10 : LowerFpsLimit;
            _stopWatch.Start();
            return true;
        }

        public void Update() {
            // artificially make framerate fluctuate
            int targetRate = Random.Range(LowerFpsLimit, UpperFpsLimit);
            double elapsed = _stopWatch.Elapsed.TotalSeconds;
            _stopWatch.Restart();
            while (_stopWatch.Elapsed.TotalSeconds + elapsed < 1f / targetRate) {
                // Just loop until the time has passed
            }

            _stopWatch.Restart();
        }
    }
}