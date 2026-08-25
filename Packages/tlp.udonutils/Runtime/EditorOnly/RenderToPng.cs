#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    public class RenderToPng : MonoBehaviour
    {
        public void _RenderTextureToFile(RenderTexture rt) {
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false)
            {
                    hideFlags = HideFlags.DontSave
            };


            var oldActive = RenderTexture.active;
            try {
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                byte[] pngBytes = tex.EncodeToPNG();
                File.WriteAllBytes("Image.png", pngBytes);
            }
            finally {
                RenderTexture.active = oldActive;
                Destroy(tex);
            }
        }

        [FormerlySerializedAs("rt")] public RenderTexture RT;

        [ContextMenu("SaveAsFile")]
        public void _ConvertToPng() {
            _RenderTextureToFile(RT);
        }
    }
}
#endif