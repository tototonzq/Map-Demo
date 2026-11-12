using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // สร้างพื้นและกำแพงของแมปจากชุด Kenney "Modular Space Kit" (CC0) ที่ Assets/Resources/SpaceKit/
    //
    // เกมเพลย์ไม่เปลี่ยน: กริด การชน ตำแหน่งเควส minimap ยังเป็นข้อมูลเดิมใน MapData · แก้เฉพาะ "หน้าตา"
    //
    // ขนาด: ชิ้นส่วนในชุดนี้ 1 ช่องพื้น = 4×4 หน่วย (กำแพงสูง 4.25 หนา 1) ส่วนกริดของเกม 1 ช่อง = 1 หน่วย
    //       จึงย่อ x,z ลง 4 เท่า (Cell = 0.25) ให้ 1 ช่องพื้น = 1 ช่องกริดพอดี แล้วปรับความสูงกำแพงแยกด้วย WallYScale
    //
    // การวางกำแพง: ทุกขอบที่ "ช่องพื้น" ติดกับ "ช่องว่าง" วาง template-wall 1 ชิ้น
    //   • ชิ้นส่วนมีด้านตกแต่งอยู่ที่ −z ของตัวเอง และด้านหลังเรียบอยู่ที่ z = 0 → หมุนให้ +z ชี้ออกจากห้อง
    //   • วางชิดนอกขอบของช่องพื้น (อยู่ในช่องที่ตันอยู่แล้ว) จึงไม่กินพื้นที่เดินและไม่ต้องแก้การชน
    //   • มุมที่กำแพงสองด้านมาบรรจบ มีช่องว่างสี่เหลี่ยมเล็ก ๆ → ปิดด้วยเสา template-detail
    //
    // ประสิทธิภาพ: รวมเมชเป็นก้อนเดียวต่อ (พาเลตต์สี × พื้น/กำแพง) ไม่สร้าง GameObject หลายพันชิ้น
    public static class KitMapBuilder
    {
        const string Dir = "SpaceKit/";
        const float Cell = 0.25f;           // 4 หน่วยของชุด = 1 ช่องกริด
        public const float WallYScale = 0.32f;   // 4.25 × 0.32 ≈ 1.36 หน่วย (กำแพงเดิมของเกมสูง 1.7)
        const float PillarBoost = 1.08f;    // เสาสูงกว่ากำแพงเล็กน้อย ซ่อนรอยตัดทับกันตรงมุม
        const float FloorTintStrength = 0.35f;

        // พาเลตต์: 0 = ส้ม (ค่าเริ่มต้น + ทางเดิน), 1 = ฟ้า (variation-a), 2 = เขียว (variation-b)
        static int VariantOf(string roomId)
        {
            var f = MapData.Current.KitVariant;
            return f != null ? f(roomId) : 0;
        }

        // ทิศบนกริด (dx, dz) · z บนกริดกลับด้านกับ world → world = (dx, 0, -dz)
        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        static MeshBuilder.MeshSource Load(string name)
        {
            var prefab = Resources.Load<GameObject>(Dir + name);
            if (prefab == null) { Debug.LogWarning($"[Skeld] ไม่พบ Resources/{Dir}{name}.fbx"); return null; }
            var mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) { Debug.LogWarning($"[Skeld] {name} ไม่มีเมช"); return null; }
            if (!mf.sharedMesh.isReadable)
            {
                Debug.LogWarning($"[Skeld] เมช {name} อ่านไม่ได้ (ปิด Read/Write) — เปิดที่ Import Settings ของ FBX");
                return null;
            }
            return MeshBuilder.MeshSource.From(mf.sharedMesh, mf.transform.localToWorldMatrix);
        }

        // คืน false ถ้าชุดชิ้นส่วนไม่ครบ → ผู้เรียกจะใช้แมปแบบเดิมที่สร้างด้วยโค้ดแทน
        public static bool TryBuild(Transform parent, MapGrid grid)
        {
            var wall = Load("template-wall");
            var floor = Load("template-floor");
            var floorVent = Load("template-floor-detail-a");
            var pillar = Load("template-detail");
            if (wall == null || floor == null || floorVent == null || pillar == null) return false;

            var textures = new[]
            {
                Resources.Load<Texture2D>(Dir + "Textures/colormap"),
                Resources.Load<Texture2D>(Dir + "Textures/variation-a"),
                Resources.Load<Texture2D>(Dir + "Textures/variation-b"),
            };
            foreach (var t in textures) if (t == null) { Debug.LogWarning("[Skeld] ไม่พบเท็กซ์เจอร์ของ SpaceKit"); return false; }

            var floors = new MeshBuilder[3];
            var walls = new MeshBuilder[3];
            for (int i = 0; i < 3; i++) { floors[i] = new MeshBuilder(); walls[i] = new MeshBuilder(); }

            // ช่องกริด → พาเลตต์ และสีห้อง (ใช้ทำ tint พื้นเบา ๆ ให้ห้องยังแยกสีออกจากกันเหมือนเดิม)
            int W = MapData.GridW, H = MapData.GridH;
            var variant = new int[W * H];
            var tint = new Color[W * H];
            for (int z = 0; z < H; z++)
            {
                for (int x = 0; x < W; x++)
                {
                    var room = MapData.RoomAt(x + 0.5f, z + 0.5f);
                    variant[z * W + x] = room != null ? VariantOf(room.id) : 0;
                    tint[z * W + x] = room != null ? RoomTint(room.color) : Color.white;
                }
            }

            var wallBase = new MeshBuilder();
            var rng = new System.Random(4242);
            var pillarSeen = new HashSet<long>();
            Vector3 CellCenter(float gx, float gz) => MapData.ToWorld(gx + 0.5f, 0f, gz + 0.5f);

            for (int z = 0; z < H; z++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (!grid.IsFloor(x, z)) continue;
                    int vi = variant[z * W + x];
                    Vector3 c = CellCenter(x, z);

                    // ── พื้น: กระเบื้องเรียบเป็นหลัก มีลายช่องระบายสุ่มบางช่อง (หมุนสุ่มทีละ 90°)
                    bool vent = rng.NextDouble() < 0.07;
                    var fsrc = vent ? floorVent : floor;
                    var frot = Quaternion.Euler(0f, rng.Next(4) * 90f, 0f);
                    floors[vi].AddMesh(fsrc, Matrix4x4.TRS(c, frot, new Vector3(Cell, 1f, Cell)), tint[z * W + x]);

                    // ── กำแพง: ทุกด้านที่ติดช่องว่าง
                    foreach (var d in Dirs)
                    {
                        if (grid.IsFloor(x + d.x, z + d.y)) continue;
                        Vector3 dw = new Vector3(d.x, 0f, -d.y);                // ทิศออกจากห้องใน world
                        Vector3 pos = c + dw * (0.5f + Cell);                   // ด้านหลังเรียบ (z=0) อยู่ลึกเข้าไปในช่องที่ตัน 0.25
                        var mat = Matrix4x4.TRS(pos, Quaternion.LookRotation(dw, Vector3.up), new Vector3(Cell, WallYScale, Cell));
                        walls[vi].AddMesh(wall, mat);

                        // แถบพื้นสีเข้มใต้ฐานกำแพง: ฐานของ template-wall มีร่องเว้า ถ้าไม่มีพื้นรองจะมองทะลุเห็นพื้นหลังดำ
                        // เป็นเส้นประดำสั้น ๆ ตามแนวโคนผนัง ซึ่งกะพริบเวลากล้องขยับ
                        Vector3 size = d.x != 0 ? new Vector3(Cell, 0.06f, 1f) : new Vector3(1f, 0.06f, Cell);
                        Vector3 sc = c + dw * (0.5f + Cell * 0.5f);
                        sc.y = 0.03f;
                        wallBase.AddBox(sc, size);
                    }

                    // ── เสาปิดมุม: มุมนูน (สองด้านตัน) และมุมเว้า (สองข้างเป็นพื้นแต่ทแยงเป็นช่องว่าง)
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            bool b1 = !grid.IsFloor(x + sx, z);
                            bool b2 = !grid.IsFloor(x, z + sz);
                            bool bd = !grid.IsFloor(x + sx, z + sz);
                            if (!((b1 && b2) || (!b1 && !b2 && bd))) continue;

                            float gx = x + 0.5f + 0.625f * sx, gz = z + 0.5f + 0.625f * sz;
                            long key = ((long)Mathf.RoundToInt(gx * 8f) << 20) ^ (long)Mathf.RoundToInt(gz * 8f);
                            if (!pillarSeen.Add(key)) continue;
                            Vector3 pp = MapData.ToWorld(gx, 0f, gz);
                            walls[vi].AddMesh(pillar, Matrix4x4.TRS(pp, Quaternion.identity, new Vector3(Cell, WallYScale * PillarBoost, Cell)));
                        }
                    }
                }
            }

            var root = new GameObject("KitMap");
            root.transform.SetParent(parent, false);
            if (wallBase.VertexCount > 0)
                Emit(root.transform, "WallBase", wallBase.ToMesh("kitWallBase"), Mats.Lit(0x242a45, 0.85f, 0.05f), false);
            for (int i = 0; i < 3; i++)
            {
                var mat = Mats.Lit(0xffffff, 0.8f, 0.05f, 0, 1f, textures[i]);
                if (floors[i].VertexCount > 0) Emit(root.transform, "Floors_" + i, floors[i].ToMesh("kitFloors" + i), mat, false);
                if (walls[i].VertexCount > 0) Emit(root.transform, "Walls_" + i, walls[i].ToMesh("kitWalls" + i), mat, true);
            }
            return true;
        }

        // สีห้อง → สี tint เบา ๆ (ปรับให้องค์ประกอบสูงสุด = 1 เพื่อไม่ให้พื้นมืดลง) เหลือแต่ "โทนสี" ของห้อง
        static Color RoomTint(uint hex)
        {
            Color c = Mats.Hex(hex);
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            Color hue = m > 0f ? new Color(c.r / m, c.g / m, c.b / m, 1f) : Color.white;
            return Color.Lerp(Color.white, hue, FloorTintStrength);
        }

        static void Emit(Transform parent, string name, Mesh mesh, Material mat, bool castShadows)
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
