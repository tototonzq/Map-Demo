using UnityEngine;
using static SkeldMap.MedicalProps;

namespace SkeldMap
{
    // อุปกรณ์ประจำภารกิจที่แมปเดิมไม่มี · สร้างด้วยโค้ดโทนเดียวกับ Medical/Facility (กล่อง/ทรงกระบอกในพาเลตต์ Kenney)
    // หน้าตาในโมเดลตรงกับมินิเกมที่เล่น (TaskGames.cs): แถบเลื่อนปรับเครื่องยนต์ · ลูกศรดาวน์โหลด/อัปโหลด · สายไฟ 4 สี · ไฟโล่ 7 ช่อง
    // กรอบอ้างอิงเหมือนชิ้นอื่น: ด้านหน้า = −z ของตัวเอง · lw ตามแกน x · ld ตามแกน z (PropFactory หมุนตาม rot และสลับ lw/ld ให้)
    public static class TaskProps
    {
        static Material Navym => Std(Navy, 0.5f, 0.3f);
        static Material Lavm => Std(Lavender, 0.6f, 0.1f);
        static Material Emit(uint color, float i = 1.8f) => Std(0x161a26, 0.4f, 0f, color, i);

        // หัวแผงเอียงถอยหลัง (แบบเดียวกับ CardReader) — ลูกทั้งหมดวางหน้าแผงที่ z ติดลบ
        static Transform Head(Transform t, Vector3 pos, float tilt)
        {
            var head = new GameObject("head").transform;
            head.SetParent(t, false);
            head.localPosition = pos;
            head.localRotation = Quaternion.Euler(-tilt, 0, 0);
            return head;
        }

        // ════════════════ Align Engine Output ════════════════
        // แท่นปรับเครื่องยนต์: แถบเลื่อน 2 เส้น + เส้นเป้าหมายสีเขียว + แถวไฟสถานะ (เหมือนมินิเกม)
        public static void Aligner(Transform t, float lw, float ld)
        {
            Box(t, "base", lw, 0.78f, ld, Navym, new Vector3(0, 0.39f, 0));
            Box(t, "plinth", lw + 0.06f, 0.08f, ld + 0.06f, Std(0x2a2d38, 0.6f), new Vector3(0, 0.04f, 0));
            Box(t, "stripe", lw + 0.01f, 0.06f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 0.5f, -ld / 2 - 0.005f));

            var h = Head(t, new Vector3(0, 1.05f, 0), 28f);
            Box(h, "panel", lw, 0.62f, 0.12f, Lavm, Vector3.zero);
            Box(h, "face", lw * 0.9f, 0.5f, 0.02f, Std(0x0c1424, 0.5f), new Vector3(0, 0, -0.07f));
            float tx = lw * 0.28f;
            foreach (float s in new[] { -1f, 1f })
            {
                Box(h, "track", 0.045f, 0.42f, 0.015f, Emit(Cyan, 1.4f), new Vector3(s * tx, 0, -0.085f));
                Box(h, "knob", 0.17f, 0.07f, 0.035f, Std(Orange, 0.5f, 0.1f, Orange, 0.8f), new Vector3(s * tx, s * 0.07f, -0.1f));
            }
            Box(h, "target", lw * 0.74f, 0.012f, 0.012f, Emit(0x4cff8a, 2.2f), new Vector3(0, 0, -0.092f));
            // แถวไฟสถานะกลางแผง
            for (int i = 0; i < 5; i++)
                Box(h, "lamp", 0.07f, 0.07f, 0.02f, Emit(i < 3 ? 0x4cff8au : 0x55e0ffu, 1.8f), new Vector3((i - 2) * 0.11f, 0.16f, -0.09f));
            Box(h, "lampOff", 0.07f, 0.07f, 0.02f, Emit(0xff4a4a, 1.6f), new Vector3(0, -0.17f, -0.09f));
        }

