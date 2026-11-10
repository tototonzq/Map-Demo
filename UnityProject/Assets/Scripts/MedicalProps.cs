using UnityEngine;

namespace SkeldMap
{
    // อุปกรณ์โรงพยาบาลสำหรับห้อง MEDBAY — สร้างด้วยโค้ดในสไตล์เดียวกับพร็อพเดิม (กล่อง/ทรงกระบอกโทนสีของชุด Kenney)
    //
    // กรอบอ้างอิงของทุกชิ้น: ด้านหน้า = −z ของตัวเอง (หันเข้าหากล้องเริ่มต้นเมื่อ rot = 0) · ความกว้าง lw ตามแกน x · ความลึก ld ตามแกน z
    // PropFactory จะหมุนตาม rot ให้ (rot 90 = หันไปทาง −x · rot 270 = หันไปทาง +x · rot 180 = หันขึ้นบนจอ) และสลับ lw/ld ให้อัตโนมัติ
    public static class MedicalProps
    {
        // พาเลตต์ที่ยืมโทนจากชุด Kenney: ขาวอมม่วง / น้ำเงินเทา / ส้ม / แดงกากบาท / ฟ้าเรืองแสง
        internal const uint White = 0xf2f0ff, Lavender = 0xcfcaf0, Slate = 0x6b78b8, Navy = 0x3a4468, Orange = 0xf4a53a;
        internal const uint Red = 0xe0473f, Cyan = 0x55e0ff, Steel = 0xaab2c8;

        internal static Material Std(uint c, float rough = 0.6f, float metal = 0.1f, uint em = 0, float emI = 1f, float op = 1f)
            => PropFactory.Std(c, rough, metal, em, emI, op);

        internal static GameObject Box(Transform p, string n, float w, float h, float d, Material m, Vector3 pos, float rotZ = 0f)
            => PropFactory.Part(p, n, MeshBuilder.Box(w, h, d), m, pos, true, Quaternion.Euler(0, 0, rotZ));

        internal static GameObject Cyl(Transform p, string n, float r, float h, Material m, Vector3 pos, int seg = 16, Quaternion? rot = null, bool shadows = true)
            => PropFactory.Part(p, n, MeshBuilder.Cylinder(r, r, h, seg), m, pos, shadows, rot);

        // ล้อเล็ก ๆ ใต้เครื่อง
        internal static void Caster(Transform p, float x, float z, float r = 0.07f)
            => PropFactory.Part(p, "caster", MeshBuilder.Sphere(r, 10, 8), Std(Navy, 0.5f), new Vector3(x, r, z));

        // ───────────── เตียงโรงพยาบาล ─────────────
        // หัวเตียงอยู่ด้าน −x (เหมือนเตียงเดิม) · พนักหัวยกขึ้น · ราวข้าง · ผ้าห่มฟ้า · ล้อ 4 ล้อ
        public static void Bed(Transform t, float w, float d)
        {
            var frame = Std(Navy, 0.5f, 0.3f);
            var mattress = Std(Lavender, 0.7f, 0f);
            var white = Std(White, 0.7f, 0f);

            Box(t, "chassis", w * 0.9f, 0.1f, d * 0.7f, frame, new Vector3(0, 0.3f, 0));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    Cyl(t, "wheel", 0.1f, 0.09f, Std(0x2b3040, 0.5f), new Vector3(sx * w * 0.4f, 0.1f, sz * d * 0.34f), 14,
                        Quaternion.Euler(90, 0, 0));
                    Box(t, "leg", 0.07f, 0.2f, 0.07f, Std(Steel, 0.4f, 0.5f), new Vector3(sx * w * 0.4f, 0.2f, sz * d * 0.34f));
                }

