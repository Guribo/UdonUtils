using UnityEngine;

namespace TLP.UdonUtils.Runtime.Physics
{
    public static class ConstantLinearVelocity
    {
        /// <summary>
        /// s(t) = v * t + s_0
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="deltaTime"></param>
        /// <returns>position vector</returns>
        public static Vector3 Position(Vector3 position, Vector3 velocity, float deltaTime) {
            return Distance(velocity, deltaTime) + position;
        }

        /// <summary>
        /// t = (s(t) - s_0) / v
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="deltaTime"></param>
        /// <returns>position vector</returns>
        public static float DeltaTime(Vector3 start, Vector3 end, Vector3 velocity) {
            float sign;
            if (!Mathf.Approximately(velocity.x, 0)) {
                sign = Mathf.Sign((end.x - start.x) / velocity.x);
            } else if (!Mathf.Approximately(velocity.y, 0)) {
                sign = Mathf.Sign((end.y - start.y) / velocity.y);
            } else if (!Mathf.Approximately(velocity.z, 0)) {
                sign = Mathf.Sign((end.z - start.z) / velocity.z);
            } else {
                return 0;
            }
            return sign * (end - start).magnitude / velocity.magnitude;
        }

        /// <summary>
        /// s(t) = v * t
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="deltaTime"></param>
        /// <returns>distance vector</returns>
        public static Vector3 Distance(Vector3 velocity, float deltaTime) {
            return velocity * deltaTime;
        }


        /// <summary>
        /// v = ds/dt = (s_1 - s_0)/dt
        /// <remarks>deltaTime must not be zero!</remarks>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public static Vector3 Velocity(Vector3 start, Vector3 end, float deltaTime) {
            return VelocityFromDistance(end - start, deltaTime);
        }

        /// <summary>
        /// v = s(t)/t
        /// <remarks>deltaTime must not be zero!</remarks>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public static Vector3 VelocityFromDistance(Vector3 distance, float deltaTime) {
            if (deltaTime == 0) return Vector3.zero;
            return distance / deltaTime;
        }

        /// <summary>
        /// s_0 = s(t) - v * t
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="elapsed"></param>
        /// <returns></returns>
        public static Vector3 StartPosition(Vector3 position, Vector3 velocity, float elapsed) {
            return Position(position, velocity, -elapsed);
        }
    }
}