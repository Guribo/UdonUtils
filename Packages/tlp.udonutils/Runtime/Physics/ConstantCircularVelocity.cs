using System;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Physics
{
    public static class ConstantCircularVelocity
    {
        /// <summary>
        ///  r = v/omega
        ///  omega = rotationRate * pi / 180
        ///  r = v / (rotationRate * pi / 180) = v * 180 / (rotationRate * pi)
        /// </summary>
        /// <param name="tangentialVelocity">velocity on the circle</param>
        /// <param name="rotationRate">turnrate on the circle in degrees/s, must not be 0</param>
        /// <returns>absolute radius (>= 0), also returns 0 if rotationRate is 0</returns>
        public static float Radius(Vector3 tangentialVelocity, float rotationRate) {
            if (rotationRate == 0) return 0;
            // Convert rotation rate from degrees per second to radians per second
            float omega = rotationRate * Mathf.Deg2Rad;

            // Calculate the radius of the circular path
            float radius = tangentialVelocity.magnitude / omega;

            return Mathf.Abs(radius);
        }


        /// <param name="position"></param>
        /// <param name="tangentialVelocity">Expected have a length > 0</param>
        /// <param name="circleAxis">expected to be normalized and perpendicular to tangentialVelocity</param>
        /// <param name="radius">expected to be >= 0</param>
        /// <returns>The position of the center of the circle</returns>
        public static Vector3 Center(Vector3 position, Vector3 tangentialVelocity, Vector3 circleAxis, float radius) {
#if TLP_DEBUG
            if (radius < 0) Debug.LogError($"{nameof(Center)}: {nameof(radius)} must be >= 0");
            if (Math.Abs(circleAxis.sqrMagnitude - 1) > 1e-6f)
                Debug.LogError($"{nameof(Center)}: {nameof(circleAxis)} must be normalized");
            if (Math.Abs(tangentialVelocity.sqrMagnitude) < 1e-6f)
                Debug.LogError($"{nameof(Center)}: {nameof(tangentialVelocity)} must not be zero");
            if (Math.Abs(Vector3.Dot(tangentialVelocity.normalized, circleAxis)) > 1e-3f)
                Debug.LogError(
                        $"{nameof(Center)}: {nameof(circleAxis)} must be perpendicular to {nameof(tangentialVelocity)}");
#endif
            var directionToCenter = Vector3.Cross(circleAxis ,tangentialVelocity.normalized).normalized;
            return position + directionToCenter * radius;
        }

        public static Vector3 PositionOnCircle(
                Vector3 initialPosition,
                Vector3 tangentialVelocity,
                Vector3 velocityChangeDirection,
                float rotationRateDegrees,
                float deltaTime,
                out Vector3 velocity,
                out Quaternion rotationDelta
        ) {
            var rotationAxis = Vector3.Cross( tangentialVelocity.normalized, velocityChangeDirection.normalized)
                    .normalized;
            float radius = Radius(tangentialVelocity, rotationRateDegrees);
            var center = Center(initialPosition, tangentialVelocity, rotationAxis, radius);
            var centerToPosition = initialPosition - center;
            rotationDelta = Quaternion.AngleAxis(rotationRateDegrees * deltaTime, rotationAxis);
            velocity = rotationDelta * tangentialVelocity;
            return center + rotationDelta * centerToPosition;
        }

        public static bool IsCircularMovement(
                Vector3 velocity0,
                Vector3 velocity1,
                float deltaTime,
                out float angle,
                float angleThreshold = 15f,
                float maxVelocityDelta = 0.005f
        ) {
            angle = Vector3.Angle(velocity0.normalized, velocity1.normalized) / deltaTime;
            if (angle < angleThreshold) return false;
            float size0 = velocity0.magnitude;
            if (size0 < 1e-3f) return false;
            float size1 = velocity1.magnitude;
            return Mathf.Abs(size0 - size1) < maxVelocityDelta * size0;
        }
    }
}