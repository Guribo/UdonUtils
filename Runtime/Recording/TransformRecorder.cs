using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Recording
{
    public class TransformRecorder : UdonSharpBehaviour
    {
        public float directionThresholdAngle = 10f;
        public float speedDifferenceThreshold = 0.1f;

        internal float StartTime;

        internal AnimationCurve PositionHistoryX = new AnimationCurve();
        internal AnimationCurve PositionHistoryY = new AnimationCurve();
        internal AnimationCurve PositionHistoryZ = new AnimationCurve();

        internal AnimationCurve RotationAxisX = new AnimationCurve();
        internal AnimationCurve RotationAxisY = new AnimationCurve();
        internal AnimationCurve RotationAxisZ = new AnimationCurve();
        internal AnimationCurve RotationAngle = new AnimationCurve();

        internal Vector3 PreviousAxis;
        internal float PreviousAngle;

        internal float PreviousTime;
        internal Vector3 PreviousPosition;
        internal Quaternion PreviousRotation;

        internal Vector3 PreviousDelta;
        internal Vector3 PreviousVelocity;

        public void OnEnable()
        {
            StartRecording();
        }

        public void OnDisable()
        {
            StopRecording();
        }

        internal void Record(float gameTime, float startTime, Transform t)
        {
            float time = gameTime - startTime;

            var position = t.position;
            var currentRotation = t.rotation;

            var newDelta = position - PreviousPosition;
            var newVelocity = newDelta / Time.deltaTime;
            if (!IsCloseToDirection(PreviousDelta, newDelta, directionThresholdAngle)
                || !SameLength(PreviousVelocity, newVelocity, speedDifferenceThreshold))
            {
                PositionHistoryX.AddKey(PreviousTime, PreviousPosition.x);
                PositionHistoryY.AddKey(PreviousTime, PreviousPosition.y);
                PositionHistoryZ.AddKey(PreviousTime, PreviousPosition.z);

                PreviousRotation.ToAngleAxis(out PreviousAngle, out PreviousAxis);
                RotationAxisX.AddKey(PreviousTime, PreviousAxis.x);
                RotationAxisY.AddKey(PreviousTime, PreviousAxis.y);
                RotationAxisZ.AddKey(PreviousTime, PreviousAxis.z);
                RotationAngle.AddKey(PreviousTime, PreviousAngle);

                PreviousDelta = newDelta;
                PreviousVelocity = newVelocity;
            }
            else
            {
                PreviousDelta += newDelta;
            }

            PreviousRotation = currentRotation;
            PreviousPosition = position;
            PreviousTime = time;
        }

        internal void StartRecording()
        {
            PositionHistoryX = new AnimationCurve();
            PositionHistoryY = new AnimationCurve();
            PositionHistoryZ = new AnimationCurve();

            RotationAxisX = new AnimationCurve();
            RotationAxisY = new AnimationCurve();
            RotationAxisZ = new AnimationCurve();
            RotationAngle = new AnimationCurve();

            StartTime = Time.time;

            PreviousTime = 0f;
            PreviousPosition = transform.position;
            PreviousDelta = Vector3.zero;
            PreviousVelocity = Vector3.zero;

            PreviousAxis = Vector3.forward;
            PreviousAngle = 0f;
        }

        public Vector3 GetPosition(float time)
        {
            return new Vector3(
                PositionHistoryX.Evaluate(time),
                PositionHistoryY.Evaluate(time),
                PositionHistoryZ.Evaluate(time)
            );
        }

        public Quaternion GetRotation(float time)
        {
            return Quaternion.AngleAxis(
                RotationAngle.Evaluate(time),
                new Vector3(
                    RotationAxisX.Evaluate(time),
                    RotationAxisY.Evaluate(time),
                    RotationAxisZ.Evaluate(time)
                )
            );
        }

        public override void PostLateUpdate()
        {
            Record(Time.time, StartTime, transform);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldDelta"></param>
        /// <param name="newDelta"></param>
        /// <param name="threshold">threshold angle in degree beyond which the new direction is not considered close</param>
        /// <returns></returns>
        internal bool IsCloseToDirection(Vector3 oldDelta, Vector3 newDelta, float threshold)
        {
            return Vector3.Angle(oldDelta, newDelta) <= threshold;
        }

        internal bool SameLength(Vector3 oldDelta, Vector3 newDelta, float threshold)
        {
            float length = oldDelta.magnitude;
            if (length == 0f)
            {
                return newDelta == oldDelta;
            }

            float lengthDelta = Mathf.Abs(length - newDelta.magnitude);
            return lengthDelta / length <= threshold;
        }

        internal void StopRecording()
        {
            PositionHistoryX.AddKey(PreviousTime, PreviousPosition.x);
            PositionHistoryY.AddKey(PreviousTime, PreviousPosition.y);
            PositionHistoryZ.AddKey(PreviousTime, PreviousPosition.z);

            PreviousRotation.ToAngleAxis(out PreviousAngle, out PreviousAxis);
            RotationAxisX.AddKey(PreviousTime, PreviousAxis.x);
            RotationAxisY.AddKey(PreviousTime, PreviousAxis.y);
            RotationAxisZ.AddKey(PreviousTime, PreviousAxis.z);
            RotationAngle.AddKey(PreviousTime, PreviousAngle);
        }
    }
}