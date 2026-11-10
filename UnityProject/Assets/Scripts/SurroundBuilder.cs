using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static SkeldMap.MedicalProps;

namespace SkeldMap
{
    // สภาพแวดล้อมนอกแมป (แทนพื้นหลังอวกาศดำ) ตามภาพ Blueprint "Military Medbase":
    //   พื้นกรวดของคอมพาวด์ในรั้ว · รั้วลวดตาข่าย · ป่าต้นไม้ low-poly นอกรั้ว · หิน/พุ่มไม้ · เสาไฟพร้อมแสงเรือง
    //   ลานเฮลิคอปเตอร์มุมซ้ายบน · เต็นท์พยาบาลมุมขวาล่าง
    //
    // ทุกอย่างอยู่นอกกริดเดิน (ไม่มีการชน เกมเพลย์ไม่เปลี่ยน) และรวมเมชเป็นก้อนใหญ่ไม่กี่ก้อน ไม่สร้าง GameObject นับพันชิ้น
    // พิกัดในไฟล์นี้เป็น "พิกัดกริด" (x ขวา, z ลง) แปลงเป็น world ด้วย MapData.ToWorld เหมือนที่อื่น
    public static class SurroundBuilder
    {
        // ขอบรั้ว/คอมพาวด์ (พิกัดกริด) — แมปมีห้องอยู่ในช่วง x 3..95, z 3..62
        const float FX0 = -4.5f, FX1 = 100.5f, FZ0 = -4.5f, FZ1 = 68.5f;
        const float ForestMargin = 24f;   // ป่าลึกจากรั้วออกไปกี่หน่วย (พอสำหรับโหมด Overview)

        static Vector3 W(float gx, float y, float gz) => MapData.ToWorld(gx, y, gz);
        static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        public static void Build(Transform parent, MapGrid grid)
        {
            var root = new GameObject("Surroundings").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(20260);

            AddGround(root);
            AddFence(root);
            AddForest(root, rng);
            AddCompoundDecor(root, grid, rng);
            AddHelipad(root);
            AddTent(root);
            AddLamps(root, grid);
        }

        // ───────────── เท็กซ์เจอร์ที่วาดด้วยโค้ด (tile ได้) ─────────────
        static Texture2D grass, gravel, fence, glow;

