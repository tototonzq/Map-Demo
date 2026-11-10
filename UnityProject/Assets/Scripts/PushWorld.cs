using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // ของชิ้นเล็ก/เบา (ถัง ถังขยะ เสาน้ำเกลือ รถเข็น ลังเล็ก ฯลฯ) ที่ผู้เล่น "เดินชนแล้วผลักได้"
    // · ชนเป็นวงกลมจริง (ไม่ใช่ช่องกริด 1×1) · หนักไม่เท่ากัน (k) = ยิ่งหนักผู้เล่นยิ่งถูกหน่วงตอนผลัก
    // · ผลักได้ไม่เกิน Leash จากจุดเดิมและไม่ออกนอกห้อง · ไม่เข้าใกล้จุดเควส/จุดเกิด · กด R คืนตำแหน่งทั้งหมด
    // ของชิ้นใหญ่/หนักยังเป็นช่องกริดตันตามเดิม (MapGrid.MarkProp ข้ามเฉพาะชิ้นที่ผลักได้)
    public static class PushWorld
    {
        // false = ปิดการผลัก ของชิ้นเล็กเป็นสิ่งกีดขวางตันธรรมดา (ไม่มีวงกลม/ไม่ขยับ)
        public static bool Enabled = true;

        public const float Leash = 2.4f;          // ผลักออกจากจุดเดิมได้ไกลสุด (หน่วยกริด)
        public const float KeepOut = 1.05f;       // ห้ามเข้าใกล้จุดเควส/จุดเกิดเกินนี้
        const float HomeKeepOut = 1.3f;           // ชิ้นที่เกิดใกล้จุดเควสกว่านี้ → ไม่ให้ผลัก (กันบังเควสถาวร)

        public sealed class Body
        {
            public PropDef def;
            public Transform t;
            public float homeX, homeZ, x, z, cr, k;
            public float minX, maxX, minZ, maxZ;   // กรอบห้องที่ห้ามผลักออกนอก
        }

        static readonly List<Body> bodies = new List<Body>();
        public static int Count => bodies.Count;
        public static IReadOnlyList<Body> Bodies => bodies;

        public static void Clear() => bodies.Clear();

        // ───── ชิ้นไหนผลักได้ ─────
        static float Radius(PropDef p) => p.r ?? Mathf.Max(p.w ?? 0.6f, p.d ?? 0.6f) / 2f;

        static float Weight(string kind)
        {
            switch (kind)
            {
                case "barrel": return 0.80f;
                case "iv": return 0.95f;
                case "bin": return 0.90f;
                case "watercooler": return 0.60f;
                case "plant": return 0.55f;
                case "wheelchair": return 0.90f;
                case "medcart": return 0.75f;
                case "cargocart": return 0.65f;
                case "cablereel": return 0.50f;
                case "crate": return 0.45f;
                case "weaponcrate": return 0.40f;
                default: return 0f;
            }
        }

        static RoomDef RoomOf(float x, float z, float margin)
        {
            foreach (var r in MapData.Rooms)
                if (x >= r.x + margin && x <= r.x + r.w - margin && z >= r.z + margin && z <= r.z + r.d - margin) return r;
            return null;
        }

        public static bool IsPushable(PropDef p)
        {
            if (!Enabled) return false;
            if (Weight(p.kind) <= 0f) return false;
            float cr = Radius(p);
            if (cr > 0.8f) return false;                         // ชิ้นใหญ่ = ของตัน
            if (RoomOf(p.x, p.z, cr) == null) return false;      // ต้องอยู่ในห้อง (ไม่ผลักในทางเดิน)
            if (Dist(p.x, p.z, MapData.Spawn.x, MapData.Spawn.y) < HomeKeepOut) return false;
            foreach (var t in MapData.Tasks)
                if (Dist(p.x, p.z, t.x, t.z) < HomeKeepOut) return false;
            return true;
        }

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static void Register(PropDef p, Transform t)
        {
            if (!IsPushable(p)) return;
            float cr = Radius(p);
            var room = RoomOf(p.x, p.z, cr);
            const float m = 0.05f;
            bodies.Add(new Body
            {
                def = p, t = t, homeX = p.x, homeZ = p.z, x = p.x, z = p.z, cr = cr, k = Weight(p.kind),
                minX = room.x + cr + m, maxX = room.x + room.w - cr - m,
                minZ = room.z + cr + m, maxZ = room.z + room.d - cr - m,
            });
        }

        public static void ResetAll()
        {
            foreach (var b in bodies) { b.x = b.homeX; b.z = b.homeZ; }
            Sync();
        }

        public static void Sync()
        {
            foreach (var b in bodies)
                if (b.t != null) b.t.position = MapData.ToWorld(b.x, 0f, b.z);
        }

        // ───── ผู้เล่น (วงกลมรัศมี pr) ผลักวัตถุ ─────
        public static void ResolvePlayer(ref float px, ref float pz, float pr, MapGrid grid)
        {
            for (int iter = 0; iter < 3; iter++)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    var b = bodies[i];
                    float dx = b.x - px, dz = b.z - pz;
                    float min = pr + b.cr;
                    float d2 = dx * dx + dz * dz;
                    if (d2 >= min * min) continue;

                    float d = Mathf.Sqrt(d2);
                    float nx, nz;
                    if (d > 1e-5f) { nx = dx / d; nz = dz / d; }
                    else { nx = 1f; nz = 0f; }
                    float pen = min - d;

                    float bx0 = b.x, bz0 = b.z;
                    b.x += nx * pen * b.k;                       // วัตถุขยับตามน้ำหนัก
                    b.z += nz * pen * b.k;
                    Settle(b, grid);
                    float moved = (b.x - bx0) * nx + (b.z - bz0) * nz;

                    float back = Mathf.Max(0f, pen - moved);    // ส่วนที่วัตถุไม่ยอมขยับ → ผู้เล่นถอย
                    px -= nx * back;
                    pz -= nz * back;
                }
            }
            Sync();
        }

        // จัดตำแหน่งวัตถุให้ไม่ทะลุกำแพง/วัตถุอื่น/จุดเควส และอยู่ใน leash
        static void Settle(Body b, MapGrid grid)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                Constrain(b, grid);

                for (int i = 0; i < bodies.Count; i++)
                {
                    var o = bodies[i];
                    if (o == b) continue;
                    float dx = o.x - b.x, dz = o.z - b.z;
                    float min = o.cr + b.cr;
                    float d2 = dx * dx + dz * dz;
                    if (d2 >= min * min) continue;
                    float d = Mathf.Sqrt(d2);
                    float nx = d > 1e-5f ? dx / d : 1f, nz = d > 1e-5f ? dz / d : 0f;
                    float pen = min - d;

                    // ดันตัวอื่นออกก่อน (ผลักต่อกันเป็นทอด) · ถ้าตัวนั้นติดขอบให้ถอยตัวนี้แทน
                    float ox0 = o.x, oz0 = o.z;
                    o.x += nx * pen;
                    o.z += nz * pen;
                    Constrain(o, grid);
                    float moved = (o.x - ox0) * nx + (o.z - oz0) * nz;
                    float back = Mathf.Max(0f, pen - moved);
                    b.x -= nx * back;
                    b.z -= nz * back;
                }
            }
            Constrain(b, grid);
        }

        // ข้อจำกัดของวัตถุเดี่ยว: กริดตัน · จุดเควส · leash · กรอบห้อง
        static void Constrain(Body b, MapGrid grid)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                PushOutOfCells(ref b.x, ref b.z, b.cr, grid);

                foreach (var t in MapData.Tasks) PushOutOfPoint(ref b.x, ref b.z, t.x, t.z, KeepOut + b.cr);
                PushOutOfPoint(ref b.x, ref b.z, MapData.Spawn.x, MapData.Spawn.y, KeepOut + b.cr);

                float hx = b.x - b.homeX, hz = b.z - b.homeZ;
                float hd = Mathf.Sqrt(hx * hx + hz * hz);
                if (hd > Leash) { b.x = b.homeX + hx / hd * Leash; b.z = b.homeZ + hz / hd * Leash; }

                b.x = Mathf.Clamp(b.x, b.minX, b.maxX);
                b.z = Mathf.Clamp(b.z, b.minZ, b.maxZ);
            }
        }

        static void PushOutOfPoint(ref float x, ref float z, float cx, float cz, float r)
        {
            float dx = x - cx, dz = z - cz;
            float d2 = dx * dx + dz * dz;
            if (d2 >= r * r) return;
            float d = Mathf.Sqrt(d2);
            if (d < 1e-5f) { x = cx + r; return; }
            x = cx + dx / d * r;
            z = cz + dz / d * r;
        }

        // วงกลมชนช่องกริดที่ตัน (รวมนอกพื้น) — ใช้ตรรกะเดียวกับผู้เล่น
        public static void PushOutOfCellsPublic(ref float x, ref float z, float r, MapGrid grid) => PushOutOfCells(ref x, ref z, r, grid);

        static void PushOutOfCells(ref float x, ref float z, float r, MapGrid grid)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                int cx = Mathf.FloorToInt(x), cz = Mathf.FloorToInt(z);
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int gx = cx + i, gz = cz + j;
                        if (!grid.IsBlocked(gx, gz)) continue;
                        float nx = Mathf.Clamp(x, gx, gx + 1);
                        float nz = Mathf.Clamp(z, gz, gz + 1);
                        float dx = x - nx, dz = z - nz;
                        float d2 = dx * dx + dz * dz;
                        if (d2 >= r * r) continue;
                        if (d2 > 1e-8f)
                        {
                            float d = Mathf.Sqrt(d2);
                            x += dx / d * (r - d);
                            z += dz / d * (r - d);
                        }
                        else
                        {
                            float l = x - gx, rr = gx + 1 - x, t = z - gz, bt = gz + 1 - z;
                            float m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(t, bt));
                            if (m == l) x = gx - r;
                            else if (m == rr) x = gx + 1 + r;
                            else if (m == t) z = gz - r;
                            else z = gz + 1 + r;
                        }
                    }
                }
            }
        }
    }
}
