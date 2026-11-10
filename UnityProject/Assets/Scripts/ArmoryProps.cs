using UnityEngine;
using static SkeldMap.MedicalProps;

namespace SkeldMap
{
    // อุปกรณ์ห้อง WEAPONS แบบ "Armory" (คลังอาวุธ) — สร้างด้วยโค้ดโทนมะกอก/น้ำตาล/ส้ม ตามภาพอ้างอิง
    // กรอบอ้างอิงเหมือน MedicalProps: ด้านหน้า = −z ของตัวเอง · lw ตามแกน x · ld ตามแกน z
    public static class ArmoryProps
    {
        const uint Olive = 0x4a5a3a, OliveDark = 0x2f3a27, Khaki = 0x6a5a3a, PanelDark = 0x3b4030, Gun = 0x2b2f38, Wood = 0x6a4a2a;
        const uint Hazard = 0xf4c430;

        static Material Navym => Std(Navy, 0.5f, 0.3f);
        static Material Steelm => Std(Steel, 0.35f, 0.6f);
        static Material Glow(uint c, float i = 2f) => Std(0x161a26, 0.4f, 0f, c, i);

        // ───────────── ปืนไรเฟิล 1 กระบอก (ยาวตามแกน x, ปากกระบอกชี้ +x) ─────────────
        static void Rifle(Transform p, Vector3 pos)
        {
            var dark = Std(Gun, 0.45f, 0.5f);
            Box(p, "stock", 0.30f, 0.10f, 0.05f, Std(Wood, 0.7f, 0f), pos + new Vector3(-0.40f, -0.01f, 0));
            Box(p, "body", 0.46f, 0.08f, 0.06f, dark, pos + new Vector3(-0.05f, 0, 0));
            Cyl(p, "barrel", 0.018f, 0.52f, Steelm, pos + new Vector3(0.42f, 0.01f, 0), 8, Quaternion.Euler(0, 0, 90));
            Box(p, "mag", 0.05f, 0.15f, 0.04f, dark, pos + new Vector3(-0.02f, -0.10f, 0), -10f);
            Box(p, "grip", 0.04f, 0.10f, 0.04f, dark, pos + new Vector3(-0.19f, -0.08f, 0), 15f);
            Box(p, "scope", 0.14f, 0.035f, 0.035f, dark, pos + new Vector3(-0.05f, 0.06f, 0));
            Box(p, "handguard", 0.2f, 0.06f, 0.05f, Std(Wood, 0.7f, 0f), pos + new Vector3(0.14f, -0.005f, 0));
        }

        // ───────────── แผงแขวนปืน (ติดผนัง) + ไฟส้มด้านบน ─────────────
        public static void GunRack(Transform t, float lw, float ld)
        {
            float zb = ld / 2 - 0.05f;
            Box(t, "panel", lw, 1.05f, 0.08f, Std(PanelDark, 0.8f, 0.1f), new Vector3(0, 1.0f, zb));
            Box(t, "frameTop", lw + 0.1f, 0.09f, 0.14f, Navym, new Vector3(0, 1.55f, zb - 0.02f));
            Box(t, "frameBottom", lw + 0.1f, 0.09f, 0.14f, Navym, new Vector3(0, 0.45f, zb - 0.02f));
            foreach (float s in new[] { -1f, 1f }) Box(t, "post", 0.09f, 1.2f, 0.14f, Navym, new Vector3(s * (lw / 2 + 0.02f), 1.0f, zb - 0.02f));

            // แถวปืน 2 แถว × 3 กระบอก
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 3; i++)
                {
                    float x = -lw * 0.3f + i * lw * 0.3f;
                    Rifle(t, new Vector3(x, 1.2f - row * 0.45f, zb - 0.09f));
                    Box(t, "hook", 0.04f, 0.04f, 0.05f, Steelm, new Vector3(x - 0.15f, 1.2f - row * 0.45f + 0.06f, zb - 0.05f));
                }

