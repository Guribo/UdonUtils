using UnityEngine;

namespace TLP.UdonUtils.Sync
{
    public static class CompressionUtils
    {
        /// <summary>
        /// Decompresses the first 30 bits back into a quaternion. See also <see cref="CompressToUint32"/>.
        /// </summary>
        /// <param name="compressedRotation">Expects an euler representation encoded in the first 10 bit being z,
        /// 11-20 being y and 21-30 being x.</param>
        /// <returns></returns>
        public static Quaternion DecompressToQuaternion(
                this uint compressedRotation
        ) {
            uint x10Dec = (compressedRotation & (0b11_11111111 << 20)) >> 20;
            uint y10Dec = (compressedRotation & (0b11_11111111 << 10)) >> 10;
            uint z10Dec = compressedRotation & 0b11_11111111;

            float decompressionFactor = 360f / 1024f;
            float xDec = x10Dec * decompressionFactor;
            float yDec = y10Dec * decompressionFactor;
            float zDec = z10Dec * decompressionFactor;

#if TLP_UNIT_TESTING
            Debug.Log($"Decompressed x {xDec}, y {yDec}, z {zDec}");
#endif

            return Quaternion.Euler(xDec, yDec, zDec);
        }

        /// <summary>
        /// Compresses the euler representation of the quaternion into the first 30 bits of a uint32 variable,
        /// with the first 10 bit being z, 11-20 being y and 21-30 being x.
        /// See also <see cref="DecompressToQuaternion"/>.
        /// TODO: investigate alternative solution: https://gist.github.com/gafferongames/bb7e593ba1b05da35ab6#file-delta_compression-cpp-L1004
        /// <remarks>The compression introduces a loss of accuracy, up to around 0.3 degrees per axis</remarks>
        /// </summary>
        /// <param name="rotation">Must be normalizable, as in ideally just local/world rotations</param>
        /// <returns></returns>
        public static uint CompressToUint32(this Quaternion rotation) {
#if TLP_UNIT_TESTING
            Debug.Log($"Original rotation: {rotation.eulerAngles}");
            Debug.Log($"Normalized rotation: {rotation.normalized.eulerAngles}");
#endif
            // produce angles in range +- 360°
            var eulerAngles = rotation.normalized.eulerAngles;

            // convert angles to range 0-360°
            float x = eulerAngles.x < 0f ? eulerAngles.x + 360f : eulerAngles.x;
            float y = eulerAngles.y < 0f ? eulerAngles.y + 360f : eulerAngles.y;
            float z = eulerAngles.z < 0f ? eulerAngles.z + 360f : eulerAngles.z;

            // convert to 10 bit representation
            float compressionFactor = 1024 / 360f;
            uint x10 = (uint)Mathf.RoundToInt(x * compressionFactor);
            uint y10 = (uint)Mathf.RoundToInt(y * compressionFactor);
            uint z10 = (uint)Mathf.RoundToInt(z * compressionFactor);


            // 32 bit with 2 bits leftover for the entire quaternion, 96 bits saved
            uint compressedRotation = (z10 & 0b11_11111111)
                                      | ((y10 & 0b11_11111111) << 10)
                                      | ((x10 & 0b11_11111111) << 20);
#if TLP_UNIT_TESTING
            Debug.Log($"Raw compressed: {compressedRotation}");
#endif
            return compressedRotation;
        }
    }
}