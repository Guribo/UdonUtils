using JetBrains.Annotations;
using UnityEngine;

namespace TLP.UdonUtils.Runtime.Physics
{
    /// <summary>
    /// Utilities to calculate velocities, accelerations etc.
    /// </summary>
    public static class PhysicsUtils
    {
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
        [PublicAPI]
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
    }
}