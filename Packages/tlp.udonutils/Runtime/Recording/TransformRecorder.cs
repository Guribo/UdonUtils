using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace TLP.UdonUtils.Recording
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class TransformRecorder : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.Max;

        [FormerlySerializedAs("directionThresholdAngle")]
        public float DirectionThresholdAngle = 10f;

        [FormerlySerializedAs("speedDifferenceThreshold")]
        public float SpeedDifferenceThreshold = 0.1f;

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

        [SerializeField]
        internal Transform Target;

        public void OnEnable()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif

            #endregion

            if (!Utilities.IsValid(Target))
            {
                ErrorAndDisableComponent($"{nameof(Target)} is not set");
                return;
            }

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
            if (!IsCloseToDirection(PreviousDelta, newDelta, DirectionThresholdAngle)
                || !SameLength(PreviousVelocity, newVelocity, SpeedDifferenceThreshold))
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
            if (!Utilities.IsValid(Target))
            {
                ErrorAndDisableComponent($"{nameof(Target)} is not set");
                return;
            }

            PositionHistoryX = new AnimationCurve();
            PositionHistoryY = new AnimationCurve();
            PositionHistoryZ = new AnimationCurve();

            RotationAxisX = new AnimationCurve();
            RotationAxisY = new AnimationCurve();
            RotationAxisZ = new AnimationCurve();
            RotationAngle = new AnimationCurve();

            StartTime = Time.timeSinceLevelLoad;

            PreviousTime = 0f;
            PreviousPosition = Target.position;
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
            if (Utilities.IsValid(Target))
            {
                Record(Time.timeSinceLevelLoad, StartTime, Target);
            }
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