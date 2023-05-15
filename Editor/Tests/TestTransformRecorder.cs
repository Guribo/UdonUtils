using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Recording;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestTransformRecorder
    {
        private TransformRecorder m_TransformRecorder;

        [SetUp]
        public void Prepare()
        {
            m_TransformRecorder = new GameObject().AddComponent<TransformRecorder>();
            m_TransformRecorder.OnEnable();
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_TransformRecorder);
        }

        [Test]
        public void Record()
        {
            var transform = m_TransformRecorder.transform;
            var expected0 = Vector3.forward;
            transform.position = expected0;
            m_TransformRecorder.StartRecording();
            m_TransformRecorder.StartTime = 0;
            m_TransformRecorder.Record(0, 0, transform);


            var expected1 = Vector3.up;
            transform.position = expected1;
            m_TransformRecorder.Record(1, 0, transform);
            m_TransformRecorder.StopRecording();

            Assert.AreEqual(expected0, m_TransformRecorder.GetPosition(0));
            Assert.AreEqual(expected1, m_TransformRecorder.GetPosition(1));
            Assert.AreEqual(Vector3.Lerp(expected0, expected1, 0.5f), m_TransformRecorder.GetPosition(0.5f));
        }

        [Test]
        public void Record_SkipsRecording()
        {
            var transform = m_TransformRecorder.transform;
            var expected0 = Vector3.forward;
            transform.position = expected0;

            m_TransformRecorder.StartRecording();
            m_TransformRecorder.StartTime = 0;
            m_TransformRecorder.Record(0, 0, transform);

            var expected1 = Vector3.forward * 2f;
            transform.position = expected1;
            m_TransformRecorder.Record(1, 0, transform);

            var expected2 = Vector3.forward * 3f;
            transform.position = expected2;
            m_TransformRecorder.Record(2, 0, transform);

            var expected3 = Vector3.forward * 4f;
            transform.position = expected3;
            m_TransformRecorder.Record(3, 0, transform);

            var expected4 = Vector3.forward * 5f;
            transform.position = expected4;
            m_TransformRecorder.Record(4, 0, transform);

            var expected5 = Vector3.forward * 5f;
            transform.position = expected5;
            m_TransformRecorder.Record(5, 0, transform);

            var expected6 = expected5 + Vector3.right;
            transform.position = expected6;
            m_TransformRecorder.Record(6, 0, transform);

            m_TransformRecorder.StopRecording();
            Assert.AreEqual(4, m_TransformRecorder.PositionHistoryX.length);

            Assert.AreEqual(expected0, m_TransformRecorder.GetPosition(0));
            Assert.AreEqual(new Vector3(0.000000f, 0.000000f, 2.093750f), m_TransformRecorder.GetPosition(1));
            Assert.AreEqual(new Vector3(0.000000f, 0.000000f, 3.250000f), m_TransformRecorder.GetPosition(2));
            Assert.AreEqual(new Vector3(0.000000f, 0.000000f, 4.281250f), m_TransformRecorder.GetPosition(3));
            Assert.AreEqual(expected4, m_TransformRecorder.GetPosition(4));
            Assert.AreEqual(expected5, m_TransformRecorder.GetPosition(5));
            Assert.AreEqual(expected6, m_TransformRecorder.GetPosition(6));
        }

        [Test]
        public void StartRecording()
        {
            m_TransformRecorder.StartTime = 0f;
            m_TransformRecorder.StartRecording();
            Assert.AreEqual(Time.time, m_TransformRecorder.StartTime);
        }


        [Test]
        public void IsCloseToDirection_SameDirectionNoThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.forward;
            Assert.True(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 0));
        }

        [Test]
        public void IsCloseToDirection_DifferentDirectionNoThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.back;
            Assert.False(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 0));
        }

        [Test]
        public void IsCloseToDirection_DifferentDirectionHighThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.forward + Vector3.up;
            Assert.True(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 50));
        }

        [Test]
        public void IsCloseToDirection_OppositeDirectionMaxThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.back;
            Assert.True(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 180));
        }

        [Test]
        public void IsCloseToDirection_Zero()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.zero;
            Assert.True(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 0));

            oldDelta = Vector3.zero;
            newDelta = Vector3.zero;
            Assert.True(m_TransformRecorder.IsCloseToDirection(oldDelta, newDelta, 0));
        }

        [Test]
        public void SameLength_Same()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.forward;
            Assert.True(m_TransformRecorder.SameLength(oldDelta, newDelta, 0));
        }

        [Test]
        public void SameLength_DifferentLengthMinThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.back * 2f;
            Assert.False(m_TransformRecorder.SameLength(oldDelta, newDelta, 0));
        }

        [Test]
        public void SameLength_DifferentLengthBelowThreshold()
        {
            var oldDelta = Vector3.forward;
            var newDelta = Vector3.forward * 1.1f;
            Assert.True(m_TransformRecorder.SameLength(oldDelta, newDelta, 0.2f));
        }

        [Test]
        public void SameLength_Zero()
        {
            var oldDelta = Vector3.zero;
            var newDelta = Vector3.zero;
            Assert.True(m_TransformRecorder.SameLength(oldDelta, newDelta, 0.2f));
        }

        [Test]
        public void StopRecording()
        {
            m_TransformRecorder.StartRecording();
            m_TransformRecorder.StartTime = 0;
            m_TransformRecorder.PreviousTime = 0;
            m_TransformRecorder.StopRecording();
            Assert.AreEqual(1, m_TransformRecorder.PositionHistoryX.length);
            Assert.AreEqual(Vector3.zero, m_TransformRecorder.GetPosition(0));
        }
    }
}