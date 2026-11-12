using UnityEngine;

namespace SkeldMap
{
    // ข้อมูลแมปสไตล์ The Skeld (port จาก js/mapData.js)
    // หน่วย 1 = 1 ช่องกริด · cell i ครอบคลุม [i, i+1)
    //
    // ข้อตกลงเรื่องพิกัด: ข้อมูลทั้งหมดใช้ "พิกัดกริด" (x ไปทางขวา, z ไปทางล่างของจอ ตามเว็บเดิม)
    // ตอนวางของลงโลก Unity (มือซ้าย) จะแปลงเป็น world = (x, y, -z) ผ่าน MapData.ToWorld
    // ภาพที่ได้จึงเหมือนเวอร์ชัน three.js ทุกประการ (ไม่กลับซ้าย-ขวา)

    public sealed class RoomDef
    {
        public readonly string id, name;
        public readonly int x, z, w, d;   // x, z = มุมซ้ายบน
        public readonly uint color;
        public readonly float? labelX, labelZ;

        public RoomDef(string id, string name, int x, int z, int w, int d, uint color,
                       float? labelX = null, float? labelZ = null)
        {
            this.id = id; this.name = name;
            this.x = x; this.z = z; this.w = w; this.d = d;
            this.color = color; this.labelX = labelX; this.labelZ = labelZ;
        }
    }

    public sealed class TaskDef
    {
        public readonly string id, room, name;
        public readonly float x, z;
        public readonly float y;   // ความสูงพื้นที่ยืน (เช่น ยืนบนแท่นสแกน) — ยกวงแหวนจุดเหลืองและตัวละครขึ้น

        public TaskDef(string id, string room, string name, float x, float z, float y = 0f)
        {
            this.id = id; this.room = room; this.name = name; this.x = x; this.z = z; this.y = y;
        }
    }

    public sealed class PropDef
    {
        public string kind;
        public float x, z;
        public float? r, w, d, h;
        public float? rot;                 // หมุนรอบแกน Y เป็นองศา (ดู PropFactory.Build)
        public float? sx, sz;              // ขนาดของ "ของตกแต่งที่ไม่มีการชน" (เช่นขีดบนพื้น) — ห้ามใช้ w/d เพราะจะถูกนับเป็นสิ่งกีดขวาง
        public uint? color, screen, glow;
        public bool button;
    }

    public static class MapData
    {
        public const int GridW = 96;
        public const int GridH = 64;

        // กริด (x, y, z) → world ของ Unity
        public static Vector3 ToWorld(float x, float y, float z) => new Vector3(x, y, -z);

        static MapDef current;
        public static MapDef Current => current ?? (current = SkeldMapDef.Create());

        public static readonly string[] MapIds = { "skeld", "aurora" };
        public static readonly string[] MapNames = { "THE SKELD", "AURORA STATION" };

        public static MapDef Create(string mapId)
        {
            switch (mapId)
            {
                case "aurora": return AuroraMapDef.Create();
                default: return SkeldMapDef.Create();
            }
        }

        public static void Select(string mapId)
        {
            if (current != null && current.id == mapId) return;
            current = Create(mapId);
        }

        public static string CurrentId => Current.id;

        public static RoomDef[] Rooms => Current.Rooms;
        public static int[][] Corridors => Current.Corridors;
        public static Vector2 Spawn => Current.Spawn;
        public static TaskDef[] Tasks => Current.Tasks;
        public static Vector2[] Vents => Current.Vents;
        public static PropDef[] Props => Current.Props;
        public static Vector2 ButtonPos => Current.ButtonPos;
        public static string MeetingRoomId => Current.MeetingRoomId;


        public static RoomDef RoomAt(float px, float pz)
        {
            foreach (var r in Rooms)
                if (px >= r.x && px < r.x + r.w && pz >= r.z && pz < r.z + r.d) return r;
            return null;
        }

        public static string RoomName(string id)
        {
            foreach (var r in Rooms) if (r.id == id) return r.name;
            return id;
        }
    }

    // กริดพื้น (เดินได้) + กริดสิ่งกีดขวางจากของตกแต่ง
    public sealed class MapGrid
    {
        public readonly byte[] Floor = new byte[MapData.GridW * MapData.GridH];
        public readonly byte[] Solid = new byte[MapData.GridW * MapData.GridH];

        const int W = MapData.GridW;
        const int H = MapData.GridH;

        public MapGrid()
        {
            foreach (var r in MapData.Rooms) Carve(r.x, r.z, r.w, r.d);
            foreach (var c in MapData.Corridors) Carve(c[0], c[1], c[2], c[3]);
            foreach (var p in MapData.Props) MarkProp(p);
        }

        public bool IsFloor(int x, int z) => x >= 0 && z >= 0 && x < W && z < H && Floor[z * W + x] == 1;

        public bool IsSolid(int x, int z) => Solid[z * W + x] == 1;

        public bool IsBlocked(int x, int z) => !IsFloor(x, z) || Solid[z * W + x] == 1;

        void Carve(int x, int z, int w, int d)
        {
            for (int j = z; j < z + d; j++)
                for (int i = x; i < x + w; i++)
                    Floor[j * W + i] = 1;
        }

        void MarkProp(PropDef p)
        {
            if (PushWorld.IsPushable(p)) return;
            if (p.kind == "scanner")
            {
                // แท่นสแกน: เดินขึ้น/ยืนบนแท่นได้ — ตันเฉพาะช่องที่เสาสองข้างอยู่ (กึ่งกลางโล่งให้ผ่านและยืนได้)
                int cz = Mathf.FloorToInt(p.z);
                float off = (p.w ?? 2.4f) / 2f - 0.22f;
                foreach (float sx in new[] { -off, off })
                {
                    int cx = Mathf.FloorToInt(p.x + sx);
                    if (cx >= 0 && cz >= 0 && cx < W && cz < H) Solid[cz * W + cx] = 1;
                }
                return;
            }   // ชิ้นที่ผลักได้ชนเป็นวงกลมจริงใน PushWorld ไม่ใช่ช่องกริดตัน
            if (p.r.HasValue)
            {
                float r = p.r.Value;
                float rr = r - 0.15f;
                int z0 = Mathf.FloorToInt(p.z - r), z1 = Mathf.CeilToInt(p.z + r);
                int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r);
                for (int z = z0; z <= z1; z++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (x < 0 || z < 0 || x >= W || z >= H) continue;
                        float nx = Mathf.Clamp(p.x, x, x + 1);
                        float nz = Mathf.Clamp(p.z, z, z + 1);
                        float dx = p.x - nx, dz = p.z - nz;
                        if (Mathf.Sqrt(dx * dx + dz * dz) < rr) Solid[z * W + x] = 1;
                    }
                }
            }
            else
            {
                float pw = p.w ?? 0, pd = p.d ?? 0;
                float x0 = p.x - pw / 2, x1 = p.x + pw / 2, z0 = p.z - pd / 2, z1 = p.z + pd / 2;
                for (int z = Mathf.FloorToInt(z0); z < Mathf.CeilToInt(z1); z++)
                {
                    for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x1); x++)
                    {
                        if (x < 0 || z < 0 || x >= W || z >= H) continue;
                        float ox = Mathf.Min(x1, x + 1) - Mathf.Max(x0, x);
                        float oz = Mathf.Min(z1, z + 1) - Mathf.Max(z0, z);
                        if (ox > 0.2f && oz > 0.2f) Solid[z * W + x] = 1;
                    }
                }
            }
        }
    }
}
