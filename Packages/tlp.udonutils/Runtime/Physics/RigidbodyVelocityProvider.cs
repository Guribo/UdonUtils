using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Sources;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Physics
{
    /// <summary>
    /// uses rigidbody velocity and calculates acceleration after everything that can affect physics of objects which
    /// should be everything except audio
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RigidbodyVelocityProvider), ExecutionOrder)]
    public class RigidbodyVelocityProvider : VelocityProvider
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = FixedUpdateVelocityProvider.ExecutionOrder + 1;
        #region Dependencies
        public Rigidbody ToTrack;

        [SerializeField]
        internal TimeSource TimeSource;
        #endregion

        #region Settings
        [Tooltip(
                "Turnrate in degrees/second considered circular movement. " +
                "Lower values increase the chance for motion to be considered circular.")]
        [Range(5, 90)]
        public float CircularTurnThreshold = 15f;
        #endregion

        #region State
        private float _circleTurnRate;
        private Vector3 _position;
        private Quaternion _rotation;
        #endregion

        #region U# Lifecycle
        private void FixedUpdate() {
            var trackTransform = ToTrack.transform;
            _UpdatePositionSnapshot(
                    trackTransform.position,
                    ToTrack.velocity,
                    TimeSource.FixedDeltaTime(),
                    TimeSource.TimeAsDouble()
            );

            _UpdateRotationSnapshot(
                    trackTransform.rotation,
                    ToTrack.angularVelocity,
                    TimeSource.FixedDeltaTime()
            );

#if TLP_DEBUG
            UpdateDebugEditorValues();
#endif
        }
        #endregion

        #region Public
        public override double GetLatestSnapShot(
                out Vector3 position,
                out Vector3 velocity,
                out Vector3 acceleration,
                out Quaternion rotation,
                out Vector3 angularVelocity,
                out Vector3 angularAcceleration,
                out Transform relativeTo,
                out float circleAngularVelocityDegrees
        ) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(GetLatestSnapShot));
#endif
            #endregion

            position = _position;
            velocity = _velocity[2];
            acceleration = _acceleration[2];
            rotation = _rotation;
            angularVelocity = _angularVelocity[2];
            angularAcceleration = _angularAcceleration[2];
            relativeTo = RelativeTo;
            circleAngularVelocityDegrees = _circleTurnRate;

            return _accelerationTime[2];
        }

        public void _UpdateRotationSnapshot(Quaternion worldRotation, Vector3 angularVelocity, float deltaTime) {
            _angularVelocity[0] = _angularVelocity[1];
            _angularVelocity[1] = _angularVelocity[2];

            _angularAcceleration[0] = _angularAcceleration[1];
            _angularAcceleration[1] = _angularAcceleration[2];

            _angularVelocity[2] = RelativeTo.InverseTransformVector(angularVelocity);
            _angularAcceleration[2] = (_angularVelocity[2] - _angularVelocity[1]) / deltaTime;


            _rotation = RelativeTo.rotation.normalized.GetDeltaAToB(worldRotation.normalized).normalized;
        }
        #endregion

        #region Internal
        internal void _UpdatePositionSnapshot(
                Vector3 worldPosition,
                Vector3 rigidBodyVelocity,
                float deltaTime,
                double time
        ) {
            _velocity[0] = _velocity[1];
            _velocity[1] = _velocity[2];

            _acceleration[0] = _acceleration[1];
            _acceleration[1] = _acceleration[2];

            _velocity[2] = RelativeTo.InverseTransformVector(rigidBodyVelocity);

            _acceleration[2] = ConstantLinearAcceleration.Acceleration2(_velocity[1], _velocity[2], deltaTime);

            if (!ConstantCircularVelocity.IsCircularMovement(
                        _velocity[2],
                        _velocity[1],
                        deltaTime,
                        out _circleTurnRate,
                        CircularTurnThreshold,
                        0.003f)) {
                _circleTurnRate = 0;
            }

            _velocityTime[2] = time;
            _accelerationTime[2] = time;

            _position = RelativeTo.InverseTransformPoint(worldPosition);
        }
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(ToTrack)) {
                Error($"{nameof(ToTrack)} not set");
                return false;
            }

            if (!Utilities.IsValid(TimeSource)) {
                Error($"{nameof(TimeSource)} not set");
                return false;
            }

            return true;
        }
        #endregion
    }
}