using UnityEngine;

namespace SkeldMap
{
    // post-process ACES tone mapping ของกล้องหลัก (Built-in RP ไม่มีในตัว)
    // three.js: renderer.toneMapping = ACESFilmicToneMapping, toneMappingExposure = 1.15
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class ToneMapper : MonoBehaviour
    {
        public float exposure = 1.15f;
        Material mat;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null)
            {
                var sh = Shader.Find("Hidden/Skeld/Tonemap");
                if (sh != null) mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (mat == null) { Graphics.Blit(src, dst); return; }
            mat.SetFloat("_Exposure", exposure);
            Graphics.Blit(src, dst, mat);
        }

        void OnDisable()
        {
            if (mat != null) DestroyImmediate(mat);
        }
    }
}
