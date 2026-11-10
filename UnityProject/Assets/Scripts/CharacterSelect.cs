using UnityEngine;

namespace SkeldMap
{
    // หน้าเลือกตัวละคร (IMGUI) — เปิดตอนเริ่มเกม และกด C เปิดซ้ำได้ทุกเมื่อ
    //
    //  • รูปย่อ + ตัวอย่างใหญ่เป็นโมเดล 3D จริง: เรนเดอร์ใน "ฉากถ่ายตัวอย่าง" ที่ซ่อนอยู่ไกลจากแมป (layer 31, ไฟของตัวเอง)
    //    กล้องหลักและดวงอาทิตย์ของเกมไม่เห็น layer นี้ จึงไม่ปะปนกับฉากเกม
    //  • เลือกแล้วเปลี่ยนตัวละครในเกมทันที (SkeldGame.SetCharacter) — ปิดเมนูแล้วเดินต่อจากที่เดิม
    //  • ปุ่ม: คลิกการ์ด / ลูกศร = เลือก · Enter / Space / Esc / C = เริ่มเล่น (ปิดเมนู)
    public sealed class CharacterSelect
    {
        public const int PreviewLayer = 31;

        // พื้นที่วาดแบบคงที่ (หน่วยเสมือน) แล้วย่อให้พอดีจอ
        const float PanelW = 1060f, PanelH = 596f;
        const int Cols = 8;
        const float CardW = 80f, CardH = 104f, Gap = 6f;
        const float ThumbPx = 2f;           // เรนเดอร์รูปย่อที่ความละเอียด 2 เท่าของขนาดบนจอ

        readonly SkeldGame game;
        public bool Open { get; private set; }
        int index;

        // ฉากถ่ายตัวอย่าง
        GameObject stage;
        Camera pcam;
        RenderTexture bigRT;
        RenderTexture[] thumbs;
        bool thumbsDone;
        Avatar bigAvatar;
        string bigId;
        float turn;

        // style
        Font font;
        GUIStyle sTitle, sSub, sName, sCardName, sHint, sButton;
        bool stylesReady;

        static readonly Color Dim = new Color(0.01f, 0.02f, 0.05f, 0.72f);
        static readonly Color PanelFill = new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.96f);
        static readonly Color PanelEdge = new Color(160 / 255f, 190 / 255f, 255 / 255f, 0.35f);
        static readonly Color CardFill = new Color(1f, 1f, 1f, 0.06f);
        static readonly Color CardHover = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color Yellow = Mats.Hex(0xffd23f);

        public CharacterSelect(SkeldGame game) { this.game = game; }

        // ───────────── เปิด/ปิด ─────────────
        public void Show()
        {
            Open = true;
            int i = CharacterCatalog.IndexOf(game.CharacterId);
            index = i >= 0 ? i : 0;
            EnsureStage();
        }

        public void Hide() => Open = false;

        public void Toggle() { if (Open) Hide(); else Show(); }

        // ───────────── input (เรียกทุกเฟรมจาก SkeldGame.Update) ─────────────
        public void HandleInput()
        {
            if (!Open) return;
            int n = CharacterCatalog.All.Count;
            int next = index;
            if (GameInput.Pressed(KeyCode.LeftArrow)) next = Mathf.Max(0, index - 1);
            if (GameInput.Pressed(KeyCode.RightArrow)) next = Mathf.Min(n - 1, index + 1);
            if (GameInput.Pressed(KeyCode.UpArrow)) next = index - Cols >= 0 ? index - Cols : index;
            if (GameInput.Pressed(KeyCode.DownArrow)) next = index + Cols < n ? index + Cols : index;
            if (next != index) Select(next);

            if (GameInput.Pressed(KeyCode.Return) || GameInput.Pressed(KeyCode.Space) || GameInput.Pressed(KeyCode.Escape))
                Hide();
        }

        void Select(int i)
        {
            index = i;
            game.SetCharacter(CharacterCatalog.All[i].id);
        }

