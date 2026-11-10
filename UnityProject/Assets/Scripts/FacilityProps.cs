using UnityEngine;
using static SkeldMap.MedicalProps;

namespace SkeldMap
{
    // อุปกรณ์ประจำห้อง (ห้องที่ยังไม่มีชุดเฉพาะ) — Engine / Cafeteria / Weapons / Security / O2 / Navigation / Admin / Electrical /
    // Storage / Communications / Shields · สร้างด้วยโค้ดโทนเดียวกับ MedicalProps และ ReactorProps
    // กรอบอ้างอิง: ด้านหน้า = −z ของตัวเอง · lw ตามแกน x · ld ตามแกน z (PropFactory หมุนตาม rot และสลับ lw/ld ให้)
    public static class FacilityProps
    {
        const uint Gunmetal = 0x4a557f, Dark = 0x2a2d38, Hazard = 0xf4c430, Glow = 0xff7a2a, Copper = 0xd98a4a;

        // ───── ตัวช่วย ─────
        static GameObject Ball(Transform p, string n, float r, Material m, Vector3 pos, Vector3? scale = null, int seg = 14)
        {
            var g = PropFactory.Part(p, n, MeshBuilder.Sphere(r, seg, Mathf.Max(6, seg - 4)), m, pos);
            if (scale.HasValue) g.transform.localScale = scale.Value;
            return g;
        }

        static GameObject TiltBox(Transform p, string n, float w, float h, float d, Material m, Vector3 pos, Vector3 euler)
            => PropFactory.Part(p, n, MeshBuilder.Box(w, h, d), m, pos, true, Quaternion.Euler(euler));

        static Material Steelm => Std(Steel, 0.35f, 0.6f);
        static Material Navym => Std(Navy, 0.5f, 0.3f);
        static Material Lavm => Std(Lavender, 0.6f, 0.1f);
        static Material Orangem => Std(Orange, 0.6f);

        static Material Emit(uint color, float i = 1.8f) => Std(0x161a26, 0.4f, 0f, color, i);

        // ════════════════ ENGINE ════════════════

        public static void Workbench(Transform t, float lw, float ld)
        {
            Box(t, "top", lw, 0.07f, ld, Std(0x8a97b3, 0.5f, 0.4f), new Vector3(0, 0.92f, 0));
            Box(t, "edge", lw + 0.02f, 0.03f, ld + 0.02f, Orangem, new Vector3(0, 0.88f, 0));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box(t, "leg", 0.08f, 0.88f, 0.08f, Navym, new Vector3(sx * (lw / 2 - 0.08f), 0.44f, sz * (ld / 2 - 0.08f)));
            Box(t, "shelf", lw * 0.92f, 0.05f, ld * 0.8f, Steelm, new Vector3(0, 0.3f, 0));
            uint[] bins = { Red, 0x4fa3d9, Orange };
            for (int i = 0; i < 3; i++) Box(t, "bin", lw * 0.2f, 0.14f, ld * 0.55f, Std(bins[i], 0.7f), new Vector3(-lw * 0.28f + i * lw * 0.28f, 0.4f, 0));

            // แผ่นแขวนเครื่องมือด้านหลัง (+z)
            Box(t, "pegboard", lw, 0.75f, 0.04f, Std(Slate, 0.6f, 0.2f), new Vector3(0, 1.38f, ld / 2 - 0.02f));
            uint[] tools = { Orange, Steel, Red, Cyan, Steel };
            for (int i = 0; i < 5; i++)
                Box(t, "tool", 0.06f, 0.3f - (i % 2) * 0.08f, 0.02f, Std(tools[i], 0.4f, 0.5f), new Vector3(-lw * 0.4f + i * lw * 0.2f, 1.4f, ld / 2 - 0.055f));

            Box(t, "toolbox", 0.5f, 0.2f, 0.28f, Std(Red, 0.55f), new Vector3(-lw * 0.3f, 1.06f, -0.04f));
            Box(t, "toolboxHandle", 0.26f, 0.03f, 0.03f, Steelm, new Vector3(-lw * 0.3f, 1.18f, -0.04f));
            Box(t, "vise", 0.22f, 0.16f, 0.22f, Navym, new Vector3(lw * 0.18f, 1.04f, 0));
            Cyl(t, "viseLever", 0.015f, 0.26f, Steelm, new Vector3(lw * 0.18f, 1.16f, 0), 8, Quaternion.Euler(0, 0, 90));
            // โคมไฟตั้งโต๊ะ
            Cyl(t, "lampPole", 0.015f, 0.5f, Steelm, new Vector3(lw * 0.38f, 1.2f, 0.1f), 8);
            Cyl(t, "lamp", 0.07f, 0.06f, Emit(0xffe9b0), new Vector3(lw * 0.38f, 1.47f, 0.1f), 12);
        }

        public static void FuelCells(Transform t)
        {
            Cyl(t, "tray", 0.62f, 0.06f, Navym, new Vector3(0, 0.03f, 0), 20);
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2f + 0.5f;
                float x = Mathf.Cos(a) * 0.27f, z = Mathf.Sin(a) * 0.27f;
                Cyl(t, "can", 0.2f, 1.1f, Std(Lavender, 0.4f, 0.4f), new Vector3(x, 0.6f, z), 16);
                Cyl(t, "band", 0.207f, 0.1f, Std(0x2a1a10, 0.4f, 0f, Glow, 1.3f), new Vector3(x, 0.66f, z), 16, null, false);
                Cyl(t, "cap", 0.2f, 0.06f, Navym, new Vector3(x, 1.17f, z), 16);
                Cyl(t, "valve", 0.045f, 0.13f, Steelm, new Vector3(x, 1.26f, z), 8);
            }
        }

        // ════════════════ CAFETERIA ════════════════

