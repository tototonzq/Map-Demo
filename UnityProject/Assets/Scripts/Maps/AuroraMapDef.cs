using UnityEngine;

namespace SkeldMap
{
    // แมปที่ 2: AURORA STATION — สถานีวิจัยขั้วโลก · ห้องรวม ATRIUM อยู่กลาง มีทางเดินวงแหวนล้อมและทางแยกออกไปแต่ละห้อง
    // พิกัดกริด 96×64 เหมือนแมปอื่น · rot: 0 หันลงล่างจอ, 90 หันซ้าย, 180 หันขึ้น, 270 หันขวา
    public static class AuroraMapDef
    {
        static PropDef P(string kind, float x, float z, float? r = null, float? w = null, float? d = null,
                         float? h = null, uint? color = null, uint? screen = null, uint? glow = null,
                         bool button = false, float? rot = null, float? sx = null, float? sz = null)
            => MapDef.P(kind, x, z, r, w, d, h, color, screen, glow, button, rot, sx, sz);

        static int KitVariant(string roomId)
        {
            switch (roomId)
            {
                case "bridge": case "medlab": case "science": case "shield": case "observatory": case "security": return 1;
                case "greenhouse": case "engineering": case "reactor": return 2;
                default: return 0;
            }
        }

        static RoomDef[] BuildRooms() => new RoomDef[]
        {
            new RoomDef("atrium", "ATRIUM", 40, 26, 16, 14, 0xc8b98e),
            new RoomDef("bridge", "BRIDGE", 38, 4, 20, 10, 0x8fa3c8),
            new RoomDef("comms", "COMMS", 22, 4, 12, 10, 0x8fc0c8),
            new RoomDef("observatory", "OBSERVATORY", 62, 4, 14, 12, 0x9a8fc8),
            new RoomDef("admin", "ADMIN", 60, 16, 10, 8, 0xc8ab8f),
            new RoomDef("science", "SCIENCE LAB", 70, 20, 14, 12, 0x8fc89e),
            new RoomDef("medlab", "MEDLAB", 66, 34, 16, 10, 0xa9c4de),
            new RoomDef("shield", "SHIELD BAY", 78, 50, 12, 10, 0x7e9ac8),
            new RoomDef("storage", "STORAGE", 60, 46, 14, 12, 0xb0a08a),
            new RoomDef("reactor", "REACTOR CORE", 40, 48, 16, 12, 0xc88f8f),
            new RoomDef("engineering", "ENGINEERING", 24, 50, 12, 10, 0xbfa36a),
            new RoomDef("greenhouse", "GREENHOUSE", 6, 52, 16, 10, 0x86c88a),
            new RoomDef("security", "SECURITY", 8, 38, 12, 10, 0x9aa6b8),
            new RoomDef("habitat", "HABITAT", 6, 22, 14, 12, 0xc89eb0),
            new RoomDef("armory", "ARMORY", 22, 22, 12, 10, 0x9aa37e),
        };

        static int[][] BuildCorridors() => new int[][]
        {
            // วงแหวนรอบ Atrium
            new[] { 36, 20, 24, 3 }, new[] { 36, 43, 24, 3 }, new[] { 36, 23, 3, 20 }, new[] { 57, 23, 3, 20 },
            // Atrium ↔ วงแหวน
            new[] { 46, 23, 4, 3 }, new[] { 46, 40, 4, 3 }, new[] { 39, 31, 1, 4 }, new[] { 56, 31, 1, 4 },
            // ทางแยก
            new[] { 46, 14, 4, 6 },   // Bridge ↔ วงแหวนบน
            new[] { 34, 8, 4, 3 },    // Comms ↔ Bridge
            new[] { 58, 8, 4, 3 },    // Bridge ↔ Observatory
            new[] { 60, 26, 10, 3 },  // วงแหวนขวา ↔ Science
            new[] { 62, 24, 3, 2 },   // Admin ↔ ทางเดินนั้น
            new[] { 74, 32, 3, 2 },   // Science ↔ Medlab
            new[] { 68, 44, 3, 2 },   // Medlab ↔ Storage
            new[] { 60, 44, 4, 3 },   // วงแหวนล่าง ↔ Storage
            new[] { 74, 52, 4, 3 },   // Storage ↔ Shield
            new[] { 46, 46, 4, 2 },   // วงแหวนล่าง ↔ Reactor
            new[] { 56, 52, 4, 3 },   // Reactor ↔ Storage
            new[] { 36, 52, 4, 3 },   // Engineering ↔ Reactor
            new[] { 22, 54, 2, 3 },   // Greenhouse ↔ Engineering
            new[] { 20, 44, 16, 3 },  // Security ↔ วงแหวนล่าง
            new[] { 12, 34, 3, 4 },   // Habitat ↔ Security
            new[] { 12, 48, 3, 4 },   // Security ↔ Greenhouse
            new[] { 20, 26, 2, 3 },   // Habitat ↔ Armory
            new[] { 34, 26, 2, 3 },   // Armory ↔ วงแหวนซ้าย
            new[] { 26, 14, 3, 8 },   // Comms ↔ Armory
        };

