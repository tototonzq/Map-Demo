using UnityEngine;

namespace SkeldMap
{
    // ของตกแต่งแต่ละชนิด (port จาก propBuilders ใน js/main.js)
    // ทุกชิ้นสร้างเป็น GameObject ลูกใต้ root ของตัวเอง · z ภายในตัวชิ้น (เช่น เก้าอี้โต๊ะ) ถูกกลับเครื่องหมายตามข้อตกลง world = (x, y, -z)
    public static class PropFactory
    {
        // ───── ตัวช่วย (เทียบเท่า std / cyl / box ใน JS) ─────
        internal static Material Std(uint color, float rough = 0.6f, float metal = 0.2f, uint emissive = 0, float emissiveI = 1f, float opacity = 1f)
            => Mats.Lit(color, rough, metal, emissive, emissiveI, null, opacity);

        internal static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos,
                               bool shadows = true, Quaternion? rot = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            return go;
        }

        static void Cyl(Transform p, string n, float rt, float rb, float h, Material m, Vector3 pos, int seg = 24, bool shadows = true)
            => Part(p, n, MeshBuilder.Cylinder(rt, rb, h, seg), m, pos, shadows);

        static void Box(Transform p, string n, float w, float h, float d, Material m, Vector3 pos)
            => Part(p, n, MeshBuilder.Box(w, h, d), m, pos);

