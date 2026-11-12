using UnityEngine;

namespace SkeldMap
{
    // HUD ทั้งหมด (location / tasks / minimap / prompt / banner / help) วาดด้วย IMGUI
    // port จาก <div class="hud"> + minimap ใน index.html และ js/main.js
    public sealed class Hud
    {
        readonly SkeldGame g;

        // minimap
        const int MmPx = 4;                  // พิกเซลต่อช่องกริดใน texture
        Texture2D mmTex, dotTex, arrowTex, arrowOutlineTex;

        // style
        Font font;
        GUIStyle sLoc, sLocSub, sTitle, sTitleRight, sTask, sTaskDone, sTaskRoom, sPrompt, sBanner, sHelp, sMm;
        bool stylesReady;
        Matrix4x4 guiScale = Matrix4x4.identity;

        static readonly Color PanelFill = new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.72f);
        static readonly Color PanelEdge = new Color(160 / 255f, 190 / 255f, 255 / 255f, 0.28f);
        static readonly Color Text = new Color(232 / 255f, 238 / 255f, 252 / 255f, 1f);
        static readonly Color Yellow = Mats.Hex(0xffd23f);
        static readonly Color Green = Mats.Hex(0x4cff8a);

        public Hud(SkeldGame game)
        {
            g = game;
            BuildMinimapTexture();
            dotTex = MakeDisc(32);
            arrowOutlineTex = MakeArrow(64, 1.45f);
            arrowTex = MakeArrow(64, 1f);
        }

        // ───────────── textures ─────────────
        static Color32 Rgba(uint hex, float a = 1f)
            => new Color32((byte)((hex >> 16) & 255), (byte)((hex >> 8) & 255), (byte)(hex & 255), (byte)(a * 255f));

        void BuildMinimapTexture()
        {
            int w = MapData.GridW * MmPx, h = MapData.GridH * MmPx;
            mmTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "minimap" };
            var px = new Color32[w * h];
            void Fill(int gx, int gz, int cw, int cd, Color32 c)
            {
                for (int j = 0; j < cd * MmPx; j++)
                    for (int i = 0; i < cw * MmPx; i++)
                    {
                        int x = gx * MmPx + i;
                        int y = (MapData.GridH - 1 - gz) * MmPx - (cd - 1) * MmPx + j; // z=0 อยู่บนสุดของจอ (texture y=0 อยู่ล่างสุด)
                        if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c;
                    }
            }
            var bg = Rgba(0x050810, 0.9f);
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            var floor = Rgba(0x566178);
            for (int z = 0; z < MapData.GridH; z++)
                for (int x = 0; x < MapData.GridW; x++)
                    if (g.Grid.IsFloor(x, z)) Fill(x, z, 1, 1, floor);
            foreach (var r in MapData.Rooms) Fill(r.x, r.z, r.w, r.d, Rgba(r.color));
            var solid = Rgba(0x3a4258);
            for (int z = 0; z < MapData.GridH; z++)
                for (int x = 0; x < MapData.GridW; x++)
                    if (g.Grid.IsFloor(x, z) && g.Grid.IsSolid(x, z)) Fill(x, z, 1, 1, solid);