        public static void VendingMachine(Transform t, float lw, float ld)
        {
            Box(t, "base", lw + 0.04f, 0.1f, ld + 0.04f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw, 1.85f, ld, Std(Slate, 0.5f, 0.3f), new Vector3(0, 1.0f, 0));
            Box(t, "sign", lw * 0.96f, 0.2f, 0.04f, Emit(Cyan, 1.2f), new Vector3(0, 1.82f, -ld / 2 - 0.01f));
            ReactorProps.Display(t, "products", lw * 0.7f, 1.12f, TexLib.Vending(), new Vector3(-lw * 0.1f, 1.2f, -ld / 2 - 0.012f));
            Box(t, "keypad", lw * 0.16f, 1.12f, 0.03f, Navym, new Vector3(lw * 0.38f, 1.2f, -ld / 2 - 0.012f));
            for (int i = 0; i < 4; i++) Box(t, "btn", 0.07f, 0.05f, 0.02f, Std(i % 2 == 0 ? 0x40ff80 : Orange, 0.4f, 0f, i % 2 == 0 ? 0x40ff80u : Orange, 1.2f), new Vector3(lw * 0.38f, 1.55f - i * 0.12f, -ld / 2 - 0.03f));
            Box(t, "slot", lw * 0.7f, 0.2f, 0.03f, Std(Dark, 0.6f), new Vector3(-lw * 0.05f, 0.42f, -ld / 2 - 0.012f));
            Box(t, "flap", lw * 0.64f, 0.1f, 0.02f, Std(Steel, 0.3f, 0.5f), new Vector3(-lw * 0.05f, 0.44f, -ld / 2 - 0.03f));
        }

        // เคาน์เตอร์เสิร์ฟอาหาร (ยาวตามแกน x)
        public static void ServingCounter(Transform t, float lw, float ld)
        {
            Box(t, "base", lw, 0.85f, ld, Lavm, new Vector3(0, 0.45f, 0));
            Box(t, "plinth", lw + 0.02f, 0.1f, ld + 0.02f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "stripe", lw * 0.98f, 0.07f, 0.02f, Orangem, new Vector3(0, 0.6f, -ld / 2 - 0.005f));
            Box(t, "top", lw + 0.04f, 0.06f, ld + 0.04f, Navym, new Vector3(0, 0.9f, 0));

            // ถาดอาหาร: ถาดเหล็ก + อาหารหลากสี
            int n = Mathf.Max(2, Mathf.FloorToInt(lw / 0.8f));
            uint[] food = { 0xe0473f, 0xf4c430, 0x6ecb63, 0xf4a53a, 0xd98a4a };
            for (int i = 0; i < n; i++)
            {
                float x = -lw / 2 + (i + 0.5f) * lw / n;
                Box(t, "tray", lw / n * 0.82f, 0.04f, ld * 0.7f, Steelm, new Vector3(x, 0.95f, -0.02f));
                Ball(t, "food", 0.13f, Std(food[i % food.Length], 0.6f), new Vector3(x, 1.06f, -0.02f), new Vector3(1.1f, 0.7f, 1f));
                Ball(t, "food2", 0.08f, Std(food[(i + 2) % food.Length], 0.6f), new Vector3(x + 0.18f, 1.03f, 0.05f), null, 10);
            }
            // กระจกกันไอ (โปร่งใส) + ขอบบน
            Box(t, "guard", lw * 0.96f, 0.38f, 0.02f, Std(0x9fe0ff, 0.1f, 0f, 0x3aa9d6, 0.3f, 0.35f), new Vector3(0, 1.2f, ld * 0.22f));
            Box(t, "guardTop", lw * 0.96f, 0.03f, 0.05f, Steelm, new Vector3(0, 1.4f, ld * 0.22f));
            // เครื่องคิดเงินปลายเคาน์เตอร์
            Box(t, "register", 0.34f, 0.2f, 0.28f, Navym, new Vector3(lw / 2 - 0.3f, 1.03f, 0.1f));
            Box(t, "registerScreen", 0.26f, 0.12f, 0.02f, Emit(0x40ff80, 1.3f), new Vector3(lw / 2 - 0.3f, 1.1f, -0.045f));
        }

        public static void WaterCooler(Transform t)
        {
            Box(t, "body", 0.4f, 0.8f, 0.4f, Lavm, new Vector3(0, 0.4f, 0));
            Box(t, "stripe", 0.41f, 0.05f, 0.41f, Orangem, new Vector3(0, 0.62f, 0));
            Cyl(t, "jug", 0.16f, 0.46f, Std(0x9fe0ff, 0.15f, 0f, 0x3aa9d6, 0.7f, 0.6f), new Vector3(0, 1.05f, 0), 16, null, false);
            Cyl(t, "jugCap", 0.1f, 0.06f, Navym, new Vector3(0, 1.31f, 0), 12);
            Box(t, "tapBlue", 0.05f, 0.05f, 0.06f, Std(0x4fa3d9, 0.4f), new Vector3(-0.08f, 0.66f, -0.21f));
            Box(t, "tapRed", 0.05f, 0.05f, 0.06f, Std(Red, 0.4f), new Vector3(0.08f, 0.66f, -0.21f));
        }

        public static void Bin(Transform t)
        {
            Cyl(t, "body", 0.27f, 0.66f, Std(Slate, 0.6f, 0.2f), new Vector3(0, 0.34f, 0), 16);
            Cyl(t, "stripe", 0.275f, 0.07f, Orangem, new Vector3(0, 0.5f, 0), 16);
            Cyl(t, "lid", 0.29f, 0.05f, Navym, new Vector3(0, 0.7f, 0), 16);
            Box(t, "flap", 0.2f, 0.02f, 0.12f, Steelm, new Vector3(0, 0.74f, 0));
        }

        // ════════════════ WEAPONS ════════════════