        // ───── entry point ─────
        public static GameObject Build(PropDef p, Transform parent)
        {
            var g = new GameObject($"Prop_{p.kind}");
            g.transform.SetParent(parent, false);
            g.transform.position = MapData.ToWorld(p.x, 0, p.z);

            // rot = หมุนรอบแกน Y (องศา, ตามแบบ Unity) · 0 = หันหน้าเข้าหากล้องเริ่มต้น · 90 = หันไป −x · 270 = หันไป +x · 180 = หันขึ้นบนจอ
            // w/d ใน PropDef คือขนาดรอยเท้า "ในโลก" (ใช้คำนวณการชน) → แปลงกลับเป็นขนาดในกรอบของตัวชิ้นเอง (สลับเมื่อหมุน 90/270)
            float rot = p.rot ?? 0f;
            g.transform.rotation = Quaternion.Euler(0f, rot, 0f);
            bool swap = Mathf.Abs(Mathf.Repeat(rot, 180f) - 90f) < 1f;
            float lw = swap ? (p.d ?? 0f) : (p.w ?? 0f);
            float ld = swap ? (p.w ?? 0f) : (p.d ?? 0f);

            switch (p.kind)
            {
                case "engine": Engine(g.transform, p); break;
                case "reactor": Reactor(g.transform, p); break;
                case "table": Table(g.transform, p); break;
                case "crate": Crate(g.transform, p); break;
                case "console": Console(g.transform, p); break;
                case "bed": MedicalProps.Bed(g.transform, lw, ld); break;
                case "iv": MedicalProps.IvStand(g.transform); break;
                case "monitor": MedicalProps.Monitor(g.transform, lw, ld); break;
                case "cabinet": MedicalProps.Cabinet(g.transform, lw, ld); break;
                case "medcart": MedicalProps.Cart(g.transform, lw, ld); break;
                case "scanner": MedicalProps.Scanner(g.transform, lw, ld); break;
                case "aligner": TaskProps.Aligner(g.transform, lw, ld); break;
                case "datakiosk": TaskProps.DataKiosk(g.transform, lw, ld, p.button); break;   // button: true = อัปโหลด (ลูกศรขึ้น)
                case "wirepanel": TaskProps.WirePanel(g.transform, lw, ld); break;
                case "shieldpanel": TaskProps.ShieldPanel(g.transform, lw, ld); break;
                case "wheelchair": ModelProp.Spawn("wheelchair", g.transform, Vector3.zero, 180f); break;
                case "crutch": ModelProp.Spawn("aid-crutch", g.transform, Vector3.zero, 0f, 2.2f, 14f); break;
                // ── ห้อง REACTOR · h ของชิ้นตกแต่งที่ไม่มีรอยเท้า (hazardring/pipe) ใช้เป็นรัศมี/ความยาว
                case "reactorpanel": ReactorProps.Panel(g.transform, lw, ld); break;
                case "pressure": ReactorProps.PressureTank(g.transform, lw, ld); break;
                case "generator": ReactorProps.Generator(g.transform, lw, ld); break;
                case "barrel": ReactorProps.Barrel(g.transform, p.r.Value); break;
                case "hazardring": ReactorProps.HazardRing(g.transform, p.h ?? 3f); break;
                case "pipe": ReactorProps.Pipe(g.transform, p.h ?? 4f); break;
                case "beacon": ReactorProps.Beacon(g.transform); break;
                case "cables": ModelProp.Spawn("SpaceKit/cables", g.transform, new Vector3(0, 0.012f, 0), 0f, 0.55f); break;
                // ── ห้องอื่น ๆ (FacilityProps) · seed ของตู้เซิร์ฟเวอร์/ชั้นวางมาจากตำแหน่ง ทำให้แต่ละชิ้นหน้าตาต่างกัน
                case "workbench": FacilityProps.Workbench(g.transform, lw, ld); break;
                case "fuelcell": FacilityProps.FuelCells(g.transform); break;
                case "vending": FacilityProps.VendingMachine(g.transform, lw, ld); break;
                case "counter": FacilityProps.ServingCounter(g.transform, lw, ld); break;
                case "watercooler": FacilityProps.WaterCooler(g.transform); break;
                case "bin": FacilityProps.Bin(g.transform); break;
                case "missilerack": FacilityProps.MissileRack(g.transform, lw, ld); break;
                case "radarstation": FacilityProps.RadarStation(g.transform, lw, ld); break;
                case "locker": FacilityProps.Lockers(g.transform, lw, ld); break;
                case "monitorwall": FacilityProps.MonitorWall(g.transform, lw, ld); break;
                case "officechair": FacilityProps.OfficeChair(g.transform); break;
                case "cardreader": FacilityProps.CardReader(g.transform); break;
                case "airfilter": FacilityProps.AirFilter(g.transform, lw, ld); break;
                case "hydroponic": FacilityProps.Hydroponic(g.transform, lw, ld); break;
                case "holotable": FacilityProps.HoloTable(g.transform, p.r.Value); break;
                case "pilotchair": FacilityProps.PilotChair(g.transform); break;
                case "starboard": FacilityProps.WallScreen(g.transform, lw, ld, TexLib.StarMap(), 0x6b78b8); break;
                case "adminboard": FacilityProps.WallScreen(g.transform, lw, ld, TexLib.Dashboard(), 0xf4a53a); break;
                case "signalboard": FacilityProps.WallScreen(g.transform, lw, ld, ReactorProps.WaveTexture(), 0xc08cff); break;
                case "filecabinet": FacilityProps.FileCabinets(g.transform, lw, ld); break;
                case "printer": FacilityProps.Printer(g.transform, lw, ld); break;
                case "serverrack": FacilityProps.ServerRack(g.transform, lw, ld, Mathf.RoundToInt(p.x * 7f + p.z * 3f)); break;
                case "breaker": FacilityProps.BreakerPanel(g.transform, lw, ld, p.button); break;
                case "transformer": FacilityProps.Transformer(g.transform, lw, ld); break;
                case "cablereel": FacilityProps.CableReel(g.transform, p.r.Value); break;
                case "shelf": FacilityProps.Shelf(g.transform, lw, ld, Mathf.RoundToInt(p.x * 5f + p.z * 11f)); break;
                case "fuelpump": FacilityProps.FuelPump(g.transform, lw, ld); break;
                case "cargocart": FacilityProps.CargoCart(g.transform, lw, ld); break;
                case "antenna": FacilityProps.Antenna(g.transform, p.r.Value); break;
                case "commsdesk": FacilityProps.CommsDesk(g.transform, lw, ld); break;
                case "shieldgen": FacilityProps.ShieldGenerator(g.transform, p.r.Value); break;
                case "capacitor": FacilityProps.CapacitorBank(g.transform, lw, ld); break;
                case "holoemitter": FacilityProps.HoloEmitter(g.transform, p.r.Value); break;
                // ── ห้อง WEAPONS แบบ Armory (ArmoryProps)
                case "gunrack": ArmoryProps.GunRack(g.transform, lw, ld); break;
                case "ammochest": ArmoryProps.AmmoChest(g.transform, lw, ld); break;
                case "ammolocker": ArmoryProps.AmmoLocker(g.transform, lw, ld); break;
                case "ammotower": ArmoryProps.AmmoTower(g.transform, p.r.Value); break;
                case "weaponlocker": ArmoryProps.WeaponLocker(g.transform, lw, ld); break;
                case "weaponcrate": ArmoryProps.WeaponCrate(g.transform, lw, ld); break;
                case "accesspanel": ArmoryProps.AccessPanel(g.transform, lw, ld); break;
                case "lightstrip": ArmoryProps.LightStrip(g.transform, p.h ?? 2f); break;
                case "hazardzone": ArmoryProps.HazardZone(g.transform, p.sx ?? 6f, p.sz ?? 4f, p.button); break;
                case "tank": Tank(g.transform, p); break;
                case "plant": Plant(g.transform, p); break;
                case "dish": Dish(g.transform, p); break;
                case "turret": Turret(g.transform, p); break;
                default:
                    Object.Destroy(g);
                    return null;
            }
            return g;
        }

