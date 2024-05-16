using TLP.UdonUtils.Sources;
using UdonSharp;
using UnityEngine;

namespace TLP.UdonUtils.Physics
{
    /// <summary>
    /// calculates velocity and acceleration after everything that can affect locations and physics of objects which
    /// should be everything except audio
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class FixedUpdateVelocityProvider : VelocityProvider
    {
        private readonly Vector3[] _position = new Vector3[3];

        #region Dependencies
        [SerializeField]
        private Transform ToTrack;

        [SerializeField]
        internal TimeSource TimeSource;
        #endregion

        #region U# Lifecycle
        private void FixedUpdate() {
            _velocity[0] = _velocity[1];
            _velocity[1] = _velocity[2];

            _velocityTime[0] = _velocityTime[1];
            _velocityTime[1] = _velocityTime[2];

            _acceleration[0] = _acceleration[1];
            _acceleration[1] = _acceleration[2];

            _position[0] = _position[1];
            _position[1] = _position[2];
            _position[2] = RelativeTo.InverseTransformPoint(ToTrack.position);

            // central difference method to calculate velocity (adds 1 frame of age)
            float fixedDeltaTime = TimeSource.FixedDeltaTime();
            _velocityTime[2] = TimeSource.TimeAsDouble() - fixedDeltaTime;
            _velocity[2] = (_position[2] - _position[0]) / (2 * fixedDeltaTime);

            // central difference method to calculate velocity (adds a second frame of age)
            _accelerationTime[2] = _velocityTime[1];

            double delta = _velocityTime[2] - _velocityTime[0];

            if (delta != 0) {
                _acceleration[2] = (_velocity[2] - _velocity[0]) / (float)delta;
            }

#if TLP_DEBUG
            UpdateDebugEditorValues();
#endif
        }
        #endregion

        #region Public
        public override void SetTeleported(bool keepVelocity = true) {
            base.SetTeleported(keepVelocity);
            if (!keepVelocity) {
                return;
            }

            var position = RelativeTo.InverseTransformPoint(ToTrack.position);
            for (int i = 0; i < _position.Length; i++) {
                _position[i] = position;
            }
        }

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
            position = _position[0];
            velocity = _velocity[1];
            acceleration = _acceleration[2];
            rotation = Quaternion.identity;
            angularVelocity = Vector3.zero;
            angularAcceleration = Vector3.zero;
            relativeTo = RelativeTo;
            circleAngularVelocityDegrees = 0;

            return _accelerationTime[2];
        }

        public override void Clear() {
            base.Clear();
            var position = RelativeTo.InverseTransformPoint(ToTrack.position);
            for (int i = 0; i < _position.Length; i++) {
                _position[i] = position;
            }
        }
        #endregion
    }
}