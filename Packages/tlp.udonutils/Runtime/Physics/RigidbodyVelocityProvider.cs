using TLP.UdonUtils.Extensions;
using TLP.UdonUtils.Sources;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Physics
{
    /// <summary>
    /// uses rigidbody velocity and calculates acceleration after everything that can affect physics of objects which
    /// should be everything except audio
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class RigidbodyVelocityProvider : VelocityProvider
    {
        #region Dependencies
        [SerializeField]
        internal Rigidbody ToTrack;

        [SerializeField]
        internal TimeSource TimeSource;
        #endregion

        #region State
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
                    TimeSource.Time()
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
        public override float GetLatestSnapShot(
                out Vector3 position,
                out Vector3 velocity,
                out Vector3 acceleration,
                out Quaternion rotation,
                out Vector3 angularVelocity,
                out Vector3 angularAcceleration,
                out Transform relativeTo) {
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
                Vector3 rigidbodyVelocity,
                float deltaTime,
                float time) {
            _velocity[0] = _velocity[1];
            _velocity[1] = _velocity[2];

            _acceleration[0] = _acceleration[1];
            _acceleration[1] = _acceleration[2];

            _velocity[2] = RelativeTo.InverseTransformVector(rigidbodyVelocity);
            _acceleration[2] = (_velocity[2] - _velocity[1]) / deltaTime;

            _velocityTime[2] = time;
            _accelerationTime[2] = time;

            _position = RelativeTo.InverseTransformPoint(worldPosition);
        }
        #endregion
    }
}