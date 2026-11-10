using UnityEngine;
using static SkeldMap.MedicalProps;

namespace SkeldMap
{
    // อุปกรณ์ห้อง REACTOR — สร้างด้วยโค้ดโทนเดียวกับแกนเครื่องปฏิกรณ์ส้มและคอนโซลที่มีอยู่ (ส้มเรืองแสง + เหล็กเข้ม + ขาวอมม่วง)
    // กรอบอ้างอิงเหมือน MedicalProps: ด้านหน้า = −z ของตัวเอง · lw ตามแกน x · ld ตามแกน z
    public static class ReactorProps
    {
        const uint Glow = 0xff7a2a, Hazard = 0xf4c430, Dark = 0x2a2d38, Gunmetal = 0x4a557f;

        // ───────────── เท็กซ์เจอร์หน้าจอ (วาดด้วยโค้ด) ─────────────
        static Texture2D gauges, wave;

        static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

        // กราฟแท่งสถานะ 4 คอลัมน์ (อุณหภูมิ/แรงดัน/พลังงาน/สารหล่อเย็น) แบ่งเป็นช่อง เขียว→เหลือง→แดง
        static Texture2D GaugeTexture()
        {
            if (gauges != null) return gauges;
            const int W = 160, H = 80;
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = C(14, 12, 20);
            int[] levels = { 8, 6, 9, 4 };
            for (int col = 0; col < 4; col++)
            {
                int x0 = 10 + col * 38;
                for (int seg = 0; seg < 10; seg++)
                {
                    int y0 = 8 + seg * 7;
                    Color32 on = seg < 5 ? C(70, 230, 120) : seg < 8 ? C(255, 205, 60) : C(255, 70, 50);
                    Color32 c = seg < levels[col] ? on : C(40, 34, 46);
                    for (int y = y0; y < y0 + 5; y++) for (int x = x0; x < x0 + 28; x++) px[y * W + x] = c;
                }
            }
            gauges = Make(px, W, H, "gauges");
            return gauges;
        }

        // คลื่นพลังงานสีส้มบนพื้นเข้ม
        internal static Texture2D WaveTexture()
        {
            if (wave != null) return wave;
            const int W = 96, H = 64;
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = C(14, 12, 20);
            for (int y = 0; y < H; y += 8) for (int x = 0; x < W; x++) px[y * W + x] = C(34, 28, 40);
            int prev = H / 2;
            for (int x = 0; x < W; x++)
            {
                int y = H / 2 + Mathf.RoundToInt(16f * Mathf.Sin(x * 0.28f) * (0.5f + 0.5f * Mathf.Sin(x * 0.07f)));
                for (int yy = Mathf.Min(prev, y); yy <= Mathf.Max(prev, y); yy++) { px[yy * W + x] = C(255, 140, 50); if (yy + 1 < H) px[(yy + 1) * W + x] = C(255, 140, 50); }
                prev = y;
            }
            wave = Make(px, W, H, "wave");
            return wave;
        }

