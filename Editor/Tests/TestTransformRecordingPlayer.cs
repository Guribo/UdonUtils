using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Recording;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestTransformRecordingPlayer
    {
        internal TransformRecorder TransformRecorder;
        internal TransformRecordingPlayer TransformRecordingPlayer;

        [SetUp]
        public void Prepare()
        {
            var gameObject = new GameObject();
            TransformRecorder = gameObject.AddComponent<TransformRecorder>();
            TransformRecordingPlayer = gameObject.AddComponent<TransformRecordingPlayer>();
            TransformRecordingPlayer.transformRecorder = TransformRecorder;
            TransformRecorder.OnEnable();
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(TransformRecorder);
        }

        private const int iterations = 10000000;

        [Test]
        public void Play()
        {
            float initial = Time.realtimeSinceStartup;
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Restart();

            float sum = 0f;
            float previous = 0f;
            for (int i = 0; i < iterations; i++)
            {
                float realtimeSinceStartup = Time.realtimeSinceStartup;
                sum += realtimeSinceStartup - previous;
                previous = realtimeSinceStartup;
            }


            double elapsedTotalMilliseconds = stopwatch.Elapsed.TotalMilliseconds * 1E-3;
            float sinceStartup = Time.realtimeSinceStartup - initial;

            Debug.Log($"Elapsed StopWatch = {elapsedTotalMilliseconds}");
            Debug.Log($"Elapsed RealTime = {sinceStartup}");
            Debug.Log(sum / iterations);
        }

        [Test]
        public void PlayStopWatch()
        {
            float sum = 0f;
            float previous = 0f;
            float initial = Time.realtimeSinceStartup;
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                float realtimeSinceStartup = (float)stopwatch.Elapsed.TotalMilliseconds;
                sum += realtimeSinceStartup - previous;
                previous = realtimeSinceStartup;
            }

            double elapsedTotalMilliseconds = stopwatch.Elapsed.TotalMilliseconds * 1E-3;
            float sinceStartup = Time.realtimeSinceStartup - initial;

            Debug.Log($"Elapsed StopWatch = {elapsedTotalMilliseconds}");
            Debug.Log($"Elapsed RealTime = {sinceStartup}");
            Debug.Log(sum / iterations);
        }
    }
}