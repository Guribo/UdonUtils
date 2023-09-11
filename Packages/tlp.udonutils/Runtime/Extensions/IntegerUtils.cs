using JetBrains.Annotations;

namespace TLP.UdonUtils.Extensions
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
        public static void MoveIndexLeftLooping(ref this int value, int maxValue, int decrement = 1)
        {
            value = (maxValue + value - decrement % maxValue) % maxValue;
        }

        [PublicAPI]
        public static int SubtractLooping(this int value, int maxValue, int decrement = 1)
        {
            return (maxValue + value - decrement % maxValue) % maxValue;
        }

        /// <summary>
        /// Increments the value in-place, result range is 0 to maxValue - 1
        /// </summary>
        /// <param name="value">value to decrement in-place, expected to be positive</param>
        /// <param name="maxValue">expected to be greater than zero</param>
        /// <param name="increment">expected to be positive, may be larger than maxValue</param>
        [PublicAPI]
        public static void MoveIndexRightLooping(ref this int value, int maxValue, int increment = 1)
        {
            value = (value + increment) % maxValue;
        }

        [PublicAPI]
        public static int AddLooping(this int value, int maxValue, int increment = 1)
        {
            return (value + increment) % maxValue;
        }
    }
}