            Box(t, "mattressFoot", w * 0.62f, 0.18f, d * 0.88f, mattress, new Vector3(w * 0.12f, 0.45f, 0));
            // พนักหัวเตียงยกเอียง ~18° (ด้านหัว −x สูงขึ้น)
            Box(t, "mattressHead", w * 0.34f, 0.18f, d * 0.88f, mattress, new Vector3(-w * 0.33f, 0.5f, 0), -18f);
            Box(t, "pillow", w * 0.17f, 0.1f, d * 0.55f, white, new Vector3(-w * 0.35f, 0.65f, 0), -18f);
            Box(t, "blanket", w * 0.5f, 0.09f, d * 0.94f, Std(0x4fa3d9, 0.85f, 0f), new Vector3(w * 0.15f, 0.6f, 0));
            Box(t, "blanketFold", w * 0.09f, 0.1f, d * 0.95f, Std(Orange, 0.8f, 0f), new Vector3(-w * 0.12f, 0.61f, 0));

            Box(t, "headboard", 0.08f, 0.8f, d * 0.92f, frame, new Vector3(-w * 0.485f, 0.65f, 0));
            Box(t, "footboard", 0.08f, 0.45f, d * 0.92f, frame, new Vector3(w * 0.485f, 0.58f, 0));

            // ราวข้าง 2 ฝั่ง
            foreach (float sz in new[] { -1f, 1f })
            {
                float z = sz * d * 0.47f;
                Box(t, "rail", w * 0.46f, 0.04f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(w * 0.05f, 0.82f, z));
                Box(t, "railLow", w * 0.46f, 0.04f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(w * 0.05f, 0.7f, z));
                Box(t, "post", 0.04f, 0.16f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(-w * 0.18f, 0.76f, z));
                Box(t, "post", 0.04f, 0.16f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(w * 0.28f, 0.76f, z));
            }
        }

        // ───────────── เสาน้ำเกลือ ─────────────
        public static void IvStand(Transform t)
        {
            var steel = Std(Steel, 0.35f, 0.6f);
            Cyl(t, "hub", 0.05f, 0.1f, Std(Navy, 0.5f), new Vector3(0, 0.1f, 0));
            // ฐาน 5 แฉก
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                var arm = Box(t, "leg", 0.3f, 0.04f, 0.06f, Std(Navy, 0.5f), new Vector3(Mathf.Cos(a) * 0.15f, 0.07f, Mathf.Sin(a) * 0.15f));
                arm.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                Caster(t, Mathf.Cos(a) * 0.29f, Mathf.Sin(a) * 0.29f, 0.05f);
            }
            Cyl(t, "pole", 0.022f, 1.75f, steel, new Vector3(0, 0.98f, 0), 10);
            Box(t, "bar", 0.55f, 0.035f, 0.035f, steel, new Vector3(0, 1.85f, 0));
            Cyl(t, "knob", 0.04f, 0.06f, Std(Orange, 0.5f), new Vector3(0, 1.88f, 0), 10);

