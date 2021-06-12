using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace Guribo.UdonUtils.Scripts.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonMath : UdonSharpBehaviour
    {
        public float Remap(float inMin, float inMax, float outMin, float outMax, float value)
        {
            var t = InverseLerp(inMin, inMax, value);
            return Lerp(outMin, outMax, t);
        }

        /// <summary>
        /// unclamped
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public float InverseLerp(float a, float b, float value)
        {
            var divisor = b - a;
            if (divisor == 0f)
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
        public float Lerp(float a, float b, float t)
        {
            return (1f - t) * a + t * b;
        }

        /// <summary>
        /// input should be between -1 to 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public float ApplyExpo(float input, float factor)
        {
            return (1f - factor) * (input * input * input) + (factor * input);
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
        public Vector3 PidUpdate(Vector3 pid,
            float previousIntegral,
            float previousError,
            float currentValue,
            float targetValue,
            float deltaTime)
        {
            var currentError = targetValue - currentValue;

            var proportional = pid.x * currentError;
            var integral = previousIntegral + deltaTime * pid.y * currentError;
            var derivative = pid.z * (currentError - previousError) / deltaTime;

            var result = proportional + integral + derivative;

            return new Vector3(result, integral, currentError);
        }

        /// <summary>
        /// given two (e.g. world) rotations a and b it will return the rotation c which can transform a into b (e.g. b = a * c)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>delta rotation which can turn a into b</returns>
        public Quaternion GetDeltaAToB(Quaternion a, Quaternion b)
        {
            return Quaternion.Inverse(a) * b;
        }
    }
}