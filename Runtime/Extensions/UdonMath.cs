using UnityEngine;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class UdonMath
    {
        public static float Remap(float inMin, float inMax, float outMin, float outMax, float value)
        {
            float t = InverseLerp(inMin, inMax, value);
            return Lerp(outMin, outMax, t);
        }

        /// <summary>
        /// unclamped
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float InverseLerp(float a, float b, float value)
        {
            float divisor = b - a;
            if (Mathf.Approximately(divisor, 0f))
            {
                return a;
            }

            return (value - a) / divisor;
        }

        /// <summary>
        /// unclamped
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float Lerp(float a, float b, float t)
        {
            return (1f - t) * a + t * b;
        }

        /// <summary>
        /// input should be between -1 to 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static float ApplyExpo(float input, float factor)
        {
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
        public static float AdaptiveInterpolation(float a, float b, float factor)
        {
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
        )
        {
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
        public static Quaternion GetDeltaAToB(Quaternion a, Quaternion b)
        {
            return Quaternion.Inverse(a) * b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="currentVelocityIn"></param>
        /// <param name="smoothTime"></param>
        /// <param name="maxSpeed"></param>
        /// <param name="deltaTime"></param>
        /// <param name="result"></param>
        /// <returns>array of {blended value, current velocity}</returns>
        public static void SmoothDampNonAlloc(
            float current,
            float target,
            float currentVelocityIn,
            float smoothTime,
            float maxSpeed,
            float deltaTime,
            float[] result
        )
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float num1 = 2f / smoothTime;
            float num2 = num1 * deltaTime;
            float num3 = (float)(1.0 / (1.0 + num2 + 0.479999989271164 * num2 * num2 +
                                        0.234999999403954 * num2 * num2 * num2));
            float num4 = current - target;
            float num5 = target;
            float max = maxSpeed * smoothTime;
            float num6 = Mathf.Clamp(num4, -max, max);
            target = current - num6;
            float num7 = (currentVelocityIn + num1 * num6) * deltaTime;
            currentVelocityIn = (currentVelocityIn - num1 * num7) * num3;
            float num8 = target + (num6 + num7) * num3;
            if (num5 - (double)current > 0.0 == num8 > (double)num5)
            {
                num8 = num5;
                currentVelocityIn = (num8 - num5) / deltaTime;
            }

            result[0] = num8;
            result[1] = currentVelocityIn;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerInWatts"></param>
        /// <param name="rpm"></param>
        /// <returns>Torque in Newton-meter [Nm]</returns>
        public static float PowerWToTorque(float powerInWatts, float rpm)
        {
            return powerInWatts / rpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="torque">[Nm]</param>
        /// <param name="rpm"></param>
        /// <returns>Power in Watts</returns>
        public static float TorqueToPowerW(float torque, float rpm)
        {
            return torque * rpm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="powerInHp"></param>
        /// <param name="rpm"></param>
        /// <param name="hpToWatt">default conversion is for mechanical HP to Watt</param>
        /// <returns>Torque in Newton-meter [Nm]</returns>
        public static float PowerHpToTorque(float powerInHp, float rpm, float hpToWatt = 745.70f)
        {
            return powerInHp / rpm * hpToWatt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="torque">[Nm]</param>
        /// <param name="rpm"></param>
        /// <param name="hpToWatt">default conversion is for mechanical HP to Watt</param>
        /// <returns>Power in Horsepower</returns>
        public static float TorqueToPowerHp(float torque, float rpm, float hpToWatt = 745.70f)
        {
            return torque / hpToWatt * rpm;
        }
    }
}