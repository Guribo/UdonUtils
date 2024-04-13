#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace TLP.UdonUtils.EditorOnly
{
    public class RenderToPng : MonoBehaviour
    {
        public void RenderTextureToFile(RenderTexture rt) {
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

        public RenderTexture rt;

        [ContextMenu("SaveAsFile")]
        public void ConvertToPng() {
            RenderTextureToFile(rt);
        }
    }
}
#endif