        public static void MissileRack(Transform t, float lw, float ld)
        {
            Box(t, "base", lw, 0.12f, ld, Navym, new Vector3(0, 0.06f, 0));
            foreach (float sx in new[] { -1f, 1f }) Box(t, "upright", 0.1f, 1.3f, ld * 0.9f, Std(Gunmetal, 0.5f, 0.4f), new Vector3(sx * (lw / 2 - 0.06f), 0.7f, 0));
            uint[] bands = { Red, Orange, 0xffe060 };
            for (int lvl = 0; lvl < 2; lvl++)
            {
                float y = 0.42f + lvl * 0.55f;
                Box(t, "cradle", lw * 0.9f, 0.04f, ld * 0.85f, Steelm, new Vector3(0, y - 0.16f, 0));
                float len = lw * 0.82f, r = 0.13f;
                Cyl(t, "missile", r, len, Std(0xdad8f0, 0.4f, 0.3f), new Vector3(0, y, 0), 14, Quaternion.Euler(0, 0, 90));
                Ball(t, "nose", r, Std(Red, 0.4f, 0.2f), new Vector3(len / 2, y, 0), new Vector3(1.8f, 1f, 1f));
                for (int b = 0; b < 3; b++)
                    Cyl(t, "band", r + 0.008f, 0.06f, Std(bands[b], 0.5f, 0.1f, bands[b], 0.4f), new Vector3(-len * 0.25f + b * len * 0.22f, y, 0), 14, Quaternion.Euler(0, 0, 90));
                foreach (float s in new[] { -1f, 1f })
                    Box(t, "fin", 0.16f, 0.02f, 0.14f, Navym, new Vector3(-len / 2 + 0.06f, y, s * (r + 0.05f)));
                Box(t, "finV", 0.16f, 0.2f, 0.02f, Navym, new Vector3(-len / 2 + 0.06f, y + r + 0.04f, 0));
            }
        }

        public static void RadarStation(Transform t, float lw, float ld)
        {
            var slate = Std(Gunmetal, 0.55f, 0.3f);
            Box(t, "plinth", lw, 0.2f, ld * 0.92f, Navym, new Vector3(0, 0.1f, 0));
            Box(t, "body", lw * 0.98f, 0.62f, ld * 0.9f, slate, new Vector3(0, 0.51f, 0));
            Box(t, "stripe", lw * 0.92f, 0.05f, 0.02f, Std(Red, 0.6f), new Vector3(0, 0.2f, -ld * 0.46f - 0.005f));
            float zb = ld * 0.5f - 0.1f;
            Box(t, "riser", lw * 0.98f, 0.95f, 0.14f, slate, new Vector3(0, 1.22f, zb));
            ReactorProps.Display(t, "radar", Mathf.Min(lw * 0.5f, 0.9f), Mathf.Min(lw * 0.5f, 0.9f), TexLib.Radar(), new Vector3(-lw * 0.18f, 1.25f, zb - 0.075f));
            ReactorProps.Display(t, "wave", lw * 0.26f, 0.5f, ReactorProps.WaveTexture(), new Vector3(lw * 0.3f, 1.25f, zb - 0.075f));
            var slope = new GameObject("slope").transform;
            slope.SetParent(t, false); slope.localPosition = new Vector3(0, 0.86f, -0.02f); slope.localRotation = Quaternion.Euler(-18f, 0, 0);
            Box(slope, "top", lw * 0.98f, 0.07f, ld * 0.66f, Navym, Vector3.zero);
            foreach (float s in new[] { -1f, 1f })
            {
                Cyl(slope, "stick", 0.025f, 0.3f, Steelm, new Vector3(s * lw * 0.22f, 0.17f, -0.05f), 8, Quaternion.Euler(-20, 0, 0));
                Ball(slope, "knob", 0.06f, Std(Red, 0.5f, 0f, Red, 0.6f), new Vector3(s * lw * 0.22f, 0.31f, -0.1f));
            }
            for (int i = 0; i < 5; i++) Box(slope, "btn", 0.08f, 0.03f, 0.08f, Std(i % 2 == 0 ? 0x40ff80u : Orange, 0.4f, 0f, i % 2 == 0 ? 0x40ff80u : Orange, 1.2f), new Vector3(-lw * 0.3f + i * 0.14f, 0.06f, 0.1f));
        }

        // ════════════════ SECURITY ════════════════