        // ════════════════ Download / Upload Data ════════════════
        // ตู้ข้อมูล: จอเอียงมีลูกศรลง (ดาวน์โหลด) หรือขึ้น (อัปโหลด) · ช่องเสียบแท็บเล็ต + แท็บเล็ตวางในช่อง
        public static void DataKiosk(Transform t, float lw, float ld, bool upload)
        {
            uint col = upload ? 0xffb347 : Cyan;
            Box(t, "base", lw * 0.92f, 0.1f, ld * 0.92f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw * 0.84f, 0.92f, ld * 0.78f, Std(Steel, 0.45f, 0.5f), new Vector3(0, 0.56f, 0));
            Box(t, "stripe", lw * 0.85f, 0.06f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 0.2f, -ld * 0.39f - 0.005f));

            var h = Head(t, new Vector3(0, 1.22f, 0.0f), 24f);
            Box(h, "panel", lw * 0.9f, 0.62f, 0.14f, Navym, Vector3.zero);
            Box(h, "screen", lw * 0.78f, 0.5f, 0.02f, Std(0x0a1220, 0.4f), new Vector3(0, 0, -0.08f));
            // ลูกศร: แกน + หัว (V สองแท่งเอียง 45°)
            float dir = upload ? 1f : -1f;
            Box(h, "shaft", 0.07f, 0.24f, 0.015f, Emit(col, 2.2f), new Vector3(0, -dir * -0.0f, -0.095f));
            Box(h, "headL", 0.15f, 0.06f, 0.015f, Emit(col, 2.2f), new Vector3(-0.055f, dir * 0.13f, -0.095f), dir * 45f);
            Box(h, "headR", 0.15f, 0.06f, 0.015f, Emit(col, 2.2f), new Vector3(0.055f, dir * 0.13f, -0.095f), -dir * 45f);
            Box(h, "bar", lw * 0.5f, 0.045f, 0.015f, Emit(col, 1.4f), new Vector3(0, -dir * 0.2f, -0.095f));