        // ───────────── ฉากถ่ายตัวอย่าง ─────────────
        void EnsureStage()
        {
            if (stage != null) return;
            stage = new GameObject("CharacterStage");
            stage.transform.position = new Vector3(3000f, -3000f, 3000f); // ไกลจากแมปมาก
            stage.layer = PreviewLayer;

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(stage.transform, false);
            camGo.layer = PreviewLayer;
            pcam = camGo.AddComponent<Camera>();
            pcam.enabled = false;                       // เรนเดอร์เองด้วย Render()
            pcam.clearFlags = CameraClearFlags.SolidColor;
            pcam.backgroundColor = new Color(0, 0, 0, 0); // พื้นหลังโปร่งใส ให้การ์ด/แผงเป็นฉากหลัง
            pcam.cullingMask = 1 << PreviewLayer;
            pcam.fieldOfView = 24f;
            pcam.nearClipPlane = 0.1f;
            pcam.farClipPlane = 20f;
            pcam.allowHDR = false;
            pcam.allowMSAA = true;
            // ตัวละครหันหน้าไป +z ของตัวเอง → วางกล้องฝั่ง +z มองกลับมาที่ตัวละคร
            camGo.transform.localPosition = new Vector3(0f, 0.72f, 3.7f);
            camGo.transform.LookAt(stage.transform.position + new Vector3(0f, 0.62f, 0f));

            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(stage.transform, false);
            lightGo.layer = PreviewLayer;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.0f;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << PreviewLayer;          // ไม่ส่องของในแมป
            lightGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.55f, -0.8f));

            bigRT = NewRT(300, 340);
        }

        static RenderTexture NewRT(int w, int h)
            => new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, filterMode = FilterMode.Bilinear };

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
        }

        // เรนเดอร์ด้วยหมอกปิด (หมอกของเกมคิดระยะจากผู้เล่น ฉากถ่ายตัวอย่างอยู่ไกลเลยจะมืดสนิท)
        void RenderPreview(RenderTexture rt)
        {
            var fog = Shader.GetGlobalVector("_SkeldFogRange");
            Shader.SetGlobalVector("_SkeldFogRange", new Vector4(fog.x, fog.y, fog.z, 0f));
            pcam.targetTexture = rt;
            pcam.Render();
            Shader.SetGlobalVector("_SkeldFogRange", fog);
        }

        Avatar Spawn(string id, float yaw)
        {
            var av = Avatar.Create(id);
            av.Root.transform.SetParent(stage.transform, false);
            av.Root.transform.localPosition = Vector3.zero;
            av.Root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            SetLayerRecursive(av.Root, PreviewLayer);
            return av;
        }

        // รูปย่อของทุกตัว เรนเดอร์ครั้งเดียวตอนเปิดเมนูครั้งแรก
        void RenderThumbnails()
        {
            var all = CharacterCatalog.All;
            thumbs = new RenderTexture[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                thumbs[i] = NewRT((int)(CardW * ThumbPx), (int)((CardH - 18f) * ThumbPx));
                var av = Spawn(all[i].id, 25f);   // กล้องอยู่ฝั่ง +z และตัวละครหัน +z → yaw 0 = หน้าตรง · เบี่ยง 25° ให้ดูมีมิติ
                RenderPreview(thumbs[i]);
                av.Dispose(true);
            }
            thumbsDone = true;
        }

        // ตัวอย่างใหญ่: เปลี่ยนตัวเมื่อเลือกใหม่ แล้วหมุนช้า ๆ พร้อมแอนิเมชันยืน
        public void Tick(float dt)
        {
            if (!Open) return;
            EnsureStage();
            if (!thumbsDone) RenderThumbnails();

            string id = CharacterCatalog.All[index].id;
            if (bigAvatar == null || bigId != id)
            {
                bigAvatar?.Dispose(true);
                bigAvatar = Spawn(id, 25f);
                bigId = id;
                turn = 25f;
            }
            turn += dt * 38f;
            bigAvatar.Root.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
            bigAvatar.Animate(false, dt, 0f);
            RenderPreview(bigRT);
        }

        // ───────────── วาด + เมาส์ ─────────────
        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Thonburi", "Segoe UI", "Arial" }, 16);
            GUIStyle S(int size, FontStyle fs, Color c, TextAnchor a)
            {
                var st = new GUIStyle(GUI.skin.label) { font = font, fontSize = size, fontStyle = fs, alignment = a, wordWrap = false, richText = true, clipping = TextClipping.Overflow };
                st.normal.textColor = c;
                return st;
            }
            var white = new Color(0.91f, 0.93f, 0.99f, 1f);
            sTitle = S(24, FontStyle.Bold, white, TextAnchor.UpperLeft);
            sSub = S(12, FontStyle.Normal, new Color(white.r, white.g, white.b, 0.6f), TextAnchor.UpperLeft);
            sName = S(26, FontStyle.Bold, Yellow, TextAnchor.UpperCenter);
            sCardName = S(11, FontStyle.Normal, white, TextAnchor.UpperCenter);
            sHint = S(12, FontStyle.Normal, new Color(white.r, white.g, white.b, 0.65f), TextAnchor.UpperLeft);
            sButton = S(18, FontStyle.Bold, new Color(0.05f, 0.08f, 0.14f), TextAnchor.MiddleCenter);
        }

        Rect CardRect(int i, float gx, float gy)
            => new Rect(gx + (i % Cols) * (CardW + Gap), gy + (i / Cols) * (CardH + Gap), CardW, CardH);

        public void OnGUI()
        {
            if (!Open) return;
            var e = Event.current;
            if (e.type != EventType.Repaint && e.type != EventType.MouseDown) return;
            EnsureStyles();

            float s = GameSettings.GuiScale;
            float W = Screen.width / s, H = Screen.height / s;
            float k = Mathf.Min(1f, (W - 24f) / PanelW, (H - 24f) / PanelH);

            // ฉากหลังมืดเต็มจอ (ไม่ผ่านการย่อ)
            if (e.type == EventType.Repaint)
            {
                GUI.matrix = Matrix4x4.identity;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Dim, Vector4.zero, Vector4.zero);
            }

            // พื้นที่แผง: ย่อ k รอบกลางจอ
            Vector3 c = new Vector3(W / 2f, H / 2f, 0f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f)) * Matrix4x4.TRS(c, Quaternion.identity, new Vector3(k, k, 1f)) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);

            var panel = new Rect((W - PanelW) / 2f, (H - PanelH) / 2f, PanelW, PanelH);
            var all = CharacterCatalog.All;

            float leftX = panel.x + 28f, leftW = 300f;
            float gridX = panel.x + 28f + leftW + 28f, gridY = panel.y + 82f;
            var playBtn = new Rect(leftX, panel.yMax - 28f - 44f, leftW, 44f);

            // ── คลิก
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                for (int i = 0; i < all.Count; i++)
                    if (CardRect(i, gridX, gridY).Contains(e.mousePosition)) { Select(i); e.Use(); return; }
                if (playBtn.Contains(e.mousePosition)) { Hide(); e.Use(); }
                return;
            }

            // ── วาด
            Hud.Panel(panel, PanelFill, PanelEdge, 16f);
            GUI.Label(new Rect(leftX, panel.y + 22f, 700f, 32f), "เลือกตัวละคร  <size=14><color=#9fb0d0>SELECT CHARACTER</color></size>", sTitle);
            GUI.Label(new Rect(leftX, panel.y + 54f, 700f, 20f), $"ตัวละครทั้งหมด {all.Count} ตัว", sSub);

            // ตัวอย่างใหญ่ (ซ้าย)
            var big = new Rect(leftX, panel.y + 86f, leftW, 350f);
            Hud.Panel(big, new Color(1f, 1f, 1f, 0.05f), new Color(1f, 1f, 1f, 0.12f), 14f);
            if (bigRT != null) GUI.DrawTexture(big, bigRT, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(leftX, panel.y + 444f, leftW, 34f), all[index].name, sName);
            GUI.Label(new Rect(leftX, panel.y + 480f, leftW, 20f), $"<color=#9fb0d0>{index + 1} / {all.Count}</color>", new GUIStyle(sSub) { alignment = TextAnchor.UpperCenter });

            // ปุ่มเริ่มเล่น
            bool hoverPlay = playBtn.Contains(e.mousePosition);
            GUI.DrawTexture(playBtn, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, hoverPlay ? Color.white : Yellow, Vector4.zero, Vector4.one * 10f);
            GUI.Label(playBtn, "เริ่มเล่น  ▶", sButton);

            // กริดการ์ด (ขวา)
            for (int i = 0; i < all.Count; i++)
            {
                var r = CardRect(i, gridX, gridY);
                bool sel = i == index, hov = r.Contains(e.mousePosition);
                Hud.Panel(r, hov ? CardHover : CardFill, sel ? Yellow : new Color(1f, 1f, 1f, 0.1f), 10f);
                if (sel) GUI.DrawTexture(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Yellow, Vector4.one * 2f, Vector4.one * 11f);
                if (thumbs != null && thumbs[i] != null)
                    GUI.DrawTexture(new Rect(r.x, r.y + 2f, r.width, r.height - 20f), thumbs[i], ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x, r.yMax - 18f, r.width, 16f), all[i].name, sCardName);
            }

            GUI.Label(new Rect(gridX, panel.yMax - 58f, 640f, 40f),
                "คลิกหรือกดลูกศรเพื่อเลือก · Enter เริ่มเล่น\nกด <b>C</b> เปิดหน้านี้ได้ทุกเมื่อ · กด <b>[</b> และ <b>]</b> สลับตัวละครระหว่างเดิน", sHint);

            GUI.matrix = Matrix4x4.identity;
        }
    }
}
