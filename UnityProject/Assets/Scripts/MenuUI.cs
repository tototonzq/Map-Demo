using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // เมนูทั้งหมดของเกม (IMGUI): หน้าชื่อเกม → เมนูหลัก → สร้างห้อง / เข้าร่วมห้อง / ตั้งค่า / วิธีเล่น · เมนูพัก (Esc) ระหว่างเล่น
    // วาดบนผืนผ้าใบคงที่ 1600×900 แล้วปรับสเกลตามหน้าจอ (ดูเหมือนกันทุกความละเอียด)
    public sealed class MenuUI
    {
        const float CW = 1600f, CH = 900f;

        enum Page { None, Title, Main, Lobby, Join, Settings, HowTo, Pause, Confirm }

        readonly SkeldGame g;
        Page page = Page.Title, ret = Page.Main;
        int tab;
        float t;                  // เวลาแอนิเมชัน
        int dragId = -1;
        string toast = ""; float toastT;
        string confirmText = ""; System.Action confirmYes; Page confirmBack;
        string joinCode = "";
        Font font; GUIStyle st, tf;
        List<Vector2Int> resList;

        static readonly Color Red = Mats.Hex(0xff4a4a), Green = Mats.Hex(0x4cff8a), Yellow = Mats.Hex(0xffd23f), Cyan = Mats.Hex(0x55e0ff);
        static readonly Color Panel = new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.92f), Edge = new Color(160 / 255f, 190 / 255f, 255 / 255f, 0.3f);
        static readonly Color Txt = new Color(232 / 255f, 238 / 255f, 252 / 255f, 1f), Dim = new Color(1, 1, 1, 0.55f);

        static readonly string[] Tips =
        {
            "ผู้ติดเชื้อจะกัดตอนไม่มีใครเห็น — อย่าอยู่ลำพังกับคนที่คุณไม่ไว้ใจ",
            "เสียงไอคือเบาะแส แต่ผู้รอดชีวิตที่เครียดก็ไอหลอกได้",
            "กลางคืนไฟดับและซอมบี้บุก — อยู่รวมกลุ่มจะปลอดภัยกว่า",
            "หมอตรวจเลือดได้แค่ 2 ครั้งต่อเกม เลือกคนให้ดี",
            "ตำรวจมีปืนติดตัวแต่ยิงได้นัดเดียว ยิงผิดคนคุณจะเสียชีวิต",
            "ผลิตวัคซีนให้ครบก่อนที่ผู้ติดเชื้อจะมีมากกว่าผู้รอดชีวิต",
        };

        public MenuUI(SkeldGame game) { g = game; }

        public bool Visible => page != Page.None;
        public bool IsPause => page == Page.Pause || (ret == Page.Pause && (page == Page.Settings || page == Page.HowTo || page == Page.Confirm));

        public void ShowTitle() { page = Page.Title; t = 0f; }
        public void ShowMain() { page = Page.Main; }
        public void ShowPause() { page = Page.Pause; ret = Page.Pause; }
        public void Hide() { page = Page.None; GameSettings.Save(); }

        void Toast(string s) { toast = s; toastT = 2.2f; }

        // ───── ออนไลน์ ─────
        // ออกจากหน้าสร้าง/เข้าร่วมห้องโดยยังไม่ได้เริ่มเล่น → ปิดการเชื่อมต่อที่ค้างอยู่
        void LeaveOnline()
        {
            if (!SkeldGame.OnlineMode && !NetSession.Busy) { NetSession.Shutdown(); NetSession.Status = ""; NetSession.Error = ""; }
        }

        void StartOnline()
        {
            SkeldGame.OnlineMode = true;
            GameSettings.Save();
            g.StartMatch(GameSettings.Room);
        }

        async void DoHost()
        {
            string code = await NetSession.HostAsync();
            if (code == null) Toast("สร้างห้องออนไลน์ไม่สำเร็จ");
        }

        void DoHostLan()
        {
            if (!NetSession.HostLan()) Toast("สร้างห้อง LAN ไม่สำเร็จ");
        }

        async void DoJoin(string code)
        {
            // มีจุด = IP (LAN) · ไม่มี = รหัส Relay
            if (code.Contains(".")) await NetSession.JoinLan(code);
            else await NetSession.JoinAsync(code);
            if (g == null) return;   // ฉากถูกโหลดใหม่ระหว่างรอ
            if (NetSession.Active) StartOnline();
        }

        // ═════════════ ปุ่มลัด ═════════════
        public void Tick(float dt)
        {
            t += dt;
            toastT = Mathf.Max(0f, toastT - dt);
            if (g.Select != null && g.Select.Open) return;
            switch (page)
            {
                case Page.Title:
                    if (t > 0.8f && (GameInput.Pressed(KeyCode.Return) || GameInput.Pressed(KeyCode.Space))) page = Page.Main;
                    break;
                case Page.Main:
                    if (GameInput.Pressed(KeyCode.Escape)) page = Page.Title;
                    break;
                case Page.Lobby:
                case Page.Join:
                    if (GameInput.Pressed(KeyCode.Escape)) { GameSettings.Save(); LeaveOnline(); page = Page.Main; }
                    break;
                case Page.Settings:
                case Page.HowTo:
                    if (GameInput.Pressed(KeyCode.Escape)) { GameSettings.Save(); page = ret; }
                    break;
                case Page.Pause:
                    if (GameInput.Pressed(KeyCode.Escape)) { g.Resume(); }
                    break;
                case Page.Confirm:
                    if (GameInput.Pressed(KeyCode.Escape)) page = confirmBack;
                    break;
            }
        }

        // ═════════════ วาด ═════════════
        void EnsureStyle()
        {
            if (st != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Thonburi", "Segoe UI", "Arial" }, 16);
            st = new GUIStyle(GUI.skin.label) { font = font, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
            tf = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 18, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 4, 4) };
            tf.normal.textColor = Txt; tf.focused.textColor = Color.white; tf.hover.textColor = Txt;
            var bg = Texture2D.whiteTexture;
            tf.normal.background = null; tf.focused.background = null; tf.hover.background = null;
        }

        public void OnGUI()
        {
            if (page == Page.None) return;
            if (g.Select != null && g.Select.Open) return;
            EnsureStyle();
            var e = Event.current;
            float s = Mathf.Min(Screen.width / CW, Screen.height / CH);
            float ox = (Screen.width - CW * s) / 2f, oy = (Screen.height - CH * s) / 2f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(s, s, 1f));
            bool rep = e.type == EventType.Repaint;

            // พื้นหลัง: ในเมนูหลักเห็นแผนที่หมุนอยู่ด้านหลัง · ตอนพักเกมมืดลง
            if (rep)
            {
                float dark = page == Page.Title ? 0.38f : (g.InMenu ? 0.62f : 0.72f);
                Fill(new Rect(-ox / s - 5, -oy / s - 5, Screen.width / s + 10, Screen.height / s + 10), new Color(0.01f, 0.02f, 0.05f, dark));
            }

            switch (page)
            {
                case Page.Title: DrawTitle(e); break;
                case Page.Main: DrawMain(e); break;
                case Page.Lobby: DrawLobby(e); break;
                case Page.Join: DrawJoin(e); break;
                case Page.Settings: DrawSettings(e); break;
                case Page.HowTo: DrawHowTo(e); break;
                case Page.Pause: DrawPause(e); break;
                case Page.Confirm: DrawConfirm(e); break;
            }

            if (rep && toastT > 0f)
            {
                float a = Mathf.Clamp01(toastT / 0.4f);
                var r = new Rect(CW / 2 - 300, CH - 90, 600, 44);
                Box(r, new Color(0.08f, 0.1f, 0.18f, 0.95f * a), new Color(Yellow.r, Yellow.g, Yellow.b, 0.7f * a));
                Text(r, toast, 17, new Color(Yellow.r, Yellow.g, Yellow.b, a), TextAnchor.MiddleCenter, true);
            }
            if (e.isMouse || e.type == EventType.ScrollWheel) e.Use();
            GUI.matrix = Matrix4x4.identity;
        }

        // ───────── หน้าชื่อเกม ─────────
        void DrawTitle(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            if (rep)
            {
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 2.6f)), 10f);
                Fill(new Rect(0, CH * 0.28f, CW, 220), new Color(0.5f, 0.02f, 0.02f, 0.10f + 0.12f * beat));
                int sz = 112 + Mathf.RoundToInt(beat * 6f);
                Text(new Rect(0, 280, CW, 150), "PATIENT ZERO", sz, new Color(Red.r, Red.g, Red.b, 0.22f + 0.15f * beat), TextAnchor.MiddleCenter, true);
                Text(new Rect(0, 280, CW, 150), "PATIENT ZERO", 108, Red, TextAnchor.MiddleCenter, true);
                Text(new Rect(0, 420, CW, 40), "ผู้ติดเชื้ออยู่ในกลุ่มของคุณ — หาให้เจอก่อนสายเกินไป", 24, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter);
                float blink = 0.5f + 0.5f * Mathf.Sin(t * 3f);
                Text(new Rect(0, 640, CW, 36), "กด Enter หรือคลิกเพื่อเริ่ม", 22, new Color(1, 1, 1, 0.35f + 0.5f * blink), TextAnchor.MiddleCenter);
                Text(new Rect(0, CH - 52, CW, 26), "เวอร์ชันทดลอง · โหมดออฟไลน์ (เล่นกับบอท)", 14, new Color(1, 1, 1, 0.4f), TextAnchor.MiddleCenter);
                if (t < 1.2f) Fill(new Rect(-50, -50, CW + 100, CH + 100), new Color(0, 0, 0, 1f - t / 1.2f));
            }
            if (e.type == EventType.MouseDown && e.button == 0 && t > 0.8f) { page = Page.Main; e.Use(); }
        }

        // ───────── เมนูหลัก ─────────
        void DrawMain(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            if (rep)
            {
                Text(new Rect(110, 90, 800, 80), "PATIENT ZERO", 64, Red, TextAnchor.MiddleLeft, true);
                Text(new Rect(112, 160, 800, 28), "ผู้ติดเชื้ออยู่ในกลุ่มของคุณ", 18, Dim);
            }
            float y = 250f; const float bw = 400f, bh = 58f, gap = 14f;
            if (Btn(new Rect(110, y, bw, bh), "เริ่มเกมทันที", true)) { g.StartMatch(GameSettings.Room); }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "สร้างห้อง")) { page = Page.Lobby; GameSettings.Room.Clamp(); }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "เข้าร่วมห้อง")) { page = Page.Join; }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "เลือกตัวละคร")) { g.Select.Show(); }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "วิธีเล่น")) { ret = Page.Main; page = Page.HowTo; }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "ตั้งค่า")) { ret = Page.Main; page = Page.Settings; tab = 0; }
            y += bh + gap;
            if (Btn(new Rect(110, y, bw, bh), "ออกจากเกม")) { Confirm("ออกจากเกมหรือไม่?", Quit, Page.Main); }

            // การ์ดผู้เล่น
            var card = new Rect(CW - 560, 250, 450, 230);
            if (rep) Box(card, Panel, Edge, 14f);
            Text(new Rect(card.x + 24, card.y + 16, 300, 24), "ผู้เล่น", 14, Dim);
            string nm = TextBox(new Rect(card.x + 24, card.y + 42, card.width - 48, 44), GameSettings.S.playerName, 14, "ชื่อของคุณ");
            if (nm != GameSettings.S.playerName) GameSettings.S.playerName = nm;
            var ce = CharacterCatalog.All[Mathf.Max(0, CharacterCatalog.IndexOf(g.CharacterId))];
            Text(new Rect(card.x + 24, card.y + 100, card.width - 48, 24), "ตัวละคร: " + ce.name, 17, Txt, TextAnchor.UpperLeft, true);
            var r = GameSettings.Room;
            Text(new Rect(card.x + 24, card.y + 134, card.width - 48, 24), "ห้องล่าสุด", 14, Dim);
            Text(new Rect(card.x + 24, card.y + 156, card.width - 48, 50),
                 $"{r.roomName} · {r.players} คน · ผู้ติดเชื้อ {r.infected} · {(r.difficulty == 0 ? "ง่าย" : r.difficulty == 1 ? "ปกติ" : "ยาก")}", 15, Txt, TextAnchor.UpperLeft, false, true);

            if (rep)
            {
                var tipR = new Rect(CW - 560, 500, 450, 110);
                Box(tipR, new Color(0.18f, 0.06f, 0.06f, 0.85f), new Color(Red.r, Red.g, Red.b, 0.4f), 14f);
                Text(new Rect(tipR.x + 24, tipR.y + 14, 400, 22), "เคล็ดลับ", 13, Red, TextAnchor.UpperLeft, true);
                Text(new Rect(tipR.x + 24, tipR.y + 38, 402, 66), Tips[(int)(t / 7f) % Tips.Length], 16, Txt, TextAnchor.UpperLeft, false, true);
                Text(new Rect(0, CH - 40, CW, 24), "ออนไลน์: เดินเล่นร่วมกัน (ทดลอง) · สร้างห้องหรือเข้าร่วมด้วยรหัส", 13, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
            }
        }

        // ───────── สร้างห้อง ─────────
        void DrawLobby(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var rs = GameSettings.Room;
            var left = new Rect(70, 40, 820, 770);
            if (rep) { Box(left, Panel, Edge, 16f); Text(new Rect(left.x + 32, left.y + 18, 500, 40), "สร้างห้อง", 32, Yellow, TextAnchor.MiddleLeft, true); }
            float y = left.y + 76f; const float rh = 53f; float x = left.x + 32f, w = left.width - 64f;

            rs.roomName = TextRow(new Rect(x, y, w, 46), "ชื่อห้อง", rs.roomName, 24); y += rh;
            // รหัสห้อง
            Label(new Rect(x, y, 300, 46), "รหัสห้อง");
            bool hosting = NetSession.IsHost;
            if (rep) Text(new Rect(x + 400, y, 360, 46), hosting ? NetSession.JoinCode : rs.code, hosting ? 30 : 26, hosting ? Green : Cyan, TextAnchor.MiddleLeft, true);
            if (hosting)
            {
                if (Btn(new Rect(x + w - 150, y + 4, 150, 38), "คัดลอก", false, true, 15))
                {
                    GUIUtility.systemCopyBuffer = NetSession.JoinCode;
                    Toast("คัดลอก " + NetSession.JoinCode + " แล้ว");
                }
            }
            else if (Btn(new Rect(x + w - 150, y + 4, 150, 38), "สุ่มรหัสใหม่", false, true, 15)) rs.code = RoomSettings.NewCode();
            y += rh;
            // โหมด
            Label(new Rect(x, y, 300, 46), "โหมด");
            if (hosting || NetSession.Busy || NetSession.Error != "")
            {
                string msg = NetSession.Error != "" ? "<color=#ff6a6a>" + NetSession.Error + "</color>" : hosting ? (NetSession.JoinCode.Contains(".") ? "<color=#4cff8a>ห้อง LAN พร้อมแล้ว — ให้เพื่อนใส่ IP นี้ (เครือข่ายเดียวกัน)</color>" : "<color=#4cff8a>ห้องออนไลน์พร้อมแล้ว — ส่งรหัสให้เพื่อน</color>") : NetSession.Status;
                if (rep) Text(new Rect(x + 400, y, w - 400, 46), msg, 15, Txt, TextAnchor.MiddleLeft);
            }
            else if (rep) Text(new Rect(x + 400, y, w - 400, 46), "ออฟไลน์ (เล่นกับบอท) หรือสร้างห้อง LAN / ออนไลน์ด้านล่าง", 17, Txt, TextAnchor.MiddleLeft);
            y += rh;

            rs.players = Mathf.RoundToInt(SliderRow(1, new Rect(x, y, w, 46), "จำนวนผู้เล่น", rs.players, 5, 10, v => $"{Mathf.RoundToInt(v)} คน")); y += rh;
            rs.infected = Mathf.Clamp(rs.infected, 1, rs.MaxInfected);
            string[] inf = new string[rs.MaxInfected];
            for (int i = 0; i < inf.Length; i++) inf[i] = $"{i + 1} คน";
            rs.infected = ChoiceRow(new Rect(x, y, w, 46), "ผู้ติดเชื้อต้นตอ", rs.infected - 1, inf) + 1; y += rh;
            rs.difficulty = ChoiceRow(new Rect(x, y, w, 46), "ความยาก", rs.difficulty, new[] { "ง่าย", "ปกติ", "ยาก" }); y += rh;
            rs.dayLen = Step10(SliderRow(2, new Rect(x, y, w, 46), "ความยาวกลางวัน", rs.dayLen, 60, 300, v => $"{Mathf.RoundToInt(v / 10) * 10} วินาที")); y += rh;
            rs.nightLen = Step10(SliderRow(3, new Rect(x, y, w, 46), "ความยาวกลางคืน", rs.nightLen, 15, 120, v => $"{Mathf.RoundToInt(v / 5) * 5} วินาที"), 5f); y += rh;
            rs.nightZombies = Mathf.RoundToInt(SliderRow(4, new Rect(x, y, w, 46), "ซอมบี้กลางคืน", rs.nightZombies, 0, 8, v => $"{Mathf.RoundToInt(v)} ตัว")); y += rh;
            rs.tasksPerPlayer = Mathf.RoundToInt(SliderRow(5, new Rect(x, y, w, 46), "ภารกิจต่อคน", rs.tasksPerPlayer, 2, 8, v => $"{Mathf.RoundToInt(v)} อย่าง")); y += rh;
            rs.humanRole = ChoiceRow(new Rect(x, y, w, 46), "บทบาทของคุณ", rs.humanRole, new[] { "สุ่ม", "ผู้รอดชีวิต", "ผู้ติดเชื้อต้นตอ" }); y += rh;
            rs.abilities = ToggleRow(new Rect(x, y, w, 46), "ความสามารถอาชีพ (หมอ/ตำรวจ)", rs.abilities);
            rs.Clamp();

            // ผู้เล่นในห้อง
            var right = new Rect(930, 40, 600, 770);
            if (rep)
            {
                Box(right, Panel, Edge, 16f);
                Text(new Rect(right.x + 32, right.y + 18, 400, 40), $"ผู้เล่นในห้อง  {rs.players}/{rs.players}", 26, Txt, TextAnchor.MiddleLeft, true);
                var ce = CharacterCatalog.All[Mathf.Max(0, CharacterCatalog.IndexOf(g.CharacterId))];
                float py = right.y + 80;
                Slot(new Rect(right.x + 28, py, right.width - 56, 48), GameSettings.PlayerName, "เจ้าของห้อง · " + ce.name, Mats.Hex(0xff5252), true);
                py += 56;
                for (int i = 1; i < rs.players; i++, py += 52)
                    Slot(new Rect(right.x + 28, py, right.width - 56, 44), "บอท " + (i), "ผู้เล่นคอมพิวเตอร์", SlotColor(i), false);
                Text(new Rect(right.x + 32, right.yMax - 120, right.width - 64, 90),
                     hosting ? "ห้องออนไลน์: กด \"เริ่มเดินเล่น\" แล้วให้เพื่อนใส่รหัส " + NetSession.JoinCode + " ในหน้า \"เข้าร่วมห้อง\" (โหมดเดินเล่นร่วมกัน ไม่มีบอท/ภารกิจแมตช์)"
                             : "\"เริ่มเกม\" เล่นออฟไลน์กับบอท · \"สร้างห้องออนไลน์\" เพื่อเดินเล่นร่วมกับเพื่อน", 14, Dim, TextAnchor.UpperLeft, false, true);
            }

            if (Btn(new Rect(70, 836, 200, 48), "← กลับ", false, true, 17)) { GameSettings.Save(); LeaveOnline(); page = Page.Main; }
            if (Btn(new Rect(290, 836, 240, 48), "เลือกตัวละคร", false, true, 17)) g.Select.Show();
            if (!hosting)
            {
                if (Btn(new Rect(550, 836, 300, 48), "สร้างห้อง LAN (วงเดียวกัน)", false, !NetSession.Busy, 17)) DoHostLan();
                if (Btn(new Rect(860, 836, 260, 48), NetSession.Busy ? "กำลังสร้างห้อง..." : "สร้างห้องออนไลน์ (Relay)", false, !NetSession.Busy, 16)) DoHost();
            }
            else if (Btn(new Rect(550, 836, 260, 48), "เริ่มเดินเล่น", false, true, 17)) StartOnline();
            if (Btn(new Rect(1130, 830, 400, 60), "เริ่มเกม", true)) { GameSettings.Save(); g.StartMatch(rs); }
        }

        static Color SlotColor(int i)
        {
            Color[] c = { Mats.Hex(0x4d8dff), Mats.Hex(0x4cdc7a), Mats.Hex(0xff7be5), Mats.Hex(0xffa53a), Mats.Hex(0xf4e04d), Mats.Hex(0xb07dff), Mats.Hex(0x4de0e0), Mats.Hex(0xc9c9d4), Mats.Hex(0xff9ec4) };
            return c[(i - 1) % c.Length];
        }

        void Slot(Rect r, string name, string sub, Color col, bool host)
        {
            Box(r, new Color(0.12f, 0.16f, 0.26f, 0.85f), host ? Yellow : new Color(1, 1, 1, 0.1f), 8f);
            TaskGames.Disc(new Vector2(r.x + 24, r.center.y), 9f, col);
            Text(new Rect(r.x + 48, r.y + 3, 260, 24), name, 17, Txt, TextAnchor.UpperLeft, true);
            Text(new Rect(r.x + 48, r.y + 25, 300, 20), sub, 12, Dim);
            if (host) Text(new Rect(r.xMax - 130, r.y, 118, r.height), "พร้อม", 14, Green, TextAnchor.MiddleRight, true);
        }

        // ───────── เข้าร่วมห้อง ─────────
        void DrawJoin(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var p = new Rect(CW / 2 - 400, 220, 800, 440);
            if (rep) { Box(p, Panel, Edge, 16f); Text(new Rect(p.x + 32, p.y + 18, 600, 40), "เข้าร่วมห้อง", 32, Yellow, TextAnchor.MiddleLeft, true); }
            Label(new Rect(p.x + 32, p.y + 100, 200, 46), "รหัสห้อง");
            string c = TextBox(new Rect(p.x + 200, p.y + 100, 320, 46), joinCode, 21, "IP (192.168.x.x) หรือรหัส Relay");
            joinCode = c.Trim(); if (!joinCode.Contains(".") && !joinCode.Contains(":")) joinCode = joinCode.ToUpperInvariant();
            if (Btn(new Rect(p.x + 540, p.y + 100, 220, 46), NetSession.Busy ? "กำลังเชื่อมต่อ..." : "เข้าร่วม", true, !NetSession.Busy && joinCode.Length > 0)) DoJoin(joinCode);
            if (rep)
            {
                bool err = NetSession.Error != "";
                Color ec = err ? Red : Yellow;
                Box(new Rect(p.x + 32, p.y + 180, p.width - 64, 120), err ? new Color(0.25f, 0.06f, 0.06f, 0.9f) : new Color(0.2f, 0.12f, 0.04f, 0.9f), new Color(ec.r, ec.g, ec.b, 0.5f), 12f);
                string msg = err ? NetSession.Error : NetSession.Busy ? NetSession.Status : "ใส่ IP ของเพื่อน (เช่น 192.168.1.5) = เล่น LAN · ใส่รหัส Relay = เล่นออนไลน์"+(joinCode.Length > 0 ? "\nตรวจพบ: " + (joinCode.Contains(".") ? "โหมด LAN (IP)" : "โหมดออนไลน์ (รหัส Relay)") : "")+"\nโหมดออนไลน์ตอนนี้เป็นการเดินเล่นร่วมกัน (ยังไม่มีบอท/ภารกิจแมตช์)";
                Text(new Rect(p.x + 52, p.y + 192, p.width - 104, 100), msg, 16, Txt, TextAnchor.UpperLeft, false, true);
            }
            if (Btn(new Rect(p.x + 32, p.yMax - 70, 200, 48), "← กลับ", false, true, 17)) { LeaveOnline(); page = Page.Main; }
            if (Btn(new Rect(p.xMax - 332, p.yMax - 70, 300, 48), "สร้างห้องแทน", true, true, 17)) page = Page.Lobby;
        }

        // ───────── ตั้งค่า ─────────
        void DrawSettings(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var S = GameSettings.S;
            var p = new Rect(CW / 2 - 520, 70, 1040, 760);
            if (rep) { Box(p, Panel, Edge, 16f); Text(new Rect(p.x + 36, p.y + 18, 400, 40), "ตั้งค่า", 32, Yellow, TextAnchor.MiddleLeft, true); }

            string[] tabs = { "ภาพ", "เสียง", "การเล่น", "ปุ่มควบคุม" };
            for (int i = 0; i < tabs.Length; i++)
                if (TabBtn(new Rect(p.x + 36 + i * 185, p.y + 76, 175, 44), tabs[i], tab == i)) tab = i;

            float x = p.x + 40, w = p.width - 80, y = p.y + 150, rh = 56f;
            if (tab == 0)
            {
                int dm = ChoiceRow(new Rect(x, y, w, 48), "โหมดหน้าจอ", S.displayMode, GameSettings.DisplayModes);
                if (dm != S.displayMode) { S.displayMode = dm; if (dm == 2 && S.resW == 0) { S.resW = 1920; S.resH = 1080; } GameSettings.ApplyDisplay(); }
                y += rh;

                resList ??= GameSettings.Resolutions();
                bool locked = S.displayMode == 0;
                var cur = locked ? GameSettings.NativeResolution() : new Vector2Int(S.resW > 0 ? S.resW : Screen.width, S.resH > 0 ? S.resH : Screen.height);
                int ri = resList.FindIndex(v => v == cur);
                if (ri < 0) { resList.Add(cur); resList.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y)); ri = resList.FindIndex(v => v == cur); }
                var names = resList.ConvertAll(v => $"{v.x} × {v.y}");
                int nr = ChoiceRow(new Rect(x, y, w, 48), "ความละเอียด" + (locked ? " (ตามหน้าจอ)" : ""), ri, names.ToArray(), !locked);
                if (nr != ri && !locked) { S.resW = resList[nr].x; S.resH = resList[nr].y; GameSettings.ApplyDisplay(); }
                y += rh;

                bool vs = ToggleRow(new Rect(x, y, w, 48), "V-Sync (ซิงค์กับจอ)", S.vsync);
                if (vs != S.vsync) { S.vsync = vs; GameSettings.ApplyPerformance(); }
                y += rh;

                int fi = System.Array.IndexOf(GameSettings.FpsCaps, S.fpsCap); if (fi < 0) fi = 0;
                var fn = new string[GameSettings.FpsCaps.Length];
                for (int i = 0; i < fn.Length; i++) fn[i] = GameSettings.FpsCaps[i] == 0 ? "ไม่จำกัด" : GameSettings.FpsCaps[i] + " FPS";
                int nf = ChoiceRow(new Rect(x, y, w, 48), "จำกัดเฟรมเรต" + (S.vsync ? " (ปิดเมื่อเปิด V-Sync)" : ""), fi, fn, !S.vsync);
                if (nf != fi) { S.fpsCap = GameSettings.FpsCaps[nf]; GameSettings.ApplyPerformance(); }
                y += rh;

                int q = ChoiceRow(new Rect(x, y, w, 48), "คุณภาพภาพ (เงา/ลดรอยหยัก)", S.quality, GameSettings.Qualities);
                if (q != S.quality) { S.quality = q; GameSettings.ApplyQuality(); }
                y += rh;

                float us = SliderRow(10, new Rect(x, y, w, 48), "ขนาด UI (สเกลหน้าจอ)", S.uiScale, 0.7f, 1.6f, v => $"{Mathf.Round(v * 20f) / 20f * 100f:F0}%");
                S.uiScale = Mathf.Round(us * 20f) / 20f;
                y += rh;
                if (rep) Text(new Rect(x, y, w, 40), "ขนาด UI มีผลกับ HUD/มินิเกมในเกม · เมนูปรับสเกลตามหน้าจอเองอัตโนมัติ", 13, Dim);
            }
            else if (tab == 1)
            {
                S.master = SliderRow(11, new Rect(x, y, w, 48), "เสียงรวม", S.master, 0f, 1f, v => $"{Mathf.RoundToInt(v * 100)}%"); y += rh;
                S.sfx = SliderRow(12, new Rect(x, y, w, 48), "เสียงเอฟเฟกต์ (ก้าวเดิน/ไอ/ปุ่ม)", S.sfx, 0f, 1f, v => $"{Mathf.RoundToInt(v * 100)}%"); y += rh;
                S.music = SliderRow(13, new Rect(x, y, w, 48), "เสียงบรรยากาศ", S.music, 0f, 1f, v => $"{Mathf.RoundToInt(v * 100)}%"); y += rh;
                bool mu = ToggleRow(new Rect(x, y, w, 48), "ปิดเสียงทั้งหมด (ปุ่ม N)", S.mute);
                if (mu != S.mute) { S.mute = mu; }
                GameSettings.ApplyAudio();
                y += rh;
                if (Btn(new Rect(x, y + 8, 260, 46), "ทดสอบเสียง", false, true, 17)) g.Blip(1.2f, 0.8f);
            }
            else if (tab == 2)
            {
                string nm = TextRow(new Rect(x, y, w, 48), "ชื่อผู้เล่น", S.playerName, 14);
                S.playerName = nm; y += rh;
                S.camSpeed = Mathf.Round(SliderRow(14, new Rect(x, y, w, 48), "ความไวการหมุนกล้อง (Q/E)", S.camSpeed, 0.4f, 2.5f, v => $"{Mathf.Round(v * 10f) / 10f:F1}×") * 10f) / 10f; y += rh;
                S.camShake = ToggleRow(new Rect(x, y, w, 48), "กล้องสั่นเมื่อมีอาการป่วย", S.camShake); y += rh;
                S.nameTags = ToggleRow(new Rect(x, y, w, 48), "แสดงป้ายชื่อเหนือหัวตัวละคร", S.nameTags); y += rh;
                S.showHelp = ToggleRow(new Rect(x, y, w, 48), "แสดงกล่องคำอธิบายปุ่มมุมขวาล่าง", S.showHelp); y += rh;
                S.showFps = ToggleRow(new Rect(x, y, w, 48), "แสดงเฟรมเรต (FPS)", S.showFps); y += rh;
                Label(new Rect(x, y, 300, 48), "ภาษา");
                if (rep) Text(new Rect(x + 300, y, 300, 48), "ไทย", 18, Txt, TextAnchor.MiddleLeft);
            }
            else
            {
                string[,] keys =
                {
                    { "W A S D / ลูกศร", "เดิน (สัมพันธ์กับทิศกล้อง)" }, { "Q / E", "หมุนกล้อง" }, { "ล้อเมาส์", "ซูมเข้า/ออก" },
                    { "M / Tab", "แผนที่ทั้งหมด (Overview)" }, { "F / Space", "ทำภารกิจที่จุดเหลือง · เรียกประชุมที่ปุ่มแดงโรงอาหาร" },
                    { "B", "กัด (ผู้ติดเชื้อ)" }, { "T", "ตรวจเลือด (หมอ)" }, { "G", "ยิง (ตำรวจ)" },
                    { "V", "เปิด/ปิดหมอก" }, { "N", "เปิด/ปิดเสียง" }, { "C / [ ]", "เลือก/สลับตัวละคร" },
                    { "Esc", "พักเกม / ปิดมินิเกม" }, { "F11", "สลับเต็มจอ/หน้าต่าง" },
                };
                for (int i = 0; i < keys.GetLength(0); i++)
                {
                    if (rep)
                    {
                        Box(new Rect(x, y + i * 40, 190, 34), new Color(0.14f, 0.18f, 0.3f, 0.9f), new Color(1, 1, 1, 0.15f), 6f);
                        Text(new Rect(x, y + i * 40, 190, 34), keys[i, 0], 15, Yellow, TextAnchor.MiddleCenter, true);
                        Text(new Rect(x + 210, y + i * 40, w - 210, 34), keys[i, 1], 16, Txt, TextAnchor.MiddleLeft);
                    }
                }
            }

            if (Btn(new Rect(p.x + 40, p.yMax - 72, 260, 48), "คืนค่าเริ่มต้น", false, true, 17)) { GameSettings.ResetDefaults(); resList = null; Toast("คืนค่าเริ่มต้นแล้ว"); }
            if (Btn(new Rect(p.xMax - 300, p.yMax - 72, 260, 48), "ปิด", true, true, 17)) { GameSettings.Save(); page = ret; }
            if (e.type == EventType.MouseUp) GameSettings.Save();
        }

        // ───────── วิธีเล่น ─────────
        void DrawHowTo(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var p = new Rect(CW / 2 - 640, 60, 1280, 780);
            if (rep)
            {
                Box(p, Panel, Edge, 16f);
                Text(new Rect(p.x + 40, p.y + 18, 600, 40), "วิธีเล่น", 32, Yellow, TextAnchor.MiddleLeft, true);
                float cx1 = p.x + 40, cx2 = p.x + 660, y = p.y + 84;
                Section(cx1, y, 580, "เป้าหมาย", "<color=#4cff8a>ผู้รอดชีวิต</color> ทำภารกิจเพื่อผลิตวัคซีนให้ครบ และกักกันผู้ติดเชื้อต้นตอ\n<color=#ff5a5a>ผู้ติดเชื้อ</color> ซ่อนตัว กัดผู้อื่นตอนไม่มีใครเห็น จนมีจำนวนมากกว่าหรือเท่ากับผู้รอดชีวิต");
                Section(cx1, y + 150, 580, "การติดเชื้อ", "ถูกกัดแล้วจะ <b>ฟักเชื้อ 30–60 วินาที</b> ก่อนกลายเป็นซอมบี้\nผู้ฟักเชื้อมีอาการ: ไอ จอแดง กล้องสั่น — สังเกตคนรอบตัว แต่ผู้รอดชีวิตที่เครียดก็ไอหลอกได้");
                Section(cx1, y + 300, 580, "ประชุมและโหวต", "กดปุ่มแดงที่โต๊ะโรงอาหาร (คนละ 1 ครั้ง) หรือรอรุ่งเช้า\nโหวตกักกันผู้ต้องสงสัย — ผู้ถูกกักกันจะถูกเปิดเผยว่าติดเชื้อจริงหรือไม่");
                Section(cx1, y + 440, 580, "กลางวัน-กลางคืน", "กลางวันทำภารกิจ · กลางคืนไฟดับ ซอมบี้ NPC บุกเข้ามา ผู้ติดเชื้อใช้ความมืดบังตัว");
                Section(cx2, y, 580, "อาชีพ", "<b>หมอ</b>  ตรวจเลือด (T) 2 ครั้ง ผลออกใน 20 วินาที\n<b>ตำรวจ</b>  มีปืนพกติดตัว ยิง (G) 1 นัด — ยิงผิดคนคุณตาย\n<b>นักวิทยาศาสตร์</b>  เห็นความคืบหน้าวัคซีนละเอียด\n<b>ผู้รอดชีวิต</b>  ไม่มีความสามารถพิเศษ แต่ไม่ถูกสงสัยมากที่สุด");
                Section(cx2, y + 190, 580, "ปุ่มหลัก", "WASD เดิน · F ทำภารกิจ/เรียกประชุม · B กัด · T ตรวจเลือด · G ยิง\nQ/E หมุนกล้อง · M แผนที่ · Esc พักเกม · C เลือกตัวละคร");
                Section(cx2, y + 340, 580, "เคล็ดลับ", "อยู่รวมกลุ่มตอนกลางคืน · สังเกตว่าใครอยู่ใกล้ผู้ป่วยตอนก่อนป่วย · อย่าโหวตตามกระแสเสมอไป");
            }
            if (Btn(new Rect(p.xMax - 280, p.yMax - 70, 240, 48), "ปิด", true, true, 17)) page = ret;
        }

        void Section(float x, float y, float w, string title, string body)
        {
            Text(new Rect(x, y, w, 28), title, 20, Cyan, TextAnchor.UpperLeft, true);
            Text(new Rect(x, y + 32, w, 150), body, 16, Txt, TextAnchor.UpperLeft, false, true);
        }

        // ───────── เมนูพัก ─────────
        void DrawPause(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var p = new Rect(CW / 2 - 260, 150, 520, 600);
            if (rep) { Box(p, Panel, Edge, 16f); Text(new Rect(p.x, p.y + 22, p.width, 50), "พักเกม", 34, Yellow, TextAnchor.MiddleCenter, true); }
            float y = p.y + 100; const float bh = 56, gap = 12;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "ต่อเกม", true)) g.Resume();
            y += bh + gap;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "ตั้งค่า")) { ret = Page.Pause; page = Page.Settings; tab = 0; }
            y += bh + gap;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "วิธีเล่น")) { ret = Page.Pause; page = Page.HowTo; }
            y += bh + gap;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "เริ่มแมตช์ใหม่")) Confirm("เริ่มแมตช์ใหม่? ความคืบหน้าปัจจุบันจะหายไป", () => g.RestartMatch(), Page.Pause);
            y += bh + gap;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "กลับเมนูหลัก")) Confirm("กลับเมนูหลัก? ความคืบหน้าปัจจุบันจะหายไป", () => g.BackToMenu(), Page.Pause);
            y += bh + gap;
            if (Btn(new Rect(p.x + 50, y, p.width - 100, bh), "ออกจากเกม")) Confirm("ออกจากเกมหรือไม่?", Quit, Page.Pause);
        }

        void Confirm(string text, System.Action yes, Page back) { confirmText = text; confirmYes = yes; confirmBack = back; page = Page.Confirm; }

        void DrawConfirm(Event e)
        {
            bool rep = e.type == EventType.Repaint;
            var p = new Rect(CW / 2 - 330, 330, 660, 240);
            if (rep) { Box(p, Panel, new Color(Yellow.r, Yellow.g, Yellow.b, 0.6f), 16f); Text(new Rect(p.x + 30, p.y + 34, p.width - 60, 90), confirmText, 22, Txt, TextAnchor.MiddleCenter, true, true); }
            if (Btn(new Rect(p.x + 50, p.yMax - 80, 250, 52), "ยกเลิก", false, true, 18)) page = confirmBack;
            if (Btn(new Rect(p.xMax - 300, p.yMax - 80, 250, 52), "ตกลง", true, true, 18)) { var y = confirmYes; page = confirmBack; y?.Invoke(); }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ═════════════ วิดเจ็ต ═════════════
        void Fill(Rect r, Color c, float rad = 0f) => TaskGames.Fill(r, c, rad);

        void Box(Rect r, Color fill, Color edge, float rad = 10f)
        {
            if (Event.current.type != EventType.Repaint) return;
            TaskGames.Fill(r, fill, rad);
            TaskGames.Ring(r, edge, rad, 1f);
        }

        void Text(Rect r, string s, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false, bool wrap = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            st.fontSize = size; st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; st.normal.textColor = c; st.wordWrap = wrap;
            GUI.Label(r, s, st);
            st.wordWrap = false;
        }

        void Label(Rect r, string s) => Text(r, s, 18, Txt, TextAnchor.MiddleLeft);

        bool Hover(Rect r) => r.Contains(Event.current.mousePosition);

        bool Click(Rect r)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        bool Btn(Rect r, string label, bool primary = false, bool enabled = true, int size = 20)
        {
            var e = Event.current;
            if (e.type == EventType.Repaint)
            {
                bool h = enabled && Hover(r);
                Color fill = !enabled ? new Color(0.14f, 0.16f, 0.22f, 0.8f)
                           : primary ? (h ? Color.Lerp(Yellow, Color.white, 0.25f) : Yellow)
                           : (h ? new Color(0.24f, 0.3f, 0.46f, 0.98f) : new Color(0.14f, 0.18f, 0.3f, 0.92f));
                Fill(r, fill, 12f);
                if (!primary) TaskGames.Ring(r, new Color(1, 1, 1, h ? 0.4f : 0.16f), 12f, 1f);
                Text(r, label, size, !enabled ? new Color(1, 1, 1, 0.3f) : primary ? new Color(0.06f, 0.07f, 0.12f) : Txt, TextAnchor.MiddleCenter, true);
            }
            return enabled && Click(r);
        }

        bool TabBtn(Rect r, string label, bool on)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fill(r, on ? new Color(Yellow.r, Yellow.g, Yellow.b, 0.2f) : (Hover(r) ? new Color(1, 1, 1, 0.08f) : new Color(1, 1, 1, 0.04f)), 10f);
                if (on) TaskGames.Ring(r, Yellow, 10f, 1.5f);
                Text(r, label, 18, on ? Yellow : Txt, TextAnchor.MiddleCenter, true);
            }
            return Click(r);
        }

        float SliderRow(int id, Rect r, string label, float v, float min, float max, System.Func<float, string> fmt)
        {
            var e = Event.current;
            Label(new Rect(r.x, r.y, 380, r.height), label);
            var track = new Rect(r.x + 400, r.center.y - 5, r.width - 400 - 150, 10);
            var hit = new Rect(track.x - 12, r.y, track.width + 24, r.height);
            float k = Mathf.InverseLerp(min, max, v);
            if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition)) { dragId = id; e.Use(); }
            if (dragId == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                k = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                v = Mathf.Lerp(min, max, k);
                e.Use();
            }
            if (dragId == id && e.type == EventType.MouseUp) { dragId = -1; GameSettings.Save(); e.Use(); }
            if (e.type == EventType.Repaint)
            {
                Fill(track, new Color(1, 1, 1, 0.14f), 5f);
                Fill(new Rect(track.x, track.y, Mathf.Max(8f, track.width * k), track.height), Yellow, 5f);
                TaskGames.Disc(new Vector2(track.x + track.width * k, track.center.y), dragId == id ? 13f : 11f, Color.white);
                Text(new Rect(track.xMax + 20, r.y, 130, r.height), fmt(v), 17, Txt, TextAnchor.MiddleLeft, true);
            }
            return v;
        }

        int ChoiceRow(Rect r, string label, int idx, string[] opts, bool enabled = true)
        {
            Label(new Rect(r.x, r.y, 380, r.height), label);
            var box = new Rect(r.x + 400, r.y, r.width - 400, r.height);
            var left = new Rect(box.x, box.y, 56, box.height);
            var right = new Rect(box.xMax - 56, box.y, 56, box.height);
            idx = Mathf.Clamp(idx, 0, opts.Length - 1);
            if (Event.current.type == EventType.Repaint)
            {
                Fill(box, new Color(0.12f, 0.16f, 0.26f, enabled ? 0.95f : 0.5f), 10f);
                TaskGames.Ring(box, new Color(1, 1, 1, 0.14f), 10f, 1f);
                Color ac = enabled ? Yellow : new Color(1, 1, 1, 0.2f);
                Text(left, "◀", 18, Hover(left) && enabled ? Color.white : ac, TextAnchor.MiddleCenter, true);
                Text(right, "▶", 18, Hover(right) && enabled ? Color.white : ac, TextAnchor.MiddleCenter, true);
                Text(box, opts[idx], 18, enabled ? Txt : Dim, TextAnchor.MiddleCenter, true);
            }
            if (enabled)
            {
                if (Click(left)) idx = (idx - 1 + opts.Length) % opts.Length;
                else if (Click(right)) idx = (idx + 1) % opts.Length;
                else if (Click(new Rect(box.x + 56, box.y, box.width - 112, box.height))) idx = (idx + 1) % opts.Length;
            }
            return idx;
        }

        bool ToggleRow(Rect r, string label, bool v)
        {
            Label(new Rect(r.x, r.y, 600, r.height), label);
            var box = new Rect(r.xMax - 90, r.center.y - 16, 80, 32);
            if (Event.current.type == EventType.Repaint)
            {
                Fill(box, v ? new Color(Green.r, Green.g, Green.b, 0.35f) : new Color(1, 1, 1, 0.12f), 16f);
                TaskGames.Ring(box, v ? Green : new Color(1, 1, 1, 0.25f), 16f, 1.5f);
                TaskGames.Disc(new Vector2(v ? box.xMax - 16 : box.x + 16, box.center.y), 11f, v ? Green : new Color(1, 1, 1, 0.6f));
            }
            if (Click(new Rect(r.x, r.y, r.width, r.height))) v = !v;
            return v;
        }

        string TextBox(Rect r, string v, int max, string hint)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Fill(r, new Color(0.08f, 0.1f, 0.18f, 0.95f), 10f);
                TaskGames.Ring(r, new Color(1, 1, 1, Hover(r) ? 0.4f : 0.2f), 10f, 1f);
                if (string.IsNullOrEmpty(v)) Text(new Rect(r.x + 12, r.y, r.width, r.height), hint, 18, new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft);
            }
            tf.fontSize = 18;
            return GUI.TextField(r, v ?? "", max, tf);
        }

        string TextRow(Rect r, string label, string v, int max)
        {
            Label(new Rect(r.x, r.y, 380, r.height), label);
            return TextBox(new Rect(r.x + 400, r.y, r.width - 400, r.height), v, max, "");
        }

        static float Step10(float v, float step = 10f) => Mathf.Round(v / step) * step;
    }
}