            // ช่องเสียบ + แท็บเล็ตวางพิง
            Box(t, "dock", lw * 0.6f, 0.05f, 0.18f, Std(0x2a2d38, 0.5f), new Vector3(0, 0.84f, -ld * 0.42f));
            Box(t, "tablet", 0.3f, 0.03f, 0.22f, Std(White, 0.5f, 0.1f), new Vector3(lw * 0.2f, 0.88f, -ld * 0.42f)).transform.localRotation = Quaternion.Euler(-14, 0, 0);
            Box(t, "tabletScreen", 0.26f, 0.012f, 0.18f, Emit(col, 1.3f), new Vector3(lw * 0.2f, 0.9f, -ld * 0.42f)).transform.localRotation = Quaternion.Euler(-14, 0, 0);
            Box(t, "led", 0.06f, 0.06f, 0.02f, Emit(0x4cff8a, 2f), new Vector3(-lw * 0.3f, 0.84f, -ld * 0.4f));
        }

        // ════════════════ Fix Wiring ════════════════
        // แผงสายไฟติดผนัง: ฝาเปิด + ขั้วต่อซ้าย/ขวา + สาย 4 สี (แดง ฟ้า เหลือง ชมพู) พาดไขว้กัน
        public static void WirePanel(Transform t, float lw, float ld)
        {
            float bh = 1.25f;
            Box(t, "body", lw, bh, ld, Lavm, new Vector3(0, bh / 2, 0));
            Box(t, "capTop", lw + 0.04f, 0.07f, ld + 0.03f, Std(Orange, 0.6f), new Vector3(0, bh + 0.035f, 0));
            Box(t, "recess", lw * 0.86f, 0.92f, 0.03f, Std(0x161a26, 0.6f), new Vector3(0, 0.66f, -ld / 2 - 0.012f));
            Box(t, "plinth", lw + 0.04f, 0.06f, ld + 0.04f, Navym, new Vector3(0, 0.03f, 0));

            uint[] cols = { 0xff4040, 0x4d80ff, 0xffd633, 0xff4de6 };
            int[] to = { 2, 0, 3, 1 };               // สายซ้ายแต่ละเส้นไปจบขวาช่องไหน (ไขว้)
            float lx = -lw * 0.36f, rx = lw * 0.36f, y0 = 0.9f, dy = 0.17f;
            float zFace = -ld / 2 - 0.03f;
            for (int i = 0; i < 4; i++)
            {
                float yl = y0 - i * dy, yr = y0 - to[i] * dy;
                Box(t, "plugL", 0.12f, 0.1f, 0.04f, Std(cols[i], 0.45f, 0.1f), new Vector3(lx, yl, zFace));
                Box(t, "plugR", 0.12f, 0.1f, 0.04f, Std(cols[i], 0.45f, 0.1f), new Vector3(rx, yr, zFace));
                // สายตรงเส้นเดียวเอียงตามผลต่างความสูง (ซ้อนเลเยอร์ z ห่างกันกัน z-fight ตอนเส้นไขว้)
                float dx = rx - lx - 0.12f, dyy = yr - yl;
                float len = Mathf.Sqrt(dx * dx + dyy * dyy);
                float ang = Mathf.Atan2(dyy, dx) * Mathf.Rad2Deg;
                Box(t, "wire", len, 0.035f, 0.025f, Std(cols[i], 0.5f), new Vector3((lx + rx) / 2f, (yl + yr) / 2f, zFace - 0.03f - i * 0.012f), ang);
            }
            Box(t, "lamp", 0.07f, 0.07f, 0.03f, Emit(0xff4a4a, 1.8f), new Vector3(lw * 0.38f, bh - 0.08f, -ld / 2 - 0.02f));
            Box(t, "warn", 0.3f, 0.12f, 0.01f, Std(Hazard(), 0.6f), new Vector3(-lw * 0.3f, bh - 0.1f, -ld / 2 - 0.012f));
        }

        static uint Hazard() => 0xf4c430;

        // ════════════════ Prime Shields ════════════════
        // แท่นควบคุมโล่: ไฟกลม 7 ดวงเรียงรังผึ้ง (กลาง + วงนอก 6) บางดวงดับสีแดง + แท่งมิเตอร์สองข้าง
        public static void ShieldPanel(Transform t, float lw, float ld)
        {
            Box(t, "base", lw, 0.7f, ld, Navym, new Vector3(0, 0.35f, 0));
            Box(t, "plinth", lw + 0.06f, 0.07f, ld + 0.06f, Std(0x2a2d38, 0.6f), new Vector3(0, 0.035f, 0));
            Box(t, "stripe", lw + 0.01f, 0.06f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 0.45f, -ld / 2 - 0.005f));

            var h = Head(t, new Vector3(0, 1.05f, 0), 24f);
            Box(h, "panel", lw, 0.82f, 0.12f, Lavm, Vector3.zero);
            Box(h, "face", lw * 0.92f, 0.7f, 0.02f, Std(0x0c1424, 0.5f), new Vector3(0, 0, -0.07f));

            bool[] on = { true, true, false, true, true, false, true };   // 0 = กลาง · 1..6 = วงนอก
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = Vector2.zero;
                if (i > 0) { float a = (90f + (i - 1) * 60f) * Mathf.Deg2Rad; p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.2f; }
                var disc = PropFactory.Part(h, "cell", MeshBuilder.Cylinder(0.082f, 0.082f, 0.02f, 24), on[i] ? Emit(Cyan, 2.2f) : Emit(0xaa2030, 1.2f),
                                            new Vector3(p.x, p.y, -0.09f), false, Quaternion.Euler(90, 0, 0));
                PropFactory.Part(h, "cellRing", MeshBuilder.Cylinder(0.1f, 0.1f, 0.012f, 24), Std(0x2a2d38, 0.5f),
                                 new Vector3(p.x, p.y, -0.085f), false, Quaternion.Euler(90, 0, 0));
            }
            // มิเตอร์โล่สองข้าง
            foreach (float s in new[] { -1f, 1f })
            {
                Box(h, "meterBack", 0.09f, 0.56f, 0.015f, Std(0x161a26, 0.5f), new Vector3(s * lw * 0.38f, 0, -0.085f));
                Box(h, "meter", 0.06f, 0.38f, 0.015f, Emit(Cyan, 1.8f), new Vector3(s * lw * 0.38f, -0.09f, -0.095f));
            }
        }
    }
}