            // ถุงน้ำเกลือ 2 ถุง: ทรงกลมแบนโปร่งใสเรืองแสงอ่อน + สายยาง
            foreach (float sx in new[] { -1f, 1f })
            {
                var bag = PropFactory.Part(t, "bag", MeshBuilder.Sphere(0.12f, 14, 10), Std(0x9fe0ff, 0.2f, 0f, 0x3aa9d6, 0.7f, 0.75f),
                                           new Vector3(sx * 0.2f, 1.62f, 0), false);
                bag.transform.localScale = new Vector3(0.8f, 1.5f, 0.45f);
                Box(t, "hook", 0.015f, 0.1f, 0.015f, steel, new Vector3(sx * 0.2f, 1.8f, 0));
                Cyl(t, "tube", 0.008f, 0.55f, Std(0xdff6ff, 0.3f, 0f, 0x88ccee, 0.3f), new Vector3(sx * 0.2f, 1.2f, 0), 6, null, false);
            }
        }

        // ───────────── จอมอนิเตอร์สัญญาณชีพ ─────────────
        static Texture2D ecg;

        // กราฟ ECG สีเขียวบนพื้นเข้ม (วาดครั้งเดียวด้วยโค้ด)
        static Texture2D EcgTexture()
        {
            if (ecg != null) return ecg;
            const int W = 128, H = 80;
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(8, 20, 26, 255);
            for (int y = 0; y < H; y += 10) for (int x = 0; x < W; x++) px[y * W + x] = new Color32(16, 40, 48, 255);
            for (int x = 0; x < W; x += 10) for (int y = 0; y < H; y++) px[y * W + x] = new Color32(16, 40, 48, 255);

            void Plot(int x, int y, Color32 c, int th = 1)
            {
                for (int dy = -th + 1; dy <= th - 1 + 1; dy++)
                    if (x >= 0 && x < W && y + dy >= 0 && y + dy < H) px[(y + dy) * W + x] = c;
            }

            // คลื่น P-QRS-T ซ้ำเป็นคาบ ๆ ที่แถบบน
            var green = new Color32(80, 255, 140, 255);
            int baseY = 58, prev = baseY;
            for (int x = 0; x < W; x++)
            {
                float ph = (x % 42) / 42f;
                float v = 0f;
                if (ph > 0.05f && ph < 0.15f) v = 3f * Mathf.Sin((ph - 0.05f) / 0.10f * Mathf.PI);            // P
                else if (ph > 0.28f && ph < 0.32f) v = -4f;                                                    // Q
                else if (ph >= 0.32f && ph < 0.38f) v = 20f * (1f - Mathf.Abs((ph - 0.35f) / 0.03f));          // R
                else if (ph >= 0.38f && ph < 0.42f) v = -6f;                                                   // S
                else if (ph > 0.55f && ph < 0.75f) v = 5f * Mathf.Sin((ph - 0.55f) / 0.20f * Mathf.PI);        // T
                int y = baseY + Mathf.RoundToInt(v);
                for (int yy = Mathf.Min(prev, y); yy <= Mathf.Max(prev, y); yy++) Plot(x, yy, green);
                Plot(x, y, green); prev = y;
            }
            // เส้นรองสีฟ้า (SpO2) และสีเหลือง (ความดัน)
            var blue = new Color32(90, 190, 255, 255);
            var yellow = new Color32(255, 210, 80, 255);
            for (int x = 0; x < W; x++)
            {
                Plot(x, 34 + Mathf.RoundToInt(5f * Mathf.Sin(x / 5f)), blue);
                Plot(x, 14 + Mathf.RoundToInt(3f * Mathf.Sin(x / 9f + 1f)), yellow);
            }
            // ตัวเลขสัญญาณชีพจำลองเป็นแท่งสี่เหลี่ยมมุมขวาบน
            for (int y = 66; y < 76; y++) for (int x = 98; x < 122; x++) px[y * W + x] = ((x + y) % 5 < 3) ? new Color32(80, 255, 140, 255) : new Color32(8, 20, 26, 255);

            ecg = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "ecg" };
            ecg.SetPixels32(px);
            ecg.Apply(false);
            return ecg;
        }

        public static void Monitor(Transform t, float w, float d)
        {
            var navy = Std(Navy, 0.5f, 0.3f);
            Box(t, "base", 0.5f, 0.06f, 0.5f, navy, new Vector3(0, 0.03f, 0));
            Cyl(t, "pole", 0.04f, 1.0f, Std(Steel, 0.35f, 0.6f), new Vector3(0, 0.55f, 0));
            Caster(t, -0.2f, -0.2f); Caster(t, 0.2f, -0.2f); Caster(t, -0.2f, 0.2f); Caster(t, 0.2f, 0.2f);

            // ตัวจอ (เอียงเล็กน้อยไปทางด้านหน้า −z)
            var head = new GameObject("head").transform;
            head.SetParent(t, false);
            head.localPosition = new Vector3(0, 1.28f, 0);
            head.localRotation = Quaternion.Euler(-8f, 0, 0);
            Box(head, "body", 0.82f, 0.56f, 0.16f, navy, Vector3.zero);
            Box(head, "bezelTop", 0.82f, 0.05f, 0.18f, Std(Orange, 0.5f), new Vector3(0, 0.3f, 0));
            // จอ: ใช้ Unlit + ภาพกราฟ ECG (สว่างในตัวเอง)
            var screenMat = Mats.Unlit(Color.white, tex: EcgTexture());
            PropFactory.Part(head, "screen", MeshBuilder.Box(0.7f, 0.42f, 0.01f), screenMat, new Vector3(0, -0.01f, -0.086f), false);
            // ไฟสถานะ
            Box(head, "led", 0.05f, 0.03f, 0.02f, Std(0x40ff80, 0.4f, 0f, 0x40ff80, 2f), new Vector3(0.3f, -0.26f, -0.085f));
            Box(head, "back", 0.4f, 0.4f, 0.12f, Std(Slate, 0.6f, 0.2f), new Vector3(0, 0, 0.12f));
        }

        // ───────────── ตู้ยา ─────────────
        public static void Cabinet(Transform t, float lw, float ld)
        {
            var body = Std(Lavender, 0.65f, 0.05f);
            var white = Std(White, 0.55f, 0f);
            Box(t, "plinth", lw + 0.04f, 0.12f, ld + 0.04f, Std(Navy, 0.5f, 0.3f), new Vector3(0, 0.06f, 0));
            Box(t, "body", lw, 1.45f, ld, body, new Vector3(0, 0.84f, 0));
            Box(t, "top", lw + 0.06f, 0.06f, ld + 0.06f, Std(Navy, 0.5f, 0.3f), new Vector3(0, 1.59f, 0));
            Box(t, "stripe", lw * 0.96f, 0.06f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 1.43f, -ld / 2 - 0.005f));

            // บานประตูกระจก 2 บาน (ด้านหน้า −z)
            foreach (float s in new[] { -1f, 1f })
            {
                float x = s * lw * 0.245f;
                Box(t, "door", lw * 0.45f, 1.1f, 0.03f, white, new Vector3(x, 0.8f, -ld / 2 - 0.015f));
                Box(t, "glass", lw * 0.36f, 0.5f, 0.012f, Std(0x9fe0ff, 0.15f, 0f, 0x3aa9d6, 0.45f, 0.55f), new Vector3(x, 1.0f, -ld / 2 - 0.035f), 0f);
                Box(t, "handle", 0.03f, 0.22f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(-s * lw * 0.04f, 0.8f, -ld / 2 - 0.045f));
            }
            // กากบาทแดงบนบานขวา (+x)
            var red = Std(Red, 0.5f, 0f, Red, 0.6f);
            Box(t, "crossH", 0.22f, 0.06f, 0.015f, red, new Vector3(lw * 0.245f, 0.5f, -ld / 2 - 0.04f));
            Box(t, "crossV", 0.06f, 0.22f, 0.015f, red, new Vector3(lw * 0.245f, 0.5f, -ld / 2 - 0.04f));
        }

        // ───────────── รถเข็นยา ─────────────
        public static void Cart(Transform t, float lw, float ld)
        {
            var body = Std(Lavender, 0.65f, 0.05f);
            var navy = Std(Navy, 0.5f, 0.3f);
            Box(t, "base", lw * 0.92f, 0.07f, ld * 0.85f, navy, new Vector3(0, 0.17f, 0));
            Box(t, "body", lw, 0.62f, ld, body, new Vector3(0, 0.56f, 0));
            Box(t, "tray", lw + 0.06f, 0.05f, ld + 0.06f, navy, new Vector3(0, 0.9f, 0));
            Box(t, "rim", lw + 0.06f, 0.06f, 0.03f, navy, new Vector3(0, 0.95f, ld / 2 + 0.015f));

            // ลิ้นชัก 3 ชั้น + มือจับส้ม
            for (int i = 0; i < 3; i++)
            {
                float y = 0.77f - i * 0.19f;
                Box(t, "drawer", lw * 0.9f, 0.16f, 0.02f, Std(White, 0.55f, 0f), new Vector3(0, y, -ld / 2 - 0.01f));
                Box(t, "handle", 0.28f, 0.03f, 0.035f, Std(Orange, 0.5f), new Vector3(0, y + 0.01f, -ld / 2 - 0.035f));
            }
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Caster(t, sx * lw * 0.42f, sz * ld * 0.36f);

            // ของบนถาด: กล่องปฐมพยาบาลแดง + ขวดยา 2 ขวด
            Box(t, "firstAid", 0.26f, 0.13f, 0.2f, Std(Red, 0.55f), new Vector3(-lw * 0.3f, 1.005f, 0.04f));
            Box(t, "firstAidCross", 0.1f, 0.02f, 0.03f, Std(White, 0.4f, 0f, White, 0.4f), new Vector3(-lw * 0.3f, 1.075f, 0.04f));
            Cyl(t, "bottle", 0.04f, 0.16f, Std(0x9fe0ff, 0.2f, 0f, 0x3aa9d6, 0.5f, 0.8f), new Vector3(lw * 0.1f, 1.05f, 0.1f), 10);
            Cyl(t, "bottle", 0.04f, 0.2f, Std(0xffd68a, 0.2f, 0f, 0xf4a53a, 0.4f, 0.8f), new Vector3(lw * 0.2f, 1.07f, 0.1f), 10);
            // เครื่องกระตุกหัวใจ (โมเดลสำเร็จรูป Kenney) วางบนถาด
            ModelProp.Spawn("aid-defibrillator-red", t, new Vector3(lw * 0.02f, 0.925f, -0.04f), 180f, 1.0f);
        }

        // ───────────── เครื่องสแกนวินิจฉัย ─────────────
        public static void Scanner(Transform t, float lw, float ld)
        {
            var body = Std(Lavender, 0.6f, 0.1f);
            var navy = Std(Navy, 0.5f, 0.3f);
            var glow = Std(0x1b2a3c, 0.4f, 0f, Cyan, 1.8f);

            // แท่นและเตียงเลื่อน
            Box(t, "platform", lw, 0.16f, ld, navy, new Vector3(0, 0.08f, 0));
            Box(t, "platformStripe", lw + 0.02f, 0.06f, 0.03f, Std(Orange, 0.6f), new Vector3(0, 0.1f, -ld / 2 - 0.005f)); // แถบส้มขอบหน้าแท่น
            Box(t, "bed", lw * 0.8f, 0.1f, ld * 0.5f, Std(White, 0.6f, 0f), new Vector3(0, 0.25f, 0));
            Box(t, "pad", lw * 0.18f, 0.12f, ld * 0.42f, Std(0x4fa3d9, 0.85f, 0f), new Vector3(-lw * 0.3f, 0.3f, 0));

            // ซุ้มสแกน (เสา 2 ต้น + คานบน) กว้างตามแกน x แต่บางตามแกน z
            float colX = lw / 2 - 0.22f;
            foreach (float s in new[] { -1f, 1f })
            {
                Box(t, "column", 0.34f, 1.35f, ld * 0.62f, body, new Vector3(s * colX, 0.83f, 0));
                Box(t, "columnBase", 0.4f, 0.1f, ld * 0.66f, navy, new Vector3(s * colX, 0.24f, 0));
                Box(t, "laserV", 0.04f, 1.0f, ld * 0.5f, glow, new Vector3(s * (colX - 0.19f), 0.85f, 0));
            }
            Box(t, "beam", lw - 0.16f, 0.34f, ld * 0.62f, body, new Vector3(0, 1.62f, 0));
            Box(t, "beamBar", lw * 0.7f, 0.04f, ld * 0.5f, glow, new Vector3(0, 1.42f, 0));
            Box(t, "beamStripe", lw - 0.2f, 0.07f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 1.62f, -ld * 0.31f - 0.005f)); // แถบส้มบนหน้าคาน
            // แสงสแกนกลางซุ้ม (แผ่นบางโปร่งแสง)
            PropFactory.Part(t, "scanPlane", MeshBuilder.Box(lw - 0.8f, 0.9f, 0.02f),
                             Std(Cyan, 0.2f, 0f, Cyan, 1.2f, 0.18f), new Vector3(0, 0.9f, 0), false);
        }
    }
}