            // ไฟส้มเหนือแผง (เหมือนไฟเพดานสีส้มในภาพ) + แสงอุ่นส่องลงบนพื้น
            Box(t, "lampHousing", lw * 0.56f, 0.12f, 0.12f, Navym, new Vector3(0, 1.72f, zb - 0.03f));
            Box(t, "lamp", lw * 0.5f, 0.07f, 0.09f, Glow(0xffa030, 2.6f), new Vector3(0, 1.7f, zb - 0.08f));
            var l = new GameObject("lampLight").transform;
            l.SetParent(t, false);
            l.localPosition = new Vector3(0, 1.5f, -0.3f);
            var light = l.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Mats.Hex(0xffa030);
            light.intensity = 1.2f;
            light.range = 6f;
            light.shadows = LightShadows.None;
        }

        // ───────────── หีบกระสุนใหญ่ ─────────────
        public static void AmmoChest(Transform t, float lw, float ld)
        {
            var olive = Std(Olive, 0.75f, 0.15f);
            Box(t, "body", lw, 0.62f, ld, olive, new Vector3(0, 0.33f, 0));
            Box(t, "lid", lw + 0.04f, 0.14f, ld + 0.04f, Std(OliveDark, 0.75f, 0.2f), new Vector3(0, 0.7f, 0));
            for (int i = -1; i <= 1; i++) Box(t, "strap", 0.07f, 0.72f, ld + 0.06f, Steelm, new Vector3(i * lw * 0.3f, 0.36f, 0));
            foreach (float s in new[] { -1f, 1f }) Box(t, "latch", 0.1f, 0.1f, 0.03f, Std(Orange, 0.5f, 0.2f), new Vector3(s * lw * 0.3f, 0.58f, -ld / 2 - 0.035f));
            Box(t, "stencil", lw * 0.4f, 0.14f, 0.012f, Std(Hazard, 0.7f), new Vector3(0, 0.36f, -ld / 2 - 0.008f));
            Box(t, "handle", 0.34f, 0.04f, 0.05f, Steelm, new Vector3(0, 0.81f, 0));
            // กระป๋องกระสุนวางบนหีบ
            Box(t, "can", 0.5f, 0.28f, 0.34f, Std(Khaki, 0.75f, 0.2f), new Vector3(-lw * 0.22f, 0.95f, 0.05f));
            Box(t, "canLabel", 0.2f, 0.07f, 0.01f, Std(Orange, 0.6f), new Vector3(-lw * 0.22f, 0.96f, -0.125f));
            Box(t, "canHandle", 0.22f, 0.03f, 0.04f, Steelm, new Vector3(-lw * 0.22f, 1.12f, 0.05f));
        }

        // ───────────── ตู้เก็บกระสุนทรงสูง (หน้าต่างกระจกเห็นกล่องกระสุนข้างใน) ─────────────
        public static void AmmoLocker(Transform t, float lw, float ld)
        {
            Box(t, "plinth", lw + 0.04f, 0.1f, ld + 0.04f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw, 1.75f, ld, Std(0x3d4a5c, 0.5f, 0.35f), new Vector3(0, 0.98f, 0));
            Box(t, "cap", lw + 0.04f, 0.06f, ld + 0.04f, Navym, new Vector3(0, 1.88f, 0));
            Box(t, "stripe", lw * 0.96f, 0.07f, 0.02f, Std(Orange, 0.6f), new Vector3(0, 1.7f, -ld / 2 - 0.005f));
            // ชั้นวางกล่องกระสุนด้านใน (เห็นผ่านกระจก)
            uint[] boxes = { Khaki, Olive, 0xb98a50 };
            for (int r = 0; r < 3; r++)
            {
                float y = 0.75f + r * 0.3f;
                Box(t, "shelf", lw * 0.74f, 0.03f, ld * 0.5f, Steelm, new Vector3(0, y, -ld * 0.1f));
                for (int i = 0; i < 3; i++)
                    Box(t, "ammo", lw * 0.2f, 0.17f, ld * 0.36f, Std(boxes[(i + r) % 3], 0.8f, 0.1f), new Vector3(-lw * 0.24f + i * lw * 0.24f, y + 0.1f, -ld * 0.1f));
            }
            Box(t, "glass", lw * 0.78f, 1.0f, 0.02f, Std(0x9fe0ff, 0.1f, 0f, 0x3aa9d6, 0.25f, 0.28f), new Vector3(0, 1.1f, -ld / 2 - 0.02f));
            Box(t, "slot", lw * 0.6f, 0.14f, 0.03f, Std(0x2a2d38, 0.6f), new Vector3(0, 0.38f, -ld / 2 - 0.012f));
            Box(t, "handle", 0.03f, 0.3f, 0.04f, Steelm, new Vector3(lw * 0.42f, 1.0f, -ld / 2 - 0.045f));
        }

