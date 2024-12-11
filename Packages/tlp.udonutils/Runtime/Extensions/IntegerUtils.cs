using JetBrains.Annotations;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class IntegerUtils
    {
        /// <summary>
        /// Decrements the value in-place, result range is 0 to maxValue - 1
        /// </summary>
        /// <param name="value">value to decrement in-place, expected to be positive</param>
        /// <param name="maxValue">expected to be greater than zero</param>
        /// <param name="decrement">expected to be positive, may be larger than maxValue</param>
        [PublicAPI]
        public static void MoveIndexLeftLooping(ref this int value, int maxValue, int decrement = 1) {
            if (maxValue == 0) {
                value = 0;
                return;
            }
            value = (maxValue + value - decrement % maxValue) % maxValue;
        }

        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="increment"></param>
        /// <returns>0 if maxValue == 0</returns>
        [PublicAPI]
        public static int SubtractLooping(this int value, int maxValue, int decrement = 1) {
            if (maxValue == 0) return 0;
            return (maxValue + value - decrement % maxValue) % maxValue;
        }

        /// <summary>
        /// Increments the value in-place, result range is 0 to maxValue - 1
        /// </summary>
        /// <param name="value">value to decrement in-place, expected to be positive</param>
        /// <param name="maxValue">expected to be greater than zero</param>
        /// <param name="increment">expected to be positive, may be larger than maxValue</param>
        [PublicAPI]
        public static void MoveIndexRightLooping(ref this int value, int maxValue, int increment = 1) {
            if (maxValue == 0) {
                value = 0;
                return;
            }
            value = (value + increment) % maxValue;
        }

        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="increment"></param>
        /// <returns>0 if maxValue == 0</returns>
        [PublicAPI]
        public static int AddLooping(this int value, int maxValue, int increment = 1) {
            if (maxValue == 0) return 0;
            return (value + increment) % maxValue;
        }
    }
}