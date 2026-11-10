using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // วางโมเดลสำเร็จรูป (FBX จาก Kenney) เป็นของตกแต่ง — โหลดจาก Assets/Resources/MedicalAids/<ชื่อ>.fbx
    // วัสดุถูกสลับเป็น Skeld/Lit เพื่อให้ได้เงาและหมอกจากระยะตัวละครเหมือนของอื่นในฉาก
    public static class ModelProp
    {
        const string Dir = "MedicalAids/";

        // scale: ชุด Mini Characters ทั้งชุดใช้สเกลเดียวกัน (ตัวละครสูง ~0.7) · ตัวละครในเกมขยาย 1.7 เท่า (ModelMascot.Scale)
        // name = ชื่อไฟล์ใน Resources/MedicalAids/ หรือพาธเต็มใต้ Resources ถ้ามี "/" (เช่น "SpaceKit/cables")
        public static GameObject Spawn(string name, Transform parent, Vector3 localPos, float yaw = 0f,
                                       float scale = ModelMascot.Scale, float tiltX = 0f)
        {
            string path = name.Contains("/") ? name : Dir + name;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Skeld] ไม่พบโมเดล Resources/{path}.fbx");
                return null;
            }

            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(tiltX, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;

            var fallback = Resources.Load<Texture2D>(Dir + "Textures/colormap");
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    Texture tex = src[i] != null ? src[i].mainTexture : null;
                    dst[i] = Mats.Lit(0xffffff, 0.85f, 0f, 0, 1f, tex != null ? tex : fallback);
                }
                r.sharedMaterials = dst;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            return go;
        }
    }
}