        // ───────────── แท่นกระสุนทรงกระบอกสูง ─────────────
        public static void AmmoTower(Transform t, float r)
        {
            PropFactory.Part(t, "base", MeshBuilder.Cylinder(r * 0.9f, r, 0.18f, 8), Navym, new Vector3(0, 0.09f, 0));
            Cyl(t, "body", r * 0.75f, 1.7f, Std(Olive, 0.5f, 0.3f), new Vector3(0, 1.0f, 0), 18);
            foreach (float y in new[] { 0.5f, 1.0f, 1.5f })
                Cyl(t, "band", r * 0.78f, 0.07f, Std(0x3a2a10, 0.5f, 0.2f, Orange, 0.8f), new Vector3(0, y, 0), 18, null, false);
            Box(t, "window", 0.07f, 0.8f, 0.03f, Glow(Cyan, 1.8f), new Vector3(0, 0.98f, -r * 0.75f - 0.01f));
            PropFactory.Part(t, "dome", MeshBuilder.Sphere(r * 0.75f, 16, 8, Mathf.PI / 2f), Std(OliveDark, 0.5f, 0.3f), new Vector3(0, 1.85f, 0));
        }

        // ───────────── ตู้เก็บอาวุธใหญ่ (ล็อกด้วยรหัส) ─────────────
        public static void WeaponLocker(Transform t, float lw, float ld)
        {
            Box(t, "plinth", lw + 0.04f, 0.1f, ld + 0.04f, Navym, new Vector3(0, 0.05f, 0));
            Box(t, "body", lw, 1.55f, ld, Std(PanelDark, 0.6f, 0.3f), new Vector3(0, 0.88f, 0));
            Box(t, "top", lw + 0.05f, 0.06f, ld + 0.05f, Navym, new Vector3(0, 1.69f, 0));
            foreach (float s in new[] { -1f, 1f })
            {
                Box(t, "door", lw * 0.44f, 1.35f, 0.03f, Std(Olive, 0.6f, 0.25f), new Vector3(s * lw * 0.24f, 0.88f, -ld / 2 - 0.015f));
                Box(t, "doorStripe", lw * 0.38f, 0.06f, 0.012f, Std(Orange, 0.6f), new Vector3(s * lw * 0.24f, 1.35f, -ld / 2 - 0.03f));
                Box(t, "doorStripe2", lw * 0.38f, 0.06f, 0.012f, Std(Orange, 0.6f), new Vector3(s * lw * 0.24f, 0.45f, -ld / 2 - 0.03f));
            }
            Box(t, "keypad", 0.16f, 0.26f, 0.03f, Navym, new Vector3(0, 0.95f, -ld / 2 - 0.035f));
            Box(t, "keypadScreen", 0.1f, 0.08f, 0.012f, Glow(Cyan, 1.8f), new Vector3(0, 1.02f, -ld / 2 - 0.055f));
            Box(t, "warning", 0.16f, 0.16f, 0.012f, Glow(Hazard, 1.4f), new Vector3(0, 1.5f, -ld / 2 - 0.02f)).transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        // ───────────── ลังอาวุธ (ลังใหญ่ + ลังเล็กซ้อนบน) ─────────────
        public static void WeaponCrate(Transform t, float lw, float ld)
        {
            var olive = Std(0x55663a, 0.8f, 0.1f);
            Box(t, "body", lw, 0.7f, ld, olive, new Vector3(0, 0.36f, 0));
            Box(t, "lid", lw + 0.03f, 0.06f, ld + 0.03f, Std(OliveDark, 0.8f, 0.2f), new Vector3(0, 0.73f, 0));
            foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                Box(t, "corner", 0.08f, 0.72f, 0.08f, Steelm, new Vector3(sx * (lw / 2), 0.36f, sz * (ld / 2)));
            Box(t, "stencil", lw * 0.4f, 0.13f, 0.012f, Std(Hazard, 0.7f), new Vector3(0, 0.4f, -ld / 2 - 0.008f));
            foreach (float s in new[] { -1f, 1f }) Box(t, "latch", 0.09f, 0.09f, 0.03f, Std(Orange, 0.5f, 0.2f), new Vector3(s * lw * 0.32f, 0.66f, -ld / 2 - 0.03f));
            PropFactory.Part(t, "top", MeshBuilder.Box(lw * 0.66f, 0.5f, ld * 0.66f), Std(Khaki, 0.8f, 0.1f), new Vector3(lw * 0.08f, 1.04f, 0), true, Quaternion.Euler(0, 10f, 0));
            PropFactory.Part(t, "topLid", MeshBuilder.Box(lw * 0.68f, 0.05f, ld * 0.68f), Std(OliveDark, 0.8f, 0.2f), new Vector3(lw * 0.08f, 1.3f, 0), true, Quaternion.Euler(0, 10f, 0));
        }

        // ───────────── แผงควบคุมประตู (หน้าจอสีฟ้า ติดผนัง) ─────────────
        public static void AccessPanel(Transform t, float lw, float ld)
        {
            float zb = ld / 2 - 0.06f;
            Box(t, "frame", lw, 0.9f, 0.12f, Navym, new Vector3(0, 1.0f, zb));
            Box(t, "screen", lw * 0.78f, 0.5f, 0.02f, Glow(0x39d0ff, 1.7f), new Vector3(0, 1.12f, zb - 0.07f));
            for (int i = 0; i < 3; i++) Box(t, "key", 0.07f, 0.05f, 0.02f, Std(Orange, 0.5f, 0f, Orange, 1.2f), new Vector3(-lw * 0.25f + i * lw * 0.25f, 0.68f, zb - 0.07f));
        }

        // ───────────── แถบไฟสีฟ้าติดผนัง (ยาวตามแกน x ของตัวเอง) ─────────────
        public static void LightStrip(Transform t, float length)
        {
            Box(t, "housing", length + 0.1f, 0.14f, 0.1f, Navym, new Vector3(0, 1.0f, 0.04f));
            Box(t, "strip", length, 0.08f, 0.06f, Glow(Cyan, 2.4f), new Vector3(0, 1.0f, -0.01f));
        }

        // ───────────── ขีดประเหลืองบนพื้น: มุมกรอบสี่เหลี่ยม + (เลือกได้) ขีดประตามขอบล่าง ─────────────
        public static void HazardZone(Transform t, float w, float d, bool bottomLine)
        {
            var mb = new MeshBuilder();
            const float dash = 0.42f, gap = 0.24f, width = 0.12f, leg = 1.6f;

            void Dashes(Vector3 start, Vector3 dir, float length)
            {
                for (float s = 0; s < length; s += dash + gap)
                {
                    float len = Mathf.Min(dash, length - s);
                    Vector3 c = start + dir * (s + len / 2f);
                    Vector3 size = dir.x != 0 ? new Vector3(len, 0.012f, width) : new Vector3(width, 0.012f, len);
                    mb.AddBox(c, size);
                }
            }

            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    Vector3 corner = new Vector3(sx * w / 2, 0.012f, sz * d / 2);
                    Dashes(corner, new Vector3(-sx, 0, 0), leg);   // ขาแนวนอนชี้เข้าหาศูนย์กลาง
                    Dashes(corner, new Vector3(0, 0, -sz), leg);   // ขาแนวตั้งชี้เข้าหาศูนย์กลาง
                }
            if (bottomLine)
                Dashes(new Vector3(-w / 2 + leg + 0.6f, 0.012f, -d / 2), Vector3.right, w - 2f * (leg + 0.6f));

            PropFactory.Part(t, "dashes", mb.ToMesh("hazardZone"), Std(Hazard, 0.8f, 0f, Hazard, 0.35f), Vector3.zero, false);
        }
    }
}
