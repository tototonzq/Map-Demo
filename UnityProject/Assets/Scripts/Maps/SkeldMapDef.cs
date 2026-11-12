using UnityEngine;

namespace SkeldMap
{
    // ข้อมูลแมป The Skeld (ย้ายมาจาก MapData เดิม · ค่าทุกอย่างเหมือนเดิม)
    public static class SkeldMapDef
    {
        static RoomDef[] BuildRooms() => new RoomDef[]
        {
            new RoomDef("upperEngine", "UPPER ENGINE", 4, 4, 14, 12, 0x7d8da3, labelX: 12.2f),
            new RoomDef("medbay", "MEDBAY", 22, 4, 10, 10, 0x7fa6a8, labelX: 26, labelZ: 10.4f),
            new RoomDef("cafeteria", "CAFETERIA", 38, 3, 22, 16, 0x93a0b5, labelZ: 5.4f),
            new RoomDef("weapons", "WEAPONS", 68, 4, 14, 12, 0x8a8fa8, labelZ: 9.0f),
            new RoomDef("reactor", "REACTOR", 3, 22, 11, 14, 0xa88a85, labelX: 10.5f, labelZ: 24.6f),
            new RoomDef("security", "SECURITY", 20, 22, 8, 10, 0x7e9a86),
            new RoomDef("o2", "O2", 66, 20, 9, 10, 0x7fa592),
            new RoomDef("navigation", "NAVIGATION", 86, 16, 9, 14, 0x7f92b3),
            new RoomDef("admin", "ADMIN", 56, 33, 12, 11, 0xa39a7c),
            new RoomDef("electrical", "ELECTRICAL", 20, 40, 10, 10, 0xa9a27a),
            new RoomDef("storage", "STORAGE", 36, 42, 16, 16, 0x9a8d78),
            new RoomDef("lowerEngine", "LOWER ENGINE", 4, 42, 14, 14, 0x7d8da3, labelX: 12.2f),
            new RoomDef("comms", "COMMUNICATIONS", 58, 52, 14, 10, 0x8b86ad),
            new RoomDef("shields", "SHIELDS", 78, 42, 14, 12, 0x7aa0b8),
        };

        // ทางเดิน: { x, z, w, d }
        static int[][] BuildCorridors() => new int[][]
        {
            new[] { 18, 8, 4, 3 },   // Upper Engine ↔ MedBay
            new[] { 9, 16, 3, 6 },   // Upper Engine ↔ Reactor
            new[] { 9, 36, 3, 6 },   // Reactor ↔ Lower Engine
            new[] { 14, 26, 6, 3 },  // Reactor ↔ Security
            new[] { 24, 14, 3, 8 },  // MedBay ↔ Security
            new[] { 32, 8, 6, 3 },   // MedBay ↔ Cafeteria
            new[] { 23, 32, 3, 8 },  // Security ↔ Electrical
            new[] { 18, 45, 2, 3 },  // Lower Engine ↔ Electrical
            new[] { 30, 45, 6, 3 },  // Electrical ↔ Storage
            new[] { 44, 19, 3, 23 }, // Cafeteria ↔ Storage
            new[] { 60, 8, 8, 3 },   // Cafeteria ↔ Weapons
            new[] { 56, 19, 3, 14 }, // Cafeteria ↔ Admin
            new[] { 82, 9, 10, 3 },  // Weapons → Navigation (แนวนอน)
            new[] { 89, 12, 3, 4 },  // Weapons → Navigation (แนวตั้ง)
            new[] { 70, 16, 3, 4 },  // Weapons ↔ O2
            new[] { 66, 30, 3, 3 },  // O2 ↔ Admin
            new[] { 52, 42, 4, 3 },  // Storage ↔ Admin
            new[] { 52, 54, 6, 3 },  // Storage ↔ Communications
            new[] { 72, 52, 6, 3 },  // Communications ↔ Shields
            new[] { 89, 30, 3, 12 }, // Navigation ↔ Shields
        };