        public static void Lockers(Transform t, float lw, float ld)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(lw / 0.5f));
            Box(t, "plinth", lw + 0.02f, 0.1f, ld + 0.02f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw, 1.7f, ld, Std(Slate, 0.5f, 0.35f), new Vector3(0, 0.95f, 0));
            Box(t, "top", lw + 0.04f, 0.05f, ld + 0.04f, Navym, new Vector3(0, 1.82f, 0));
            uint[] cols = { Lavender, 0xb9c4ea };
            for (int i = 0; i < n; i++)
            {
                float x = -lw / 2 + (i + 0.5f) * lw / n;
                Box(t, "door", lw / n * 0.9f, 1.58f, 0.03f, Std(cols[i % 2], 0.5f, 0.2f), new Vector3(x, 0.95f, -ld / 2 - 0.015f));
                for (int v = 0; v < 3; v++) Box(t, "vent", lw / n * 0.5f, 0.02f, 0.015f, Std(Dark, 0.7f), new Vector3(x, 1.5f - v * 0.06f, -ld / 2 - 0.035f));
                Box(t, "handle", 0.03f, 0.18f, 0.04f, Steelm, new Vector3(x + lw / n * 0.3f, 0.95f, -ld / 2 - 0.045f));
                Box(t, "tag", lw / n * 0.4f, 0.07f, 0.01f, Std(Orange, 0.6f), new Vector3(x, 1.2f, -ld / 2 - 0.035f));
            }
        }

        // ผนังจอ CCTV 3×2 (ติดผนัง)
        public static void MonitorWall(Transform t, float lw, float ld)
        {
            Box(t, "frame", lw, 1.05f, ld * 0.5f, Navym, new Vector3(0, 1.0f, ld * 0.2f));
            Box(t, "header", lw, 0.08f, ld * 0.52f, Orangem, new Vector3(0, 1.56f, ld * 0.2f));
            float sw = (lw - 0.2f) / 3f, sh = 0.4f;
            int idx = 0;
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    ReactorProps.Display(t, "feed", sw * 0.92f, sh, TexLib.CctvFeed(idx++), new Vector3(-lw / 2 + 0.1f + (col + 0.5f) * sw, 1.2f - row * 0.46f, ld * 0.2f - ld * 0.25f - 0.008f));
            Box(t, "ledA", 0.05f, 0.03f, 0.02f, Emit(0x40ff80), new Vector3(lw / 2 - 0.1f, 0.58f, ld * 0.2f - ld * 0.25f - 0.01f));
        }

        public static void OfficeChair(Transform t)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                var arm = Box(t, "leg", 0.28f, 0.03f, 0.05f, Navym, new Vector3(Mathf.Cos(a) * 0.14f, 0.13f, Mathf.Sin(a) * 0.14f));
                arm.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                Caster(t, Mathf.Cos(a) * 0.27f, Mathf.Sin(a) * 0.27f, 0.045f);
            }
            Cyl(t, "pole", 0.035f, 0.3f, Steelm, new Vector3(0, 0.3f, 0), 10);
            Box(t, "seat", 0.5f, 0.1f, 0.5f, Std(0x4fa3d9, 0.7f), new Vector3(0, 0.5f, 0));
            Box(t, "back", 0.5f, 0.55f, 0.08f, Std(0x4fa3d9, 0.7f), new Vector3(0, 0.82f, 0.25f));
            Box(t, "head", 0.3f, 0.14f, 0.07f, Navym, new Vector3(0, 1.18f, 0.25f));
            foreach (float s in new[] { -1f, 1f }) Box(t, "arm", 0.05f, 0.04f, 0.3f, Navym, new Vector3(s * 0.28f, 0.66f, 0.05f));
        }

        public static void CardReader(Transform t)
        {
            Box(t, "base", 0.5f, 0.1f, 0.4f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "stand", 0.3f, 0.78f, 0.22f, Std(Slate, 0.5f, 0.3f), new Vector3(0, 0.45f, 0));
            var head = new GameObject("head").transform;
            head.SetParent(t, false); head.localPosition = new Vector3(0, 0.88f, 0); head.localRotation = Quaternion.Euler(-30, 0, 0);
            Box(head, "panel", 0.44f, 0.3f, 0.12f, Navym, Vector3.zero);
            Box(head, "screen", 0.3f, 0.12f, 0.02f, Emit(0x40ff80, 1.4f), new Vector3(0, 0.06f, -0.065f));
            Box(head, "slot", 0.34f, 0.04f, 0.03f, Std(Dark, 0.5f), new Vector3(0, -0.07f, -0.06f));
            Box(head, "card", 0.2f, 0.12f, 0.01f, Orangem, new Vector3(0.0f, 0.0f, -0.075f)).transform.localPosition = new Vector3(0, -0.07f, -0.085f);
        }

        // ════════════════ O2 ════════════════

        public static void AirFilter(Transform t, float lw, float ld)
        {
            Box(t, "base", lw + 0.04f, 0.1f, ld + 0.04f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw, 1.5f, ld, Lavm, new Vector3(0, 0.85f, 0));
            Box(t, "stripe", lw * 0.96f, 0.07f, 0.02f, Orangem, new Vector3(0, 1.5f, -ld / 2 - 0.005f));
            // พัดลม: ขอบ + ใบพัด + ตะแกรง
            float fr = Mathf.Min(lw, ld) * 0.38f;
            Cyl(t, "fanRim", fr + 0.04f, 0.06f, Navym, new Vector3(0, 0.95f, -ld / 2 - 0.02f), 24, Quaternion.Euler(90, 0, 0));
            Cyl(t, "fanBack", fr, 0.04f, Std(Dark, 0.6f), new Vector3(0, 0.95f, -ld / 2 - 0.035f), 24, Quaternion.Euler(90, 0, 0), false);
            for (int i = 0; i < 4; i++)
            {
                var b = Box(t, "blade", fr * 0.9f, 0.1f, 0.012f, Std(Steel, 0.4f, 0.5f), new Vector3(0, 0.95f, -ld / 2 - 0.05f));
                b.transform.localRotation = Quaternion.Euler(0, 0, 45f + i * 90f);
                b.transform.localPosition += b.transform.right * 0.0f;
            }
            Cyl(t, "hub", 0.06f, 0.05f, Navym, new Vector3(0, 0.95f, -ld / 2 - 0.07f), 12, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 5; i++) Box(t, "louver", lw * 0.8f, 0.02f, 0.02f, Std(Dark, 0.6f), new Vector3(0, 0.25f + i * 0.07f, -ld / 2 - 0.02f));
            Box(t, "ledBar", lw * 0.5f, 0.04f, 0.015f, Emit(0x40ff80, 1.6f), new Vector3(0, 1.38f, -ld / 2 - 0.02f));
        }

        public static void Hydroponic(Transform t, float lw, float ld)
        {
            Box(t, "trough", lw, 0.4f, ld, Navym, new Vector3(0, 0.25f, 0));
            Box(t, "rim", lw + 0.04f, 0.05f, ld + 0.04f, Steelm, new Vector3(0, 0.46f, 0));
            Box(t, "soil", lw * 0.95f, 0.05f, ld * 0.8f, Std(0x5a3a26, 0.95f, 0f), new Vector3(0, 0.485f, 0));
            int n = Mathf.Max(2, Mathf.RoundToInt(lw / 0.7f));
            for (int i = 0; i < n; i++)
            {
                float x = -lw / 2 + (i + 0.5f) * lw / n;
                PropFactory.Part(t, "bush", MeshBuilder.Icosahedron(0.27f), Std(i % 2 == 0 ? 0x3fae5au : 0x5ccf6eu, 0.9f), new Vector3(x, 0.68f, 0));
            }
            // โคมไฟปลูก (ม่วง/ชมพู) ลอยเหนือแปลง
            foreach (float sx in new[] { -1f, 1f }) Box(t, "post", 0.04f, 0.8f, 0.04f, Steelm, new Vector3(sx * (lw / 2 - 0.05f), 0.9f, ld / 2 - 0.05f));
            Box(t, "lamp", lw * 0.9f, 0.06f, 0.14f, Std(0x2a1030, 0.4f, 0f, 0xff5ad8, 1.5f), new Vector3(0, 1.28f, ld / 2 - 0.05f));
        }

        // ════════════════ NAVIGATION ════════════════

        public static void HoloTable(Transform t, float r)
        {
            Cyl(t, "foot", r * 0.8f, 0.08f, Navym, new Vector3(0, 0.04f, 0), 24);
            PropFactory.Part(t, "stem", MeshBuilder.Cylinder(r * 0.55f, r * 0.8f, 0.5f, 24), Std(Gunmetal, 0.5f, 0.4f), new Vector3(0, 0.33f, 0));
            Cyl(t, "top", r, 0.1f, Lavm, new Vector3(0, 0.62f, 0), 28);
            Cyl(t, "topRing", r + 0.02f, 0.04f, Orangem, new Vector3(0, 0.58f, 0), 28);
            Cyl(t, "emitter", r * 0.55f, 0.04f, Emit(Cyan, 1.8f), new Vector3(0, 0.69f, 0), 24, null, false);
            // ลูกโลกโฮโลแกรม + วงโคจร
            Ball(t, "globe", 0.38f, Std(0x9fe0ff, 0.1f, 0f, Cyan, 1.0f, 0.5f), new Vector3(0, 1.15f, 0), null, 18).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Ball(t, "core", 0.12f, Std(0xffffff, 0.3f, 0f, 0xffd060, 2.2f), new Vector3(0, 1.15f, 0), null, 10);
            for (int i = 0; i < 2; i++)
                Cyl(t, "orbit", 0.62f, 0.012f, Std(Cyan, 0.2f, 0f, Cyan, 1.6f, 0.55f), new Vector3(0, 1.15f, 0), 36, Quaternion.Euler(i == 0 ? 20 : -28, 0, i == 0 ? 0 : 35), false);
        }

        public static void PilotChair(Transform t)
        {
            Cyl(t, "foot", 0.3f, 0.06f, Navym, new Vector3(0, 0.03f, 0), 20);
            Cyl(t, "pole", 0.06f, 0.35f, Steelm, new Vector3(0, 0.23f, 0), 12);
            var seat = Std(0x6b78b8, 0.65f, 0.1f);
            Box(t, "seat", 0.66f, 0.13f, 0.66f, seat, new Vector3(0, 0.46f, 0));
            Box(t, "seatStripe", 0.67f, 0.03f, 0.67f, Orangem, new Vector3(0, 0.4f, 0));
            Box(t, "back", 0.66f, 0.85f, 0.12f, seat, new Vector3(0, 0.95f, 0.34f));
            Box(t, "head", 0.4f, 0.22f, 0.1f, Navym, new Vector3(0, 1.5f, 0.36f));
            foreach (float s in new[] { -1f, 1f })
            {
                Box(t, "arm", 0.12f, 0.06f, 0.45f, Navym, new Vector3(s * 0.4f, 0.66f, 0.05f));
                Box(t, "armPanel", 0.1f, 0.02f, 0.2f, Emit(0x55e0ff, 1.2f), new Vector3(s * 0.4f, 0.7f, -0.04f));
            }
        }

        // จอติดผนังอเนกประสงค์: ยาวตามแกน x ของตัวเอง
        public static void WallScreen(Transform t, float lw, float ld, Texture2D tex, uint accent)
        {
            Box(t, "frame", lw, 1.0f, ld * 0.6f, Navym, new Vector3(0, 1.0f, ld * 0.2f));
            Box(t, "header", lw, 0.07f, ld * 0.62f, Std(accent, 0.5f), new Vector3(0, 1.53f, ld * 0.2f));
            ReactorProps.Display(t, "screen", lw - 0.16f, 0.8f, tex, new Vector3(0, 1.0f, ld * 0.2f - ld * 0.3f - 0.008f));
        }

        // ════════════════ ADMIN ════════════════

        public static void FileCabinets(Transform t, float lw, float ld)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(lw / 0.6f));
            Box(t, "plinth", lw + 0.02f, 0.08f, ld + 0.02f, Navym, new Vector3(0, 0.04f, 0));
            Box(t, "body", lw, 1.3f, ld, Std(0x8e9ac8, 0.5f, 0.35f), new Vector3(0, 0.73f, 0));
            Box(t, "top", lw + 0.04f, 0.05f, ld + 0.04f, Navym, new Vector3(0, 1.4f, 0));
            for (int c = 0; c < n; c++)
            {
                float x = -lw / 2 + (c + 0.5f) * lw / n;
                for (int d = 0; d < 4; d++)
                {
                    float y = 0.28f + d * 0.31f;
                    Box(t, "drawer", lw / n * 0.9f, 0.27f, 0.025f, Std(Lavender, 0.5f, 0.2f), new Vector3(x, y, -ld / 2 - 0.012f));
                    Box(t, "handle", lw / n * 0.4f, 0.03f, 0.04f, Steelm, new Vector3(x, y + 0.04f, -ld / 2 - 0.04f));
                    Box(t, "label", lw / n * 0.32f, 0.05f, 0.01f, Std(White, 0.6f), new Vector3(x, y - 0.05f, -ld / 2 - 0.03f));
                }
            }
        }

        public static void Printer(Transform t, float lw, float ld)
        {
            Box(t, "cabinet", lw, 0.7f, ld, Navym, new Vector3(0, 0.35f, 0));
            Box(t, "body", lw * 0.92f, 0.34f, ld * 0.85f, Std(White, 0.6f, 0.1f), new Vector3(0, 0.87f, 0));
            Box(t, "lid", lw * 0.88f, 0.05f, ld * 0.8f, Std(Lavender, 0.5f), new Vector3(0, 1.06f, 0));
            Box(t, "panel", 0.22f, 0.1f, 0.02f, Emit(0x40ff80, 1.3f), new Vector3(lw * 0.3f, 0.95f, -ld * 0.43f - 0.01f));
            TiltBox(t, "tray", lw * 0.6f, 0.02f, 0.3f, Std(Steel, 0.4f, 0.5f), new Vector3(0, 0.78f, -ld * 0.5f - 0.1f), new Vector3(15, 0, 0));
            TiltBox(t, "paper", lw * 0.5f, 0.012f, 0.26f, Std(White, 0.8f), new Vector3(0, 0.8f, -ld * 0.5f - 0.1f), new Vector3(15, 0, 0));
            Box(t, "stripe", lw * 0.9f, 0.04f, 0.02f, Orangem, new Vector3(0, 0.55f, -ld / 2 - 0.005f));
        }

        public static void ServerRack(Transform t, float lw, float ld, int seed)
        {
            Box(t, "base", lw + 0.04f, 0.08f, ld + 0.04f, Navym, new Vector3(0, 0.04f, 0));
            Box(t, "body", lw, 1.8f, ld, Std(0x20243a, 0.5f, 0.4f), new Vector3(0, 0.98f, 0));
            ReactorProps.Display(t, "front", lw * 0.84f, 1.62f, TexLib.Server(seed), new Vector3(0, 0.98f, -ld / 2 - 0.008f));
            Box(t, "top", lw + 0.03f, 0.05f, ld + 0.03f, Navym, new Vector3(0, 1.9f, 0));
            foreach (float s in new[] { -1f, 1f }) Box(t, "rail", 0.05f, 1.8f, 0.04f, Std(Slate, 0.5f, 0.4f), new Vector3(s * (lw / 2 - 0.02f), 0.98f, -ld / 2 - 0.02f));
        }

        // ════════════════ ELECTRICAL ════════════════

        public static void BreakerPanel(Transform t, float lw, float ld, bool wires)
        {
            Box(t, "cabinet", lw, 1.5f, ld, Std(0x8e9ac8, 0.5f, 0.35f), new Vector3(0, 1.0f, 0));
            Box(t, "frame", lw * 0.92f, 1.34f, 0.03f, Navym, new Vector3(0, 1.0f, -ld / 2 - 0.012f));
            ReactorProps.Display(t, "panel", lw * 0.84f, 1.2f, TexLib.Breaker(wires), new Vector3(0, 1.0f, -ld / 2 - 0.03f));
            // แถบเตือนอันตรายด้านบน + ป้ายฟ้าผ่า
            Box(t, "hazard", lw, 0.12f, ld + 0.02f, Std(Hazard, 0.6f), new Vector3(0, 1.82f, 0));
            Box(t, "bolt", 0.16f, 0.16f, 0.015f, Emit(0xffd23f, 1.6f), new Vector3(0, 1.82f, -ld / 2 - 0.02f)).transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        public static void Transformer(Transform t, float lw, float ld)
        {
            Box(t, "slab", lw, 0.12f, ld, Navym, new Vector3(0, 0.06f, 0));
            Box(t, "slabStripe", lw + 0.02f, 0.05f, 0.03f, Std(Hazard, 0.6f), new Vector3(0, 0.1f, -ld / 2 - 0.005f));
            Box(t, "body", lw * 0.8f, 0.85f, ld * 0.8f, Std(Gunmetal, 0.5f, 0.4f), new Vector3(0, 0.55f, 0));
            for (int i = 0; i < 6; i++)
                foreach (float s in new[] { -1f, 1f }) Box(t, "fin", 0.04f, 0.65f, ld * 0.08f, Std(Slate, 0.5f, 0.3f), new Vector3(s * (lw * 0.4f + 0.03f), 0.55f, -ld * 0.38f + i * (ld * 0.76f / 5f)));
            // ขดลวดทองแดง 3 ชุด + ฉนวน
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * lw * 0.26f;
                Cyl(t, "coil", 0.18f, 0.38f, Std(Copper, 0.35f, 0.7f), new Vector3(x, 1.18f, 0), 16);
                for (int k = 0; k < 3; k++) Cyl(t, "ring", 0.195f, 0.04f, Navym, new Vector3(x, 1.06f + k * 0.12f, 0), 16);
                Cyl(t, "insulator", 0.06f, 0.2f, Std(White, 0.4f), new Vector3(x, 1.5f, 0), 10);
                Ball(t, "terminal", 0.05f, Steelm, new Vector3(x, 1.62f, 0), null, 8);
            }
            Box(t, "warning", 0.2f, 0.2f, 0.015f, Emit(0xffd23f, 1.4f), new Vector3(0, 0.6f, -ld * 0.4f - 0.01f)).transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        public static void CableReel(Transform t, float r)
        {
            Cyl(t, "discBottom", r, 0.06f, Std(0x8a6a3f, 0.8f), new Vector3(0, 0.05f, 0), 24);
            Cyl(t, "core", r * 0.4f, 0.5f, Std(0x6a4f2f, 0.8f), new Vector3(0, 0.3f, 0), 16);
            Cyl(t, "cable", r * 0.82f, 0.4f, Std(0x2a2d38, 0.7f), new Vector3(0, 0.3f, 0), 24);
            Cyl(t, "cableStripe", r * 0.83f, 0.06f, Std(Hazard, 0.6f), new Vector3(0, 0.3f, 0), 24);
            Cyl(t, "discTop", r, 0.06f, Std(0x8a6a3f, 0.8f), new Vector3(0, 0.55f, 0), 24);
            Cyl(t, "end", 0.025f, 0.5f, Std(0x2a2d38, 0.7f), new Vector3(r * 0.9f, 0.1f, -r * 0.5f), 8, Quaternion.Euler(0, 0, 90));
            Box(t, "plug", 0.12f, 0.1f, 0.1f, Std(Red, 0.5f), new Vector3(r * 0.9f + 0.28f, 0.1f, -r * 0.5f));
        }

        // ════════════════ STORAGE ════════════════

        public static void Shelf(Transform t, float lw, float ld, int seed)
        {
            var rng = new System.Random(seed);
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box(t, "post", 0.06f, 1.3f, 0.06f, Std(0x4a6aa8, 0.5f, 0.4f), new Vector3(sx * (lw / 2 - 0.04f), 0.65f, sz * (ld / 2 - 0.04f)));
            uint[] cols = { 0x8a6a3f, 0xb98a50, 0x4a5a3a, Orange, 0x6b78b8, Lavender };
            for (int lvl = 0; lvl < 3; lvl++)
            {
                float y = 0.12f + lvl * 0.5f;
                Box(t, "plank", lw, 0.05f, ld, Steelm, new Vector3(0, y, 0));
                int n = rng.Next(2, 4);
                for (int i = 0; i < n; i++)
                {
                    float w = rng.Next(40, 70) / 100f * lw / n;
                    Box(t, "box", w, 0.2f + (float)rng.NextDouble() * 0.16f, ld * 0.7f, Std(cols[rng.Next(cols.Length)], 0.8f, 0.05f),
                        new Vector3(-lw / 2 + (i + 0.5f) * lw / n, y + 0.2f, 0));
                }
            }
            Box(t, "plankTop", lw, 0.04f, ld, Steelm, new Vector3(0, 1.32f, 0));
        }

        public static void FuelPump(Transform t, float lw, float ld)
        {
            Box(t, "base", lw, 0.1f, ld, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw * 0.7f, 1.25f, ld * 0.7f, Std(0xf2f0ff, 0.5f, 0.1f), new Vector3(0, 0.72f, 0));
            Box(t, "band", lw * 0.72f, 0.12f, ld * 0.72f, Orangem, new Vector3(0, 1.0f, 0));
            Box(t, "top", lw * 0.74f, 0.08f, ld * 0.74f, Navym, new Vector3(0, 1.4f, 0));
            Box(t, "screen", lw * 0.4f, 0.2f, 0.02f, Emit(0x40ff80, 1.5f), new Vector3(0, 1.2f, -ld * 0.35f - 0.01f));
            // ด้ามจ่าย + สายยางโค้ง
            Box(t, "holster", 0.12f, 0.22f, 0.1f, Navym, new Vector3(lw * 0.28f, 0.75f, -ld * 0.35f - 0.05f));
            Cyl(t, "nozzle", 0.025f, 0.3f, Std(Red, 0.5f, 0.2f), new Vector3(lw * 0.28f, 0.88f, -ld * 0.35f - 0.07f), 8, Quaternion.Euler(30, 0, 0));
            for (int i = 0; i < 4; i++)
                Cyl(t, "hose", 0.03f, 0.22f, Std(0x2a2d38, 0.6f), new Vector3(lw * 0.28f + 0.03f * i, 0.55f - i * 0.1f, -ld * 0.35f - 0.1f - i * 0.04f), 8, Quaternion.Euler(60 - i * 15, 0, 0));
        }

        public static void CargoCart(Transform t, float lw, float ld)
        {
            Box(t, "platform", lw, 0.1f, ld, Navym, new Vector3(0, 0.3f, 0));
            Box(t, "stripe", lw + 0.02f, 0.04f, 0.03f, Std(Hazard, 0.6f), new Vector3(0, 0.3f, -ld / 2 - 0.005f));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f }) Caster(t, sx * lw * 0.4f, sz * ld * 0.4f, 0.1f);
            // ที่จับด้านหลัง (+z)
            foreach (float sx in new[] { -1f, 1f }) Box(t, "handlePost", 0.05f, 0.75f, 0.05f, Steelm, new Vector3(sx * lw * 0.4f, 0.72f, ld / 2 - 0.04f));
            Box(t, "handle", lw * 0.84f, 0.05f, 0.05f, Steelm, new Vector3(0, 1.08f, ld / 2 - 0.04f));
            // กล่องสินค้า 3 ใบ + สายรัด
            Box(t, "boxA", lw * 0.5f, 0.4f, ld * 0.55f, Std(0x8a6a3f, 0.85f), new Vector3(-lw * 0.22f, 0.55f, -ld * 0.08f));
            Box(t, "boxB", lw * 0.4f, 0.3f, ld * 0.5f, Std(0xb98a50, 0.85f), new Vector3(lw * 0.24f, 0.5f, -ld * 0.1f));
            Box(t, "boxC", lw * 0.36f, 0.3f, ld * 0.4f, Std(Orange, 0.8f), new Vector3(-lw * 0.22f, 0.9f, -ld * 0.08f));
            Box(t, "strap", lw * 0.52f, 0.04f, ld * 0.56f, Std(0x3a2a1a, 0.9f), new Vector3(-lw * 0.22f, 0.76f, -ld * 0.08f));
        }

        // ════════════════ COMMUNICATIONS ════════════════

        public static void Antenna(Transform t, float r)
        {
            Cyl(t, "base", r, 0.1f, Navym, new Vector3(0, 0.05f, 0), 16);
            Cyl(t, "mast", 0.06f, 2.6f, Steelm, new Vector3(0, 1.4f, 0), 10);
            for (int lvl = 0; lvl < 3; lvl++)
            {
                float y = 0.8f + lvl * 0.6f;
                float len = 0.8f - lvl * 0.22f;
                Box(t, "arm", len, 0.04f, 0.04f, Std(Steel, 0.35f, 0.6f), new Vector3(0, y, 0));
                Box(t, "armB", 0.04f, 0.04f, len, Std(Steel, 0.35f, 0.6f), new Vector3(0, y + 0.1f, 0));
                foreach (float s in new[] { -1f, 1f }) Ball(t, "tip", 0.045f, Orangem, new Vector3(s * len / 2, y, 0), null, 8);
            }
            Ball(t, "beacon", 0.08f, Std(0xff3b30, 0.3f, 0f, 0xff3b30, 2.4f), new Vector3(0, 2.75f, 0), null, 10);
            for (int i = 0; i < 3; i++)
            {
                float a = i / 3f * Mathf.PI * 2f;
                Box(t, "stay", 0.03f, 1.0f, 0.03f, Std(Dark, 0.6f), new Vector3(Mathf.Cos(a) * 0.3f, 0.55f, Mathf.Sin(a) * 0.3f)).transform.localRotation =
                    Quaternion.Euler(Mathf.Sin(a) * 18f, 0, -Mathf.Cos(a) * 18f);
            }
        }

        public static void CommsDesk(Transform t, float lw, float ld)
        {
            Box(t, "top", lw, 0.07f, ld, Std(0x8a97b3, 0.5f, 0.4f), new Vector3(0, 0.82f, 0));
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box(t, "leg", 0.07f, 0.8f, 0.07f, Navym, new Vector3(sx * (lw / 2 - 0.07f), 0.4f, sz * (ld / 2 - 0.07f)));
            Box(t, "monitor", lw * 0.45f, 0.34f, 0.05f, Navym, new Vector3(0, 1.1f, ld * 0.2f));
            ReactorProps.Display(t, "screen", lw * 0.4f, 0.28f, ReactorProps.WaveTexture(), new Vector3(0, 1.1f, ld * 0.2f - 0.03f));
            Cyl(t, "stand", 0.03f, 0.2f, Steelm, new Vector3(0, 0.93f, ld * 0.2f), 8);
            Box(t, "keyboard", lw * 0.4f, 0.03f, 0.14f, Std(Slate, 0.5f), new Vector3(0, 0.875f, -ld * 0.15f));
            // หูฟัง + ไมค์
            Cyl(t, "headBand", 0.1f, 0.025f, Navym, new Vector3(lw * 0.36f, 0.9f, 0), 14, Quaternion.Euler(90, 0, 0));
            foreach (float s in new[] { -1f, 1f }) Cyl(t, "ear", 0.05f, 0.04f, Orangem, new Vector3(lw * 0.36f + s * 0.1f, 0.9f, 0), 10, Quaternion.Euler(0, 0, 90));
            Cyl(t, "micPole", 0.012f, 0.28f, Steelm, new Vector3(-lw * 0.36f, 0.98f, 0.05f), 8);
            Ball(t, "mic", 0.04f, Std(Dark, 0.5f), new Vector3(-lw * 0.36f, 1.13f, 0.05f), null, 8);
        }

        // ════════════════ SHIELDS ════════════════

        public static void ShieldGenerator(Transform t, float r)
        {
            PropFactory.Part(t, "base", MeshBuilder.Cylinder(r * 0.85f, r, 0.25f, 6), Navym, new Vector3(0, 0.125f, 0));
            PropFactory.Part(t, "col", MeshBuilder.Cylinder(r * 0.42f, r * 0.55f, 1.4f, 6), Std(Lavender, 0.4f, 0.3f), new Vector3(0, 0.95f, 0));
            for (int i = 0; i < 3; i++)
                Cyl(t, "glow", r * 0.62f, 0.07f, Std(0x1b2a3c, 0.3f, 0f, Cyan, 2.0f), new Vector3(0, 0.45f + i * 0.4f, 0), 6, null, false);
            PropFactory.Part(t, "crystal", MeshBuilder.Octahedron(0.22f), Std(0xc8f4ff, 0.15f, 0f, Cyan, 1.8f), new Vector3(0, 1.95f, 0), false);
            Cyl(t, "cap", r * 0.5f, 0.08f, Navym, new Vector3(0, 1.68f, 0), 6);
        }

        public static void CapacitorBank(Transform t, float lw, float ld)
        {
            Box(t, "tray", lw, 0.12f, ld, Navym, new Vector3(0, 0.06f, 0));
            Box(t, "stripe", lw + 0.02f, 0.04f, 0.03f, Std(Hazard, 0.6f), new Vector3(0, 0.1f, -ld / 2 - 0.005f));
            int n = Mathf.Max(2, Mathf.RoundToInt(lw / 0.5f));
            for (int i = 0; i < n; i++)
            {
                float x = -lw / 2 + (i + 0.5f) * lw / n;
                Cyl(t, "cell", 0.2f, 0.95f, Std(0xdad8f0, 0.35f, 0.4f), new Vector3(x, 0.6f, 0), 16);
                Cyl(t, "ring", 0.208f, 0.08f, Std(0x1b2a3c, 0.3f, 0f, Cyan, 1.8f), new Vector3(x, 0.75f, 0), 16, null, false);
                Cyl(t, "ring2", 0.208f, 0.05f, Std(0x1b2a3c, 0.3f, 0f, Cyan, 1.2f), new Vector3(x, 0.5f, 0), 16, null, false);
                Cyl(t, "cap", 0.2f, 0.06f, Navym, new Vector3(x, 1.1f, 0), 16);
                Cyl(t, "term", 0.05f, 0.1f, Steelm, new Vector3(x, 1.18f, 0), 8);
            }
        }

        public static void HoloEmitter(Transform t, float r)
        {
            Cyl(t, "disc", r, 0.07f, Navym, new Vector3(0, 0.035f, 0), 24);
            Cyl(t, "ring", r * 0.8f, 0.03f, Std(0x1b2a3c, 0.3f, 0f, Cyan, 2.0f), new Vector3(0, 0.08f, 0), 24, null, false);
            var dome = PropFactory.Part(t, "dome", MeshBuilder.Sphere(r * 1.2f, 22, 12, Mathf.PI / 2f, true), Std(0x9fe0ff, 0.1f, 0f, Cyan, 0.8f, 0.3f), new Vector3(0, 0.08f, 0), false);
            Ball(t, "core", 0.1f, Std(0xffffff, 0.3f, 0f, 0xffffff, 2.5f), new Vector3(0, 0.55f, 0), null, 10);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Box(t, "beam", 0.02f, 0.5f, 0.02f, Std(Cyan, 0.3f, 0f, Cyan, 1.6f, 0.6f), new Vector3(Mathf.Cos(a) * r * 0.7f, 0.35f, Mathf.Sin(a) * r * 0.7f));
            }
        }
    }
}