        static void Engine(Transform t, PropDef p)
        {
            float r = p.r.Value;
            Cyl(t, "base", r, r * 1.1f, 2.2f, Std(0x59647a, metal: 0.5f), new Vector3(0, 1.1f, 0));
            Cyl(t, "ring", r * 1.12f, r * 1.12f, 0.12f, Std(0x1a2a3c, emissive: 0x2fb4ff, emissiveI: 1.6f), new Vector3(0, 1.5f, 0));
            Cyl(t, "top", r * 0.8f, r, 0.3f, Std(0x8a97b3, metal: 0.4f), new Vector3(0, 2.3f, 0));
        }

        static void Reactor(Transform t, PropDef p)
        {
            float r = p.r.Value;
            Cyl(t, "outer", r, r * 1.1f, 1.2f, Std(0x3b3f4c, metal: 0.6f), new Vector3(0, 0.6f, 0));
            Cyl(t, "core", r * 0.55f, r * 0.55f, 2.6f, Std(0xff6a2a, rough: 0.3f, emissive: 0xff4a10, emissiveI: 2.2f), new Vector3(0, 1.5f, 0));
            Cyl(t, "cap", r * 0.7f, r * 0.7f, 0.25f, Std(0xff7a3a, emissive: 0xff5a1a, emissiveI: 1.4f), new Vector3(0, 2.9f, 0));

            // รายละเอียดเพิ่ม: แท่งเชื้อเพลิงเรืองแสงรอบแกน 8 แท่ง + แถบรัด 2 เส้น + ท่อหล่อเย็นต่อออกจากฐาน
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Cyl(t, "rod" + i, 0.1f, 0.1f, 1.6f, Std(0xffb070, rough: 0.3f, emissive: 0xff8a40, emissiveI: 1.8f),
                    new Vector3(Mathf.Cos(a) * r * 0.64f, 2.0f, Mathf.Sin(a) * r * 0.64f), 10);
            }
            foreach (float by in new[] { 1.55f, 2.45f })
                Cyl(t, "band", r * 0.74f, r * 0.74f, 0.09f, Std(0x2a2d38, metal: 0.5f), new Vector3(0, by, 0), 28);
            foreach (float sz in new[] { -0.7f, 0.7f })
            {
                // ปลายท่อยื่นพ้นฐานแค่ ~0.15 (ฐาน r×1.1 = 2.42) เพื่อไม่ให้ซ้อนทับพื้นที่ที่ผู้เล่นเดินได้ (การชนยังเป็นวงกลม r−0.15 เดิม)
                Part(t, "conduit", MeshBuilder.Cylinder(0.2f, 0.2f, 1.2f, 14), Std(0x59647a, metal: 0.5f),
                     new Vector3(r * 1.1f - 0.45f, 0.55f, sz), true, Quaternion.Euler(0, 0, 90));
                Cyl(t, "conduitFlange", 0.27f, 0.27f, 0.1f, Std(0x3b3f4c, metal: 0.6f), new Vector3(r * 1.1f + 0.05f, 0.55f, sz), 14);
            }

