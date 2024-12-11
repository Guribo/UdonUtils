using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Testing
{
    /// <summary>
    /// Tests whether on the current frame the deltaTime is the same as the difference
    /// between the current game time and the game time of the previous frame.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestGameTimeVsDeltaTime), ExecutionOrder)]
    public class TestGameTimeVsDeltaTime : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestSanity.ExecutionOrder + 1;

        private float _previousGameTime;

        protected override void RunTest() {
            _previousGameTime = Time.timeSinceLevelLoad;
            SendCustomEventDelayedFrames(nameof(EvaluateNextFrame), 1);
        }

        public void EvaluateNextFrame() {
            float delta = Time.timeSinceLevelLoad - _previousGameTime;

            if (Mathf.Abs(delta - Time.deltaTime) > 0.00001f) {
                Error($"Delta time is {Time.deltaTime:F6}s but game time delta is {delta:F6}s!");
                TestController.TestCompleted(false);
            }

            Info($"Delta time is {Time.deltaTime:F6}s and game time delta is {delta:F6}s!");

            TestController.TestCompleted(true);
        }
    }
}