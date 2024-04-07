using TLP.UdonUtils.Testing;
using UnityEngine;

/// <summary>
/// Tests whether on the current frame the deltaTime is the same as the difference
/// between the current game time and the game time of the previous frame.
/// </summary>
public class TestGameTimeVsDeltaTime : TestCase
{
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