using TLP.UdonUtils.Runtime.Extensions;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Physics
{
    public static class RigidbodyExtensions
    {
        /// <summary>
        /// Moves rigidbody to the given position using velocity, automatically compensating for gravity.
        /// </summary>
        /// <param name="rigidbody">must be valid</param>
        /// <param name="position"></param>
        /// <param name="fixedDeltaTime"> must be > 0, if it is 0 then no movement occurs</param>
        public static void MoveToUsingVelocity(this Rigidbody rigidbody, Vector3 position, float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0) {
                // no movement possible with non-positive fixedDeltaTime
                return;
            }

            var delta = position - rigidbody.position;
            float speed = delta.magnitude / fixedDeltaTime;
            rigidbody.AddForce(-rigidbody.GetAccumulatedForce(), ForceMode.Force);
            var rigidbodyVelocity = delta.normalized * speed;
            rigidbody.velocity = rigidbodyVelocity;
            if (rigidbody.useGravity) {
                rigidbody.AddForce(-UnityEngine.Physics.gravity, ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// Calculates the angular velocity required to reach a target rotation in one fixed time step.
        /// </summary>
        /// <param name="rigidbody">The rigidbody whose current rotation is used as the starting point</param>
        /// <param name="targetRotation">The target rotation to reach</param>
        /// <param name="fixedDeltaTime">The time step to reach the target rotation in</param>
        /// <returns>Angular velocity vector in radians per second</returns>
        /// TODO FIXME
        public static Vector3 CalculateAngularVelocityToRotation(
                this Rigidbody rigidbody,
                Quaternion targetRotation,
                float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0) {
                return rigidbody.angularVelocity;
            }

            var currentRotation = rigidbody.rotation.normalized;
            var adjustedTargetRotation = targetRotation.normalized;
            
            // Handle double cover: -q and q represent the same rotation
            if (Quaternion.Dot(currentRotation, adjustedTargetRotation) < 0f) {
                adjustedTargetRotation = new Quaternion(
                        -adjustedTargetRotation.x,
                        -adjustedTargetRotation.y,
                        -adjustedTargetRotation.z,
                        -adjustedTargetRotation.w);
            }

            var deltaRotation = currentRotation.GetDeltaToB(adjustedTargetRotation);

            deltaRotation.ToAngleAxis(out float angleDegrees, out var axis);

            // Handle edge cases
            if (axis.sqrMagnitude < 1e-8f || float.IsNaN(angleDegrees)) {
                return Vector3.zero;
            }

            // Normalize angle to [-180, 180]
            if (angleDegrees > 180f) {
                angleDegrees -= 360f;
            }

            // Convert to angular velocity: radians per second
            return axis.normalized * (angleDegrees * Mathf.Deg2Rad / fixedDeltaTime);
        }
    }
}