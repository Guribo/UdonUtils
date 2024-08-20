using System;

namespace TLP.UdonUtils.Runtime.Extensions
{
    public static class ArrayUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="original"></param>
        /// <param name="newSize"></param>
        /// <param name="clear">if true will ensure that the array is empty</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] ResizeOrCreate<T>(this T[] original, int newSize, bool clear = false) {
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

        public static T[] IncreaseSizeBy<T>(this T[] original, int additional) {
            return original.ResizeOrCreate(original == null ? additional : original.Length + additional);
        }

        public static T[] Append<T>(this T[] original, T value) {
            var copy = original.IncreaseSizeBy(1);
            copy[copy.Length - 1] = value;
            return copy;
        }

        public static bool IsNullOrEmpty<T>(this T[] array) {
            return array == null || array.Length == 0;
        }

        public static int LengthSafe<T>(this T[] array) {
            return array == null ? 0 : array.Length;
        }


        public static void CreateCopy<T>(this T[] source, ref T[] destination, bool copyNull = false) {
            int lengthSafe = source.LengthSafe();
            if (lengthSafe < 1) {
                if (source == null && copyNull) {
                    destination = null;
                } else {
                    destination = destination.ResizeOrCreate(0);
                }

                return;
            }

            destination = destination.ResizeOrCreate(lengthSafe);
            Array.Copy(source, destination, lengthSafe);
        }
    }
}