        static Texture2D Make(Color32[] px, int w, int h, string name)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = name };
            t.SetPixels32(px);
            t.Apply(false);
            return t;
        }

        internal static GameObject Display(Transform parent, string name, float w, float h, Texture2D tex, Vector3 pos)
            => PropFactory.Part(parent, name, MeshBuilder.Box(w, h, 0.012f), Mats.Unlit(Color.white, tex: tex), pos, false);

        // ───────────── แผงควบคุมสตาร์ทเครื่อง ─────────────
        public static void Panel(Transform t, float lw, float ld)
        {
            var slate = Std(Gunmetal, 0.55f, 0.3f);
            var navy = Std(Navy, 0.5f, 0.3f);
            var orange = Std(Orange, 0.6f);

            Box(t, "plinth", lw, 0.2f, ld * 0.92f, navy, new Vector3(0, 0.1f, 0));
            Box(t, "stripe", lw * 0.92f, 0.05f, 0.02f, orange, new Vector3(0, 0.2f, -ld * 0.46f - 0.005f));
            Box(t, "body", lw * 0.98f, 0.62f, ld * 0.9f, slate, new Vector3(0, 0.51f, 0));

            // ฉากหลังตั้งตรง มีกราฟแท่งสถานะ
            float zBack = ld * 0.5f - 0.1f;
            Box(t, "riser", lw * 0.98f, 0.85f, 0.14f, slate, new Vector3(0, 1.17f, zBack));
            Box(t, "riserTop", lw, 0.06f, 0.18f, navy, new Vector3(0, 1.62f, zBack));
            Display(t, "gauges", lw * 0.78f, 0.5f, GaugeTexture(), new Vector3(-lw * 0.07f, 1.2f, zBack - 0.075f));

            // ไฟสถานะบนหัวแผง
            uint[] leds = { 0x40ff80, 0x40ff80, 0xffd23f, 0xff8a2a, 0xff3b30 };
            for (int i = 0; i < leds.Length; i++)
                Box(t, "led", 0.09f, 0.05f, 0.04f, Std(leds[i], 0.4f, 0f, leds[i], 2f), new Vector3(-lw * 0.42f + i * 0.14f, 1.68f, zBack - 0.08f));

            // พื้นเอียงของแผง: จอเล็ก 2 จอ + ปุ่มสตาร์ท + คันโยก
            var slope = new GameObject("slope").transform;
            slope.SetParent(t, false);
            slope.localPosition = new Vector3(0, 0.86f, -0.02f);
            slope.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Box(slope, "top", lw * 0.98f, 0.07f, ld * 0.66f, navy, Vector3.zero);
            Display(slope, "scrA", lw * 0.22f, 0.26f, WaveTexture(), new Vector3(-lw * 0.32f, 0.045f, 0)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Display(slope, "scrB", lw * 0.22f, 0.26f, GaugeTexture(), new Vector3(-lw * 0.07f, 0.045f, 0)).transform.localRotation = Quaternion.Euler(90, 0, 0);

            // ปุ่มสตาร์ทใหญ่ (วงกันกดพลาด + ปุ่มแดงเรืองแสง)
            Cyl(slope, "guard", 0.2f, 0.07f, Std(Hazard, 0.5f), new Vector3(lw * 0.2f, 0.07f, 0), 20);
            Cyl(slope, "button", 0.14f, 0.11f, Std(0xff3b30, 0.35f, 0f, 0xff3b30, 1.6f), new Vector3(lw * 0.2f, 0.12f, 0), 20);

            // คันโยก
            Box(slope, "slot", 0.1f, 0.02f, 0.5f, Std(Dark, 0.6f), new Vector3(lw * 0.38f, 0.04f, 0));
            Cyl(slope, "stick", 0.03f, 0.34f, Std(Steel, 0.35f, 0.6f), new Vector3(lw * 0.38f, 0.2f, -0.1f), 8, Quaternion.Euler(-28, 0, 0));
            PropFactory.Part(slope, "knob", MeshBuilder.Sphere(0.075f, 12, 8), Std(Orange, 0.5f, 0f, Orange, 0.6f), new Vector3(lw * 0.38f, 0.35f, -0.16f));
        }

        // ───────────── ถังแรงดันสารหล่อเย็น (แนวนอน) ─────────────
        public static void PressureTank(Transform t, float lw, float ld)
        {
            float r = Mathf.Min(ld * 0.42f, 0.46f);
            float len = lw * 0.78f;
            var shell = Std(Lavender, 0.4f, 0.5f);
            var navy = Std(Navy, 0.5f, 0.3f);

            // ขาตั้ง 2 ข้าง
            foreach (float s in new[] { -1f, 1f })
                Box(t, "cradle", 0.16f, 0.34f, ld * 0.9f, navy, new Vector3(s * len * 0.32f, 0.17f, 0));

            float y = 0.34f + r * 0.78f;
            Cyl(t, "shell", r, len, shell, new Vector3(0, y, 0), 24, Quaternion.Euler(0, 0, 90));
            foreach (float s in new[] { -1f, 1f })
            {
                PropFactory.Part(t, "cap", MeshBuilder.Sphere(r, 20, 12), shell, new Vector3(s * len / 2, y, 0));
                Cyl(t, "ring", r + 0.02f, 0.07f, Std(Orange, 0.5f, 0.2f), new Vector3(s * len * 0.34f, y, 0), 24, Quaternion.Euler(0, 0, 90));
            }
            Cyl(t, "ringMid", r + 0.015f, 0.05f, navy, new Vector3(0, y, 0), 24, Quaternion.Euler(0, 0, 90));

            // วาล์ว + มือหมุน + ท่อบนหลังถัง
            Cyl(t, "valve", 0.07f, 0.16f, Std(Steel, 0.35f, 0.6f), new Vector3(-len * 0.12f, y + r + 0.06f, 0), 12);
            Cyl(t, "wheel", 0.15f, 0.03f, Std(Red, 0.5f), new Vector3(-len * 0.12f, y + r + 0.17f, 0), 16);
            Cyl(t, "pipe", 0.05f, 0.5f, Std(Steel, 0.35f, 0.6f), new Vector3(len * 0.2f, y + r + 0.08f, 0), 10);

            // เกจแรงดันด้านหน้า (−z): จานกลม เรืองเขียว/ส้ม
            Cyl(t, "gaugeRim", 0.13f, 0.05f, navy, new Vector3(len * 0.1f, y + 0.05f, -r - 0.01f), 16, Quaternion.Euler(90, 0, 0));
            Cyl(t, "gauge", 0.1f, 0.03f, Std(0x1b1b24, 0.4f, 0f, 0xffa040, 1.2f), new Vector3(len * 0.1f, y + 0.05f, -r - 0.04f), 16, Quaternion.Euler(90, 0, 0), false);
        }

        // ───────────── เครื่องกำเนิดไฟฟ้า/เทอร์ไบน์ ─────────────
        public static void Generator(Transform t, float lw, float ld)
        {
            var body = Std(Gunmetal, 0.5f, 0.4f);
            var navy = Std(Navy, 0.5f, 0.3f);
            var light = Std(Lavender, 0.5f, 0.2f);

            Box(t, "slab", lw, 0.14f, ld, navy, new Vector3(0, 0.07f, 0));
            Box(t, "slabStripe", lw + 0.02f, 0.05f, 0.03f, Std(Hazard, 0.6f), new Vector3(0, 0.1f, -ld / 2 - 0.005f));

            float drumR = Mathf.Min(ld * 0.34f, 0.5f), drumLen = lw * 0.62f, y = 0.14f + drumR + 0.12f;
            // ฐานรองถัง
            foreach (float s in new[] { -1f, 1f }) Box(t, "mount", 0.2f, 0.28f, ld * 0.7f, navy, new Vector3(s * drumLen * 0.36f, 0.28f, 0));
            Cyl(t, "drum", drumR, drumLen, body, new Vector3(-lw * 0.08f, y, 0), 24, Quaternion.Euler(0, 0, 90));
            // ครีบระบายความร้อน
            for (int i = 0; i < 6; i++)
                Cyl(t, "fin", drumR + 0.07f, 0.045f, light, new Vector3(-lw * 0.08f - drumLen * 0.4f + i * drumLen * 0.16f, y, 0), 24, Quaternion.Euler(0, 0, 90));
            // ห้องเครื่องปลายด้านขวา + หัวจ่ายไฟส้ม
            Box(t, "housing", lw * 0.2f, drumR * 2.1f, ld * 0.82f, light, new Vector3(lw * 0.36f, y, 0));
            Box(t, "housingStripe", lw * 0.2f + 0.02f, 0.06f, ld * 0.84f, Std(Orange, 0.6f), new Vector3(lw * 0.36f, y + drumR * 0.7f, 0));
            Box(t, "indicator", 0.28f, 0.1f, 0.03f, Std(0x1b2a3c, 0.4f, 0f, Cyan, 1.8f), new Vector3(lw * 0.36f, y - drumR * 0.2f, -ld * 0.41f - 0.01f));
            // ปล่องไอเสีย + ท่อ
            Cyl(t, "stack", 0.11f, 0.55f, Std(Steel, 0.35f, 0.6f), new Vector3(-lw * 0.3f, y + drumR + 0.25f, 0), 12);
            Cyl(t, "stackCap", 0.15f, 0.05f, navy, new Vector3(-lw * 0.3f, y + drumR + 0.55f, 0), 12);
            Cyl(t, "pipeL", 0.07f, 0.35f, Std(Steel, 0.35f, 0.6f), new Vector3(-lw / 2 + 0.1f, y, 0), 10, Quaternion.Euler(0, 0, 90));
        }

        // ───────────── ถังสารกัมมันตรังสี ─────────────
        public static void Barrel(Transform t, float r)
        {
            float h = 0.82f;
            Cyl(t, "body", r, h, Std(Hazard, 0.55f, 0.2f), new Vector3(0, h / 2, 0), 18);
            foreach (float y in new[] { 0.2f, 0.62f })
                Cyl(t, "band", r + 0.012f, 0.05f, Std(Dark, 0.5f, 0.3f), new Vector3(0, y, 0), 18);
            Cyl(t, "lid", r * 0.92f, 0.05f, Std(Navy, 0.5f, 0.3f), new Vector3(0, h + 0.02f, 0), 18);
            Cyl(t, "cap", r * 0.3f, 0.06f, Std(Steel, 0.35f, 0.6f), new Vector3(r * 0.35f, h + 0.06f, 0), 10);
            // สัญลักษณ์เตือนรังสีด้านหน้า: ข้าวหลามตัดเรืองเขียว + จุดกลาง
            var mark = Box(t, "mark", 0.16f, 0.16f, 0.012f, Std(0x1b1b24, 0.5f, 0f, 0x40ff80, 1.6f), new Vector3(0, 0.42f, -r - 0.004f));
            mark.transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        // ───────────── วงแหวนเตือนอันตรายบนพื้นรอบแกน (ลายเหลือง-ดำ) ─────────────
        public static void HazardRing(Transform t, float outer)
        {
            float inner = outer - 0.38f;
            const int n = 40;
            var yel = new MeshBuilder();
            var blk = new MeshBuilder();
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                var mb = i % 2 == 0 ? yel : blk;
                int p0 = mb.Vertex(new Vector3(Mathf.Sin(a0) * inner, 0, Mathf.Cos(a0) * inner), Vector3.up, Vector2.zero);
                int p1 = mb.Vertex(new Vector3(Mathf.Sin(a0) * outer, 0, Mathf.Cos(a0) * outer), Vector3.up, Vector2.zero);
                int p2 = mb.Vertex(new Vector3(Mathf.Sin(a1) * outer, 0, Mathf.Cos(a1) * outer), Vector3.up, Vector2.zero);
                int p3 = mb.Vertex(new Vector3(Mathf.Sin(a1) * inner, 0, Mathf.Cos(a1) * inner), Vector3.up, Vector2.zero);
                mb.Quad(p0, p1, p2, p3);
            }
            PropFactory.Part(t, "ringYellow", yel.ToMesh("hazardY"), Std(Hazard, 0.8f, 0f), new Vector3(0, 0.045f, 0), false);
            PropFactory.Part(t, "ringBlack", blk.ToMesh("hazardK"), Std(Dark, 0.8f, 0f), new Vector3(0, 0.045f, 0), false);
        }

        // ───────────── ท่อหล่อเย็นแนวผนัง (ยาวตามแกน x ของตัวเอง) ─────────────
        public static void Pipe(Transform t, float length)
        {
            var steel = Std(Lavender, 0.4f, 0.5f);
            float y = 1.05f;
            Cyl(t, "pipe", 0.09f, length, steel, new Vector3(0, y, 0), 14, Quaternion.Euler(0, 0, 90));
            Cyl(t, "pipe2", 0.07f, length, steel, new Vector3(0, y - 0.22f, 0), 12, Quaternion.Euler(0, 0, 90));
            int segs = Mathf.Max(2, Mathf.RoundToInt(length / 1.3f));
            for (int i = 0; i <= segs; i++)
            {
                float x = -length / 2 + i * length / segs;
                Cyl(t, "flange", 0.13f, 0.06f, Std(Navy, 0.5f, 0.3f), new Vector3(x, y, 0), 14, Quaternion.Euler(0, 0, 90));
                Cyl(t, "flange2", 0.1f, 0.05f, Std(Navy, 0.5f, 0.3f), new Vector3(x, y - 0.22f, 0), 12, Quaternion.Euler(0, 0, 90));
            }
            // แถบเรืองส้ม (สารหล่อเย็นร้อน) กลางท่อ
            Cyl(t, "hot", 0.097f, 0.32f, Std(0x3a2a20, 0.4f, 0f, Glow, 1.4f), new Vector3(0, y, 0), 14, Quaternion.Euler(0, 0, 90), false);
        }

        // ───────────── ไฟเตือนบนคอนโซล ─────────────
        public static void Beacon(Transform t)
        {
            const float baseY = 0.95f; // ความสูงของคอนโซลเดิม
            Cyl(t, "base", 0.11f, 0.08f, Std(Navy, 0.5f, 0.3f), new Vector3(0, baseY + 0.04f, 0), 14);
            var dome = PropFactory.Part(t, "dome", MeshBuilder.Sphere(0.1f, 14, 10), Std(0xff3b30, 0.3f, 0f, 0xff3b30, 2.2f), new Vector3(0, baseY + 0.1f, 0));
            dome.transform.localScale = new Vector3(1f, 1.1f, 1f);
        }
    }
}
