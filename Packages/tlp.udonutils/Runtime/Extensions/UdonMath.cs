using UnityEngine;

namespace TLP.UdonUtils.Extensions
{
    public static class UdonMath
    {
        public static float Remap(float inMin, float inMax, float outMin, float outMax, float value) {
            float t = inMin.InverseLerp(inMax, value);
            return outMin.Lerp(outMax, t);
        }

        /// <summary>
        /// unclamped
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float InverseLerp(this float a, float b, float value) {
            float divisor = b - a;
            if (Mathf.Approximately(divisor, 0f)) {
                return a;
            }

            return (value - a) / divisor;
        }

        /// <summary>
        /// unclamped
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float Lerp(this float source, float target, float t) {
            return (1f - t) * source + t * target;
        }

        public static double LerpDouble(this double source, double target, double t) {
            return (1.0 - t) * source + t * target;
        }

        /// <summary>
        /// input should be between -1 to 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static float ApplyExpo(this float input, float factor) {
            return (1f - factor) * (input * input * input) + factor * input;
        }

        /// <summary>
        /// Interpolation that combines fast response time with reduced high frequency noise.
        ///
        /// Note: does not seem to be able to filter 
        ///
        /// <remarks>Original by Piotr Zurek: https://twitter.com/evil_arev/status/1128062338156900353</remarks>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static float AdaptiveInterpolation(float a, float b, float factor) {
            return Mathf.Lerp(a, b, factor * Mathf.Abs(a - b));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid">x=p, y=i, z=d</param>
        /// <param name="previousIntegral"></param>
        /// <param name="previousError"></param>
        /// <param name="currentValue"></param>
        /// <param name="targetValue"></param>
        /// <param name="deltaTime"></param>
        /// <returns>result, integral, currentError</returns>
        public static Vector3 PidUpdate(
                Vector3 pid,
                float previousIntegral,
                float previousError,
                float currentValue,
                float targetValue,
                float deltaTime
        ) {
            float currentError = targetValue - currentValue;

            float proportional = pid.x * currentError;
            float integral = previousIntegral + deltaTime * pid.y * currentError;
            float derivative = pid.z * (currentError - previousError) / deltaTime;

            float result = proportional + integral + derivative;

            return new Vector3(result, integral, currentError);
        }

