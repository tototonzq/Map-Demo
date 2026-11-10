using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // ───────────────────────────────────────────────────────────────────────────
    // มินิเกมประจำภารกิจ (แทนการยืนกด F ค้าง) · กด F/Space ใกล้จุดเหลืองเพื่อเปิด · Esc = ปิดโดยไม่สำเร็จ
    // วาด/รับเมาส์ด้วย IMGUI ล้วน (ทำงานได้ทั้ง Input Manager เก่าและ Input System) · พิกัดในเกมเป็นพิกัดท้องถิ่น 0..W × 0..H
    // ───────────────────────────────────────────────────────────────────────────
    public abstract class MiniGame
    {
        protected TaskGames C;
        protected static readonly Color Green = Mats.Hex(0x4cff8a), Yellow = Mats.Hex(0xffd23f), Red = Mats.Hex(0xff4a4a);
        public bool Done;
        public float DoneT;
        public void Bind(TaskGames c) { C = c; }
        public abstract string Title { get; }
        public abstract string Help { get; }
        public virtual void Tick(float dt) { }
        public abstract void Draw();
        protected void Finish() { if (Done) return; Done = true; DoneT = 0f; C.Blip(1.6f); }
    }

    public sealed class TaskGames
    {
        public const float W = 700f, H = 380f;       // พื้นที่เล่นของมินิเกม
        static readonly Color Cyan = Mats.Hex(0x55e0ff), Yellow = Mats.Hex(0xffd23f), Green = Mats.Hex(0x4cff8a), Red = Mats.Hex(0xff4a4a);

        readonly SkeldGame g;
        MiniGame cur;
        TaskState curTask;
        Font font;
        GUIStyle st;

        public Vector2 Pos;                // เมาส์ (พิกัดท้องถิ่นของมินิเกม)
        public bool Down, Up, Held;        // กดลง/ปล่อย (เฉพาะอีเวนต์นั้น) / กำลังกดค้าง
        public float T => Time.unscaledTime;
        public bool Open => cur != null;

        public TaskGames(SkeldGame game) { g = game; }

        // ───── สร้างเกมตามชนิดภารกิจ ─────
        public void Begin(TaskState t)
        {
            MiniGame m;
            switch (t.def.id)
            {
                case "alignUpper": case "alignLower": m = new AlignGame(); break;
                case "scan": m = new ScanGame(); break;
                case "download": case "comms": m = new TransferGame(false); break;
                case "upload": m = new TransferGame(true); break;
                case "asteroids": m = new AsteroidsGame(); break;
                case "reactor": m = new SimonGame(); break;
                case "swipe": m = new SwipeGame(); break;
                case "filter": m = new LeavesGame(); break;
                case "course": m = new CourseGame(); break;
                case "wiring": m = new WiringGame(); break;
                case "fuel": m = new FuelGame(); break;
                case "shields": m = new ShieldsGame(); break;
                default: g.FinishTask(t); return;
            }
            m.Bind(this);
            cur = m; curTask = t; Held = false;
            Blip(1.0f);
        }

        public void Update(float dt)
        {
            if (cur == null) return;
            if (GameInput.Pressed(KeyCode.Escape)) { cur = null; curTask = null; return; }
            cur.Tick(dt);
            if (cur.Done)
            {
                cur.DoneT += dt;
                if (cur.DoneT > 1.1f) { var t = curTask; cur = null; curTask = null; g.FinishTask(t); }
            }
        }

        public void Close() { cur = null; curTask = null; }

        public void Blip(float pitch = 1f) => g.Blip(pitch, 0.45f);
        public void Buzz() => g.Blip(0.42f, 0.55f);

        // ───── วาดทับทั้งจอ ─────
        public void OnGUI()
        {
            if (cur == null) return;
            var e = Event.current;
            EnsureStyle();

            float s = GameSettings.GuiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float VW = Screen.width / s, VH = Screen.height / s;
            var box = new Rect((VW - 760f) / 2f, (VH - 520f) / 2f, 760f, 520f);
            var inner = new Rect(box.x + 30f, box.y + 78f, W, H);
            var close = new Rect(box.xMax - 44f, box.y + 12f, 32f, 32f);

            Vector2 mp = e.mousePosition;
            bool leftDown = e.type == EventType.MouseDown && e.button == 0;
            bool leftUp = e.type == EventType.MouseUp && e.button == 0;
            if (leftDown && close.Contains(mp)) { cur = null; curTask = null; GUI.matrix = Matrix4x4.identity; e.Use(); return; }
            Pos = mp - inner.position;
            Down = leftDown && !cur.Done; Up = leftUp && !cur.Done;
            if (leftDown) Held = true;
            if (leftUp) Held = false;

            if (e.type == EventType.Repaint)
            {
                Fill(new Rect(0, 0, VW, VH), new Color(0, 0, 0, 0.62f));
                Fill(box, new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.96f), 14f);
                Ring(box, new Color(1f, 0.82f, 0.25f, 0.55f), 14f);
                Fill(new Rect(box.x + 24, box.y + 64, box.width - 48, 2), new Color(1, 1, 1, 0.12f));
                Txt(new Rect(box.x + 30, box.y + 14, box.width - 100, 40), cur.Title, 22, Yellow, TextAnchor.MiddleLeft, true);
                Fill(close, new Color(1, 1, 1, close.Contains(mp) ? 0.22f : 0.1f), 8f);
                Txt(close, "X", 15, Color.white, TextAnchor.MiddleCenter, true);
                Txt(new Rect(box.x + 30, box.yMax - 46, box.width - 60, 30), cur.Help + "     (Esc = ปิด)", 13, new Color(1, 1, 1, 0.65f), TextAnchor.MiddleLeft);
            }

            GUI.BeginGroup(inner);
            cur.Draw();
            GUI.EndGroup();

            if (e.type == EventType.Repaint && cur.Done)
            {
                float a = Mathf.Clamp01(cur.DoneT * 4f);
                Fill(inner, new Color(0.05f, 0.25f, 0.12f, 0.55f * a), 10f);
                Txt(inner, "สำเร็จ!", 46, new Color(Green.r, Green.g, Green.b, a), TextAnchor.MiddleCenter, true);
            }

            if (e.isMouse && mp.x >= box.x && mp.x <= box.xMax && mp.y >= box.y && mp.y <= box.yMax) e.Use();
            GUI.matrix = Matrix4x4.identity;
        }

        void EnsureStyle()
        {
            if (st != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Thonburi", "Segoe UI", "Arial" }, 16);
            st = new GUIStyle(GUI.skin.label) { font = font, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
        }

        // ───── ตัววาด (ใช้ในเกมต่าง ๆ) ─────
        static bool Painting => Event.current.type == EventType.Repaint;

        public static void Fill(Rect r, Color c, float rad = 0f)
        {
            if (!Painting) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, Vector4.one * rad);
        }

        public static void Ring(Rect r, Color c, float rad, float thick = 2f)
        {
            if (!Painting) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.one * thick, Vector4.one * rad);
        }

        public static void Disc(Vector2 c, float r, Color col)
        {
            if (!Painting) return;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, col, Vector4.zero, Vector4.one * r);
        }

        public static void DiscRing(Vector2 c, float r, float thick, Color col)
        {
            if (!Painting) return;
            GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, col, Vector4.one * thick, Vector4.one * r);
        }

        public static void Line(Vector2 a, Vector2 b, float w, Color c)
        {
            if (!Painting) return;
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
            GUI.DrawTexture(new Rect(a.x, a.y - w / 2f, len, w), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, Vector4.one * (w / 2f));
            GUI.matrix = m;
        }

        public static void FillRot(Vector2 c, float w, float h, float deg, Color col, float rad = 0f)
        {
            if (!Painting) return;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(deg, c);
            GUI.DrawTexture(new Rect(c.x - w / 2f, c.y - h / 2f, w, h), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, col, Vector4.zero, Vector4.one * rad);
            GUI.matrix = m;
        }

        public void Txt(Rect r, string s, int size, Color col, TextAnchor a = TextAnchor.MiddleCenter, bool bold = false)
        {
            if (!Painting || st == null) return;
            st.fontSize = size; st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            st.normal.textColor = col;
            GUI.Label(r, s, st);
        }

        // ปุ่มกด (คืน true ตอนคลิกลงบนปุ่ม)
        public bool Button(Rect r, string label, Color col, int size = 16)
        {
            bool hover = r.Contains(Pos);
            Fill(r, hover ? Color.Lerp(col, Color.white, 0.18f) : col, 10f);
            Txt(r, label, size, new Color(0.05f, 0.07f, 0.12f), TextAnchor.MiddleCenter, true);
            return Down && hover;
        }

        public static Vector2 Closest(Vector2 a, Vector2 b, Vector2 p, out float t)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            t = l2 < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            return a + ab * t;
        }
    }

    // ═══════════════════════ 1) Align Engine Output ═══════════════════════
    // ลากแถบเลื่อนให้ลำไอพ่นตรงแนวนอน (กึ่งกลาง) แล้วปล่อย · 2 รอบ
    sealed class AlignGame : MiniGame
    {
        float val; int stage; bool drag; float flash;
        public override string Title => "Align Engine Output";
        public override string Help => "ลากปุ่มเลื่อนให้ลำพลังงานตรงเส้นกึ่งกลาง แล้วปล่อยเมาส์";
        public AlignGame() { val = RandomAway(); }
        static float RandomAway() { float v = Random.Range(0.12f, 0.35f); return Random.value < 0.5f ? v : 1f - v; }
        Rect Track => new Rect(560, 30, 16, 320);
        public override void Tick(float dt) { flash = Mathf.Max(0f, flash - dt); }

        public override void Draw()
        {
            var tr = Track;
            Vector2 handle = new Vector2(tr.center.x, tr.y + val * tr.height);
            if (Done) { drag = false; val = Mathf.MoveTowards(val, 0.5f, 0.02f); }
            if (C.Down && (C.Pos - handle).magnitude < 28f) drag = true;
            if (drag && C.Held) val = Mathf.Clamp01((C.Pos.y - tr.y) / tr.height);
            if (drag && C.Up)
            {
                drag = false;
                if (Mathf.Abs(val - 0.5f) <= 0.035f)
                {
                    val = 0.5f; stage++; C.Blip(1.2f);
                    if (stage >= 2) Finish(); else val = RandomAway();
                }
                else { C.Buzz(); flash = 0.4f; }
            }

            handle = new Vector2(tr.center.x, tr.y + val * tr.height);
            float off = Mathf.Abs(val - 0.5f);
            Color beam = Color.Lerp(Green, Red, Mathf.Clamp01(off * 5f));
            Vector2 nozzle = new Vector2(250, 190);
            float ang = (val - 0.5f) * 70f;

            TaskGames.Fill(new Rect(60, 120, 190, 140), new Color(0.2f, 0.24f, 0.32f), 18f);
            TaskGames.Fill(new Rect(80, 140, 150, 100), new Color(0.12f, 0.15f, 0.22f), 12f);
            for (int i = 0; i < 4; i++) TaskGames.Fill(new Rect(95 + i * 34, 150, 14, 80), new Color(0.3f, 0.4f, 0.55f), 4f);
            TaskGames.Disc(nozzle, 18, new Color(0.35f, 0.4f, 0.5f));
            TaskGames.Line(new Vector2(262, 190), new Vector2(520, 190), 2f, new Color(1, 1, 1, 0.25f));
            TaskGames.Line(nozzle, nozzle + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * 290f, 12f, new Color(beam.r, beam.g, beam.b, 0.85f));

            TaskGames.Fill(tr, new Color(1, 1, 1, 0.12f), 8f);
            TaskGames.Fill(new Rect(tr.x - 14, tr.y + tr.height * 0.5f - tr.height * 0.035f, tr.width + 28, tr.height * 0.07f), new Color(Green.r, Green.g, Green.b, 0.4f), 4f);
            TaskGames.Disc(handle, 18, flash > 0f ? Red : (drag ? Yellow : new Color(0.9f, 0.92f, 1f)));
            TaskGames.DiscRing(handle, 18, 3, new Color(0, 0, 0, 0.5f));
            C.Txt(new Rect(0, 5, 330, 24), $"รอบที่ {Mathf.Min(stage + 1, 2)}/2", 15, Color.white, TextAnchor.MiddleLeft, true);
        }
    }

    // ═══════════════════════ 2) Submit Scan ═══════════════════════
    // กดเมาส์ค้างที่ปุ่มระหว่างสแกน · ปล่อยเมื่อไรแถบถอยกลับ
    sealed class ScanGame : MiniGame
    {
        float p; bool pressing;
        public override string Title => "Submit Scan";
        public override string Help => "กดเมาส์ค้างที่ปุ่ม \"สแกน\" จนกว่าจะครบ 100% (ปล่อยแล้วความคืบหน้าจะลดลง)";
        public override void Tick(float dt)
        {
            if (Done) return;
            if (pressing && C.Held) p += dt / 4.5f; else { pressing = false; p = Mathf.Max(0f, p - dt * 0.7f); }
            if (p >= 1f) { p = 1f; Finish(); }
        }

        public override void Draw()
        {
            var btn = new Rect(250, 318, 200, 48);
            if (C.Down && btn.Contains(C.Pos)) { pressing = true; C.Blip(0.9f); }

            Vector2 c = new Vector2(350, 150);
            TaskGames.Fill(new Rect(250, 8, 200, 296), new Color(0.05f, 0.15f, 0.2f), 14f);
            TaskGames.Ring(new Rect(250, 8, 200, 296), Cyan(0.5f), 14f);
            TaskGames.Disc(new Vector2(350, 70), 34, new Color(0.25f, 0.65f, 0.85f, 0.85f));
            TaskGames.Fill(new Rect(290, 108, 120, 190), new Color(0.25f, 0.65f, 0.85f, 0.85f), 40f);
            float y = 10f + p * 292f;
            TaskGames.Fill(new Rect(252, y - 2, 196, 5), Cyan(0.95f));
            TaskGames.Fill(new Rect(252, 10, 196, Mathf.Max(0, y - 10)), Cyan(0.12f));

            string[] k = { "ชีพจร", "อุณหภูมิ", "ออกซิเจน", "ความดัน" };
            string[] v = { $"{72 + (int)(Mathf.Sin(C.T * 6f) * 4f)} bpm", "36.6 C", $"{97 + (int)(Mathf.Sin(C.T * 3f) * 1.5f)} %", "118/76" };
            for (int i = 0; i < 4; i++)
            {
                bool shown = p > (i + 1) * 0.2f;
                C.Txt(new Rect(40, 40 + i * 44, 180, 22), k[i], 14, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleLeft);
                C.Txt(new Rect(40, 60 + i * 44, 180, 24), shown ? v[i] : "...", 18, shown ? Green : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft, true);
            }
            C.Txt(new Rect(480, 140, 200, 40), $"{Mathf.RoundToInt(p * 100)}%", 40, Color.white, TextAnchor.MiddleCenter, true);

            Color bc = pressing ? Yellow : new Color(0.95f, 0.95f, 1f);
            C.Button(btn, pressing ? "กำลังสแกน..." : "กดค้างเพื่อสแกน", bc);
        }
        static Color Cyan(float a) { var c = Mats.Hex(0x55e0ff); return new Color(c.r, c.g, c.b, a); }
    }

    // ═══════════════════════ 3) Download / Upload Data ═══════════════════════
    // กดเริ่ม → ไฟล์ไหลระหว่างเครื่อง → กลางทางการเชื่อมต่อหลุดต้องกด "เชื่อมต่อใหม่"
    sealed class TransferGame : MiniGame
    {
        readonly bool upload;
        int state;                // 0 รอ · 1 กำลังโอน · 2 หลุด
        float p; readonly float dropAt = Random.Range(0.3f, 0.7f); bool dropped;
        public TransferGame(bool up) { upload = up; }
        public override string Title => upload ? "Upload Data" : "Download Data";
        public override string Help => upload ? "กด \"อัปโหลด\" และคอยกด \"เชื่อมต่อใหม่\" เมื่อสัญญาณหลุด" : "กด \"ดาวน์โหลด\" และคอยกด \"เชื่อมต่อใหม่\" เมื่อสัญญาณหลุด";
        public override void Tick(float dt)
        {
            if (Done || state != 1) return;
            p += dt / 5.5f;
            if (!dropped && p >= dropAt) { dropped = true; state = 2; C.Buzz(); }
            if (p >= 1f) { p = 1f; Finish(); }
        }

        public override void Draw()
        {
            Rect left = new Rect(30, 70, 170, 150), right = new Rect(500, 70, 170, 150);
            TaskGames.Fill(left, new Color(0.16f, 0.2f, 0.3f), 14f);
            TaskGames.Fill(right, new Color(0.16f, 0.2f, 0.3f), 14f);
            TaskGames.Fill(new Rect(left.x + 14, left.y + 14, left.width - 28, left.height - 54), new Color(0.05f, 0.12f, 0.2f), 8f);
            TaskGames.Fill(new Rect(right.x + 14, right.y + 14, right.width - 28, right.height - 54), new Color(0.05f, 0.12f, 0.2f), 8f);
            C.Txt(new Rect(left.x, left.yMax - 36, left.width, 30), upload ? "แท็บเล็ต" : "เซิร์ฟเวอร์", 15, Color.white, TextAnchor.MiddleCenter, true);
            C.Txt(new Rect(right.x, right.yMax - 36, right.width, 30), upload ? "เซิร์ฟเวอร์" : "แท็บเล็ต", 15, Color.white, TextAnchor.MiddleCenter, true);

            float yy = 145f;
            TaskGames.Line(new Vector2(205, yy), new Vector2(495, yy), 4f, new Color(1, 1, 1, state == 2 ? 0.1f : 0.25f));
            if (state == 1 && !Done)
            {
                for (int i = 0; i < 5; i++)
                {
                    float f = Mathf.Repeat(C.T * 0.7f + i / 5f, 1f);
                    if (upload) f = 1f - f;           // อัปโหลดไหลไปซ้าย → ขวา ตามที่เลือก (ดาวน์โหลดสวนทาง)
                    float x = Mathf.Lerp(495f, 205f, f);
                    TaskGames.Fill(new Rect(x - 11, yy - 14, 22, 28), Yellow, 3f);
                    TaskGames.Fill(new Rect(x - 7, yy - 8, 14, 3), new Color(0.2f, 0.15f, 0f), 1f);
                }
            }
            if (state == 2) C.Txt(new Rect(205, yy - 18, 290, 36), "การเชื่อมต่อหลุด!", 22, Red, TextAnchor.MiddleCenter, true);

            var bar = new Rect(60, 270, 580, 26);
            TaskGames.Fill(bar, new Color(1, 1, 1, 0.12f), 13f);
            if (p > 0.005f) TaskGames.Fill(new Rect(bar.x, bar.y, Mathf.Max(26f, bar.width * p), bar.height), state == 2 ? Red : Green, 13f);
            C.Txt(new Rect(60, 298, 580, 28), $"{Mathf.RoundToInt(p * 100)}%", 18, Color.white, TextAnchor.MiddleCenter, true);

            var btn = new Rect(250, 330, 200, 44);
            if (state == 0 && C.Button(btn, upload ? "อัปโหลด" : "ดาวน์โหลด", Yellow)) { state = 1; C.Blip(1.1f); }
            else if (state == 2 && C.Button(btn, "เชื่อมต่อใหม่", Cyan())) { state = 1; C.Blip(1.3f); }
        }
        static Color Cyan() => Mats.Hex(0x55e0ff);
    }

    // ═══════════════════════ 4) Clear Asteroids ═══════════════════════
    sealed class AsteroidsGame : MiniGame
    {
        sealed class Rock { public Vector2 pos, vel; public float r, rot, spin, boom = -1f; }
        readonly List<Rock> rocks = new List<Rock>();
        readonly List<Vector2> stars = new List<Vector2>();
        int hits; float spawnT; float missFlash;
        const int Goal = 15;
        public override string Title => "Clear Asteroids";
        public override string Help => "คลิกยิงอุกกาบาตให้ครบ " + Goal + " ก้อน";

        public AsteroidsGame()
        {
            for (int i = 0; i < 60; i++) stars.Add(new Vector2(Random.value * TaskGames.W, Random.value * TaskGames.H));
            for (int i = 0; i < 4; i++) Spawn(true);
        }

        void Spawn(bool anywhere)
        {
            var r = new Rock { r = Random.Range(20f, 36f), rot = Random.value * 360f, spin = Random.Range(-90f, 90f) };
            r.pos = new Vector2(anywhere ? Random.Range(120f, TaskGames.W - 40f) : TaskGames.W + 40f, Random.Range(40f, TaskGames.H - 40f));
            r.vel = new Vector2(-Random.Range(60f, 130f), Random.Range(-30f, 30f));
            rocks.Add(r);
        }

        public override void Tick(float dt)
        {
            missFlash = Mathf.Max(0f, missFlash - dt);
            spawnT -= dt;
            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                var r = rocks[i];
                if (r.boom >= 0f) { r.boom += dt; if (r.boom > 0.4f) rocks.RemoveAt(i); continue; }
                r.pos += r.vel * dt; r.rot += r.spin * dt;
                if (r.pos.y < 30f || r.pos.y > TaskGames.H - 30f) r.vel.y = -r.vel.y;
                if (r.pos.x < -50f) { rocks.RemoveAt(i); }
            }
            int alive = 0; foreach (var r in rocks) if (r.boom < 0f) alive++;
            if (!Done && alive < 6 && spawnT <= 0f) { Spawn(false); spawnT = 0.5f; }
        }

        public override void Draw()
        {
            TaskGames.Fill(new Rect(0, 0, TaskGames.W, TaskGames.H), new Color(0.02f, 0.03f, 0.08f), 10f);
            foreach (var s in stars) TaskGames.Fill(new Rect(s.x, s.y, 2, 2), new Color(1, 1, 1, 0.5f));

            if (C.Down)
            {
                bool hit = false;
                for (int i = rocks.Count - 1; i >= 0 && !hit; i--)
                {
                    var r = rocks[i];
                    if (r.boom >= 0f) continue;
                    if ((C.Pos - r.pos).magnitude < r.r + 10f) { r.boom = 0f; hits++; hit = true; C.Blip(0.8f + hits * 0.03f); }
                }
                if (!hit) missFlash = 0.15f;
                if (hits >= Goal) Finish();
            }

            foreach (var r in rocks)
            {
                if (r.boom >= 0f)
                {
                    float k = r.boom / 0.4f;
                    TaskGames.DiscRing(r.pos, r.r * (1f + k * 1.5f), 4f, new Color(1f, 0.7f, 0.2f, 1f - k));
                    TaskGames.Disc(r.pos, r.r * (1f - k) * 0.8f, new Color(1f, 0.9f, 0.5f, 1f - k));
                    continue;
                }
                TaskGames.Disc(r.pos, r.r, new Color(0.45f, 0.42f, 0.4f));
                TaskGames.Disc(r.pos + new Vector2(-r.r * 0.3f, -r.r * 0.25f), r.r * 0.3f, new Color(0.33f, 0.31f, 0.3f));
                TaskGames.Disc(r.pos + new Vector2(r.r * 0.35f, r.r * 0.3f), r.r * 0.22f, new Color(0.33f, 0.31f, 0.3f));
                TaskGames.DiscRing(r.pos, r.r, 2f, new Color(0.7f, 0.66f, 0.6f));
            }

            Color cc = missFlash > 0f ? Red : Green;
            TaskGames.DiscRing(C.Pos, 16f, 2f, cc);
            TaskGames.Line(C.Pos + new Vector2(-24, 0), C.Pos + new Vector2(-8, 0), 2f, cc);
            TaskGames.Line(C.Pos + new Vector2(8, 0), C.Pos + new Vector2(24, 0), 2f, cc);
            TaskGames.Line(C.Pos + new Vector2(0, -24), C.Pos + new Vector2(0, -8), 2f, cc);
            TaskGames.Line(C.Pos + new Vector2(0, 8), C.Pos + new Vector2(0, 24), 2f, cc);
            C.Txt(new Rect(14, 8, 300, 26), $"ทำลายแล้ว {hits}/{Goal}", 17, Color.white, TextAnchor.MiddleLeft, true);
        }
    }

    // ═══════════════════════ 5) Start Reactor ═══════════════════════
    // จำลำดับแสงบนปุ่ม 3×3 แล้วกดตาม · 5 รอบ ยาวขึ้นเรื่อย ๆ
    sealed class SimonGame : MiniGame
    {
        readonly int[] seq = new int[5];
        int stage = 1;            // จำนวนตัวที่ต้องจำ
        bool showing = true; int showIdx; float timer = 0.7f; bool lit;
        int inputIdx; int flashTile = -1; float flashT; float errT;
        public override string Title => "Start Reactor";
        public override string Help => "ดูลำดับไฟที่ติด แล้วกดตามลำดับเดิม (ทั้งหมด 5 รอบ)";

        public SimonGame()
        {
            int prev = -1;
            for (int i = 0; i < seq.Length; i++) { int t; do t = Random.Range(0, 9); while (t == prev); seq[i] = t; prev = t; }
        }

        public override void Tick(float dt)
        {
            flashT = Mathf.Max(0f, flashT - dt); errT = Mathf.Max(0f, errT - dt);
            if (Done || !showing) return;
            timer -= dt;
            if (timer > 0f) return;
            if (!lit) { lit = true; timer = 0.5f; C.Blip(0.8f + seq[showIdx] * 0.08f); }
            else
            {
                lit = false; timer = 0.25f; showIdx++;
                if (showIdx >= stage) { showing = false; inputIdx = 0; }
            }
        }

        Rect Tile(int i) => new Rect(203 + (i % 3) * 102, 43 + (i / 3) * 102, 90, 90);

        public override void Draw()
        {
            for (int i = 0; i < 9; i++)
            {
                bool on = (showing && lit && showIdx < stage && seq[showIdx] == i) || (flashTile == i && flashT > 0f);
                var r = Tile(i);
                TaskGames.Fill(r, on ? Cyan() : (errT > 0f ? new Color(0.45f, 0.12f, 0.12f) : new Color(0.14f, 0.2f, 0.3f)), 14f);
                if (!showing && !Done && r.Contains(C.Pos)) TaskGames.Ring(r, new Color(1, 1, 1, 0.5f), 14f);
                if (on) TaskGames.Ring(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.6f, 0.95f, 1f, 0.6f), 18f, 3f);
            }
            for (int i = 0; i < 5; i++)
                TaskGames.Disc(new Vector2(70, 80 + i * 44), 13, i < stage - 1 ? Green : (i == stage - 1 ? Yellow : new Color(1, 1, 1, 0.15f)));
            C.Txt(new Rect(30, 330, 150, 26), $"รอบที่ {Mathf.Min(stage, 5)}/5", 15, Color.white, TextAnchor.MiddleLeft, true);
            C.Txt(new Rect(500, 160, 200, 60), showing ? "จำลำดับ..." : "ถึงตาคุณ!", 22, showing ? Yellow : Green, TextAnchor.MiddleCenter, true);

            if (!showing && C.Down)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (!Tile(i).Contains(C.Pos)) continue;
                    flashTile = i; flashT = 0.25f;
                    if (i == seq[inputIdx])
                    {
                        C.Blip(0.8f + i * 0.08f);
                        inputIdx++;
                        if (inputIdx >= stage)
                        {
                            stage++;
                            if (stage > 5) Finish();
                            else { showing = true; showIdx = 0; lit = false; timer = 0.9f; }
                        }
                    }
                    else { C.Buzz(); errT = 0.5f; showing = true; showIdx = 0; lit = false; timer = 0.9f; }
                    break;
                }
            }
        }
        static Color Cyan() => Mats.Hex(0x55e0ff);
    }

    // ═══════════════════════ 6) Swipe Card ═══════════════════════
    // ลากบัตรผ่านเครื่องอ่าน · เร็วไป/ช้าไป = อ่านไม่ได้
    sealed class SwipeGame : MiniGame
    {
        float cardX = 40f; bool drag; float grab; float t0 = -1f;
        string msg = "รูดบัตรผ่านเครื่องอ่าน"; Color msgCol = Color.white;
        const float StartX = 40f, L = 250f, R = 470f, MaxX = 560f;
        public override string Title => "Swipe Card";
        public override string Help => "ลากบัตรไปทางขวาผ่านเครื่องอ่านด้วยความเร็วพอดี (ไม่เร็ว/ช้าเกินไป)";

        public override void Draw()
        {
            var reader = new Rect(240, 90, 230, 210);
            TaskGames.Fill(new Rect(0, 150, TaskGames.W, 90), new Color(0.12f, 0.15f, 0.22f), 12f);
            TaskGames.Fill(reader, new Color(0.22f, 0.27f, 0.38f), 18f);
            TaskGames.Fill(new Rect(reader.x + 14, reader.y + 14, reader.width - 28, 50), new Color(0.04f, 0.1f, 0.15f), 8f);
            C.Txt(new Rect(reader.x + 14, reader.y + 14, reader.width - 28, 50), msg, 14, msgCol, TextAnchor.MiddleCenter, true);
            TaskGames.Fill(new Rect(reader.x, 176, reader.width, 38), new Color(0.05f, 0.06f, 0.1f), 6f);

            var card = new Rect(cardX, 160, 130, 76);
            if (C.Down && card.Contains(C.Pos) && !Done) { drag = true; grab = C.Pos.x - cardX; t0 = -1f; }
            if (drag && C.Held && !Done)
            {
                cardX = Mathf.Clamp(C.Pos.x - grab, StartX, MaxX);
                if (cardX >= L && t0 < 0f) t0 = C.T;
                if (cardX < L) t0 = -1f;
                if (cardX >= R) Evaluate();
            }
            if (drag && C.Up) { if (cardX < R) { msg = "อ่านบัตรไม่สำเร็จ ลองใหม่"; msgCol = Red; C.Buzz(); cardX = StartX; t0 = -1f; } drag = false; }

            card = new Rect(cardX, 160, 130, 76);
            TaskGames.Fill(card, new Color(0.92f, 0.94f, 1f), 10f);
            TaskGames.Fill(new Rect(card.x, card.y + 14, card.width, 14), new Color(0.2f, 0.2f, 0.28f));
            TaskGames.Fill(new Rect(card.x + 10, card.y + 40, 40, 26), Yellow, 4f);
            TaskGames.Fill(new Rect(card.x + 60, card.y + 44, 56, 6), new Color(0.6f, 0.62f, 0.7f), 3f);
            TaskGames.Fill(new Rect(card.x + 60, card.y + 56, 40, 6), new Color(0.6f, 0.62f, 0.7f), 3f);
        }

        void Evaluate()
        {
            drag = false;
            float d = C.T - t0;
            if (d < 0.38f) { msg = "เร็วเกินไป!"; msgCol = Red; Fail(); }
            else if (d > 1.15f) { msg = "ช้าเกินไป!"; msgCol = Red; Fail(); }
            else { msg = "ยอมรับบัตรแล้ว"; msgCol = Green; Finish(); }
        }

        void Fail() { C.Buzz(); cardX = StartX; t0 = -1f; }
    }

    // ═══════════════════════ 7) Clean O2 Filter ═══════════════════════
    // ลากใบไม้ออกจากตัวกรองจนหมด
    sealed class LeavesGame : MiniGame
    {
        sealed class Leaf { public Vector2 pos; public float rot; public bool alive = true; public float fade = 1f; }
        readonly List<Leaf> leaves = new List<Leaf>();
        int drag = -1; Vector2 grab;
        static readonly Rect Housing = new Rect(170, 30, 360, 320);
        public override string Title => "Clean O2 Filter";
        public override string Help => "ลากใบไม้ออกนอกตัวกรองให้หมดทุกใบ";

        public LeavesGame()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 p;
                int guard = 0;
                do { p = new Vector2(Random.Range(Housing.x + 45, Housing.xMax - 45), Random.Range(Housing.y + 40, Housing.yMax - 40)); guard++; }
                while (guard < 30 && leaves.Exists(l => (l.pos - p).magnitude < 55f));
                leaves.Add(new Leaf { pos = p, rot = Random.Range(0f, 180f) });
            }
        }

        public override void Tick(float dt)
        {
            foreach (var l in leaves) if (!l.alive) l.fade = Mathf.Max(0f, l.fade - dt * 3f);
        }

        public override void Draw()
        {
            TaskGames.Fill(Housing, new Color(0.14f, 0.18f, 0.24f), 16f);
            for (int i = 0; i < 9; i++) TaskGames.Fill(new Rect(Housing.x + 12, Housing.y + 14 + i * 33, Housing.width - 24, 4), new Color(0.04f, 0.06f, 0.1f), 2f);
            TaskGames.Ring(Housing, new Color(0.55f, 0.65f, 0.8f), 16f, 3f);

            if (C.Down)
            {
                for (int i = leaves.Count - 1; i >= 0; i--)
                {
                    if (!leaves[i].alive) continue;
                    if ((C.Pos - leaves[i].pos).magnitude < 32f) { drag = i; grab = leaves[i].pos - C.Pos; break; }
                }
            }
            if (drag >= 0 && C.Held) leaves[drag].pos = C.Pos + grab;
            if (drag >= 0 && C.Up)
            {
                if (!Housing.Contains(leaves[drag].pos)) { leaves[drag].alive = false; C.Blip(1.0f); }
                drag = -1;
                int left = 0; foreach (var l in leaves) if (l.alive) left++;
                if (left == 0) Finish();
            }

            int cnt = 0;
            foreach (var l in leaves)
            {
                if (l.alive) cnt++;
                if (l.fade <= 0f) continue;
                float a = l.fade;
                var gcol = new Color(0.25f, 0.62f, 0.25f, a);
                TaskGames.FillRot(l.pos, 58f, 26f, l.rot, gcol, 13f);
                TaskGames.FillRot(l.pos, 46f, 3f, l.rot, new Color(0.12f, 0.35f, 0.14f, a), 1.5f);
            }
            C.Txt(new Rect(10, 8, 180, 26), $"เหลือ {cnt} ใบ", 16, Color.white, TextAnchor.MiddleLeft, true);
        }
    }

    // ═══════════════════════ 8) Chart Course ═══════════════════════
    // ลากยานไปตามเส้นทางจากต้นทางถึงดาวปลายทาง ห้ามออกนอกเส้น
    sealed class CourseGame : MiniGame
    {
        readonly Vector2[] pts;
        int seg; float t; bool drag; float errT;
        const float Tol = 36f;
        public override string Title => "Chart Course";
        public override string Help => "ลากยานตามเส้นทางไปถึงดาวปลายทาง อย่าให้เมาส์ออกนอกเส้น";

        public CourseGame()
        {
            Vector2[] b = { new Vector2(60, 300), new Vector2(170, 110), new Vector2(330, 270), new Vector2(480, 90), new Vector2(640, 200) };
            pts = new Vector2[b.Length];
            for (int i = 0; i < b.Length; i++)
                pts[i] = b[i] + (i == 0 || i == b.Length - 1 ? Vector2.zero : new Vector2(Random.Range(-20f, 20f), Random.Range(-25f, 25f)));
        }

        Vector2 ShipPos => Vector2.Lerp(pts[seg], pts[seg + 1], t);
        public override void Tick(float dt) { errT = Mathf.Max(0f, errT - dt); }

        public override void Draw()
        {
            int n = pts.Length - 1;
            if (Done) drag = false;
            if (C.Down && (C.Pos - ShipPos).magnitude < 34f) drag = true;
            if (drag && C.Held)
            {
                float bestScore = -1f; int bs = seg; float bt = t;
                for (int s = seg; s <= Mathf.Min(seg + 1, n - 1); s++)
                {
                    float tq; Vector2 q = TaskGames.Closest(pts[s], pts[s + 1], C.Pos, out tq);
                    if ((C.Pos - q).magnitude > Tol) continue;
                    float score = s + tq;
                    if (score >= seg + t - 0.02f && score > bestScore) { bestScore = score; bs = s; bt = tq; }
                }
                if (bestScore >= 0f) { seg = bs; t = bt; }
                else { drag = false; seg = 0; t = 0f; errT = 0.5f; C.Buzz(); }
                if (seg == n - 1 && t > 0.97f) { t = 1f; Finish(); }
            }
            if (drag && C.Up) drag = false;

            TaskGames.Fill(new Rect(0, 0, TaskGames.W, TaskGames.H), new Color(0.03f, 0.05f, 0.12f), 10f);
            for (int i = 0; i < 40; i++)
            {
                float sx = (i * 97.3f) % TaskGames.W, sy = (i * 53.7f) % TaskGames.H;
                TaskGames.Fill(new Rect(sx, sy, 2, 2), new Color(1, 1, 1, 0.35f));
            }
            for (int s = 0; s < n; s++)
            {
                bool passed = s < seg;
                TaskGames.Line(pts[s], pts[s + 1], 8f, passed ? new Color(Green.r, Green.g, Green.b, 0.5f) : new Color(1, 1, 1, 0.16f));
            }
            TaskGames.Line(pts[seg], ShipPos, 8f, new Color(Green.r, Green.g, Green.b, 0.6f));
            for (int i = 1; i < pts.Length - 1; i++) TaskGames.Disc(pts[i], 7, i <= seg ? Green : new Color(1, 1, 1, 0.4f));
            TaskGames.Disc(pts[n], 26, new Color(0.3f, 0.55f, 0.95f));
            TaskGames.Disc(pts[n] + new Vector2(-7, -7), 9, new Color(0.55f, 0.8f, 1f, 0.6f));
            TaskGames.DiscRing(pts[n], 34f + Mathf.Sin(C.T * 3f) * 3f, 2f, new Color(0.5f, 0.8f, 1f, 0.5f));

            Vector2 ship = ShipPos;
            Vector2 dir = (pts[seg + 1] - pts[seg]).normalized;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            TaskGames.FillRot(ship, 34f, 18f, ang, errT > 0f ? Red : (drag ? Yellow : Color.white), 9f);
            TaskGames.Disc(ship + dir * 8f, 5f, Cyan());
            if (errT > 0f) C.Txt(new Rect(0, 8, TaskGames.W, 30), "ออกนอกเส้นทาง! เริ่มใหม่", 18, Red, TextAnchor.MiddleCenter, true);
        }
        static Color Cyan() => Mats.Hex(0x55e0ff);
    }

    // ═══════════════════════ 9) Fix Wiring ═══════════════════════
    sealed class WiringGame : MiniGame
    {
        static readonly Color[] Cols = { new Color(1f, 0.25f, 0.25f), new Color(0.3f, 0.5f, 1f), new Color(1f, 0.85f, 0.2f), new Color(1f, 0.3f, 0.9f) };
        readonly int[] rightOrder = { 0, 1, 2, 3 };
        readonly bool[] done = new bool[4];
        int drag = -1;
        public override string Title => "Fix Wiring";
        public override string Help => "ลากสายจากฝั่งซ้ายไปต่อกับช่องสีเดียวกันฝั่งขวา";

        public WiringGame()
        {
            do
            {
                for (int i = 3; i > 0; i--) { int j = Random.Range(0, i + 1); int tmp = rightOrder[i]; rightOrder[i] = rightOrder[j]; rightOrder[j] = tmp; }
            } while (rightOrder[0] == 0 && rightOrder[1] == 1 && rightOrder[2] == 2 && rightOrder[3] == 3);
        }

        static Vector2 LeftP(int i) => new Vector2(90, 50 + i * 90);
        static Vector2 RightP(int j) => new Vector2(610, 50 + j * 90);

        public override void Draw()
        {
            TaskGames.Fill(new Rect(20, 10, 90, 360), new Color(0.2f, 0.22f, 0.3f), 10f);
            TaskGames.Fill(new Rect(590, 10, 90, 360), new Color(0.2f, 0.22f, 0.3f), 10f);

            if (C.Down)
                for (int i = 0; i < 4; i++)
                    if (!done[i] && (C.Pos - LeftP(i)).magnitude < 30f) drag = i;
            if (drag >= 0 && C.Up)
            {
                for (int j = 0; j < 4; j++)
                    if ((C.Pos - RightP(j)).magnitude < 34f)
                    {
                        if (rightOrder[j] == drag) { done[drag] = true; C.Blip(1.0f + 0.15f * drag); }
                        else C.Buzz();
                    }
                drag = -1;
                bool all = true; foreach (var d in done) all &= d;
                if (all) Finish();
            }

            for (int j = 0; j < 4; j++)
            {
                int ci = rightOrder[j];
                TaskGames.Fill(new Rect(RightP(j).x - 30, RightP(j).y - 16, 60, 32), Cols[ci], 6f);
                TaskGames.Disc(RightP(j) + new Vector2(70, 0), 6, done[ci] ? Green : new Color(1, 1, 1, 0.2f));
                if (done[ci]) TaskGames.Line(LeftP(ci), RightP(j), 12f, Cols[ci]);
            }
            for (int i = 0; i < 4; i++)
            {
                TaskGames.Fill(new Rect(LeftP(i).x - 30, LeftP(i).y - 16, 60, 32), Cols[i], 6f);
                TaskGames.Disc(LeftP(i) - new Vector2(70, 0) + new Vector2(0, 0), 6, done[i] ? Green : new Color(1, 1, 1, 0.2f));
            }
            if (drag >= 0 && C.Held)
            {
                TaskGames.Line(LeftP(drag), C.Pos, 12f, Cols[drag]);
                TaskGames.Disc(C.Pos, 12f, Cols[drag]);
            }
        }
    }

    // ═══════════════════════ 10) Fuel Engines ═══════════════════════
    // กดค้างเติมน้ำมันให้เข้าแถบเขียวแล้วปล่อย · ล้นแถบ = หก เริ่มใหม่ · 2 ถัง
    sealed class FuelGame : MiniGame
    {
        float level; bool pressing; int canister; float speed; string msg = ""; float msgT;
        const float ZoneLo = 0.85f, ZoneHi = 1.0f;
        public override string Title => "Fuel Engines";
        public override string Help => "กดค้างที่ปุ่มเติมน้ำมัน แล้วปล่อยเมื่อระดับอยู่ในแถบเขียว (ห้ามล้น) · 2 ถัง";
        public FuelGame() { speed = Random.Range(0.32f, 0.42f); }

        public override void Tick(float dt)
        {
            msgT = Mathf.Max(0f, msgT - dt);
            if (Done) return;
            if (pressing && C.Held)
            {
                level += dt * speed;
                if (level > ZoneHi) { level = 0f; pressing = false; msg = "น้ำมันล้น! เริ่มใหม่"; msgT = 1.2f; C.Buzz(); }
            }
            else { pressing = false; level = Mathf.Max(0f, level - dt * 0.6f); }
        }

        public override void Draw()
        {
            var tank = new Rect(300, 14, 100, 270);
            TaskGames.Fill(tank, new Color(1, 1, 1, 0.1f), 14f);
            float zy = tank.yMax - tank.height * ZoneHi, zh = tank.height * (ZoneHi - ZoneLo);
            TaskGames.Fill(new Rect(tank.x - 8, zy, tank.width + 16, zh), new Color(Green.r, Green.g, Green.b, 0.35f), 4f);
            float fh = tank.height * Mathf.Clamp01(level);
            if (fh > 2f) TaskGames.Fill(new Rect(tank.x + 6, tank.yMax - fh, tank.width - 12, fh - 4), new Color(1f, 0.6f, 0.15f), 10f);
            TaskGames.Ring(tank, new Color(0.7f, 0.78f, 0.9f), 14f, 3f);
            C.Txt(new Rect(410, zy - 4, 120, 24), "เป้าหมาย", 13, Green, TextAnchor.MiddleLeft, true);
            C.Txt(new Rect(0, 8, 250, 26), $"ถังที่ {Mathf.Min(canister + 1, 2)}/2", 17, Color.white, TextAnchor.MiddleLeft, true);
            C.Txt(new Rect(0, 150, 280, 60), $"{Mathf.RoundToInt(level * 100)}%", 34, Color.white, TextAnchor.MiddleCenter, true);
            if (msgT > 0f) C.Txt(new Rect(420, 120, 280, 40), msg, 18, Red, TextAnchor.MiddleLeft, true);

            var btn = new Rect(230, 316, 240, 52);
            if (C.Down && btn.Contains(C.Pos) && !Done) pressing = true;
            C.Button(btn, pressing ? "กำลังเติม..." : "กดค้างเพื่อเติมน้ำมัน", pressing ? Yellow : new Color(0.95f, 0.95f, 1f));

            if (pressing && C.Up)
            {
                pressing = false;
                if (level >= ZoneLo && level <= ZoneHi)
                {
                    canister++; C.Blip(1.2f); level = 0f; speed = Random.Range(0.36f, 0.48f);
                    if (canister >= 2) Finish();
                }
                else { msg = "ยังไม่ถึงแถบเขียว"; msgT = 1.0f; C.Buzz(); }
            }
        }
    }

    // ═══════════════════════ 11) Prime Shields ═══════════════════════
    // ปริศนาเปิดไฟ 7 ช่อง: คลิกช่องแล้วสลับสถานะช่องนั้น + ช่องข้างเคียง (สร้างจากการสุ่มคลิกย้อนกลับ ⇒ แก้ได้เสมอ)
    sealed class ShieldsGame : MiniGame
    {
        readonly bool[] on = new bool[7];
        public override string Title => "Prime Shields";
        public override string Help => "เปิดโล่ให้สว่างครบ 7 ช่อง · คลิกช่องใดจะสลับช่องนั้นกับช่องข้างเคียง";

        public ShieldsGame()
        {
            for (int i = 0; i < 7; i++) on[i] = true;
            var order = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            for (int i = order.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int tmp = order[i]; order[i] = order[j]; order[j] = tmp; }
            for (int k = 0; k < 4; k++) Toggle(order[k]);
            bool all = true; foreach (var b in on) all &= b;
            if (all) Toggle(order[0]);
        }

        // 0 = กลาง · 1..6 = วงนอก (วนรอบ)
        void Toggle(int i)
        {
            on[i] = !on[i];
            if (i == 0) { for (int k = 1; k <= 6; k++) on[k] = !on[k]; }
            else
            {
                on[0] = !on[0];
                int prev = i == 1 ? 6 : i - 1, next = i == 6 ? 1 : i + 1;
                on[prev] = !on[prev]; on[next] = !on[next];
            }
        }

        static Vector2 CellPos(int i)
        {
            if (i == 0) return new Vector2(350, 185);
            float a = (90f + (i - 1) * 60f) * Mathf.Deg2Rad;
            return new Vector2(350 + Mathf.Cos(a) * 112f, 185 - Mathf.Sin(a) * 112f);
        }

        public override void Draw()
        {
            if (C.Down && !Done)
                for (int i = 0; i < 7; i++)
                    if ((C.Pos - CellPos(i)).magnitude < 48f) { Toggle(i); C.Blip(0.9f + i * 0.05f); break; }

            bool all = true;
            foreach (var b in on) all &= b;
            if (all) Finish();

            // เส้นเชื่อมช่อง
            for (int i = 1; i <= 6; i++)
            {
                TaskGames.Line(CellPos(0), CellPos(i), 4f, new Color(1, 1, 1, 0.12f));
                TaskGames.Line(CellPos(i), CellPos(i == 6 ? 1 : i + 1), 4f, new Color(1, 1, 1, 0.12f));
            }
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = CellPos(i);
                bool hover = (C.Pos - p).magnitude < 48f && !Done;
                if (on[i])
                {
                    TaskGames.Disc(p, 54f, new Color(0.33f, 0.88f, 1f, 0.18f));
                    TaskGames.Disc(p, 46f, new Color(0.33f, 0.88f, 1f));
                    TaskGames.Disc(p, 30f, new Color(0.85f, 0.97f, 1f));
                }
                else
                {
                    TaskGames.Disc(p, 46f, new Color(0.35f, 0.08f, 0.1f));
                    TaskGames.Disc(p, 30f, new Color(0.55f, 0.12f, 0.14f));
                }
                if (hover) TaskGames.DiscRing(p, 50f, 3f, Color.white);
            }
            int lit = 0; foreach (var b in on) if (b) lit++;
            C.Txt(new Rect(10, 8, 200, 26), $"สว่าง {lit}/7", 16, Color.white, TextAnchor.MiddleLeft, true);
        }
    }
}
