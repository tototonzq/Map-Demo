using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // สร้างฉากทั้งหมดแบบ procedural ตอนเริ่มเกม (port จากส่วนสร้างฉากใน js/main.js)
    public static class MapBuilder
    {
        public const float WallH = 1.7f;

        // true = ใช้ชิ้นส่วนจาก Kenney Modular Space Kit · false = แมปเดิมที่สร้างด้วยโค้ดล้วน
        public static bool UseKit = true;

        // true = สภาพแวดล้อมนอกแมปแบบฐานทัพกลางป่า · false = อวกาศดำ + ดวงดาวแบบเดิม
        public static bool UseSurroundings = true;

        static Font labelFont;
        static Material labelMat;

        public static GameObject Build(MapGrid grid)
        {
            var root = new GameObject("SkeldMap");

            // นอกแมป: พื้นหญ้า/รั้ว/ป่า/ลานเฮลิคอปเตอร์/เต็นท์ ตามภาพ Blueprint · ปิดแล้วกลับไปใช้พื้นหลังอวกาศดวงดาวแบบเดิม
            if (UseSurroundings) SurroundBuilder.Build(root.transform, grid);
            else AddStars(root.transform);

            // พื้น+กำแพงจากชุด Kenney Modular Space Kit · ถ้าชิ้นส่วนไม่ครบจะใช้แบบเดิมที่สร้างด้วยโค้ด
            if (!UseKit || !KitMapBuilder.TryBuild(root.transform, grid))
            {
                AddFloors(root.transform);
                AddWalls(root.transform, grid);
            }
            AddRoomLabels(root.transform);

            var props = new GameObject("Props").transform;
            props.SetParent(root.transform, false);
            PushWorld.Clear();
            foreach (var p in MapData.Props)
            {
                var go = PropFactory.Build(p, props);
                if (go != null) PushWorld.Register(p, go.transform);   // ชิ้นเล็ก/เบา → ผลักได้
            }
            foreach (var v in MapData.Vents) PropFactory.Vent(v, props);
            return root;
        }

        // ───────────── ดวงดาวรอบแมป ─────────────
        static void AddStars(Transform parent)
        {
            const int n = 1800;
            var rng = new System.Random(12345);
            var pts = new Vector3[n];
            var idx = new int[n];
            for (int i = 0; i < n; i++)
            {
                float r = 350f + (float)rng.NextDouble() * 400f;
                float th = (float)rng.NextDouble() * Mathf.PI * 2f;
                float ph = Mathf.Acos((float)rng.NextDouble() * 2f - 1f);
                float x = MapData.GridW / 2f + r * Mathf.Sin(ph) * Mathf.Cos(th);
                float y = Mathf.Abs(r * Mathf.Cos(ph)) * 0.6f - 40f;
                float z = MapData.GridH / 2f + r * Mathf.Sin(ph) * Mathf.Sin(th);
                pts[i] = MapData.ToWorld(x, y, z);
                idx[i] = i;
            }
            var mesh = new Mesh { name = "stars", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = pts;
            mesh.SetIndices(idx, MeshTopology.Points, 0);
            mesh.bounds = new Bounds(new Vector3(MapData.GridW / 2f, 0, -MapData.GridH / 2f), Vector3.one * 2000f);

            var go = new GameObject("Stars");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Unlit(Color.white, fog: false, pointSize: 1.4f);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // ───────────── พื้น ─────────────
        static Texture2D MakeTileTexture()
        {
            const int S = 256;   // ละเอียดขึ้นจาก 128 (สัดส่วนขอบ/เส้นกลางคำนวณตาม S)
            int bd = S * 3 / 128, mid = S / 2;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true, false) { name = "tile" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dark = 0f;
                    // กรอบกระเบื้องหนา 3px (alpha .2) + เส้นกากบาทกลางจาง ๆ (alpha .06)
                    if (x < bd || y < bd || x >= S - bd || y >= S - bd) dark = 0.2f;
                    else if (x == mid - 1 || x == mid || y == mid - 1 || y == mid) dark = 0.03f;
                    byte v = (byte)Mathf.RoundToInt(255f * (1f - dark));
                    px[y * S + x] = new Color32(v, v, v, 255);
                }
            }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply(true);
            return tex;
        }

        static void AddFloor(Transform parent, Texture2D tile, string name, float x, float z, float w, float d, uint color, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // พื้นกว้าง x..x+w, ลึก z..z+d (กริด) → world z อยู่ที่ -(z+d) .. -z
            go.transform.position = new Vector3(x, y, -(z + d));
            go.AddComponent<MeshFilter>().sharedMesh = MeshBuilder.FloorQuad(w, d);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Lit(color, 0.85f, 0.05f, 0, 1f, tile);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        static void AddFloors(Transform parent)
        {
            var floors = new GameObject("Floors").transform;
            floors.SetParent(parent, false);
            var tile = MakeTileTexture();
            foreach (var c in MapData.Corridors) AddFloor(floors, tile, "Corridor", c[0], c[1], c[2], c[3], 0x6b7586, 0.01f);
            foreach (var r in MapData.Rooms) AddFloor(floors, tile, "Room_" + r.id, r.x, r.z, r.w, r.d, r.color, 0.02f);
        }

        // ───────────── ป้ายชื่อห้องบนพื้น ─────────────
        static void EnsureLabelFont()
        {
            if (labelFont != null) return;
            labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 64);
            // ขอ glyph ที่ต้องใช้ล่วงหน้า แล้วคอยอัปเดต atlas เมื่อ Unity สร้าง texture ใหม่
            labelFont.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ", 64, FontStyle.Bold);
            labelMat = Mats.Text(labelFont.material.mainTexture);
            Font.textureRebuilt += f =>
            {
                if (f == labelFont && labelMat != null) labelMat.mainTexture = labelFont.material.mainTexture;
            };
        }

        static void AddRoomLabels(Transform parent)
        {
            EnsureLabelFont();
            var labels = new GameObject("RoomLabels").transform;
            labels.SetParent(parent, false);

            foreach (var r in MapData.Rooms)
            {
                float width = Mathf.Min(r.w - 1.5f, 0.75f * r.name.Length + 1.5f);
                float lx = r.labelX ?? r.x + r.w / 2f;
                float lz = r.labelZ ?? r.z + 1.9f;

                var go = new GameObject("Label_" + r.name);
                go.transform.SetParent(labels, false);
                go.transform.position = MapData.ToWorld(lx, 0.04f, lz);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // นอนราบบนพื้น หัวตัวอักษรชี้ขึ้นบนจอ

                var tm = go.AddComponent<TextMesh>();
                tm.font = labelFont;
                tm.fontSize = 64;
                tm.fontStyle = FontStyle.Bold;
                tm.characterSize = 0.1f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.richText = false;
                tm.color = new Color(1f, 1f, 1f, 0.55f);
                tm.text = string.Join(" ", System.Array.ConvertAll(r.name.ToCharArray(), ch => ch.ToString()));

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = labelMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                // JS: ป้ายสูง = width/4, ตัวอักษร 120px จาก texture 1024px ⇒ ความสูงตัวอักษร ≈ 120/1024 × width
                // ตัวหนังสือถูกบีบแนวนอนให้ไม่เกิน 980/1024 ของความกว้างป้าย (fillText maxWidth)
                Bounds b = mr.bounds;
                float natW = Mathf.Max(b.size.x, 0.0001f);
                float natH = Mathf.Max(b.size.z, 0.0001f); // หลังหมุนแล้วความสูงข้อความไปอยู่แกน z ของโลก
                float em = 120f / 1024f * width;
                float sy = em * 1.2f / natH;
                float sx = Mathf.Min(sy, 980f / 1024f * width / natW);
                go.transform.localScale = new Vector3(sx, sy, 1f);
            }
        }

        // ───────────── กำแพง (เมชรวมเดียว แทน InstancedMesh) ─────────────
        static void AddWalls(Transform parent, MapGrid grid)
        {
            var body = new MeshBuilder();
            var cap = new MeshBuilder();
            var rng = new System.Random(777);

            for (int z = 0; z < MapData.GridH; z++)
            {
                for (int x = 0; x < MapData.GridW; x++)
                {
                    if (grid.IsFloor(x, z)) continue;
                    bool near = false;
                    for (int dz = -1; dz <= 1 && !near; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (grid.IsFloor(x + dx, z + dz)) { near = true; break; }
                    if (!near) continue;

                    float v = 0.92f + (float)rng.NextDouble() * 0.16f; // สุ่มความสว่างเล็กน้อยต่อบล็อก
                    body.AddBox(MapData.ToWorld(x + 0.5f, WallH / 2f, z + 0.5f), new Vector3(1f, WallH, 1f), new Color(v, v, v, 1f));
                    cap.AddBox(MapData.ToWorld(x + 0.5f, WallH + 0.05f, z + 0.5f), new Vector3(1f, 0.1f, 1f));
                }
            }

            var walls = new GameObject("Walls");
            walls.transform.SetParent(parent, false);
            MakeRenderer(walls.transform, "WallBody", body.ToMesh("wallBody"), Mats.Lit(0x3a4766, 0.7f, 0.15f), true);
            MakeRenderer(walls.transform, "WallCap", cap.ToMesh("wallCap"), Mats.Lit(0x8795b8, 0.5f, 0.2f), false);
        }

        static void MakeRenderer(Transform parent, string name, Mesh mesh, Material mat, bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }
    }
}
