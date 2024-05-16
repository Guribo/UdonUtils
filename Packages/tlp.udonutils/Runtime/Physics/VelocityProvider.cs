using JetBrains.Annotations;
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
    public abstract class VelocityProvider : TlpBaseBehaviour
    {
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        // after everything that can affect locations and physics of objects which should be everything except audio
        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.UiEnd + 1;

        public Transform RelativeTo;

        /// <summary>
        /// moving average: most recent 2 values
        /// </summary>
        public Vector3 AccelerationAvg2 => 0.5f * (_acceleration[1] + _acceleration[2]);

        /// <summary>
        /// moving average: most recent 3 values
        /// </summary>
        public Vector3 AccelerationAvg3 =>
                0.33333333333334f * (_acceleration[0] + _acceleration[1] + _acceleration[2]);

        protected internal Vector3[] _acceleration = new Vector3[3];
        protected Vector3[] _velocity = new Vector3[3];
        protected double[] _velocityTime = new double[3];
        protected double[] _accelerationTime = new double[3];
        protected Vector3[] _angularVelocity = new Vector3[3];
        protected internal Vector3[] _angularAcceleration = new Vector3[3];

        private void OnEnable() {
            Clear();
        }

        private void OnDisable() {
            Clear();
        }

        public virtual void Clear() {
            for (int i = 0; i < _velocity.Length; i++) {
                _velocity[i] = Vector3.zero;
            }

            for (int i = 0; i < _acceleration.Length; i++) {
                _acceleration[i] = Vector3.zero;
            }

            for (int i = 0; i < _velocityTime.Length; i++) {
                _velocityTime[i] = 0;
            }

            for (int i = 0; i < _accelerationTime.Length; i++) {
                _accelerationTime[i] = 0;
            }
        }

        public virtual void SetTeleported(bool keepVelocity = true) {
            if (!keepVelocity) {
                Clear();
            }
        }

#if TLP_DEBUG

        [SerializeField]
        private float DebugVelocity;

        [SerializeField]
        private float DebugAccelerationInGs;

        [SerializeField]
        private float DebugAccelerationInGs2x;

        [SerializeField]
        private float DebugAccelerationInGs3x;

        [SerializeField]
        private float DebugAgeVelocity;

        [SerializeField]
        private float DebugAgeAcceleration;

        protected void UpdateDebugEditorValues() {
            double time = GetLatestSnapShot(
                    out var position,
                    out var velocity,
                    out var acceleration,
                    out var _unused,
                    out var __unused,
                    out var ___unused,
                    out var unused,
                    out var ____unused
            );
            DebugVelocity = velocity.magnitude;
            DebugAccelerationInGs = acceleration.magnitude / 9.81f;
            DebugAccelerationInGs2x = AccelerationAvg2.magnitude / 9.81f;
            DebugAccelerationInGs3x = AccelerationAvg3.magnitude / 9.81f;
        }
#endif
        public abstract double GetLatestSnapShot(
                out Vector3 position,
                out Vector3 velocity,
                out Vector3 acceleration,
                out Quaternion rotation,
                out Vector3 angularVelocity,
                out Vector3 angularAcceleration,
                out Transform relativeTo,
                out float circleAngularVelocityDegrees
        );
    }
}