        /// <summary>
        /// given two (e.g. world) rotations a and b it will return the rotation c which can transform a into b (e.g. b = a * c)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>delta rotation which can turn a into b</returns>
        public static Quaternion GetDeltaAToB(this Quaternion a, Quaternion b) {
            return Quaternion.Inverse(a) * b;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerInWatts"></param>
        /// <param name="rpm"></param>
        /// <returns>Torque in Newton-meter [Nm]</returns>
        public static float PowerWToTorque(float powerInWatts, float rpm) {
            return powerInWatts / rpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="torque">[Nm]</param>
        /// <param name="rpm"></param>
        /// <returns>Power in Watts</returns>
        public static float TorqueToPowerW(float torque, float rpm) {
            return torque * rpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerInHp"></param>
        /// <param name="rpm"></param>
        /// <param name="hpToWatt">default conversion is for mechanical HP to Watt</param>
        /// <returns>Torque in Newton-meter [Nm]</returns>
        public static float PowerHpToTorque(float powerInHp, float rpm, float hpToWatt = 745.70f) {
            return powerInHp / rpm * hpToWatt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="torque">[Nm]</param>
        /// <param name="rpm"></param>
        /// <param name="hpToWatt">default conversion is for mechanical HP to Watt</param>
        /// <returns>Power in Horsepower</returns>
        public static float TorqueToPowerHp(float torque, float rpm, float hpToWatt = 745.70f) {
            return torque / hpToWatt * rpm;
        }


        /// <summary>
        /// checks if two quaternions have the same orientation
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>true if a == +- b</returns>
        public static bool HaveSameOrientation(this Quaternion a, Quaternion b, float epsilon = 0.0001f) {
            return Mathf.Abs(Quaternion.Dot(a.normalized, b.normalized)) > 1.0f - epsilon;
        }

        /// <summary>
        /// checks if two quaternions have the same rotation, stricter compared to HaveSameOrientation(...)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>true if a == b</returns>
        public static bool HaveSameRotation(this Quaternion a, Quaternion b, float epsilon = 0.0001f) {
            return Quaternion.Dot(a.normalized, b.normalized) > 1.0f - epsilon;
        }

        /// <summary>
        /// given two (e.g. world) rotations this and b it will return the rotation
        /// c which can transform this rotation into b (e.g. b = this * c)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>delta rotation which can turn a into b</returns>
        public static Quaternion GetDeltaToB(this Quaternion a, Quaternion b) {
            return Quaternion.Inverse(a) * b;
        }

        /// <summary>
        /// <remarks>Original Author: Max Kaufmann (max.kaufmann@gmail.com)</remarks>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="angVel"></param>
        /// <returns></returns>
        public static Quaternion AngVelToDeriv(Quaternion current, Vector3 angVel) {
            var spin = new Quaternion(angVel.x, angVel.y, angVel.z, 0f);
            var result = spin * current;
            return new Quaternion(0.5f * result.x, 0.5f * result.y, 0.5f * result.z, 0.5f * result.w);
        }

        /// <summary>
        ///
        /// <remarks>Original Author: Max Kaufmann (max.kaufmann@gmail.com)</remarks>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="deriv"></param>
        /// <returns></returns>
        public static Vector3 DerivToAngVel(Quaternion current, Quaternion deriv) {
            var result = deriv * Quaternion.Inverse(current);
            return new Vector3(2f * result.x, 2f * result.y, 2f * result.z);
        }

        /// <summary>
        /// <remarks>Original Author: Max Kaufmann (max.kaufmann@gmail.com)</remarks>
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="angularVelocity"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public static Quaternion IntegrateRotation(Quaternion rotation, Vector3 angularVelocity, float deltaTime) {
            var deriv = AngVelToDeriv(rotation, angularVelocity);
            var pred = new Vector4(
                    rotation.x + deriv.x * deltaTime,
                    rotation.y + deriv.y * deltaTime,
                    rotation.z + deriv.z * deltaTime,
                    rotation.w + deriv.w * deltaTime
            ).normalized;
            return new Quaternion(pred.x, pred.y, pred.z, pred.w);
        }

        /// <summary>
        /// <remarks>Original Author: Max Kaufmann (max.kaufmann@gmail.com)</remarks>
        /// </summary>
        /// <param name="rot"></param>
        /// <param name="target"></param>
        /// <param name="deriv"></param>
        /// <param name="smoothTime"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="deltaTime"></param>
        /// <returns></returns>
        public static Quaternion SmoothDamp(
                Quaternion rot,
                Quaternion target,
                ref Quaternion deriv,
                float smoothTime,
                float maxSpeed,
                float deltaTime
        ) {
            // account for double-cover
            float dot = Quaternion.Dot(rot, target);
            float multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;
            // smooth damp (nlerp approx)
            var result = new Vector4(
                    Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, smoothTime, maxSpeed, deltaTime),
                    Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, smoothTime, maxSpeed, deltaTime),
                    Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, smoothTime, maxSpeed, deltaTime),
                    Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, smoothTime, maxSpeed, deltaTime)
            ).normalized;
            // compute deriv
            float dtInv = 1f / deltaTime;
            deriv.x = (result.x - rot.x) * dtInv;
            deriv.y = (result.y - rot.y) * dtInv;
            deriv.z = (result.z - rot.z) * dtInv;
            deriv.w = (result.w - rot.w) * dtInv;
            return new Quaternion(result.x, result.y, result.z, result.w);
        }

        // Source: https://wiki.unity3d.com/index.php/Averaging_Quaternions_and_Vectors
        //Get an average (mean) from more then two quaternions (with two, slerp would be used).
        //Note: this only works if all the quaternions are relatively close together.
        //Usage:
        //-Cumulative is an external Vector4 which holds all the added x y z and w components.
        //-newRotation is the next rotation to be added to the average pool
        //-firstRotation is the first quaternion of the array to be averaged
        //-addAmount holds the total amount of quaternions which are currently added
        //This function returns the current average quaternion
        public static Quaternion AverageQuaternion(
                ref Vector4 cumulative,
                Quaternion newRotation,
                Quaternion firstRotation,
                int addAmount
        ) {
            Debug.Assert(addAmount > 0);

            //Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
            //q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
            if (!AreQuaternionsClose(newRotation, firstRotation)) {
                newRotation = newRotation.InverseSignQuaternion();
            }

            //Average the values
            float addDet = 1f / addAmount;
            cumulative.w += newRotation.w;
            float w = cumulative.w * addDet;
            cumulative.x += newRotation.x;
            float x = cumulative.x * addDet;
            cumulative.y += newRotation.y;
            float y = cumulative.y * addDet;
            cumulative.z += newRotation.z;
            float z = cumulative.z * addDet;

            //note: if speed is an issue, you can skip the normalization step
            return new Quaternion(x, y, z, w).normalized;
        }

        // Source: https://wiki.unity3d.com/index.php/Averaging_Quaternions_and_Vectors
        //Changes the sign of the quaternion components. This is not the same as the inverse.
        public static Quaternion InverseSignQuaternion(this Quaternion q) {
            return new Quaternion(-q.x, -q.y, -q.z, -q.w);
        }

        // Source: https://wiki.unity3d.com/index.php/Averaging_Quaternions_and_Vectors
        //Returns true if the two input quaternions are close to each other. This can
        //be used to check whether or not one of two quaternions which are supposed to
        //be very similar but has its component signs reversed (q has the same rotation as
        //-q)
        public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2) {
            float dot = Quaternion.Dot(q1, q2);

            if (dot < 0.0f) {
                return false;
            } else {
                return true;
            }
        }
    }
}