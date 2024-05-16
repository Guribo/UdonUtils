using UnityEngine;

namespace TLP.UdonUtils.Runtime.Physics
{
    public static class ConstantLinearAcceleration
    {
        // s(t) = 0.5 at² + vt + s_0
        public static Vector3 Position(Vector3 position, Vector3 velocity, Vector3 acceleration, float deltaTime) {
            return (0.5f * deltaTime * deltaTime) * acceleration + velocity * deltaTime + position;
        }


        //                       s(t) = 0.5 at² + vt + s_0 | - vt - s_0
        // <=>        s(t) - vt - s_0 = 0.5 at²            | * 2
        // <=>     2(s(t) - vt - s_0) = at²             | / t²
        // <=>  2(s(t) - vt - s_0)/t² = a
        // <=>                      a = 2(((s(t) - s_0) - vt) / t²)
        public static Vector3 Acceleration(
                Vector3 startPosition,
                Vector3 endPosition,
                Vector3 startVelocity,
                float deltaTime
        ) {
            if (deltaTime == 0) return Vector3.zero;
            return 2f * (((endPosition - startPosition) - startVelocity * deltaTime) / (deltaTime * deltaTime));
        }

        // a = dv/dt = (v_1 - v_0) / dt
        public static Vector3 Acceleration2(Vector3 startVelocity, Vector3 endVelocity, float deltaTime) {
            return ConstantLinearVelocity.Velocity(startVelocity, endVelocity, deltaTime);
        }

        // s_0 = s(t) - 0.5 at² - vt
        public static Vector3 StartPosition(Vector3 position, Vector3 velocity, Vector3 acceleration, float deltaTime) {
            return position - (0.5f * deltaTime * deltaTime) * acceleration - velocity * deltaTime;
        }

        // s(t) = 0.5 at² + vt + s_0
        // <=>  s(t) - 0.5 at² -  s_0 = vt
        // <=>  (s(t) - 0.5 at² -  s_0)t = v
        // <=>  (s(t) -  s_0 - 0.5 at²)t = v
        public static Vector3 StartVelocity(Vector3 start, Vector3 end, Vector3 acceleration, float deltaTime) {
            if (deltaTime == 0) return Vector3.zero;
            return (end - start - (0.5f * deltaTime * deltaTime) * acceleration) / deltaTime;
        }

        // v_0 = v(t) - at
        public static Vector3 StartVelocity2(Vector3 velocity, Vector3 acceleration, float deltaTime) {
            return velocity - acceleration * deltaTime;
        }

        // v(t) = at + v_0
        public static Vector3 Velocity(Vector3 velocity, Vector3 acceleration, float deltaTime) {
            return acceleration * deltaTime + velocity;
        }

        // v = at + v_0
        // <=>  v - v_0 = at
        // <=> t = (v - v_0) / a
        public static float DeltaTime(Vector3 startVelocity, Vector3 endVelocity, Vector3 acceleration) {
            return ConstantLinearVelocity.DeltaTime(startVelocity, endVelocity, acceleration);
        }


        /// <summary>
        /// Based on s(t) = 0.5 at² + v0t + s0 solved to a = 2(s(t) - v0t - s0) / t² with t != 0; v(t) = at + v0
        ///
        /// <remarks>The accuracy of the results is HIGHLY dependent
        /// on the correctness of the <see cref="initialVelocity"/>!
        /// Using this function multiple times with the output velocity as input will eventually produce wrong results
        /// due to floating point errors!!! If possible calculate the initial velocity differently.</remarks>
        /// </summary>
        /// <param name="firstPosition"></param>
        /// <param name="secondPosition"></param>
        /// <param name="initialVelocity">Velocity at <see cref="firstPosition"/></param>
        /// <param name="deltaTime">Time elapsed while the position changed from
        /// <see cref="firstPosition"/> to <see cref="secondPosition"/></param>
        /// <param name="acceleration">result: calculated acceleration, is zero if t == 0</param>
        /// <param name="velocity">calculated velocity based on calculated <see cref="acceleration"/>
        /// at point <see cref="secondPosition"/></param>
        public static void CalculateAccelerationAndVelocity(
                Vector3 firstPosition,
                Vector3 secondPosition,
                Vector3 initialVelocity,
                float deltaTime,
                out Vector3 acceleration,
                out Vector3 velocity
        ) {
            if (deltaTime == 0) {
                acceleration = Vector3.zero; // technically it is infinity, but that is not usable
                velocity = initialVelocity;
                return;
            }

            acceleration = 2f * (secondPosition - initialVelocity * deltaTime - firstPosition) /
                           (deltaTime * deltaTime);
            velocity = acceleration * deltaTime + initialVelocity;
        }

        /// <summary>
        /// Correctly accelerate without introducing additional errors based on fixed delta time
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="acceleration"></param>
        /// <param name="previousAcceleration"></param>
        /// <param name="previousDeltaTime"></param>
        /// <param name="deltaTime"></param>
        public static void AccelerateCorrect(
                this Rigidbody rigidBody,
                ref Vector3 previousAcceleration,
                Vector3 acceleration,
                float previousDeltaTime,
                float deltaTime
        ) {
            var previousVelocity = rigidBody.VelocityCorrect(previousAcceleration, previousDeltaTime);
            var distanceFromPreviousVelocity = previousVelocity * Time.fixedDeltaTime;
            var distanceFromCurrentAcceleration = 0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime * acceleration;
            var distanceToTravel = distanceFromCurrentAcceleration + distanceFromPreviousVelocity;
            rigidBody.velocity = distanceToTravel / Time.fixedDeltaTime;
            previousAcceleration = acceleration;
        }

        /// <summary>
        /// Get the current real velocity based on the previous acceleration.
        /// <remarks>Do not use if the RigidBody was under the influence of friction,
        /// collision or any other forces! Use RigidBody.velocity instead.</remarks>
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="previousAcceleration"></param>
        /// <param name="previousDeltaTime"></param>
        /// <returns></returns>
        public static Vector3 VelocityCorrect(
                this Rigidbody rigidBody,
                Vector3 previousAcceleration,
                float previousDeltaTime
        ) {
            return rigidBody.velocity + 0.5f * Time.fixedDeltaTime * previousAcceleration;
        }
    }
}