        static readonly Vector2 SpawnPt = new Vector2(49, 11);

        // จุดทำเควส (1 จุด/ห้อง) — x, z ต้องอยู่บนพื้นที่เดินได้และไม่ชนของตกแต่ง
        static TaskDef[] BuildTasks() => new TaskDef[]
        {
            new TaskDef("alignUpper", "upperEngine", "Align Engine Output", 10.8f, 10),     // หน้าแท่นปรับเครื่องยนต์ (aligner)
            new TaskDef("scan", "medbay", "Submit Scan", 28, 12.5f, 0.3f),                  // ยืนบนแท่นสแกน
            new TaskDef("download", "cafeteria", "Download Data", 40.3f, 4.8f),              // หน้าตู้ดาวน์โหลด (datakiosk)
            new TaskDef("asteroids", "weapons", "Clear Asteroids", 76, 8.8f),                // หน้าป้อมปืน (turret)
            new TaskDef("reactor", "reactor", "Start Reactor", 12.2f, 31.6f),                // หน้าแผงควบคุมเตาปฏิกรณ์
            new TaskDef("swipe", "security", "Swipe Card", 23, 26.7f),                       // หน้าเครื่องรูดบัตร
            new TaskDef("filter", "o2", "Clean O2 Filter", 73.2f, 26.6f),                    // หน้าตัวกรองอากาศ
            new TaskDef("course", "navigation", "Chart Course", 87.8f, 22),                  // หน้าจอแผนที่ดาว (starboard)
            new TaskDef("upload", "admin", "Upload Data", 63.5f, 42.2f),                     // หน้าตู้อัปโหลด (datakiosk)
            new TaskDef("wiring", "electrical", "Fix Wiring", 27.9f, 41.8f),                 // หน้าแผงสายไฟ (wirepanel)
            new TaskDef("fuel", "storage", "Fuel Engines", 41.9f, 44.8f),                    // หน้าปั๊มน้ำมัน
            new TaskDef("alignLower", "lowerEngine", "Align Engine Output", 10.8f, 49),     // หน้าแท่นปรับเครื่องยนต์ (aligner)
            new TaskDef("comms", "comms", "Download Data", 65.5f, 59.2f),                    // หน้าคอนโซลสื่อสาร
            new TaskDef("shields", "shields", "Prime Shields", 79.8f, 48.3f),                // หน้าแท่นควบคุมโล่ (shieldpanel)
        };

        // ช่องระบายอากาศ (ตกแต่ง)
        static Vector2[] BuildVents() => new Vector2[]
        {
            new Vector2(40.5f, 16.5f), new Vector2(24.8f, 12.4f), new Vector2(24.6f, 30.8f),
            new Vector2(23.5f, 47.5f), new Vector2(9.4f, 33.4f), new Vector2(15.5f, 13.5f),
            new Vector2(78.5f, 13.0f), new Vector2(87.2f, 28.4f), new Vector2(88.8f, 50.0f),
            new Vector2(57.5f, 36), new Vector2(40.5f, 49.5f),
        };