            mmTex.SetPixels32(px);
            mmTex.Apply(false);
        }

        static Texture2D MakeDisc(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float c = (s - 1) / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(c - d + 0.5f);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px); t.Apply(false);
            return t;
        }

        // ลูกศรชี้ทิศผู้เล่น: (0,-6) (4.5,4) (0,2) (-4.5,4) เหมือน canvas เดิม · y ลง = ล่างของจอ · ปลายลูกศรชี้ขึ้นบนจอเมื่อ rotation = 0
        static Texture2D MakeArrow(int s, float scale)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            Vector2[] poly = { new Vector2(0, -6), new Vector2(4.5f, 4), new Vector2(0, 2), new Vector2(-4.5f, 4) };
            const float extent = 8f; // texture ครอบคลุม ±8 หน่วย
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                        {
                            float u = ((x + (sx + 0.5f) / 3f) / s * 2f - 1f) * extent;
                            float v = (1f - (y + (sy + 0.5f) / 3f) / s * 2f) * extent; // texture y=0 อยู่ล่าง ⇒ ได้ y แบบ canvas (ลงเป็นบวก) พอดี
                            if (Inside(poly, new Vector2(u / scale, v / scale))) hit++;
                        }
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(hit / 9f * 255f));
                }
            t.SetPixels32(px); t.Apply(false);
            return t;
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        // ───────────── styles ─────────────
        GUIStyle Style(int size, FontStyle fs, Color col, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = size, fontStyle = fs, alignment = anchor,
                wordWrap = false, richText = true, clipping = TextClipping.Overflow,
            };
            s.normal.textColor = col;
            return s;
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            // ฟอนต์ระบบที่มีอักษรไทย (Leelawadee UI มากับ Windows 10/11) · macOS: Thonburi
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Thonburi", "Segoe UI", "Arial" }, 16);
            sLoc = Style(15, FontStyle.Bold, Text);
            sLocSub = Style(11, FontStyle.Normal, new Color(Text.r, Text.g, Text.b, 0.6f));
            sTitle = Style(13, FontStyle.Bold, Text);
            sTitleRight = Style(13, FontStyle.Bold, Text, TextAnchor.UpperRight);
            sTask = Style(12, FontStyle.Normal, Yellow);
            sTaskDone = Style(12, FontStyle.Normal, new Color(Green.r, Green.g, Green.b, 0.6f));
            sTaskRoom = Style(10, FontStyle.Normal, new Color(Text.r, Text.g, Text.b, 0.55f), TextAnchor.UpperRight);
            sPrompt = Style(14, FontStyle.Normal, Text, TextAnchor.MiddleCenter);
            sBanner = Style(16, FontStyle.Bold, Green, TextAnchor.MiddleCenter);
            sHelp = Style(12, FontStyle.Normal, Text);
            sMm = Style(8, FontStyle.Bold, new Color(0, 0, 0, 0.7f), TextAnchor.MiddleCenter);
        }

        // ───────────── draw helpers ─────────────
        // กรอบมุมมน: พื้น (borderWidths = 0 → ระบายเต็ม) แล้วทับด้วยเส้นขอบ 1px (borderWidths > 0 → วาดเฉพาะขอบ)
        internal static void Panel(Rect r, Color fill, Color edge, float radius = 10f)
        {
            var rad = new Vector4(radius, radius, radius, radius);
            if (fill.a > 0.001f)
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, Vector4.zero, rad);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, edge, Vector4.one, rad);
        }

        static void Bar(Rect r, float t, Color back, Color fill)
        {
            var rad = Vector4.one * (r.height / 2f);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, back, Vector4.zero, rad);
            if (t > 0.001f)
                GUI.DrawTexture(new Rect(r.x, r.y, Mathf.Max(r.height, r.width * Mathf.Clamp01(t)), r.height),
                                Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, Vector4.zero, rad);
        }

        static void Label(Rect r, string s, GUIStyle st) => GUI.Label(r, s, st);

        // ───────────── main ─────────────
        public void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureStyles();

            float s = GameSettings.GuiScale;
            guiScale = Matrix4x4.Scale(new Vector3(s, s, 1f));
            GUI.matrix = guiScale;
            float W = Screen.width / s, H = Screen.height / s;
            bool narrow = W < 640f;
            var oldColor = GUI.color;
            GUI.color = Color.white;

            DrawLocation();
            DrawTasks(W, narrow);
            DrawMinimap(H, narrow);
            if (!narrow && GameSettings.S.showHelp) DrawHelp(W, H);
            DrawPrompt(W, H);
            DrawBanner(W);

            GUI.color = oldColor;
            GUI.matrix = Matrix4x4.identity;
        }

        void DrawLocation()
        {
            string name = g.LocationName;
            const string sub = "PATIENT ZERO · WASD เพื่อเดิน";
            float w = Mathf.Max(sLoc.CalcSize(new GUIContent(name)).x, sLocSub.CalcSize(new GUIContent(sub)).x) + 32f;
            var r = new Rect(16, 16, w, 50);
            Panel(r, PanelFill, PanelEdge);
            Label(new Rect(r.x + 16, r.y + 6, w, 22), name, sLoc);
            Label(new Rect(r.x + 16, r.y + 28, w, 16), sub, sLocSub);
        }

        void DrawTasks(float W, bool narrow)
        {
            float w = narrow ? 150f : 230f;
            int rows = narrow ? 0 : g.AssignedCount;
            float h = 14f + 18f + 5f + 16f + rows * 17f + (rows > 0 ? 8f : 0f) + 4f;
            var r = new Rect(W - 16 - w, 16, w, h);
            Panel(r, PanelFill, PanelEdge);

            float x = r.x + 14, y = r.y + 10, iw = w - 28;
            Label(new Rect(x, y, iw, 18), "MY TASKS", sTitle);
            Label(new Rect(x, y, iw, 18), $"{g.DoneCount}/{g.AssignedCount}", sTitleRight);
            y += 24;
            Bar(new Rect(x, y, iw, 5), g.AssignedCount == 0 ? 0 : (float)g.DoneCount / g.AssignedCount,
                new Color(1, 1, 1, 0.12f), Green);
            y += 13;

            if (narrow) return;
            foreach (var t in g.Tasks)
            {
                if (!t.assigned) continue;
                Label(new Rect(x, y, iw, 17), (t.done ? "[x] " : "") + t.def.name, t.done ? sTaskDone : sTask);
                Label(new Rect(x, y + 2, iw, 14), MapData.RoomName(t.def.room), sTaskRoom);
                y += 17;
            }
        }

        void DrawMinimap(float H, bool narrow)
        {
            float w = narrow ? 150f : 220f;
            float h = w * MapData.GridH / MapData.GridW;
            var r = new Rect(16, H - 16 - h, w, h);
            GUI.DrawTexture(r, mmTex, ScaleMode.StretchToFill, true, 0f, Color.white, Vector4.zero, Vector4.one * 10f);
            Panel(r, Color.clear, new Color(160 / 255f, 190 / 255f, 255 / 255f, 0.3f), 10f);

            float cell = w / MapData.GridW;

            // ชื่อห้อง
            foreach (var room in MapData.Rooms)
            {
                string n = room.name.Length > 10 ? room.name.Split(' ')[0] : room.name;
                float cx = r.x + (room.x + room.w / 2f) * cell;
                float cy = r.y + (room.z + room.d / 2f) * cell;
                Label(new Rect(cx - 40, cy - 6, 80, 12), n, sMm);
            }

            // จุดเควส
            foreach (var t in g.Tasks)
            {
                if (!t.assigned) continue;
                Vector2 p = new Vector2(r.x + t.def.x * cell, r.y + t.def.z * cell);
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(p.x - 4.2f, p.y - 4.2f, 8.4f, 8.4f), dotTex);
                GUI.color = t.done ? Green : Yellow;
                GUI.DrawTexture(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), dotTex);
            }
            GUI.color = Color.white;

            // ผู้เล่น (ลูกศรหมุนตามทิศหัน)
            Vector2 pp = new Vector2(r.x + g.PosX * cell, r.y + g.PosZ * cell);
            var saved = GUI.matrix;
            // ไม่ใช้ GUIUtility.RotateAroundPivot เพราะมันไม่นับ scale ใน GUI.matrix ⇒ จุดหมุนเพี้ยน
            Vector3 pv = new Vector3(pp.x, pp.y, 0f);
            GUI.matrix = guiScale
                * Matrix4x4.TRS(pv, Quaternion.Euler(0f, 0f, (Mathf.PI - g.Facing) * Mathf.Rad2Deg), Vector3.one)
                * Matrix4x4.TRS(-pv, Quaternion.identity, Vector3.one);
            float sz = 16f * (w / 220f);
            var rect = new Rect(pp.x - sz / 2, pp.y - sz / 2, sz, sz);
            GUI.color = Color.black;
            GUI.DrawTexture(rect, arrowOutlineTex);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, arrowTex);
            GUI.matrix = saved;
        }

        void DrawHelp(float W, float H)
        {
            string[] lines =
            {
                "<b>W A S D</b>  เดิน",
                "<b>Q E</b>  หมุนกล้อง · <b>ล้อเมาส์</b> ซูม",
                "<b>M</b>  แผนที่ทั้งหมด (Overview)",
                "<b>F</b>  ที่จุดเหลือง = มินิเกมเควส · ที่ปุ่มแดงห้องประชุม (โต๊ะกลาง) = เรียกประชุม",
                "<b>B</b> กัด (ผู้ติดเชื้อ) · <b>T</b> ตรวจเลือด (หมอ) · <b>G</b> ยิง (ตำรวจ)",
                "<b>V</b>  เปิด/ปิดหมอก",
                "<b>N</b>  เปิด/ปิดเสียง",
                "<b>C</b>  เลือกตัวละคร · <b>[ ]</b> สลับตัวละคร",
            };
            float w = 0f;
            foreach (var l in lines) w = Mathf.Max(w, sHelp.CalcSize(new GUIContent(l)).x);
            w += 28f;
            float h = lines.Length * 20f + 16f;
            var r = new Rect(W - 16 - w, H - 16 - h, w, h);
            Panel(r, PanelFill, PanelEdge);
            for (int i = 0; i < lines.Length; i++) Label(new Rect(r.x + 14, r.y + 8 + i * 20, w, 20), lines[i], sHelp);
        }

        void DrawPrompt(float W, float H)
        {
            if (!g.PromptVisible) return;
            // เฟดเข้า-ออก: ช่วงแรก (alpha < 0.15) ไม่วาดเลย กันการโผล่เฟรมเดียวตอนวิ่งผ่านขอบรัศมี
            float a = Mathf.Clamp01((g.PromptAlpha - 0.15f) / 0.85f);
            if (a <= 0f) return;
            var prevColor = GUI.color;
            GUI.color = new Color(1, 1, 1, a);
            float w = Mathf.Max(320f, sPrompt.CalcSize(new GUIContent(g.PromptText)).x + 40f);
            var r = new Rect((W - w) / 2f, H - 96f - 58f, w, 58f);
            Panel(r, new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.85f), new Color(1f, 210 / 255f, 63 / 255f, 0.55f));
            Label(new Rect(r.x, r.y + 6, w, 24), g.PromptText, sPrompt);
            if (g.HoldProgress > 0.001f) Bar(new Rect(r.x + 18, r.y + 38, w - 36, 5), g.HoldProgress, new Color(1, 1, 1, 0.15f), Yellow);
            GUI.color = prevColor;
        }

        void DrawBanner(float W)
        {
            if (g.BannerAlpha < 0.01f) return;
            const string text = "ภารกิจของคุณเสร็จครบแล้ว!";
            float w = sBanner.CalcSize(new GUIContent(text)).x + 52f;
            var r = new Rect((W - w) / 2f, 90, w, 46);
            float a = g.BannerAlpha;
            GUI.color = new Color(1, 1, 1, a);
            Panel(r, new Color(20 / 255f, 70 / 255f, 40 / 255f, 0.85f), Green);
            Label(r, text, sBanner);
            GUI.color = Color.white;
        }
    }
}