        static TaskDef[] BuildTasks() => new TaskDef[]
        {
            new TaskDef("course", "bridge", "Set Course", 44.3f, 6.0f),                // หน้าคอนโซลนำทาง
            new TaskDef("download", "observatory", "Download Data", 69f, 6.8f),         // หน้าตู้ดาวน์โหลด (datakiosk)
            new TaskDef("comms", "comms", "Reboot Comms", 26f, 6.7f),                   // หน้าโต๊ะสื่อสาร
            new TaskDef("upload", "admin", "Upload Data", 65f, 18.8f),                  // หน้าตู้อัปโหลด (datakiosk)
            new TaskDef("filter", "science", "Clean Air Filter", 77f, 22.7f),           // หน้าตัวกรองอากาศ
            new TaskDef("scan", "medlab", "Submit Scan", 74f, 39f, 0.3f),               // ยืนบนแท่นสแกน
            new TaskDef("shields", "shield", "Prime Shields", 84f, 52.7f),              // หน้าแท่นควบคุมโล่
            new TaskDef("reactor", "reactor", "Start Reactor", 44f, 50.8f),             // หน้าแผงควบคุมเตาปฏิกรณ์
            new TaskDef("fuel", "engineering", "Refuel Generator", 30f, 52.8f),         // หน้าปั๊มน้ำมัน
            new TaskDef("wiring", "storage", "Fix Wiring", 67f, 47.8f),                 // หน้าแผงสายไฟ
            new TaskDef("swipe", "security", "Swipe Card", 16.5f, 39.7f),               // หน้าเครื่องรูดบัตร (ขยับจาก x14 เพราะ x12-14 เป็นประตูเหนือ)
            new TaskDef("asteroids", "armory", "Calibrate Turret", 30.5f, 25.5f),       // หน้าป้อมปืน (ขยับจาก (28,23.7) ที่อยู่ในช่องประตูเหนือ)
        };

        static Vector2[] BuildVents() => new Vector2[]
        {
            new Vector2(42f, 12f), new Vector2(79f, 42f), new Vector2(43f, 58f),
            new Vector2(10f, 46f), new Vector2(72f, 30f), new Vector2(62f, 56f),
        };