        // ของตกแต่ง/สิ่งกีดขวาง
        static PropDef[] BuildProps() => new PropDef[]
        {
            // Upper Engine
            P("engine", 6, 7, r: 1.3f),
            P("engine", 6, 13, r: 1.3f),
            P("console", 14.5f, 4.5f, w: 3, d: 1, screen: 0x55e0ff),
            // MedBay
            P("bed", 24.5f, 5.5f, w: 3, d: 1.6f),
            P("bed", 28.5f, 5.5f, w: 3, d: 1.6f),
            P("tank", 31, 12, r: 0.9f, h: 1.9f, color: 0x9fdcd6, glow: 0x2fe0c0),
            // MedBay — อุปกรณ์โรงพยาบาล (วางเลี่ยงประตู 3 บานและจุดเควส Submit Scan ที่ (27,10))
            P("iv", 26.5f, 5.5f, r: 0.3f),                                       // เสาน้ำเกลือ ระหว่างเตียง 2 เตียง
            P("monitor", 31.1f, 5.6f, w: 0.8f, d: 0.8f),                         // จอมอนิเตอร์สัญญาณชีพ ข้างเตียงขวา
            P("cabinet", 22.45f, 6.8f, w: 0.5f, d: 1.4f, rot: 270),              // ตู้ยาชิดผนังตะวันตก หันไปทางห้อง (+x)
            P("crutch", 22.5f, 7.75f),                                           // ไม้ค้ำยันพิงผนัง (ตกแต่ง ไม่ชน)
            P("medcart", 22.9f, 12.4f, w: 0.6f, d: 1.2f, rot: 270),              // รถเข็นยา + เครื่องกระตุกหัวใจ มุมตะวันตกเฉียงใต้
            P("scanner", 28.0f, 12.5f, w: 3.0f, d: 1.4f),                        // แท่นสแกนวินิจฉัย: ยืนบนแท่นได้ (เฉพาะเสาสองข้างเป็นของตัน — ดู MapGrid.MarkProp) คู่กับเควส Submit Scan
            P("wheelchair", 31.0f, 7.5f, w: 1f, d: 1f, rot: 90),                 // รถเข็นผู้ป่วย ชิดผนังตะวันออก
            // Cafeteria
            P("table", 43, 6, r: 1.5f),
            P("table", 54, 6, r: 1.5f),
            P("table", 49, 14, r: 1.5f, button: true),
            // Weapons
            P("turret", 76, 6.5f, r: 1.4f),
            P("console", 74.5f, 13.5f, w: 3, d: 1, screen: 0xff6a5a),
            // Reactor
            P("reactor", 6.5f, 29, r: 2.2f),
            P("console", 5, 23.5f, w: 3, d: 1, screen: 0xff8855),
            P("console", 5, 34.5f, w: 3, d: 1, screen: 0xff8855),
            // Reactor — อุปกรณ์เพิ่มเติม (วางเลี่ยงประตู 3 บาน: เหนือ x9-12, ใต้ x9-12, ตะวันออก z26-29 และจุดเควส (11,29))
            P("hazardring", 6.5f, 29, h: 2.9f),                                   // วงแหวนเตือนอันตรายบนพื้นรอบแกน (ไม่ชน)
            P("reactorpanel", 13.35f, 31.6f, w: 0.9f, d: 2.2f, rot: 90),          // แผงสตาร์ทเครื่อง ชิดผนังตะวันออก หันไป −x
            P("pressure", 12.9f, 23.8f, w: 2.0f, d: 1.0f),                        // ถังแรงดันสารหล่อเย็น มุมตะวันออกเฉียงเหนือ
            P("generator", 12.9f, 34.6f, w: 2.0f, d: 1.5f),                       // เครื่องกำเนิดไฟฟ้า มุมตะวันออกเฉียงใต้
            P("barrel", 3.8f, 32.6f, r: 0.32f),                                   // ถังสารกัมมันตรังสี
            P("barrel", 4.5f, 33.2f, r: 0.32f),
            P("barrel", 3.7f, 33.5f, r: 0.32f),
            P("pipe", 3.25f, 29.8f, h: 10.6f, rot: 90),                           // ท่อหล่อเย็นแนวผนังตะวันตก
            P("pipe", 6.0f, 22.3f, h: 5.6f),                                      // ท่อหล่อเย็นแนวผนังเหนือ
            P("cables", 10.0f, 30.4f, rot: 75),                                   // สายเคเบิลบนพื้น (โมเดลจากชุด Space Kit)
            P("cables", 11.6f, 32.4f, rot: 10),
            P("beacon", 3.9f, 23.5f),                                             // ไฟเตือนบนคอนโซล
            P("beacon", 3.9f, 34.5f),
            // Security
            P("console", 27.5f, 27, w: 1, d: 6, screen: 0x7dff9a),
            // O2
            P("tank", 67.4f, 22.4f, r: 0.9f, h: 2.2f, color: 0x7fd6a0, glow: 0x1fbf6a),
            P("plant", 73.5f, 22.5f, r: 0.5f),
            P("plant", 68, 26.3f, r: 0.5f),
            // Navigation
            P("console", 93.7f, 23, w: 1.4f, d: 8, screen: 0x66aaff),
            P("crate", 91.5f, 23, w: 1, d: 1, h: 0.6f, color: 0x3d4a66),
            // Admin
            P("console", 62, 38.5f, w: 5, d: 2.6f, screen: 0xffd23f),
            P("console", 67.5f, 36.5f, w: 1, d: 3, screen: 0xffd23f),
            // Electrical
            P("console", 29.5f, 43, w: 1, d: 4, screen: 0xffd23f),
            P("console", 25, 49.5f, w: 6, d: 1, screen: 0xffd23f),
            // อุปกรณ์ประจำเควส (โมเดลใหม่ใน TaskProps.cs · จุดเหลืองใน Tasks อยู่หน้าเครื่องเหล่านี้)
            P("aligner", 9.2f, 10.0f, w: 0.6f, d: 1.8f, rot: 270),                       // Upper Engine: แท่นปรับเครื่องยนต์
            P("aligner", 9.2f, 49.0f, w: 0.6f, d: 1.8f, rot: 270),                       // Lower Engine
            P("datakiosk", 40.3f, 3.75f, w: 1.2f, d: 0.8f),                               // Cafeteria: ตู้ดาวน์โหลด (ติดผนังบน)
            P("datakiosk", 63.5f, 43.3f, w: 1.2f, d: 0.8f, rot: 180, button: true),       // Admin: ตู้อัปโหลด (ติดผนังล่าง · ลูกศรขึ้น)
            P("shieldpanel", 78.45f, 48.3f, w: 0.5f, d: 1.8f, rot: 270),                  // Shields: แท่นควบคุมโล่ (ติดผนังตะวันตก)
            // Storage
            P("crate", 38.5f, 43.5f, w: 2, d: 2),
            P("crate", 49.5f, 48, w: 2, d: 2),
            P("crate", 38.5f, 52, w: 2.4f, d: 2.4f),
            P("crate", 42, 55, w: 2, d: 2),
            P("crate", 46, 52, w: 2.6f, d: 2.6f),
            P("crate", 48.5f, 56, w: 1.6f, d: 1.6f),
            // Lower Engine
            P("engine", 6, 45, r: 1.3f),
            P("engine", 6, 53, r: 1.3f),
            P("console", 14.5f, 54.5f, w: 3, d: 1, screen: 0x55e0ff, rot: 180),
            // Communications
            P("console", 65, 60.5f, w: 8, d: 1, screen: 0xc08cff, rot: 180),   // หันจอเข้าห้อง (เควส Download Data)
            P("dish", 63, 55, r: 1.0f),
            P("console", 70.5f, 56, w: 1, d: 3, screen: 0xc08cff),
            // Shields
            P("tank", 82, 46, r: 1, h: 2.2f, color: 0x8fe6ff, glow: 0x39d0ff),
            P("tank", 87, 46, r: 1, h: 2.2f, color: 0x8fe6ff, glow: 0x39d0ff),
            P("tank", 84.5f, 50.5f, r: 1, h: 2.2f, color: 0x8fe6ff, glow: 0x39d0ff),
            P("console", 79.5f, 44, w: 1, d: 3, screen: 0x39d0ff),

            // ══ อุปกรณ์ประจำห้อง (ชุดที่ 3) — วางเลี่ยงประตูทุกบานและจุดเควส · rot: 0 หันลงล่างจอ, 90 หันซ้าย, 180 หันขึ้น, 270 หันขวา ══
            // Upper Engine
            P("hazardring", 6, 7, h: 1.9f), P("hazardring", 6, 13, h: 1.9f),
            P("pipe", 4.25f, 10f, h: 9f, rot: 90), P("pipe", 10.5f, 4.3f, h: 5f),
            P("workbench", 14.8f, 15.45f, w: 2.4f, d: 0.9f, rot: 180),
            P("fuelcell", 16.9f, 6.0f, r: 0.6f), P("fuelcell", 17.0f, 14.6f, r: 0.6f),
            // Lower Engine
            P("hazardring", 6, 45, h: 1.9f), P("hazardring", 6, 53, h: 1.9f),
            P("pipe", 4.25f, 49f, h: 3.6f, rot: 90),
            P("workbench", 7.2f, 42.6f, w: 2.2f, d: 0.9f),
            P("generator", 13.4f, 43.2f, w: 2.0f, d: 1.4f),
            P("fuelcell", 17.0f, 52.8f, r: 0.6f),
            // Cafeteria
            P("vending", 47.0f, 3.75f, w: 1.2f, d: 0.9f), P("vending", 51.0f, 3.75f, w: 1.2f, d: 0.9f),
            P("counter", 59.3f, 14.6f, w: 0.9f, d: 5.0f, rot: 90),
            P("watercooler", 59.0f, 4.5f, r: 0.3f), P("bin", 39.0f, 17.9f, r: 0.3f), P("plant", 38.8f, 4.0f, r: 0.5f),
            // Weapons — ตกแต่งเป็น "Armory" (คลังอาวุธ) ตามภาพอ้างอิง · คงป้อมปืน คอนโซล และจุดเควส (72,10) ไว้
            P("hazardzone", 75.0f, 10.4f, sx: 9.6f, sz: 7.2f, button: true),               // ขีดประเหลืองบนพื้น (ไม่ชน)
            P("gunrack", 75.9f, 4.55f, w: 5.0f, d: 0.5f),                                // แผงแขวนปืน + ไฟส้ม กลางผนังเหนือ
            P("ammochest", 69.7f, 5.2f, w: 2.2f, d: 1.5f),                               // หีบกระสุนใหญ่ มุมซ้ายบน
            P("ammolocker", 72.2f, 4.85f, w: 1.2f, d: 0.9f),                             // ตู้เก็บกระสุนทรงสูง
            P("ammotower", 79.0f, 5.0f, r: 0.5f),                                        // แท่นกระสุนทรงกระบอก
            P("weaponlocker", 80.9f, 5.6f, w: 1.8f, d: 2.0f),                            // ตู้เก็บอาวุธใหญ่ มุมขวาบน
            P("weaponcrate", 69.0f, 14.6f, w: 1.5f, d: 1.5f),                            // ลังอาวุธซ้อน มุมซ้ายล่าง
            P("weaponcrate", 69.0f, 12.7f, w: 1.1f, d: 1.1f),
            P("accesspanel", 81.55f, 15.4f, w: 0.9f, d: 0.4f, rot: 90),                  // แผงควบคุมประตูสีฟ้า มุมขวาล่าง
            P("lightstrip", 81.85f, 13.0f, h: 2.4f, rot: 90),                            // แถบไฟสีฟ้าชิดผนังตะวันออก (ไม่ชน)
            // Security
            P("monitorwall", 21.8f, 22.5f, w: 3.0f, d: 0.5f), P("officechair", 22.0f, 24.2f, rot: 180),
            P("locker", 20.4f, 30.9f, w: 0.5f, d: 2.2f, rot: 270), P("cardreader", 23.0f, 25.4f, w: 0.5f, d: 0.4f),
            // O2
            P("airfilter", 74.45f, 26.6f, w: 0.9f, d: 2.2f, rot: 90),
            P("hydroponic", 72.4f, 29.3f, w: 3.6f, d: 0.8f, rot: 180),
            P("pipe", 66.25f, 26.0f, h: 6f, rot: 90),
            // Navigation
            P("holotable", 88.8f, 25.4f, r: 1.0f), P("pilotchair", 91.9f, 26.2f, rot: 270),
            P("starboard", 86.25f, 22f, w: 0.4f, d: 4.0f, rot: 270),
            // Admin
            P("filecabinet", 56.45f, 38.5f, w: 0.6f, d: 2.4f, rot: 270), P("printer", 59.2f, 43.3f, w: 1.0f, d: 0.8f, rot: 180),
            P("serverrack", 67.45f, 40.4f, w: 0.9f, d: 1.0f, rot: 90), P("serverrack", 67.45f, 41.6f, w: 0.9f, d: 1.0f, rot: 90),
            P("adminboard", 62.0f, 33.5f, w: 4.0f, d: 0.4f),
            // Electrical
            P("breaker", 21.8f, 40.5f, w: 1.6f, d: 0.4f), P("wirepanel", 28.2f, 40.45f, w: 1.6f, d: 0.4f),     // แผงสายไฟ (เควส Fix Wiring)
            P("transformer", 21.6f, 43.0f, w: 1.6f, d: 1.6f), P("cablereel", 20.9f, 49.3f, r: 0.55f),
            // Storage
            P("shelf", 37.6f, 57.5f, w: 2.4f, d: 0.8f, rot: 180), P("shelf", 40.6f, 57.5f, w: 2.4f, d: 0.8f, rot: 180), P("shelf", 47.2f, 57.5f, w: 2.4f, d: 0.8f, rot: 180),
            P("fuelpump", 41.9f, 43.6f, w: 1.0f, d: 0.7f),
            P("barrel", 51.0f, 46.2f, r: 0.32f), P("barrel", 51.5f, 47.0f, r: 0.32f), P("barrel", 51.1f, 47.7f, r: 0.32f),
            P("cargocart", 37.2f, 49.2f, w: 1.4f, d: 1.0f),
            // Communications
            P("serverrack", 60.5f, 52.7f, w: 1.0f, d: 0.9f), P("serverrack", 61.7f, 52.7f, w: 1.0f, d: 0.9f), P("serverrack", 62.9f, 52.7f, w: 1.0f, d: 0.9f),
            P("antenna", 71.0f, 61.0f, r: 0.35f),
            P("commsdesk", 59.4f, 60.6f, w: 0.8f, d: 1.4f, rot: 270),
            P("signalboard", 71.75f, 59.2f, w: 0.4f, d: 2.4f, rot: 90),
            // Shields
            P("shieldgen", 91.0f, 48.8f, r: 0.6f), P("shieldgen", 91.0f, 52.4f, r: 0.6f),
            P("capacitor", 82.8f, 53.2f, w: 2.2f, d: 0.7f, rot: 180), P("capacitor", 86.5f, 53.2f, w: 2.2f, d: 0.7f, rot: 180),
            P("holoemitter", 84.5f, 44.6f, r: 0.5f),
        };

        static PropDef P(string kind, float x, float z, float? r = null, float? w = null, float? d = null,
                         float? h = null, uint? color = null, uint? screen = null, uint? glow = null,
                         bool button = false, float? rot = null, float? sx = null, float? sz = null)
            => MapDef.P(kind, x, z, r, w, d, h, color, screen, glow, button, rot, sx, sz);

        static int KitVariant(string roomId)
        {
            switch (roomId)
            {
                case "medbay": case "security": case "navigation": case "shields": case "comms": return 1;
                case "o2": case "electrical": return 2;
                default: return 0;
            }
        }

        public static MapDef Create() => new MapDef
        {
            id = "skeld", name = "THE SKELD",
            Rooms = BuildRooms(), Corridors = BuildCorridors(), Spawn = SpawnPt,
            Tasks = BuildTasks(), Vents = BuildVents(), Props = BuildProps(),
            ButtonPos = new Vector2(49f, 14f), MeetingRoomId = "cafeteria",
            KitVariant = KitVariant,
        };
    }
}
