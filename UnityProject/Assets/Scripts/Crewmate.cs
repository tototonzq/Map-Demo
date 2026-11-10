using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // ตัวละคร Crewmate (port จาก createCrewmate ใน js/main.js)
    // โมเดลหันหน้าไปทาง local +z · ขา 2 ข้างเป็น pivot ให้แกว่งตอนเดิน
    public sealed class Crewmate
    {
        public readonly GameObject Root;
        public readonly Transform[] Legs = new Transform[2];

        public static readonly uint[] Palette =
        {
            0xc51111, 0x132ed1, 0x117f2d, 0xed54ba, 0xef7d0e, 0xf5f557, 0x3f474e, 0x6b2fbc, 0x38fedc,
        };

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            if (scale.HasValue) go.transform.localScale = scale.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        public Crewmate(uint color)
        {
            Root = new GameObject("Crewmate");

            var bodyMat = Mats.Lit(color, 0.45f, 0.05f);
            Color c = Mats.Hex(color);
            // สีเข้มของเป้ = สี × 0.7 (คูณในค่า sRGB เหมือน THREE.Color.multiplyScalar)
            uint darkHex = ((uint)(c.r * 0.7f * 255f) << 16) | ((uint)(c.g * 0.7f * 255f) << 8) | (uint)(c.b * 0.7f * 255f);
            var darkMat = Mats.Lit(darkHex, 0.5f, 0.05f);
            var visorMat = Mats.Lit(0xa8ecff, 0.1f, 0.4f, 0x1b5a78, 0.6f);

            Part(Root.transform, "body", MeshBuilder.Capsule(0.34f, 0.5f), bodyMat, new Vector3(0, 0.7f, 0));
            Part(Root.transform, "visor", MeshBuilder.Sphere(0.2f, 18, 12), visorMat,
                 new Vector3(0, 0.92f, 0.27f), new Vector3(1.5f, 0.85f, 0.7f));
            Part(Root.transform, "pack", MeshBuilder.Box(0.5f, 0.6f, 0.24f), darkMat, new Vector3(0, 0.75f, -0.38f));

            var legMesh = MeshBuilder.Box(0.25f, 0.4f, 0.3f);
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                var pivot = new GameObject(i == 0 ? "legL" : "legR").transform;
                pivot.SetParent(Root.transform, false);
                pivot.localPosition = new Vector3(s * 0.17f, 0.4f, 0);
                Part(pivot, "leg", legMesh, bodyMat, new Vector3(0, -0.2f, 0));
                Legs[i] = pivot;
            }
        }
    }
}
