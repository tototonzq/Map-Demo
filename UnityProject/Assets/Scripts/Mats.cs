using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // โรงงานสร้าง Material (แทน THREE.MeshStandardMaterial / MeshBasicMaterial)
    // shader อยู่ใน Assets/Resources/Skeld/ จึงถูกรวมเข้า build อัตโนมัติ
    public static class Mats
    {
        static Shader lit, litFade, unlit, text;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static Shader Load(string name)
        {
            var s = Shader.Find(name);
            if (s == null)
            {
                Debug.LogError($"[Skeld] ไม่พบ shader '{name}' — ตรวจสอบโฟลเดอร์ Assets/Resources/Skeld และว่าใช้ Built-in Render Pipeline");
                s = Shader.Find("Standard");
            }
            return s;
        }

        static void EnsureShaders()
        {
            if (lit != null) return;
            lit = Load("Skeld/Lit");
            litFade = Load("Skeld/LitFade");
            unlit = Load("Skeld/Unlit");
            text = Load("Skeld/Text");
        }

        public static Color Hex(uint c, float a = 1f)
            => new Color(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f, a);

        // ค่า HDR (emission) ไม่ถูกแปลงสีให้อัตโนมัติ ต้องแปลง sRGB→Linear เองเมื่อโปรเจกต์เป็น Linear
        static Color ToWorkingSpace(Color c)
            => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        // THREE.MeshStandardMaterial: roughness → smoothness = 1 - roughness
        public static Material Lit(uint color, float roughness = 0.6f, float metalness = 0.2f,
                                   uint emissive = 0, float emissiveIntensity = 1f,
                                   Texture tex = null, float opacity = 1f)
        {
            string key = $"lit|{color:x6}|{roughness:F3}|{metalness:F3}|{emissive:x6}|{emissiveIntensity:F3}|{(tex != null ? tex.GetInstanceID() : 0)}|{opacity:F3}";
            if (cache.TryGetValue(key, out var hit) && hit != null) return hit;

            EnsureShaders();
            var m = new Material(opacity < 0.999f ? litFade : lit) { name = "SkeldLit" };
            m.SetColor("_Color", Hex(color, opacity));
            m.SetFloat("_Glossiness", 1f - roughness);
            m.SetFloat("_Metallic", metalness);
            m.SetColor("_Emission", emissive != 0 ? ToWorkingSpace(Hex(emissive)) * emissiveIntensity : Color.black);
            if (tex != null) m.SetTexture("_MainTex", tex);
            if (opacity < 0.999f) m.renderQueue = (int)RenderQueue.Transparent;
            cache[key] = m;
            return m;
        }

        // THREE.MeshBasicMaterial / PointsMaterial — ไม่ต้องใช้แสง · คืน instance ใหม่เสมอ (เพราะบางชิ้นต้องเปลี่ยนสีทีหลัง)
        public static Material Unlit(Color color, bool transparent = false, bool doubleSided = false,
                                     bool zWrite = true, bool fog = true, float pointSize = 2f, Texture tex = null)
        {
            EnsureShaders();
            var m = new Material(unlit) { name = "SkeldUnlit" };
            m.SetColor("_Color", color);
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.SetFloat("_ZWrite", zWrite ? 1f : 0f);
            m.SetFloat("_UseFog", fog ? 1f : 0f);
            m.SetFloat("_PointSize", pointSize);
            if (transparent)
            {
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }

        public static Material Text(Texture fontAtlas)
        {
            EnsureShaders();
            var m = new Material(text) { name = "SkeldText" };
            m.mainTexture = fontAtlas;
            return m;
        }
    }
}
