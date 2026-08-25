using System;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class ArrayUtils
    {
        /// <param name="original"></param>
        /// <param name="newSize"></param>
        /// <param name="clear">if true will ensure that the array is empty</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] _ResizeOrCreate<T>(this T[] original, int newSize, bool clear = false) {
            if (original == null) {
                return new T[newSize];
            }

            if (newSize < 0) {
                return new T[0];
            }

            if (original.Length == newSize) {
                if (clear) {
                    for (int i = 0; i < original.Length; i++) {
                        original[i] = default;
                    }
                }

                return original;
            }

            var newArray = new T[newSize];
            int length = original.Length <= newSize ? original.Length : newSize;
            if (!clear) {
                Array.Copy(original, newArray, length);
            }

            return newArray;
        }

        public static T[] _IncreaseSizeBy<T>(this T[] original, int additional) {
            return original._ResizeOrCreate(original == null ? additional : original.Length + additional);
        }

        public static T[] _Append<T>(this T[] original, T value) {
            var copy = original._IncreaseSizeBy(1);
            copy[copy.Length - 1] = value;
            return copy;
        }

        /// <summary>
        /// Deletes an element from the array if it exists. The order of elements does not change.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The new array if shortened, otherwise the original array.</returns>
        public static T[] _Delete<T>(this T[] original, T value) {
            if (original._LengthSafe() == 0) {
                return original;
            }

            int index = Array.IndexOf(original, value);
            if (index < 0) {
                return original;
            }

            var newArray = new T[original.Length - 1];
            Array.Copy(original, 0, newArray, 0, index);
            Array.Copy(original, index + 1, newArray, index, original.Length - index - 1);
            return newArray;
        }

        public static bool _IsNullOrEmpty<T>(this T[] array) {
            return array == null || array.Length == 0;
        }

        public static int _LengthSafe<T>(this T[] array) {
            return array == null ? 0 : array.Length;
        }


        public static void _CreateCopy<T>(this T[] source, ref T[] destination, bool copyNull = false) {
            int lengthSafe = source._LengthSafe();
            if (lengthSafe < 1) {
                if (source == null && copyNull) {
                    destination = null;
                } else {
                    destination = destination._ResizeOrCreate(0);
                }

                return;
            }

            destination = destination._ResizeOrCreate(lengthSafe);
            Array.Copy(source, destination, lengthSafe);
        }
    }
}