            // แสงส้มของแกนเครื่องปฏิกรณ์ (PointLight 30 / ระยะ 14 ใน three.js → ปรับสเกลให้เข้ากับ Unity)
            var l = new GameObject("coreLight");
            l.transform.SetParent(t, false);
            l.transform.localPosition = new Vector3(0, 2.2f, 0);
            var light = l.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Mats.Hex(0xff6a2a);
            light.intensity = 3.2f;
            light.range = 14f;
            light.shadows = LightShadows.None;
        }

        static void Table(Transform t, PropDef p)
        {
            float r = p.r.Value;
            Cyl(t, "stem", 0.28f, 0.4f, 0.7f, Std(0x5a6478), new Vector3(0, 0.35f, 0));
            Cyl(t, "top", r, r, 0.1f, Std(0xd5dbe6, rough: 0.4f), new Vector3(0, 0.75f, 0), 40);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Cyl(t, "stool" + i, 0.26f, 0.26f, 0.4f, Std(0x4c8fd6),
                    new Vector3(Mathf.Cos(a) * (r + 0.55f), 0.2f, -Mathf.Sin(a) * (r + 0.55f)), 16);
            }
            if (p.button)
            {
                Cyl(t, "buttonPad", 0.55f, 0.55f, 0.05f, Std(0x303644), new Vector3(0, 0.82f, 0), 24);
                Cyl(t, "button", 0.35f, 0.4f, 0.18f, Std(0xc01818, emissive: 0xff2020, emissiveI: 0.9f), new Vector3(0, 0.88f, 0), 24);
            }
        }

        static void Crate(Transform t, PropDef p)
        {
            float w = p.w.Value, d = p.d.Value;
            // JS: p.h ?? 1.0 + ((p.x * 7 + p.z * 3) % 5) * 0.12
            float h = p.h ?? 1.0f + ((p.x * 7f + p.z * 3f) % 5f) * 0.12f;
            Box(t, "crate", w, h, d, Std(p.color ?? 0x8a6a3f, rough: 0.8f, metal: 0.05f), new Vector3(0, h / 2, 0));
            Box(t, "strap", w + 0.02f, h * 0.18f, d + 0.02f, Std(0x4a3a24, rough: 0.9f), new Vector3(0, h * 0.5f, 0));
        }

        static void Console(Transform t, PropDef p)
        {
            float w = p.w.Value, d = p.d.Value;
            float h = p.h ?? 0.95f;
            Box(t, "base", w, h, d, Std(0x2f394d, metal: 0.4f), new Vector3(0, h / 2, 0));

            // จอเรืองแสง: MeshStandardMaterial ค่าเริ่มต้นของ three (rough 1, metal 0), ไม่เกิดเงา
            var screen = Mats.Lit(0x111111, 1f, 0f, p.screen ?? 0x55e0ff, 1.3f);
            Part(t, "screen", MeshBuilder.Box(Mathf.Max(w - 0.2f, 0.3f), 0.06f, Mathf.Max(d - 0.2f, 0.3f)),
                 screen, new Vector3(0, h + 0.03f, 0), shadows: false);
        }

        static void Tank(Transform t, PropDef p)
        {
            float r = p.r.Value, h = p.h.Value;
            Cyl(t, "glass", r, r, h, Std(p.color ?? 0xffffff, rough: 0.1f, emissive: p.glow ?? 0, emissiveI: 0.9f, opacity: 0.55f),
                new Vector3(0, h / 2 + 0.2f, 0), 28, shadows: false);
            Cyl(t, "base", r * 1.15f, r * 1.15f, 0.2f, Std(0x8a97b3, metal: 0.4f), new Vector3(0, 0.1f, 0), 28);
            Cyl(t, "cap", r * 1.15f, r * 1.15f, 0.2f, Std(0x8a97b3, metal: 0.4f), new Vector3(0, h + 0.3f, 0), 28);
        }

        static void Plant(Transform t, PropDef p)
        {
            Cyl(t, "pot", 0.4f, 0.3f, 0.5f, Std(0xb5683c, rough: 0.9f), new Vector3(0, 0.25f, 0), 16);
            Part(t, "bush", MeshBuilder.Icosahedron(0.62f), Std(0x3fae5a, rough: 0.9f), new Vector3(0, 0.95f, 0));
        }

        static void Dish(Transform t, PropDef p)
        {
            float r = p.r.Value;
            Cyl(t, "stand", 0.18f, 0.3f, 1.0f, Std(0x4a5368), new Vector3(0, 0.5f, 0));
            // three: rotation.x = π + 0.5 · z ถูกกลับ จึงเป็น π - 0.5 ใน Unity (ปากจานเงยขึ้นและเอียงมาทางผู้เล่นเหมือนเดิม)
            var bowl = Mats.Lit(0xd9def0, 0.35f, 0.5f);
            Part(t, "bowl", MeshBuilder.Sphere(r * 1.5f, 24, 12, Mathf.PI / 2.6f, doubleSided: true), bowl,
                 new Vector3(0, 1.9f, 0), true, Quaternion.Euler((Mathf.PI - 0.5f) * Mathf.Rad2Deg, 0, 0));
        }

        static void Turret(Transform t, PropDef p)
        {
            float r = p.r.Value;
            Cyl(t, "base", r, r * 1.1f, 0.5f, Std(0x3b4254, metal: 0.6f), new Vector3(0, 0.25f, 0));
            Part(t, "dome", MeshBuilder.Sphere(r * 0.7f, 20, 14), Std(0x7a869f, rough: 0.35f, metal: 0.6f), new Vector3(0, 0.85f, 0));
            Box(t, "barrel", 2.4f, 0.28f, 0.28f, Std(0x2b3040, metal: 0.7f), new Vector3(1.4f, 1.0f, 0));
        }

        // ช่องระบายอากาศ
        public static GameObject Vent(Vector2 pos, Transform parent)
        {
            var g = new GameObject("Vent");
            g.transform.SetParent(parent, false);
            g.transform.position = MapData.ToWorld(pos.x, 0, pos.y);
            Box(g.transform, "frame", 1.3f, 0.05f, 1.3f, Std(0x4a5470, metal: 0.5f), new Vector3(0, 0.05f, 0));
            for (int i = -2; i <= 2; i++)
                Box(g.transform, "slat" + i, 1.1f, 0.05f, 0.1f, Std(0x161a26), new Vector3(0, 0.09f, i * 0.22f));
            return g;
        }
    }
}
