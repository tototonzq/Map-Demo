using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // UI ของ Patient Zero (IMGUI): ป้ายชื่อเหนือหัว · เวลา/วัคซีน · การ์ดบทบาท · ข่าวสาร · อาการ · หน้าเปิดเผยบทบาท · ประชุมโหวต · สรุปผล
    public sealed class PZHud
    {
        readonly SkeldGame g;
        Font font;
        GUIStyle st;
        PZ pz => g.PZ;

        static readonly Color Panel = new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.82f);
        static readonly Color Edge = new Color(160 / 255f, 190 / 255f, 255 / 255f, 0.3f);
        static readonly Color Txt = new Color(232 / 255f, 238 / 255f, 252 / 255f, 1f);
        static readonly Color Green = Mats.Hex(0x4cff8a), Red = Mats.Hex(0xff5a5a), Yellow = Mats.Hex(0xffd23f), Cyan = Mats.Hex(0x55e0ff);

        public PZHud(SkeldGame game) { g = game; }

        void EnsureStyle()
        {
            if (st != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Thonburi", "Segoe UI", "Arial" }, 16);
            st = new GUIStyle(GUI.skin.label) { font = font, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
        }

        void T(Rect r, string s, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false, bool wrap = false)
        {
            if (Event.current.type != EventType.Repaint) return;
            st.fontSize = size; st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            st.normal.textColor = c; st.wordWrap = wrap;
            GUI.Label(r, s, st);
            st.wordWrap = false;
        }

        static void Fill(Rect r, Color c, float rad = 0f) => TaskGames.Fill(r, c, rad);

        void Box(Rect r, Color fill, Color edge, float rad = 10f)
        {
            if (Event.current.type != EventType.Repaint) return;
            Fill(r, fill, rad);
            TaskGames.Ring(r, edge, rad, 1f);
        }

        static string Clock(float t) { int s = Mathf.Max(0, Mathf.CeilToInt(t)); return $"{s / 60}:{s % 60:00}"; }

        // ═════════════ main ═════════════
        public void OnGUI()
        {
            if (pz == null) return;
            var e = Event.current;
            EnsureStyle();
            float s = GameSettings.GuiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = Screen.height / s;
            bool rep = e.type == EventType.Repaint;

            if (rep && pz.phase != Phase.Reveal)
            {
                DrawVignette(W, H);
                DrawTags(s);
                DrawTop(W);
                float cardBottom = DrawRoleCard();
                DrawFeed(cardBottom + 10f);
                DrawHints(W, H);
            }

            if (pz.phase == Phase.Day || pz.phase == Phase.Night) { BiteButton(e, W, H); ShootButton(e, W, H); }

            switch (pz.phase)
            {
                case Phase.Reveal: if (rep) DrawReveal(W, H); break;
                case Phase.Meeting: DrawMeeting(e, W, H); break;
                case Phase.GameOver: if (rep) DrawGameOver(W, H); break;
            }
            GUI.matrix = Matrix4x4.identity;
        }

        // ═════════════ ปุ่มกัด (เฉพาะผู้ติดเชื้อต้นตอ) ═════════════
        void BiteButton(Event e, float W, float H)
        {
            var me = pz.me;
            if (me.role != Role.Infected || !pz.CanAct || g.Games.Open || g.Paused) return;
            bool alsoPolice = me.job == Job.Police && pz.abilitiesOn;
            var r = new Rect(W / 2f - 135f - (alsoPolice ? 145f : 0f), H - 88f, 270f, 68f);
            var cand = pz.BiteCandidate();
            bool cooling = me.biteCd > 0f;
            bool ready = !cooling && cand != null;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { pz.HumanBite(); e.Use(); return; }
            if (e.type != EventType.Repaint) return;

            bool hover = r.Contains(e.mousePosition);
            Color baseC = ready ? new Color(0.62f, 0.08f, 0.1f, 0.96f) : new Color(0.16f, 0.1f, 0.12f, 0.92f);
            if (ready && hover) baseC = new Color(0.8f, 0.12f, 0.14f, 0.98f);
            float pulse = ready ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            Box(r, baseC, ready ? new Color(1f, 0.35f + 0.3f * pulse, 0.35f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.3f), 14f);
            T(new Rect(r.x, r.y + 8, r.width, 28), "กัด  [B]", 24, ready ? Color.white : new Color(1, 1, 1, 0.55f), TextAnchor.UpperCenter, true);
            string sub = cooling ? $"รอคูลดาวน์ {Mathf.CeilToInt(me.biteCd)} วินาที" : cand != null ? $"เป้าหมาย: {cand.name}" : "ไม่มีผู้รอดชีวิตอยู่ใกล้";
            T(new Rect(r.x, r.y + 38, r.width, 16), sub, 12, new Color(1, 1, 1, ready ? 0.9f : 0.5f), TextAnchor.UpperCenter);
            var bar = new Rect(r.x + 16, r.yMax - 10, r.width - 32, 4);
            Fill(bar, new Color(1, 1, 1, 0.14f), 2f);
            float k = cooling ? 1f - Mathf.Clamp01(me.biteCd / PZ.BiteCooldown) : 1f;
            if (k > 0.01f) Fill(new Rect(bar.x, bar.y, bar.width * k, bar.height), ready ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 1f, 1f, 0.45f), 2f);
        }

        // ═════════════ ปุ่มยิง (ตำรวจ) ═════════════
        void ShootButton(Event e, float W, float H)
        {
            var me = pz.me;
            if (me.job != Job.Police || !pz.abilitiesOn || !pz.CanAct || g.Games.Open || g.Paused) return;
            bool alsoInfected = me.role == Role.Infected;
            var r = new Rect(W / 2f - 135f + (alsoInfected ? 145f : 0f), H - 88f, 270f, 68f);
            var cand = me.ammo > 0 ? pz.ShootCandidate(me) : null;
            bool ready = me.ammo > 0 && cand != null;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { pz.HumanShoot(); e.Use(); return; }
            if (e.type != EventType.Repaint) return;

            bool hover = r.Contains(e.mousePosition);
            Color baseC = ready ? new Color(0.1f, 0.26f, 0.5f, 0.96f) : new Color(0.1f, 0.12f, 0.18f, 0.92f);
            if (ready && hover) baseC = new Color(0.14f, 0.36f, 0.7f, 0.98f);
            float pulse = ready ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            Box(r, baseC, ready ? new Color(0.45f + 0.3f * pulse, 0.75f, 1f, 0.9f) : new Color(0.5f, 0.7f, 1f, 0.3f), 14f);
            T(new Rect(r.x, r.y + 8, r.width, 28), "ยิง  [G]", 24, ready ? Color.white : new Color(1, 1, 1, 0.55f), TextAnchor.UpperCenter, true);
            string sub = me.ammo <= 0 ? "กระสุนหมดแล้ว" : cand != null ? $"เล็งอยู่: {cand.name} · เหลือ {me.ammo} นัด" : $"หันหน้าไปทางเป้าหมาย · เหลือ {me.ammo} นัด";
            T(new Rect(r.x, r.y + 38, r.width, 16), sub, 12, new Color(1, 1, 1, ready ? 0.9f : 0.5f), TextAnchor.UpperCenter);
        }

        // ═════════════ เหนือหัวตัวละคร ═════════════
        void DrawTags(float s)
        {
            var cam = g.DebugCamera;
            if (cam == null || g.Overview || !GameSettings.S.nameTags) return;
            float maxD = pz.phase == Phase.Night ? 6.5f : 11.5f;
            Actor biteTarget = pz.me.role == Role.Infected && pz.CanAct && pz.me.biteCd <= 0f ? pz.BiteCandidate() : null;
            Actor aimTarget = pz.me.job == Job.Police && pz.abilitiesOn && pz.CanAct && pz.me.ammo > 0 ? pz.ShootCandidate(pz.me) : null;
            foreach (var a in pz.actors)
            {
                if (!a.OnMap) continue;
                if (a.human && !a.Alive) continue;
                float d = PZ.Dist(a, pz.me);
                if (d > maxD && !a.human) continue;
                Vector3 sp = cam.WorldToScreenPoint(MapData.ToWorld(a.x, a.human ? 2.1f : 2.0f, a.z));
                if (sp.z <= 0f) continue;
                float gx = sp.x / s, gy = (Screen.height - sp.y) / s;
                float alpha = a.human ? 1f : Mathf.Clamp01((maxD - d) / 3f);
                if (a.npc) continue;
                string nm = a.name;
                Color col = a.color;
                string sub = PZ.JobName(a.job);
                if (a.health == Health.Zombie) { col = Red; sub = "ซอมบี้"; }
                else if (pz.me.role == Role.Infected && a.role == Role.Infected && !a.human) sub += " · พวกเดียวกัน";
                var r = new Rect(gx - 70, gy - 26, 140, 20);
                Fill(new Rect(gx - 62, gy - 24, 124, 32), new Color(0, 0, 0, 0.45f * alpha), 8f);
                TaskGames.Disc(new Vector2(gx - 50, gy - 14), 4f, new Color(col.r, col.g, col.b, alpha));
                T(new Rect(gx - 42, gy - 24, 100, 16), nm, 12, new Color(col.r, col.g, col.b, alpha), TextAnchor.UpperLeft, true);
                T(new Rect(gx - 56, gy - 9, 118, 14), sub, 10, new Color(1, 1, 1, 0.7f * alpha), TextAnchor.UpperCenter);
                if (a == aimTarget)
                {
                    float pl2 = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 8f);
                    T(new Rect(gx - 50, gy - 62, 100, 18), "◎ เล็งอยู่", 13, new Color(0.55f, 0.85f, 1f, pl2), TextAnchor.UpperCenter, true);
                }
                if (a == biteTarget)
                {
                    float pl = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 8f);
                    T(new Rect(gx - 50, gy - 46, 100, 18), "▼ กัดได้", 13, new Color(1f, 0.35f, 0.35f, pl), TextAnchor.UpperCenter, true);
                }
                if (a.bubbleT > 0f)
                {
                    var br = new Rect(gx - 28, gy - 52, 56, 22);
                    Box(br, new Color(1, 1, 1, 0.9f * alpha), new Color(0, 0, 0, 0.5f * alpha), 8f);
                    T(br, a.bubble, 13, new Color(0.1f, 0.1f, 0.15f, alpha), TextAnchor.MiddleCenter, true);
                }
            }
        }

        // ═════════════ อาการ (จอแดง) ═════════════
        void DrawVignette(float W, float H)
        {
            float lv = pz.SymptomLevel;
            if (lv > 0.01f)
            {
                var c = new Color(0.75f, 0.05f, 0.05f);
                Fill(new Rect(0, 0, W, H), new Color(c.r, c.g, c.b, 0.12f * lv));
                float b = 70f;
                Fill(new Rect(0, 0, W, b), new Color(c.r, c.g, c.b, 0.3f * lv));
                Fill(new Rect(0, H - b, W, b), new Color(c.r, c.g, c.b, 0.3f * lv));
                Fill(new Rect(0, 0, b, H), new Color(c.r, c.g, c.b, 0.3f * lv));
                Fill(new Rect(W - b, 0, b, H), new Color(c.r, c.g, c.b, 0.3f * lv));
            }
            if (pz.biteFlash > 0f) Fill(new Rect(0, 0, W, H), new Color(0.8f, 0.02f, 0.02f, pz.biteFlash * 0.9f));
            if (pz.shotFlash > 0f) Fill(new Rect(0, 0, W, H), new Color(1, 1, 1, pz.shotFlash * 1.4f));
        }

        // ═════════════ เวลา + วัคซีน ═════════════
        void DrawTop(float W)
        {
            var r = new Rect(W / 2f - 190, 14, 380, 66);
            bool night = pz.phase == Phase.Night;
            Box(r, Panel, night ? new Color(1f, 0.35f, 0.35f, 0.5f) : Edge);
            string title; Color tc = Txt;
            if (pz.phase == Phase.Day) title = $"วันที่ {pz.day} · กลางวัน  {Clock(pz.dayT)}";
            else if (night) { title = $"คืนที่ {pz.day} · กลางคืน  {Clock(pz.nightT)}"; tc = Red; }
            else if (pz.phase == Phase.Meeting) title = "กำลังประชุม";
            else title = "PATIENT ZERO";
            T(new Rect(r.x, r.y + 6, r.width, 22), title, 16, tc, TextAnchor.UpperCenter, true);

            float prog = pz.needTotal <= 0 ? 0f : Mathf.Clamp01(pz.doneTotal / pz.needTotal);
            var bar = new Rect(r.x + 20, r.y + 36, r.width - 40, 10);
            Fill(bar, new Color(1, 1, 1, 0.14f), 5f);
            if (prog > 0.002f) Fill(new Rect(bar.x, bar.y, Mathf.Max(10f, bar.width * prog), bar.height), Green, 5f);
            string lbl = pz.me.job == Job.Scientist ? $"วัคซีน {prog * 100f:F0}%  ({pz.doneTotal:F0}/{pz.needTotal:F0} ภารกิจ)" : "ความคืบหน้าวัคซีน";
            T(new Rect(r.x, r.y + 47, r.width, 16), lbl, 11, new Color(1, 1, 1, 0.65f), TextAnchor.UpperCenter);
        }

        // ═════════════ การ์ดบทบาท ═════════════
        float DrawRoleCard()
        {
            var me = pz.me;
            var lines = new List<KeyValuePair<string, Color>>();
            bool inf = me.role == Role.Infected;
            lines.Add(new KeyValuePair<string, Color>(inf ? "คุณคือ ผู้ติดเชื้อต้นตอ" : "คุณคือ ผู้รอดชีวิต", inf ? Red : Green));
            lines.Add(new KeyValuePair<string, Color>("อาชีพ: " + PZ.JobName(me.job), Txt));
            if (inf)
            {
                string mates = "";
                foreach (var a in pz.actors) if (a != me && a.role == Role.Infected) mates += (mates == "" ? "" : ", ") + a.name;
                if (mates != "") lines.Add(new KeyValuePair<string, Color>("พวกเดียวกัน: " + mates, new Color(1, 0.7f, 0.7f)));
                lines.Add(new KeyValuePair<string, Color>(me.biteCd > 0f ? $"กัด [B] รออีก {Mathf.CeilToInt(me.biteCd)} วินาที" : "กัด [B] พร้อมแล้ว!", me.biteCd > 0f ? new Color(1, 1, 1, 0.55f) : Yellow));
            }
            if (me.job == Job.Doctor && pz.abilitiesOn)
                lines.Add(new KeyValuePair<string, Color>(pz.testTarget != null ? $"กำลังตรวจเลือด {pz.testTarget.name}… {Mathf.CeilToInt(pz.testT)}s" : $"ตรวจเลือด [T] เหลือ {pz.bloodLeft} ครั้ง", Cyan));
            if (me.job == Job.Police && pz.abilitiesOn) lines.Add(new KeyValuePair<string, Color>($"ยิง [G] เหลือ {me.ammo} นัด · ปืนติดตัว (ยิงผิดคนคุณตาย)", Cyan));
            if (me.job == Job.Scientist) lines.Add(new KeyValuePair<string, Color>("เห็นความคืบหน้าวัคซีนละเอียด", Cyan));
            if (!me.Alive) lines.Add(new KeyValuePair<string, Color>(g.HumanOutText, Red));

            float h = 14f + lines.Count * 19f;
            var r = new Rect(16, 76, 270, h);
            Box(r, Panel, inf ? new Color(1f, 0.35f, 0.35f, 0.45f) : Edge);
            for (int i = 0; i < lines.Count; i++)
                T(new Rect(r.x + 14, r.y + 8 + i * 19, r.width - 20, 18), lines[i].Key, i == 0 ? 14 : 12, lines[i].Value, TextAnchor.UpperLeft, i == 0);
            return r.yMax;
        }

        // ═════════════ ข่าวสาร ═════════════
        void DrawFeed(float y0)
        {
            float now = Time.unscaledTime, y = y0;
            for (int i = pz.feed.Count - 1; i >= 0 && y < y0 + 140f; i--)
            {
                float age = now - pz.feed[i].Key;
                if (age > 9f) continue;
                float a = Mathf.Clamp01((9f - age) / 2f);
                string s = pz.feed[i].Value;
                float w = Mathf.Min(420f, st.CalcSize(new GUIContent(s)).x + 18f);
                Fill(new Rect(16, y, w, 22), new Color(0, 0, 0, 0.5f * a), 6f);
                T(new Rect(24, y + 2, w, 20), s, 12, new Color(1, 1, 1, a), TextAnchor.UpperLeft);
                y += 25f;
            }
        }

        void DrawHints(float W, float H)
        {
            string hint = null; Color c = Yellow;
            if (pz.CanAct && pz.me.role == Role.Infected)
            {
                var v = pz.BiteCandidate();
                if (v != null && pz.me.biteCd <= 0f) { hint = $"กด B เพื่อกัด {v.name}"; c = Red; }
            }
            if (pz.Spectating && pz.phase != Phase.GameOver) { hint = $"{g.HumanOutText} — กำลังรับชม · Space สลับคนที่ดู"; c = Cyan; }
            if (hint == null) return;
            float w = Mathf.Max(300f, st.CalcSize(new GUIContent(hint)).x + 40f);
            var r = new Rect((W - w) / 2f, H - 170f, w, 36);
            Box(r, new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.85f), new Color(c.r, c.g, c.b, 0.6f));
            T(r, hint, 15, c, TextAnchor.MiddleCenter, true);
        }

        // ═════════════ เปิดเผยบทบาท ═════════════
        void DrawReveal(float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.03f, 0.06f, 0.92f));
            bool inf = pz.me.role == Role.Infected;
            float cx = W / 2f, y = H / 2f - 190f;
            T(new Rect(cx - 400, y, 800, 70), "PATIENT ZERO", 58, Red, TextAnchor.MiddleCenter, true);
            T(new Rect(cx - 400, y + 66, 800, 24), "ผู้ติดเชื้ออยู่ในกลุ่มของคุณ — หาให้เจอก่อนสายเกินไป", 15, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
            T(new Rect(cx - 400, y + 130, 800, 50), inf ? "คุณคือ ผู้ติดเชื้อต้นตอ" : "คุณคือ ผู้รอดชีวิต", 36, inf ? Red : Green, TextAnchor.MiddleCenter, true);
            T(new Rect(cx - 400, y + 184, 800, 30), "อาชีพ: " + PZ.JobName(pz.me.job), 20, Txt, TextAnchor.MiddleCenter, true);
            string obj = inf
                ? "ซ่อนตัวและกัดผู้รอดชีวิตตอนไม่มีใครเห็น (กดปุ่ม 'กัด' หรือ B) · ภารกิจของคุณเป็นแค่การหลอก · อย่าให้ถูกโหวตกักกัน"
                : "ทำภารกิจเพื่อผลิตวัคซีน · สังเกตอาการไอของคนรอบตัว · ประชุมโหวตกักกันคนที่สงสัย";
            T(new Rect(cx - 380, y + 236, 760, 60), obj, 15, new Color(1, 1, 1, 0.85f), TextAnchor.UpperCenter, false, true);
            string ab = !pz.abilitiesOn ? "ความสามารถอาชีพถูกปิดในการตั้งค่าห้อง" : pz.me.job == Job.Doctor ? "ความสามารถ: ตรวจเลือด [T] 2 ครั้ง (ผลออกใน 20 วินาที)"
                      : pz.me.job == Job.Police ? "ความสามารถ: มีปืนติดตัว ยิง [G] 1 นัด — ยิงผิดคนคุณตาย"
                      : pz.me.job == Job.Scientist ? "ความสามารถ: เห็นความคืบหน้าวัคซีนแบบละเอียด" : "ไม่มีความสามารถพิเศษ — แต่คุณคือคนที่ไม่ถูกสงสัยมากที่สุด";
            T(new Rect(cx - 380, y + 290, 760, 24), ab, 14, Cyan, TextAnchor.UpperCenter);
            float k = Mathf.Clamp01(pz.phaseT / PZ.RevealLen);
            Fill(new Rect(cx - 150, y + 340, 300, 6), new Color(1, 1, 1, 0.15f), 3f);
            Fill(new Rect(cx - 150, y + 340, 300 * k, 6), inf ? Red : Green, 3f);
            T(new Rect(cx - 300, y + 354, 600, 22), "กด Enter / Space เพื่อเริ่ม", 13, new Color(1, 1, 1, 0.55f), TextAnchor.UpperCenter);
        }

        // ═════════════ ประชุม / โหวต ═════════════
        void DrawMeeting(Event e, float W, float H)
        {
            bool rep = e.type == EventType.Repaint;
            var panel = new Rect((W - 900f) / 2f, (H - 600f) / 2f, 900f, 600f);
            if (rep)
            {
                Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.72f));
                Box(panel, new Color(10 / 255f, 16 / 255f, 30 / 255f, 0.97f), new Color(1f, 0.82f, 0.25f, 0.6f), 14f);
                T(new Rect(panel.x + 28, panel.y + 14, 600, 34), "ประชุมฉุกเฉิน", 26, Yellow, TextAnchor.UpperLeft, true);
                T(new Rect(panel.x + 28, panel.y + 50, 840, 22), pz.meetReason, 14, new Color(1, 1, 1, 0.75f));
                float left = pz.resultShown ? PZ.ResultLen - pz.resultT : PZ.MeetLen - pz.meetT;
                T(new Rect(panel.xMax - 190, panel.y + 18, 160, 30), (pz.resultShown ? "ผลโหวต " : "เหลือ ") + Clock(left), 20, pz.resultShown ? Cyan : Txt, TextAnchor.UpperRight, true);
                float k = pz.resultShown ? 1f - pz.resultT / PZ.ResultLen : 1f - pz.meetT / PZ.MeetLen;
                Fill(new Rect(panel.x + 28, panel.y + 80, panel.width - 56, 5), new Color(1, 1, 1, 0.12f), 3f);
                Fill(new Rect(panel.x + 28, panel.y + 80, (panel.width - 56) * Mathf.Clamp01(k), 5), pz.resultShown ? Cyan : Yellow, 3f);
            }

            float y0 = panel.y + 100f;
            int row = 0;
            Vector2 mp = e.mousePosition;
            bool click = e.type == EventType.MouseDown && e.button == 0 && pz.HumanCanVote;
            foreach (var a in pz.actors)
            {
                if (a.npc) continue;
                var r = new Rect(panel.x + 28, y0 + row * 46, 560, 40);
                row++;
                bool votable = a.OnMap && !a.human;
                bool mine = pz.me.vote == a.id;
                if (click && votable && r.Contains(mp)) { pz.HumanVote(a.id); e.Use(); }
                if (!rep) continue;

                bool hover = votable && pz.HumanCanVote && r.Contains(mp);
                Box(r, hover ? new Color(0.2f, 0.26f, 0.4f, 0.9f) : new Color(0.12f, 0.16f, 0.26f, 0.85f), mine ? Yellow : new Color(1, 1, 1, 0.12f), 8f);
                bool out_ = !a.OnMap;
                float al = out_ ? 0.35f : 1f;
                TaskGames.Disc(new Vector2(r.x + 22, r.y + 20), 9f, new Color(a.color.r, a.color.g, a.color.b, al));
                T(new Rect(r.x + 42, r.y + 4, 220, 20), a.name + (a.human ? " (คุณ)" : ""), 16, new Color(1, 1, 1, al), TextAnchor.UpperLeft, true);
                T(new Rect(r.x + 42, r.y + 22, 220, 16), PZ.JobName(a.job), 11, new Color(1, 1, 1, 0.55f * al));
                string status = a.health == Health.Zombie ? "ซอมบี้" : a.health == Health.Quarantined ? "กักกันแล้ว" : a.health == Health.Dead ? "เสียชีวิต" : "";
                if (status != "") T(new Rect(r.x + 250, r.y + 9, 150, 22), status, 14, a.health == Health.Zombie ? Red : new Color(1, 1, 1, 0.5f), TextAnchor.UpperLeft, true);
                if (pz.me.role == Role.Infected && a.role == Role.Infected && !a.human) T(new Rect(r.x + 330, r.y + 9, 120, 22), "พวกเดียวกัน", 12, new Color(1, 0.6f, 0.6f), TextAnchor.UpperLeft);
                if (a.Alive && !pz.resultShown && a.vote != -2) T(new Rect(r.xMax - 130, r.y + 9, 120, 22), "โหวตแล้ว", 12, Green, TextAnchor.UpperRight);
                if (pz.resultShown && pz.tally.TryGetValue(a.id, out int n))
                    for (int i = 0; i < n; i++) TaskGames.Disc(new Vector2(r.xMax - 20 - i * 18, r.y + 20), 7f, Red);
                if (mine) T(new Rect(r.xMax - 130, r.y + 9, 120, 22), "คุณโหวต", 12, Yellow, TextAnchor.UpperRight, true);
            }

            var skip = new Rect(panel.x + 28, y0 + row * 46 + 4, 560, 38);
            if (click && skip.Contains(mp)) { pz.HumanVote(-1); e.Use(); }
            if (rep)
            {
                bool sk = pz.me.vote == -1;
                Box(skip, skip.Contains(mp) && pz.HumanCanVote ? new Color(0.2f, 0.26f, 0.4f, 0.9f) : new Color(0.12f, 0.16f, 0.26f, 0.85f), sk ? Yellow : new Color(1, 1, 1, 0.12f), 8f);
                T(skip, sk ? "ข้ามการโหวต (เลือกแล้ว)" : "ข้ามการโหวต", 15, Txt, TextAnchor.MiddleCenter, true);
                if (pz.resultShown && pz.skipVotes > 0) T(new Rect(skip.xMax - 120, skip.y + 8, 110, 22), $"ข้าม {pz.skipVotes}", 12, Cyan, TextAnchor.UpperRight);

                // สิ่งที่ผู้เล่นสังเกตเห็น
                var cr = new Rect(panel.x + 612, y0, 260, 360);
                Box(cr, new Color(0.08f, 0.11f, 0.2f, 0.9f), new Color(1, 1, 1, 0.12f), 10f);
                T(new Rect(cr.x + 14, cr.y + 10, cr.width - 20, 20), "สิ่งที่คุณสังเกตเห็น", 14, Cyan, TextAnchor.UpperLeft, true);
                float cy = cr.y + 36;
                if (pz.clues.Count == 0) T(new Rect(cr.x + 14, cy, cr.width - 24, 40), "ยังไม่มีเบาะแส", 12, new Color(1, 1, 1, 0.5f), TextAnchor.UpperLeft, false, true);
                for (int i = Mathf.Max(0, pz.clues.Count - 10); i < pz.clues.Count; i++)
                {
                    T(new Rect(cr.x + 14, cy, cr.width - 24, 34), "• " + pz.clues[i], 12, Txt, TextAnchor.UpperLeft, false, true);
                    cy += 32f;
                }

                if (pz.resultShown)
                {
                    var rr = new Rect(panel.x + 28, panel.yMax - 74, panel.width - 56, 56);
                    Box(rr, new Color(0.18f, 0.12f, 0.05f, 0.95f), Yellow, 10f);
                    T(rr, pz.ejectText, 19, Yellow, TextAnchor.MiddleCenter, true);
                }
                else if (!pz.me.Alive) T(new Rect(panel.x + 28, panel.yMax - 50, panel.width - 56, 30), "คุณไม่ได้อยู่ในการโหวต — กำลังรับชม", 14, new Color(1, 1, 1, 0.6f), TextAnchor.UpperCenter);
                else T(new Rect(panel.x + 28, panel.yMax - 50, panel.width - 56, 30), "คลิกชื่อเพื่อโหวตกักกัน · หรือกดข้าม", 14, new Color(1, 1, 1, 0.6f), TextAnchor.UpperCenter);
            }
            if (e.isMouse) e.Use();
        }

        // ═════════════ สรุปผล ═════════════
        void DrawGameOver(float W, float H)
        {
            bool survWin = pz.winner == "ผู้รอดชีวิต";
            bool meWon = survWin == (pz.me.role == Role.Survivor);
            Fill(new Rect(0, 0, W, H), new Color(0.01f, 0.02f, 0.05f, 0.9f));
            float cx = W / 2f, y = H / 2f - 250f;
            T(new Rect(cx - 400, y, 800, 60), survWin ? "ผู้รอดชีวิตชนะ!" : "ผู้ติดเชื้อชนะ!", 46, survWin ? Green : Red, TextAnchor.MiddleCenter, true);
            T(new Rect(cx - 400, y + 60, 800, 26), pz.winReason, 18, Txt, TextAnchor.MiddleCenter);
            T(new Rect(cx - 400, y + 92, 800, 28), meWon ? "คุณชนะ" : "คุณแพ้", 22, meWon ? Yellow : new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, true);

            var tbl = new Rect(cx - 330, y + 140, 660, 36 + pz.actors.Count * 34);
            Box(tbl, Panel, Edge, 12f);
            T(new Rect(tbl.x + 20, tbl.y + 8, 200, 20), "ผู้เล่น", 12, new Color(1, 1, 1, 0.5f));
            T(new Rect(tbl.x + 230, tbl.y + 8, 200, 20), "บทบาทจริง", 12, new Color(1, 1, 1, 0.5f));
            T(new Rect(tbl.x + 450, tbl.y + 8, 200, 20), "สถานะ", 12, new Color(1, 1, 1, 0.5f));
            float ry = tbl.y + 32;
            foreach (var a in pz.actors)
            {
                if (a.npc) continue;
                TaskGames.Disc(new Vector2(tbl.x + 28, ry + 14), 7f, a.color);
                T(new Rect(tbl.x + 44, ry + 4, 180, 22), a.name + " · " + PZ.JobName(a.job), 14, Txt, TextAnchor.UpperLeft, true);
                T(new Rect(tbl.x + 230, ry + 4, 200, 22), a.role == Role.Infected ? "ผู้ติดเชื้อต้นตอ" : "ผู้รอดชีวิต", 14, a.role == Role.Infected ? Red : Green, TextAnchor.UpperLeft, true);
                string stt = a.health == Health.Healthy ? "รอดชีวิต" : a.health == Health.Incubating ? "ฟักเชื้ออยู่" : a.health == Health.Zombie ? "ซอมบี้" : a.health == Health.Quarantined ? "ถูกกักกัน" : "เสียชีวิต";
                T(new Rect(tbl.x + 450, ry + 4, 200, 22), stt, 14, a.health == Health.Healthy ? Green : a.health == Health.Zombie ? Red : new Color(1, 1, 1, 0.65f));
                ry += 34;
            }
            T(new Rect(cx - 300, tbl.yMax + 18, 600, 26), "กด R เพื่อเริ่มเกมใหม่", 16, Yellow, TextAnchor.UpperCenter, true);
        }
    }
}