        static PropDef[] BuildProps() => new PropDef[]
        {
            // ══ ATRIUM (ห้องประชุม) ══
            P("table", 48, 30, r: 1.5f, button: true),
            P("table", 43.6f, 35.4f, r: 1.2f), P("table", 52.4f, 36.8f, r: 1.2f),
            P("table", 42.8f, 28.8f, r: 1.2f), P("table", 53.0f, 27.6f, r: 1.2f),
            P("counter", 40.5f, 36.8f, w: 0.9f, d: 3.2f, rot: 270),
            P("vending", 42.6f, 39.5f, w: 1.2f, d: 0.9f, rot: 180), P("vending", 44.0f, 39.5f, w: 1.2f, d: 0.9f, rot: 180),
            P("watercooler", 45.2f, 39.5f, r: 0.3f),
            P("plant", 40.8f, 26.8f, r: 0.5f), P("plant", 55.2f, 26.8f, r: 0.5f),
            P("plant", 40.8f, 39.2f, r: 0.5f), P("plant", 55.2f, 39.2f, r: 0.5f),
            P("holoemitter", 44.8f, 27.0f, r: 0.5f),
            P("bin", 54.9f, 38.0f, r: 0.3f),
            P("lightstrip", 55.85f, 28.5f, h: 3f, rot: 90), P("lightstrip", 55.85f, 36.0f, h: 3f, rot: 90),
            P("lightstrip", 50.5f, 26.15f, h: 3f),

            // ══ BRIDGE ══
            P("console", 44.3f, 4.7f, w: 1.2f, d: 0.8f, screen: 0x7dff9a),              // เครื่องนำทาง (เควส Set Course)
            P("console", 52.0f, 4.55f, w: 4f, d: 0.8f, screen: 0x55e0ff),               // แถวคอนโซลหลัก
            P("console", 55.9f, 4.55f, w: 3.2f, d: 0.8f, screen: 0xff8855),
            P("pilotchair", 50.5f, 6.3f, rot: 180), P("pilotchair", 53.0f, 6.3f, rot: 180),
            P("starboard", 38.3f, 6.2f, w: 0.4f, d: 3.4f, rot: 270),
            P("holotable", 48.0f, 8.0f, r: 1.0f),
            P("radarstation", 44.0f, 13.35f, w: 2.4f, d: 1.0f, rot: 180),
            P("officechair", 44.0f, 11.7f), P("officechair", 56.0f, 12.2f, rot: 270),
            P("signalboard", 57.7f, 12.2f, w: 0.4f, d: 2.4f, rot: 90),
            P("plant", 38.8f, 13.2f, r: 0.5f),
            P("lightstrip", 53.0f, 13.85f, h: 5f),
            P("cables", 41.0f, 8.6f, rot: 30),

            // ══ COMMS ══
            P("commsdesk", 26.0f, 5.6f, w: 1.6f, d: 0.8f),                              // โต๊ะสื่อสาร (เควส Reboot Comms)
            P("console", 29.3f, 4.55f, w: 2.6f, d: 0.8f, screen: 0xc08cff), P("officechair", 29.3f, 6.0f, rot: 180),
            P("antenna", 32.2f, 5.0f, r: 0.6f),
            P("serverrack", 22.5f, 5.3f, w: 0.9f, d: 1.0f, rot: 270), P("serverrack", 22.5f, 6.5f, w: 0.9f, d: 1.0f, rot: 270),
            P("serverrack", 22.5f, 7.7f, w: 0.9f, d: 1.0f, rot: 270),
            P("signalboard", 33.75f, 5.4f, w: 0.4f, d: 2.4f, rot: 90),
            P("cables", 29.4f, 8.2f, rot: 30), P("cables", 24.4f, 11.0f, rot: 100),
            P("plant", 22.8f, 12.8f, r: 0.5f), P("bin", 33.2f, 12.8f, r: 0.3f),

            // ══ OBSERVATORY ══
            P("datakiosk", 69.0f, 5.75f, w: 1.2f, d: 0.8f),                             // ตู้ดาวน์โหลด (เควส Download Data)
            P("dish", 73.0f, 6.5f, r: 1.2f),
            P("starboard", 65.5f, 4.25f, w: 3.6f, d: 0.4f), P("starboard", 75.75f, 10.0f, w: 0.4f, d: 4.0f, rot: 90),
            P("holoemitter", 66.0f, 11.5f, r: 0.5f),
            P("crate", 71.5f, 11.3f, w: 2.4f, d: 0.7f, h: 0.45f, color: 0x4a5a72),       // ม้านั่งชมดาว
            P("plant", 62.8f, 5.0f, r: 0.5f), P("plant", 75.0f, 14.8f, r: 0.5f),
            P("cables", 66.0f, 8.8f, rot: 60),
            P("lightstrip", 62.15f, 11.5f, h: 3f, rot: 90),

            // ══ ADMIN ══
            P("datakiosk", 65.0f, 17.75f, w: 1.2f, d: 0.8f, button: true),               // ตู้อัปโหลด (เควส Upload Data)
            P("adminboard", 67.2f, 23.75f, w: 3.6f, d: 0.4f, rot: 180),
            P("filecabinet", 69.45f, 16.6f, w: 0.6f, d: 1.0f, rot: 90), P("filecabinet", 69.45f, 17.7f, w: 0.6f, d: 1.0f, rot: 90),
            P("filecabinet", 69.45f, 18.8f, w: 0.6f, d: 1.0f, rot: 90),
            P("printer", 61.0f, 23.55f, w: 1.0f, d: 0.8f, rot: 180),
            P("table", 66.2f, 20.8f, r: 1.2f),
            P("officechair", 63.6f, 20.8f, rot: 270), P("officechair", 68.7f, 20.8f, rot: 90),
            P("bin", 60.7f, 16.8f, r: 0.3f),
            P("lightstrip", 60.15f, 18.0f, h: 3f, rot: 90),

            // ══ SCIENCE LAB ══
            P("airfilter", 77.0f, 21.7f, w: 2.2f, d: 0.9f),                              // ตัวกรองอากาศ (เควส Clean Air Filter)
            P("workbench", 79.4f, 20.6f, w: 2.4f, d: 0.9f), P("workbench", 73.2f, 20.6f, w: 2.4f, d: 0.9f),
            P("tank", 82.8f, 21.8f, r: 0.7f, h: 1.8f, color: 0x9fdcd6, glow: 0x55e0ff),
            P("tank", 82.8f, 29.8f, r: 0.7f, h: 1.8f, color: 0x9fdcd6, glow: 0x55e0ff),
            P("hydroponic", 79.0f, 31.55f, w: 3.6f, d: 0.8f, rot: 180),
            P("holotable", 79.5f, 26.0f, r: 1.0f),
            P("monitor", 83.3f, 25.0f, w: 0.8f, d: 0.8f, rot: 90),
            P("cabinet", 70.45f, 30.0f, w: 0.5f, d: 1.4f, rot: 270),
            P("bin", 71.8f, 24.0f, r: 0.3f),
            P("lightstrip", 83.85f, 27.5f, h: 2.5f, rot: 90),
            P("pipe", 76.5f, 20.25f, h: 3.4f),

            // ══ MEDLAB ══
            P("scanner", 74.0f, 39.0f, w: 3.0f, d: 1.4f),                                // แท่นสแกน (เควส Submit Scan)
            P("bed", 68.8f, 35.8f, w: 3f, d: 1.6f), P("bed", 78.6f, 35.8f, w: 3f, d: 1.6f), P("bed", 69.0f, 39.6f, w: 3f, d: 1.6f),
            P("iv", 70.9f, 35.6f, r: 0.3f), P("iv", 80.5f, 35.6f, r: 0.3f),
            P("monitor", 66.7f, 35.6f, w: 0.8f, d: 0.8f), P("monitor", 81.5f, 35.6f, w: 0.8f, d: 0.8f),
            P("cabinet", 66.45f, 41.2f, w: 0.5f, d: 1.4f, rot: 270), P("cabinet", 81.55f, 40.0f, w: 0.5f, d: 1.4f, rot: 90),
            P("medcart", 76.5f, 42.0f, w: 0.6f, d: 1.2f, rot: 270),
            P("wheelchair", 80.8f, 42.8f, w: 1f, d: 1f, rot: 90),
            P("crutch", 66.45f, 43.0f),
            P("plant", 81.3f, 38.0f, r: 0.5f),

            // ══ SHIELD BAY ══
            P("shieldpanel", 83.0f, 52.7f, w: 0.5f, d: 1.8f, rot: 270),                  // แท่นควบคุมโล่ (เควส Prime Shields)
            P("shieldgen", 85.0f, 55.8f, r: 1.0f),
            P("capacitor", 81.5f, 59.55f, w: 2.2f, d: 0.7f, rot: 180), P("capacitor", 87.5f, 59.55f, w: 2.2f, d: 0.7f, rot: 180),
            P("accesspanel", 89.55f, 57.5f, w: 0.9f, d: 0.4f, rot: 90),
            P("lightstrip", 89.85f, 53.5f, h: 3f, rot: 90), P("lightstrip", 87.5f, 50.15f, h: 4f),
            P("pipe", 82.0f, 50.3f, h: 5f),
            P("holoemitter", 88.5f, 54.0f, r: 0.5f),
            P("plant", 89.2f, 58.9f, r: 0.5f),

            // ══ STORAGE ══
            P("wirepanel", 67.0f, 46.5f, w: 1.6f, d: 0.4f),                              // แผงสายไฟ (เควส Fix Wiring)
            P("crate", 61.2f, 49.0f, w: 1.8f, d: 1.8f, h: 1.0f),
            P("crate", 72.4f, 48.8f, w: 1.8f, d: 1.8f, h: 1.3f, color: 0x6a7a8f),
            P("crate", 72.4f, 56.4f, w: 2.2f, d: 2.0f, h: 1.2f, color: 0x5a6b52),
            P("crate", 70.0f, 55.0f, w: 1.4f, d: 1.4f, h: 1.0f),
            P("crate", 66.0f, 55.9f, w: 1.4f, d: 1.4f, h: 0.8f, color: 0x3d4a66),
            P("crate", 64.7f, 47.3f, w: 1.2f, d: 1.2f, h: 0.8f, color: 0x8a6a3f),
            P("shelf", 65.0f, 57.55f, w: 2.4f, d: 0.8f, rot: 180), P("shelf", 68.4f, 57.55f, w: 2.4f, d: 0.8f, rot: 180),
            P("cargocart", 63.8f, 51.0f, w: 1.4f, d: 1.0f),
            P("cablereel", 64.9f, 49.6f, r: 0.5f),
            P("fuelcell", 60.9f, 51.0f, r: 0.6f), P("fuelcell", 73.0f, 51.0f, r: 0.6f),
            P("lightstrip", 73.85f, 49.5f, h: 2.4f, rot: 90),

            // ══ REACTOR CORE ══
            P("reactor", 48.0f, 54.0f, r: 2.2f),
            P("hazardring", 48.0f, 54.0f, h: 3.2f),
            P("reactorpanel", 42.9f, 50.8f, w: 0.9f, d: 2.2f, rot: 270),                 // แผงสตาร์ทเครื่อง (เควส Start Reactor)
            P("beacon", 42.9f, 49.8f), P("beacon", 42.9f, 51.8f),
            P("pressure", 54.0f, 49.3f, w: 2.0f, d: 1.0f), P("pressure", 54.0f, 58.4f, w: 2.0f, d: 1.0f),
            P("barrel", 40.9f, 48.8f, r: 0.32f), P("barrel", 41.7f, 49.0f, r: 0.32f), P("barrel", 41.2f, 49.8f, r: 0.32f),
            P("pipe", 43.0f, 48.3f, h: 5f), P("pipe", 53.0f, 48.3f, h: 5f),
            P("pipe", 40.25f, 56.5f, h: 5f, rot: 90), P("pipe", 55.75f, 56.5f, h: 5f, rot: 90),
            P("cables", 44.5f, 57.0f, rot: 75), P("cables", 52.0f, 50.2f, rot: 10),
            P("console", 48.0f, 59.5f, w: 3f, d: 1f, screen: 0xff8855, rot: 180),

            // ══ ENGINEERING ══
            P("fuelpump", 30.0f, 51.8f, w: 1.0f, d: 0.7f),                               // ปั๊มน้ำมัน (เควส Refuel Generator)
            P("generator", 25.3f, 51.0f, w: 2.0f, d: 1.4f), P("generator", 34.0f, 51.0f, w: 2.0f, d: 1.4f),
            P("breaker", 28.0f, 50.25f, w: 1.6f, d: 0.4f),
            P("transformer", 31.0f, 58.2f, w: 1.6f, d: 1.6f),
            P("workbench", 27.0f, 59.4f, w: 2.4f, d: 0.9f, rot: 180),
            P("cablereel", 28.9f, 58.0f, r: 0.5f),
            P("crate", 34.0f, 56.4f, w: 1.6f, d: 1.6f, h: 0.9f), P("crate", 34.0f, 58.4f, w: 1.4f, d: 1.4f, h: 0.7f, color: 0x5a6b52),
            P("fuelcell", 25.2f, 56.8f, r: 0.6f),
            P("pipe", 24.25f, 52.0f, h: 3f, rot: 90),

            // ══ GREENHOUSE ══
            P("hydroponic", 9.2f, 55.2f, w: 3.6f, d: 0.8f), P("hydroponic", 9.2f, 57.8f, w: 3.6f, d: 0.8f), P("hydroponic", 9.2f, 60.4f, w: 3.6f, d: 0.8f),
            P("hydroponic", 17.6f, 55.2f, w: 3.6f, d: 0.8f), P("hydroponic", 17.6f, 57.8f, w: 3.6f, d: 0.8f), P("hydroponic", 17.6f, 60.4f, w: 3.6f, d: 0.8f),
            P("tank", 14.0f, 58.8f, r: 0.6f, h: 1.4f, color: 0x9fe6a8, glow: 0x7dff9a),
            P("watercooler", 10.9f, 52.7f, r: 0.3f),
            P("counter", 14.0f, 61.5f, w: 3.2f, d: 0.9f, rot: 180),
            P("plant", 6.9f, 53.0f, r: 0.5f), P("plant", 6.9f, 61.0f, r: 0.5f),
            P("plant", 21.1f, 61.0f, r: 0.5f), P("plant", 21.1f, 58.2f, r: 0.5f),
            P("lightstrip", 6.15f, 57.0f, h: 4f, rot: 90),

            // ══ SECURITY ══
            P("cardreader", 16.5f, 38.5f, w: 0.5f, d: 0.4f),                             // เครื่องรูดบัตร (เควส Swipe Card)
            P("monitorwall", 8.3f, 42.5f, w: 0.5f, d: 3.0f, rot: 270), P("officechair", 10.2f, 42.5f, rot: 90),
            P("console", 19.3f, 41.0f, w: 1f, d: 4f, screen: 0x7dff9a),
            P("weaponlocker", 9.2f, 39.3f, w: 1.8f, d: 2.0f),
            P("table", 13.5f, 43.2f, r: 1.2f),
            P("lightstrip", 12.0f, 47.85f, h: 3f),
            P("bin", 18.7f, 47.3f, r: 0.3f),

            // ══ HABITAT ══
            P("locker", 6.45f, 23.2f, w: 0.5f, d: 2.0f, rot: 270), P("locker", 6.45f, 25.2f, w: 0.5f, d: 2.0f, rot: 270),
            P("locker", 6.45f, 27.2f, w: 0.5f, d: 2.0f, rot: 270), P("locker", 6.45f, 29.2f, w: 0.5f, d: 2.0f, rot: 270),
            P("bed", 10.4f, 23.2f, w: 3f, d: 1.6f), P("bed", 14.4f, 23.2f, w: 3f, d: 1.6f),
            P("table", 13.2f, 28.0f, r: 1.2f),
            P("vending", 9.0f, 33.5f, w: 1.2f, d: 0.9f, rot: 180), P("watercooler", 11.0f, 33.6f, r: 0.3f),
            P("plant", 18.9f, 22.9f, r: 0.5f), P("plant", 18.9f, 32.9f, r: 0.5f),
            P("bin", 7.0f, 33.0f, r: 0.3f), P("bin", 18.9f, 30.6f, r: 0.3f),
            P("shelf", 16.5f, 33.55f, w: 2.4f, d: 0.8f, rot: 180),

            // ══ ARMORY ══
            P("turret", 30.5f, 23.2f, r: 0.8f),                                          // ป้อมปืน (เควส Calibrate Turret)
            P("hazardzone", 30.5f, 23.2f, sx: 2.5f, sz: 2.5f),
            P("gunrack", 25.5f, 31.55f, w: 5.0f, d: 0.5f, rot: 180), P("gunrack", 31.2f, 31.55f, w: 5.0f, d: 0.5f, rot: 180),
            P("ammochest", 23.5f, 23.2f, w: 2.2f, d: 1.5f),
            P("ammotower", 32.8f, 23.0f, r: 0.6f),
            P("weaponcrate", 23.0f, 29.8f, w: 1.4f, d: 1.4f), P("weaponcrate", 24.4f, 29.6f, w: 1.0f, d: 1.0f),
            P("weaponlocker", 32.9f, 30.0f, w: 1.8f, d: 2.0f),
            P("accesspanel", 33.55f, 24.5f, w: 0.9f, d: 0.4f, rot: 90),
            P("lightstrip", 28.2f, 22.15f, h: 3f),
        };

        public static MapDef Create() => new MapDef
        {
            id = "aurora", name = "AURORA STATION",
            Rooms = BuildRooms(), Corridors = BuildCorridors(), Spawn = new Vector2(48f, 36f),
            Tasks = BuildTasks(), Vents = BuildVents(), Props = BuildProps(),
            ButtonPos = new Vector2(48f, 30f), MeetingRoomId = "atrium",
            KitVariant = KitVariant,
        };
    }
}