        static float Hash(int x, int y, int seed)
        {
            int h = x * 374761393 + y * 668265263 + seed * 144665;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        // noise เนียนแบบ tile ได้: สุ่มค่าที่จุดตารางทุก cell พิกเซล แล้วผสมแบบ bilinear (S ต้องหาร cell ลงตัว)
        static float Smooth(float x, float y, int cell, int seed)
        {
            const int S = 256;
            int period = S / cell;
            float fx = x / cell, fy = y / cell;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
            float H(int ix, int iy) => Hash(((ix % period) + period) % period, ((iy % period) + period) % period, seed);
            float a = Mathf.Lerp(H(x0, y0), H(x0 + 1, y0), tx);
            float b = Mathf.Lerp(H(x0, y0 + 1), H(x0 + 1, y0 + 1), tx);
            return Mathf.Lerp(a, b, ty);
        }

        static Texture2D MakeTile(string name, System.Func<int, int, Color32> px)
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true, false) { name = name };
            var d = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) d[y * S + x] = px(x, y);
            t.SetPixels32(d);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 8;
            t.Apply(true);
            return t;
        }

        // หญ้ามืด: ฐานเขียวเข้ม + ปื้นสว่าง/มืดแบบ tile ได้ (ใช้คลื่น sin คาบเต็ม) + เม็ดสุ่ม + ใบหญ้าสว่างประปราย
        static Texture2D GrassTex() => grass != null ? grass : grass = MakeTile("grass", (x, y) =>
        {
            // ปื้นใหญ่ (cell 64) + ปื้นกลาง (16) + เม็ดละเอียด (4) แบบเนียน
            float k = 0.55f + 0.5f * Smooth(x, y, 64, 1) + 0.28f * Smooth(x, y, 16, 2) + 0.14f * Smooth(x, y, 4, 3);
            if (Hash(x, y, 9) > 0.992f) k *= 1.5f; // ใบหญ้าสว่างประปราย
            return new Color32((byte)Mathf.Clamp(19 * k, 0, 255), (byte)Mathf.Clamp(36 * k, 0, 255), (byte)Mathf.Clamp(24 * k, 0, 255), 255);
        });

        // กรวด/พื้นดินอัดแน่นของคอมพาวด์: เทาอมน้ำเงินเข้ม ลายด่างเนียน + กรวดเม็ดเล็กประปราย
        static Texture2D GravelTex() => gravel != null ? gravel : gravel = MakeTile("gravel", (x, y) =>
        {
            float k = 0.5f + 0.45f * Smooth(x, y, 64, 4) + 0.3f * Smooth(x, y, 16, 5) + 0.16f * Smooth(x, y, 4, 6);
            float peb = Hash(x / 3, y / 3, 11);
            if (peb > 0.93f) k += 0.22f; else if (peb < 0.05f) k -= 0.2f;
            return new Color32((byte)Mathf.Clamp(40 * k, 0, 255), (byte)Mathf.Clamp(44 * k, 0, 255), (byte)Mathf.Clamp(54 * k, 0, 255), 255);
        });

        // ลายตาข่ายรั้ว: เส้นทแยงสองทิศ (alpha) ที่เหลือโปร่งใส
        static Texture2D FenceTex() => fence != null ? fence : fence = MakeTile("fence", (x, y) =>
        {
            // เศษของเลขติดลบใน C# เป็นลบ จึงต้องบวกกลับก่อน (ไม่งั้นเติมทึบเป็นสามเหลี่ยมใหญ่)
            int a = (x + y) % 32, b = (((x - y) % 32) + 32) % 32;
            bool line = a < 3 || b < 3;
            return line ? new Color32(190, 200, 215, 210) : new Color32(190, 200, 215, 0);
        });

        // แสงเรืองวงกลมใต้เสาไฟ (alpha ไล่จากกลางออกขอบ)
        static Texture2D GlowTex()
        {
            if (glow != null) return glow;
            const int S = 64;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false, false) { name = "glow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var d = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float r = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
                float a = Mathf.Clamp01(1f - r); a = a * a;
                d[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            t.SetPixels32(d); t.Apply(false);
            return glow = t;
        }

        // ───────────── พื้น ─────────────
        static void GroundQuad(MeshBuilder mb, float x0, float z0, float x1, float z1, float y, float tile)
        {
            int a = mb.Vertex(W(x0, y, z0), Vector3.up, new Vector2(x0 / tile, z0 / tile));
            int b = mb.Vertex(W(x1, y, z0), Vector3.up, new Vector2(x1 / tile, z0 / tile));
            int c = mb.Vertex(W(x1, y, z1), Vector3.up, new Vector2(x1 / tile, z1 / tile));
            int d = mb.Vertex(W(x0, y, z1), Vector3.up, new Vector2(x0 / tile, z1 / tile));
            mb.Quad(a, b, c, d);
        }

        static void AddGround(Transform root)
        {
            const float far = 90f;
            var g = new MeshBuilder();
            GroundQuad(g, FX0 - far, FZ0 - far, FX1 + far, FZ1 + far, -0.08f, 9f);   // หญ้านอกรั้ว (ผืนใหญ่)
            var c = new MeshBuilder();
            GroundQuad(c, FX0, FZ0, FX1, FZ1, -0.03f, 6f);                            // กรวดในรั้ว (ใต้แมปทั้งหมด → อุดช่องใต้ฐานกำแพง)
            Emit(root, "Grass", g.ToMesh("grass"), Mats.Lit(0xffffff, 0.95f, 0f, 0, 1f, GrassTex()), false, true);
            Emit(root, "Gravel", c.ToMesh("gravel"), Mats.Lit(0xffffff, 0.9f, 0f, 0, 1f, GravelTex()), false, true);
        }

        static void Emit(Transform parent, string name, Mesh mesh, Material mat, bool cast, bool receive)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = receive;
        }

        // ───────────── รั้วลวดตาข่าย ─────────────
        static void AddFence(Transform root)
        {
            var post = MeshBuilder.MeshSource.From(MeshBuilder.Cylinder(1f, 1f, 1f, 8), Matrix4x4.identity);
            var solid = new MeshBuilder();
            var panels = new MeshBuilder();
            Color steel = C(150, 160, 175);
            const float H = 1.7f;

            void Side(float ax, float az, float bx, float bz)
            {
                float len = Mathf.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
                Vector3 dir = new Vector3(bx - ax, 0, -(bz - az)).normalized; // ทิศใน world
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    Vector3 p = W(Mathf.Lerp(ax, bx, t), 0f, Mathf.Lerp(az, bz, t));
                    solid.AddMesh(post, Matrix4x4.TRS(p + Vector3.up * (H / 2f), Quaternion.identity, new Vector3(0.07f, H, 0.07f)), steel);
                }
                Vector3 a = W(ax, 0, az), b = W(bx, 0, bz), mid = (a + b) / 2f;
                foreach (float y in new[] { 0.06f, H - 0.03f })
                    solid.AddBox(mid + Vector3.up * y, new Vector3(Mathf.Abs(dir.x) > 0.5f ? len : 0.05f, 0.05f, Mathf.Abs(dir.x) > 0.5f ? 0.05f : len), steel);

                // แผ่นตาข่าย (ด้านเดียวสองหน้า) ยาวตลอดด้าน UV วนซ้ำตามความยาว
                int i0 = panels.Vertex(a, Vector3.up, new Vector2(0, 0));
                int i1 = panels.Vertex(b, Vector3.up, new Vector2(len / 0.8f, 0));
                int i2 = panels.Vertex(b + Vector3.up * H, Vector3.up, new Vector2(len / 0.8f, H / 0.8f));
                int i3 = panels.Vertex(a + Vector3.up * H, Vector3.up, new Vector2(0, H / 0.8f));
                panels.Quad(i0, i1, i2, i3);  // แผ่นตั้ง: ทำสองหน้าและคำนวณ normal ใหม่ด้านล่าง
            }

            Side(FX0, FZ0, FX1, FZ0);
            Side(FX1, FZ0, FX1, FZ1);
            Side(FX1, FZ1, FX0, FZ1);
            Side(FX0, FZ1, FX0, FZ0);

            Emit(root, "FencePosts", solid.ToMesh("fenceSolid"), Mats.Lit(0xffffff, 0.5f, 0.6f), true, true);

            // normal ของแผ่นตาข่ายตั้งตรง → ตั้งใหม่ให้ถูกทิศแล้วทำสองหน้า
            var pm = panels.MakeDoubleSided().ToMesh("fencePanels");
            pm.RecalculateNormals();
            Emit(root, "FencePanels", pm, Mats.Lit(0xdde4ee, 0.5f, 0.3f, 0, 1f, FenceTex(), 0.99f), false, false);
        }

        // ───────────── ป่านอกรั้ว ─────────────
        static void AddForest(Transform root, System.Random rng)
        {
            var trunkSrc = MeshBuilder.MeshSource.From(MeshBuilder.Cylinder(1f, 1f, 1f, 7), Matrix4x4.identity);
            var blobSrc = MeshBuilder.MeshSource.From(MeshBuilder.Icosahedron(1f, false), Matrix4x4.identity);
            var trunks = new MeshBuilder();
            var leaves = new MeshBuilder();
            float R() => (float)rng.NextDouble();

            const float step = 3.4f;
            int count = 0;
            for (float gx = FX0 - ForestMargin; gx < FX1 + ForestMargin; gx += step)
            {
                for (float gz = FZ0 - ForestMargin; gz < FZ1 + ForestMargin; gz += step)
                {
                    float x = gx + (R() - 0.5f) * step * 0.9f, z = gz + (R() - 0.5f) * step * 0.9f;
                    // เว้นระยะจากรั้วออกมา 2 หน่วย (ให้เห็นรั้วและพื้นหญ้าก่อนถึงแนวป่า)
                    if (x > FX0 - 2.5f && x < FX1 + 2.5f && z > FZ0 - 2.5f && z < FZ1 + 2.5f) continue;
                    if (R() < 0.12f) continue; // ช่องว่างเล็ก ๆ ให้ป่าดูไม่เป็นตาราง

                    float s = 0.8f + R() * 0.6f;
                    Vector3 p = W(x, 0f, z);
                    trunks.AddMesh(trunkSrc, Matrix4x4.TRS(p + Vector3.up * (0.9f * s), Quaternion.identity, new Vector3(0.26f * s, 1.8f * s, 0.26f * s)), C(70, 48, 30));

                    // ใบไม้ 3 ก้อนซ้อนกัน เฉดเขียวสุ่ม (ก้อนบนสว่างสุดเพราะมองจากด้านบน)
                    float g0 = 0.15f + R() * 0.10f;
                    for (int k = 0; k < 3; k++)
                    {
                        float rad = (1.55f - k * 0.28f) * s;
                        Vector3 off = new Vector3((R() - 0.5f) * 0.7f * s, (2.0f + k * 0.85f) * s, (R() - 0.5f) * 0.7f * s);
                        float gk = g0 + k * 0.05f;
                        var col = new Color(0.05f + R() * 0.04f, gk, 0.08f + R() * 0.04f, 1f);
                        var m = Matrix4x4.TRS(p + off, Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f), new Vector3(rad, rad * 0.85f, rad));
                        leaves.AddMesh(blobSrc, m, col);
                    }
                    count++;
                }
            }
            var green = Mats.Lit(0xffffff, 0.9f, 0f);
            Emit(root, "ForestTrunks", trunks.ToMesh("trunks"), green, true, true);
            Emit(root, "ForestLeaves", leaves.ToMesh("leaves"), green, true, true);
            Debug.Log($"[Skeld] ต้นไม้ {count} ต้น");
        }

        // ───────────── หิน/พุ่มไม้ในคอมพาวด์ (ช่องว่างระหว่างห้อง และขอบรั้ว) ─────────────
        static void AddCompoundDecor(Transform root, MapGrid grid, System.Random rng)
        {
            var blobSrc = MeshBuilder.MeshSource.From(MeshBuilder.Icosahedron(1f, false), Matrix4x4.identity);
            var rocks = new MeshBuilder();
            var bushes = new MeshBuilder();
            float R() => (float)rng.NextDouble();

            bool NearFloor(int cx, int cz, int rad)
            {
                for (int dz = -rad; dz <= rad; dz++) for (int dx = -rad; dx <= rad; dx++) if (grid.IsFloor(cx + dx, cz + dz)) return true;
                return false;
            }

            for (int z = (int)FZ0 + 1; z < (int)FZ1; z++)
            {
                for (int x = (int)FX0 + 1; x < (int)FX1; x++)
                {
                    if (NearFloor(x, z, 2)) continue;                      // เว้นรอบกำแพง 2 ช่อง
                    if (InSpecialZone(x + 0.5f, z + 0.5f)) continue;       // เว้นลานจอด/เต็นท์
                    float roll = R();
                    Vector3 p = W(x + R(), 0f, z + R());
                    if (roll < 0.035f)
                    {
                        // กลุ่มหิน 1-3 ก้อน
                        int n = 1 + (int)(R() * 3);
                        for (int k = 0; k < n; k++)
                        {
                            float s = 0.35f + R() * 0.5f;
                            Vector3 o = new Vector3((R() - 0.5f) * 0.9f, 0, (R() - 0.5f) * 0.9f);
                            float gry = 0.14f + R() * 0.10f;
                            rocks.AddMesh(blobSrc, Matrix4x4.TRS(p + o + Vector3.up * (s * 0.3f), Quaternion.Euler(R() * 40f, R() * 360f, R() * 40f), new Vector3(s, s * 0.65f, s * 1.1f)),
                                          new Color(gry, gry * 1.02f, gry * 1.1f, 1f));
                        }
                    }
                    else if (roll < 0.075f)
                    {
                        // พุ่มไม้ 2 ก้อน
                        for (int k = 0; k < 2; k++)
                        {
                            float s = 0.35f + R() * 0.35f;
                            Vector3 o = new Vector3((R() - 0.5f) * 0.6f, 0, (R() - 0.5f) * 0.6f);
                            bushes.AddMesh(blobSrc, Matrix4x4.TRS(p + o + Vector3.up * (s * 0.5f), Quaternion.Euler(0, R() * 360f, 0), new Vector3(s, s * 0.8f, s)),
                                           new Color(0.05f + R() * 0.04f, 0.15f + R() * 0.10f, 0.07f + R() * 0.04f, 1f));
                        }
                    }
                }
            }
            Emit(root, "Rocks", rocks.ToMesh("rocks"), Mats.Lit(0xffffff, 0.9f, 0.05f), true, true);
            Emit(root, "Bushes", bushes.ToMesh("bushes"), Mats.Lit(0xffffff, 0.9f, 0f), true, true);
        }

        // ลานเฮลิคอปเตอร์ (มุมซ้ายบน) และเต็นท์ (มุมขวาล่าง) — ใช้ตัดพื้นที่ออกจากการสุ่มหิน/พุ่มไม้
        static readonly Vector2 PadCenter = new Vector2(-0.4f, -0.4f);
        const float PadSize = 6.4f;
        static readonly Vector2 TentCenter = new Vector2(96.5f, 63.5f);

        static bool InSpecialZone(float gx, float gz)
            => (Mathf.Abs(gx - PadCenter.x) < PadSize / 2 + 1f && Mathf.Abs(gz - PadCenter.y) < PadSize / 2 + 1f)
            || (Mathf.Abs(gx - TentCenter.x) < 6f && Mathf.Abs(gz - TentCenter.y) < 5f);

        // ───────────── ลานเฮลิคอปเตอร์ ─────────────
        static void AddHelipad(Transform root)
        {
            var g = new GameObject("Helipad").transform;
            g.SetParent(root, false);
            g.position = W(PadCenter.x, 0f, PadCenter.y);

            var dark = Std(0x2c3038, 0.85f, 0.05f);
            PropFactory.Part(g, "pad", MeshBuilder.Box(PadSize, 0.05f, PadSize), dark, new Vector3(0, 0.02f, 0), false);

            // ขอบเหลือง-ดำสลับ
            var yel = Std(0xf4c430, 0.7f, 0f); var blk = Std(0x15171c, 0.8f, 0f);
            float half = PadSize / 2f - 0.25f;
            int seg = 16;
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < seg; i++)
                {
                    float t = -half + (i + 0.5f) * (2 * half / seg);
                    Vector3 pos = side switch { 0 => new Vector3(t, 0.05f, half), 1 => new Vector3(t, 0.05f, -half), 2 => new Vector3(half, 0.05f, t), _ => new Vector3(-half, 0.05f, t) };
                    Vector3 size = side < 2 ? new Vector3(2 * half / seg, 0.03f, 0.5f) : new Vector3(0.5f, 0.03f, 2 * half / seg);
                    PropFactory.Part(g, "stripe", MeshBuilder.Box(size.x, size.y, size.z), i % 2 == 0 ? yel : blk, pos, false);
                }
            }

            // ตัว H สีขาวขอบเหลือง + วงกลมจาง ๆ
            var white = Std(0xe8ecf4, 0.6f, 0f);
            PropFactory.Part(g, "H_l", MeshBuilder.Box(0.55f, 0.03f, 3.0f), white, new Vector3(-0.95f, 0.065f, 0), false);
            PropFactory.Part(g, "H_r", MeshBuilder.Box(0.55f, 0.03f, 3.0f), white, new Vector3(0.95f, 0.065f, 0), false);
            PropFactory.Part(g, "H_m", MeshBuilder.Box(1.5f, 0.03f, 0.55f), white, new Vector3(0, 0.065f, 0), false);
            PropFactory.Part(g, "ring", MeshBuilder.Cylinder(2.2f, 2.2f, 0.01f, 40), Std(0x3a3f4a, 0.8f, 0f), new Vector3(0, 0.045f, 0), false);

            // ไฟแดงมุมลานทั้ง 4 + แสงเรือง
            var red = Std(0x3a0a0a, 0.4f, 0f, 0xff2a2a, 2.4f);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    Vector3 c = new Vector3(sx * (PadSize / 2f - 0.15f), 0.12f, sz * (PadSize / 2f - 0.15f));
                    Cyl(g, "beacon", 0.17f, 0.18f, red, c, 12, null, false);
                    AddGlow(g, c + Vector3.down * 0.1f, 1.8f, new Color(1f, 0.2f, 0.2f, 0.55f));
                }
        }

        // ───────────── เต็นท์พยาบาล ─────────────
        static void AddTent(Transform root)
        {
            var g = new GameObject("MedTent").transform;
            g.SetParent(root, false);
            g.position = W(TentCenter.x, 0f, TentCenter.y);
            g.rotation = Quaternion.Euler(0, -8f, 0);

            var canvas = Std(0xd9c9a2, 0.9f, 0f);
            var canvasDark = Std(0xbfae85, 0.9f, 0f);
            const float Wd = 3.9f, L = 5.2f, wallH = 1.0f, run = 1.95f, rise = 1.1f;
            float panelLen = Mathf.Sqrt(run * run + rise * rise);
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;

            PropFactory.Part(g, "walls", MeshBuilder.Box(Wd, wallH, L), canvasDark, new Vector3(0, wallH / 2f, 0));
            foreach (float s in new[] { -1f, 1f })
            {
                // แผ่นหลังคาสองฝั่ง เอียงชนกันที่สันกลาง
                var pivot = new GameObject(s < 0 ? "roofL" : "roofR").transform;
                pivot.SetParent(g, false);
                pivot.localPosition = new Vector3(s * run / 2f, wallH + rise / 2f, 0);
                pivot.localRotation = Quaternion.Euler(0, 0, -s * ang);
                PropFactory.Part(pivot, "panel", MeshBuilder.Box(panelLen + 0.15f, 0.1f, L + 0.2f), canvas, Vector3.zero);
                if (s > 0)
                {
                    // กากบาทแดงบนหลังคาฝั่งขวา
                    var red = Std(0xd02a2a, 0.7f, 0f, 0xd02a2a, 0.4f);
                    PropFactory.Part(pivot, "crossH", MeshBuilder.Box(1.3f, 0.03f, 0.38f), red, new Vector3(0, 0.07f, 0.2f), false);
                    PropFactory.Part(pivot, "crossV", MeshBuilder.Box(0.38f, 0.03f, 1.3f), red, new Vector3(0, 0.07f, 0.2f), false);
                }
            }
            // หน้าจั่วปิดหัว-ท้ายเต็นท์ (สามเหลี่ยม 2 ด้าน) ให้อ่านออกว่าเป็นเต็นท์ ไม่ใช่กล่อง
            var gable = new MeshBuilder();
            foreach (float zs in new[] { -1f, 1f })
            {
                float z = zs * (L / 2f + 0.01f);
                int a = gable.Vertex(new Vector3(-Wd / 2f, wallH, z), new Vector3(0, 0, zs), Vector2.zero);
                int b = gable.Vertex(new Vector3(Wd / 2f, wallH, z), new Vector3(0, 0, zs), Vector2.zero);
                int c = gable.Vertex(new Vector3(0, wallH + rise, z), new Vector3(0, 0, zs), Vector2.zero);
                gable.Triangle(a, b, c);
            }
            PropFactory.Part(g, "gables", gable.ToMesh("tentGables"), canvasDark, Vector3.zero);

            // สันหลังคา + ปากทางเข้า (ด้านหน้า −z world) + กล่องพยาบาลหน้าเต็นท์
            PropFactory.Part(g, "ridge", MeshBuilder.Box(0.14f, 0.12f, L + 0.3f), Std(0x8a7a58, 0.9f, 0f), new Vector3(0, wallH + rise + 0.03f, 0));
            PropFactory.Part(g, "door", MeshBuilder.Box(1.2f, 1.2f, 0.1f), Std(0x2a2418, 0.9f, 0f), new Vector3(0, 0.62f, -L / 2f - 0.04f), false);
            var barrel = new GameObject("barrel").transform;
            barrel.SetParent(g, false);
            barrel.localPosition = new Vector3(-Wd / 2f - 0.8f, 0, -L / 2f + 0.5f);
            ReactorProps.Barrel(barrel, 0.32f);
            var crate = new GameObject("crate").transform;
            crate.SetParent(g, false);
            crate.localPosition = new Vector3(Wd / 2f + 1.0f, 0, -L / 2f + 0.4f);
            ArmoryProps.WeaponCrate(crate, 1.1f, 1.1f);

            // แสงอุ่นจากในเต็นท์ (เรืองหน้าทางเข้า)
            AddGlow(g, new Vector3(0, 0.05f, -L / 2f - 0.9f), 4.2f, new Color(1f, 0.75f, 0.4f, 0.55f));
        }

        // ───────────── เสาไฟ ─────────────
        static void AddGlow(Transform parent, Vector3 localPos, float size, Color color)
        {
            var mb = new MeshBuilder();
            float h = size / 2f;
            int a = mb.Vertex(new Vector3(-h, 0, -h), Vector3.up, new Vector2(0, 0));
            int b = mb.Vertex(new Vector3(h, 0, -h), Vector3.up, new Vector2(1, 0));
            int c = mb.Vertex(new Vector3(h, 0, h), Vector3.up, new Vector2(1, 1));
            int d = mb.Vertex(new Vector3(-h, 0, h), Vector3.up, new Vector2(0, 1));
            mb.Quad(a, b, c, d);
            var mat = Mats.Unlit(color, transparent: true, zWrite: false, tex: GlowTex());
            // วางที่ y คงที่เหนือพื้นเล็กน้อย (กัน z-fight กับกรวด)
            Vector3 p = parent.TransformPoint(localPos);
            p.y = 0.04f;
            var go = PropFactory.Part(parent, "glow", mb.ToMesh("glow"), mat, parent.InverseTransformPoint(p), false);
            go.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        static void AddLamps(Transform root, MapGrid grid)
        {
            var spots = new List<Vector2>();
            // 1) ตามแนวรั้ว (ห่างกัน ~17 หน่วย)
            for (float x = 4f; x < FX1 - 2f; x += 26f) { spots.Add(new Vector2(x, FZ0 + 1.2f)); spots.Add(new Vector2(x + 6f, FZ1 - 1.2f)); }
            for (float z = 6f; z < FZ1 - 4f; z += 26f) { spots.Add(new Vector2(FX0 + 1.2f, z + 5f)); spots.Add(new Vector2(FX1 - 1.2f, z)); }

            // 2) ใกล้กำแพงคอมพาวด์: ช่องที่ห่างจากพื้นเดินพอดี 2 ช่อง กระจายเว้นระยะ ≥ 13 หน่วย
            bool NearFloor(int cx, int cz, int rad)
            {
                for (int dz = -rad; dz <= rad; dz++) for (int dx = -rad; dx <= rad; dx++) if (grid.IsFloor(cx + dx, cz + dz)) return true;
                return false;
            }
            var near = new List<Vector2>();
            for (int z = (int)FZ0 + 2; z < (int)FZ1 - 1; z++)
                for (int x = (int)FX0 + 2; x < (int)FX1 - 1; x++)
                {
                    if (NearFloor(x, z, 1) || !NearFloor(x, z, 2)) continue;
                    if ((x * 7 + z * 13) % 11 != 0) continue;
                    var p = new Vector2(x + 0.5f, z + 0.5f);
                    if (InSpecialZone(p.x, p.y)) continue;
                    bool ok = true;
                    foreach (var q in near) if ((q - p).sqrMagnitude < 19f * 19f) { ok = false; break; }
                    if (ok) near.Add(p);
                }
            spots.AddRange(near);

            var lamps = new GameObject("Lamps").transform;
            lamps.SetParent(root, false);
            var postMat = Std(0x2a2d38, 0.5f, 0.5f);
            var head = Std(0x201a10, 0.4f, 0f, 0xffc860, 2.6f);
            int n = 0;
            foreach (var s in spots)
            {
                if (InSpecialZone(s.x, s.y)) continue;
                var l = new GameObject("Lamp").transform;
                l.SetParent(lamps, false);
                l.position = W(s.x, 0f, s.y);
                Cyl(l, "post", 0.07f, 2.3f, postMat, new Vector3(0, 1.15f, 0), 8);
                Box(l, "arm", 0.9f, 0.06f, 0.06f, postMat, new Vector3(0, 2.3f, 0));
                foreach (float sx in new[] { -1f, 1f })
                {
                    Box(l, "lampHead", 0.26f, 0.16f, 0.26f, head, new Vector3(sx * 0.42f, 2.22f, 0));
                    AddGlow(l, new Vector3(sx * 0.5f, 0f, 0), 3.4f, new Color(1f, 0.8f, 0.4f, 0.5f));
                }
                n++;
            }
            Debug.Log($"[Skeld] เสาไฟ {n} ต้น");
        }
    }
}
