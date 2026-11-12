using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // ───────────────────────────────────────────────────────────────────────────
    // PATIENT ZERO — ตรรกะเกม (เล่นคนเดียวกับบอท) · แยกจากภาพ/UI (PZHud.cs) เพื่อย้ายไปรันบน Server ได้ภายหลัง
    //   ผู้รอดชีวิตทำภารกิจเพื่อผลิตวัคซีน · ผู้ติดเชื้อต้นตอซ่อนตัวและกัดคนอื่น (ฟักเชื้อ 30–60 วินาทีก่อนกลายเป็นซอมบี้)
    //   วงจร: กลางวัน (ภารกิจ) → กลางคืน (ซอมบี้ NPC บุก ไฟดับ) → รุ่งเช้าประชุมโหวตกักกัน → กลางวัน …
    // ───────────────────────────────────────────────────────────────────────────
    public enum Role { Survivor, Infected }
    public enum Job { Survivor, Doctor, Scientist, Police }
    public enum Health { Healthy, Incubating, Zombie, Quarantined, Dead }
    public enum Phase { Reveal, Day, Night, Meeting, GameOver }

    public sealed class Actor
    {
        public int id;
        public string name;
        public Color color;
        public Job job;
        public Role role;
        public Health health = Health.Healthy;
        public bool human, npc;               // npc = ซอมบี้ NPC กลางคืน
        public float x, z, facing, walkT;
        public Avatar av;
        public readonly List<int> tasks = new List<int>();   // ดัชนีใน SkeldGame.Tasks
        public int taskDone;
        public bool movingNow;
        public int ammo;                  // กระสุน (ตำรวจ)
        public float shootCd;
        public Vector2 fxOff; public float fxScaleY = 1f, fxPitch, fxRoll;   // ท่าทางตอนกัด/ถูกกัด (BiteFx เขียนให้ทุกเฟรม)
        public float incT, biteCd, coughT, bubbleT, graceT;
        public string bubble = "";
        public int meetUsed;
        public bool Alive => health == Health.Healthy || health == Health.Incubating;
        public bool OnMap => Alive || health == Health.Zombie;

        // AI
        public List<Vector2> path; public int pathI;
        public int goalTask = -1; public float workT, thinkT, fleeT, repathT;
        public Actor chase;
        public readonly Dictionary<int, float> sus = new Dictionary<int, float>();
        public readonly List<int> companions = new List<int>();   // คนที่อยู่ใกล้ตอนถูกกัด (ใช้ย้อนดูเมื่อกลายเป็นซอมบี้)
        public int vote = -2;                 // -2 ยังไม่โหวต · -1 ข้าม · ≥0 id ผู้ถูกโหวต
        public float voteAt;
    }

    public sealed class PZ
    {
        // ───── ค่าเกม (ปรับสมดุลได้ที่นี่) ─────
        public int TotalPlayers = 8, InfectedCount = 2, TasksPerActor = 5;     // มาจาก RoomSettings (สร้างห้อง)
        public bool abilitiesOn = true;
        float biteMul = 1f, huntP = 0.45f, zMul = 1f;
        readonly RoomSettings room;
        public const float RevealLen = 5f, MeetLen = 45f, ResultLen = 7f, MatchLimit = 840f;
        public float DayLen = 150f, NightLen = 45f;
        public const float IncubMin = 30f, IncubMax = 60f, BiteRange = 1.35f, BiteCooldown = 25f;
        const float BotSpeed = 4.2f, FleeSpeed = 5.2f, NpcZombieSpeed = 2.6f, ConvZombieSpeed = 2.3f;
        const float BotWorkMin = 12f, BotWorkMax = 20f;

        public readonly SkeldGame g;
        public readonly List<Actor> actors = new List<Actor>();
        public Actor me;
        public Phase phase = Phase.Reveal;
        public float phaseT, dayT, nightT, matchT, meetT, resultT;
        public int day = 1;
        public float doneTotal, needTotal;
        public float NightAmount;               // 0..1 ใช้ปรับแสง/หมอกใน SkeldGame
        public string winner = "", winReason = "";
        public readonly List<KeyValuePair<float, string>> feed = new List<KeyValuePair<float, string>>();
        public readonly List<string> clues = new List<string>();
        public BiteFx fx;
        public bool autoPilot;                  // true = ผู้เล่นถูกควบคุมโดย AI (ใช้ทดสอบจำลองแมตช์)
        public static Vector2 ButtonPos => MapData.ButtonPos;   // ปุ่มประชุมบนโต๊ะห้องประชุม
        public bool CanAct => (phase == Phase.Day || phase == Phase.Night) && me != null && me.Alive;
        public bool Frozen => !CanAct;
        public bool Spectating => me != null && !me.Alive && phase != Phase.Reveal;

        // ประชุม
        public string meetReason = "";
        public bool dawnMeeting, resultShown;
        public Actor ejected; public string ejectText = "";
        public readonly Dictionary<int, int> tally = new Dictionary<int, int>();
        public int skipVotes;
        float meetCd, pendingT; Actor pendingCaller; string pendingReason;

        // ความสามารถของผู้เล่น
        public int bloodLeft = 2;
        public float testT; public Actor testTarget;
        public float shotFlash;
        int specIndex;
        float meCoughT, symptomPulse;
        public float SymptomLevel;              // 0..1 ให้ HUD/กล้องใช้
        readonly System.Random rng;

        string zombieId = "character-zombie";

        public PZ(SkeldGame game, RoomSettings rs = null)
        {
            g = game;
            fx = new BiteFx(game);
            rng = new System.Random();
            room = rs ?? new RoomSettings();
            room.Clamp();
            TotalPlayers = room.players; InfectedCount = room.infected; TasksPerActor = room.tasksPerPlayer;
            DayLen = room.dayLen; NightLen = room.nightLen; abilitiesOn = room.abilities;
            biteMul = room.difficulty == 0 ? 1.5f : room.difficulty == 2 ? 0.7f : 1f;
            huntP = room.difficulty == 0 ? 0.3f : room.difficulty == 2 ? 0.6f : 0.45f;
            zMul = room.difficulty == 0 ? 0.85f : room.difficulty == 2 ? 1.2f : 1f;
            Setup();
        }

        // ═════════════════════════ จัดผู้เล่น ═════════════════════════
        static readonly string[] BotNames = { "Ploy", "Mark", "Nok", "Ben", "Fah", "Tong", "Mint", "Gun", "Aom", "Pat" };
        static readonly Color[] Colors =
        {
            Mats.Hex(0xff5252), Mats.Hex(0x4d8dff), Mats.Hex(0x4cdc7a), Mats.Hex(0xff7be5),
            Mats.Hex(0xffa53a), Mats.Hex(0xf4e04d), Mats.Hex(0xb07dff), Mats.Hex(0x4de0e0), Mats.Hex(0xc9c9d4), Mats.Hex(0xff9ec4),
        };

        void Setup()
        {
            // หมอ = เสื้อกาวน์ขาว (Male E) · นักวิทยาศาสตร์ = เสื้อแล็บ (Female E) · ตำรวจ = Soldier
            var survivorLooks = new List<string>
            {
                "character-male-a", "character-male-b", "character-male-d", "character-male-f", "character-male-g",
                "character-female-a", "character-female-b", "character-female-c", "character-female-d", "character-female-f",
            };
            survivorLooks.Remove(g.CharacterId);
            Shuffle(survivorLooks);

            // สัดส่วนอาชีพตามจำนวนผู้เล่น (10 คน: ผู้รอดชีวิต 5 · หมอ 2 · นักวิทยาศาสตร์ 2 · ตำรวจ 1)
            var jobs = new List<Job>();
            int nDoc = Mathf.Max(1, TotalPlayers / 4), nSci = Mathf.Max(1, TotalPlayers / 4), nSol = TotalPlayers >= 7 ? 1 : 0;
            for (int i = 0; i < nDoc; i++) jobs.Add(Job.Doctor);
            for (int i = 0; i < nSci; i++) jobs.Add(Job.Scientist);
            for (int i = 0; i < nSol; i++) jobs.Add(Job.Police);
            while (jobs.Count < TotalPlayers) jobs.Add(Job.Survivor);
            Shuffle(jobs);
            var roleIdx = new List<int>();
            for (int i = 0; i < TotalPlayers; i++) roleIdx.Add(i);
            Shuffle(roleIdx);
            var infected = new HashSet<int>();
            for (int i = 0; i < InfectedCount; i++) infected.Add(roleIdx[i]);

            // ตัวเลือกดีบัก/ทดสอบ: -pz-role infected|survivor · -pz-job doctor|scientist|soldier|survivor
            string argRole = Arg("-pz-role"), argJob = Arg("-pz-job");
            if (argRole == "") argRole = room.humanRole == 1 ? "survivor" : room.humanRole == 2 ? "infected" : "";
            if (argRole == "infected") { if (!infected.Contains(0)) { int swap = new List<int>(infected)[0]; infected.Remove(swap); infected.Add(0); } }
            else if (argRole == "survivor") { if (infected.Contains(0)) { infected.Remove(0); while (infected.Count < InfectedCount) infected.Add(1 + rng.Next(TotalPlayers - 1)); } }

            var names = new List<string>(BotNames);
            Shuffle(names);
            int lookI = 0;
            for (int i = 0; i < TotalPlayers; i++)
            {
                var a = new Actor
                {
                    id = i, human = i == 0, job = jobs[i], role = infected.Contains(i) ? Role.Infected : Role.Survivor,
                    color = Colors[i % Colors.Length], name = i == 0 ? GameSettings.PlayerName : names[i - 1],
                };
                if (i == 0)
                {
                    if (argJob == "doctor") a.job = Job.Doctor; else if (argJob == "scientist") a.job = Job.Scientist;
                    else if (argJob == "soldier" || argJob == "police") a.job = Job.Police; else if (argJob == "survivor") a.job = Job.Survivor;
                    a.x = MapData.Spawn.x; a.z = MapData.Spawn.y;
                    me = a;
                }
                else
                {
                    string look = a.job == Job.Doctor ? "character-male-e" : a.job == Job.Scientist ? "character-female-e"
                                : a.job == Job.Police ? "character-male-c" : survivorLooks[lookI++ % survivorLooks.Count];
                    a.av = Avatar.Create(look);
                    a.av.Root.transform.SetParent(g.transform, false);
                    var p = FreeSpot(MapData.Rooms[IndexOfRoom(MapData.MeetingRoomId)], 0.9f);
                    a.x = p.x; a.z = p.y;
                    a.facing = (float)(rng.NextDouble() * Mathf.PI * 2);
                }
                a.biteCd = (30f + (float)rng.NextDouble() * 25f) * biteMul;
                a.ammo = a.job == Job.Police && abilitiesOn ? 1 : 0;
                if (a.av != null && a.job == Job.Police) GunProp.Attach(a.av);
                a.coughT = 8f + (float)rng.NextDouble() * 10f;
                actors.Add(a);
            }
            // ผู้รอดชีวิตทุกคนมี "ขีดเริ่ม" ไม่ซ้อนทับกัน
            Separate();

            // แจกภารกิจ (ผู้ติดเชื้อได้ภารกิจหลอก ไม่นับเป็นความคืบหน้าวัคซีน)
            var idx = new List<int>();
            for (int i = 0; i < g.Tasks.Count; i++) idx.Add(i);
            foreach (var a in actors)
            {
                Shuffle(idx);
                for (int k = 0; k < TasksPerActor; k++) a.tasks.Add(idx[k]);
                if (a.role == Role.Survivor) needTotal += TasksPerActor;
            }
            g.ApplyAssignment(me.tasks);

            foreach (var e in CharacterCatalog.All) if (e.name == "Zombie") zombieId = e.id;
            Say("PATIENT ZERO — มีผู้ติดเชื้ออยู่ในกลุ่มคุณ");
            dayT = DayLen;
            // ดีบัก: -pz-daylen <วินาที> ย่อวันแรกให้สั้น (ทดสอบกลางคืน/ประชุม)
            string dl = Arg("-pz-daylen");
            if (dl != "" && float.TryParse(dl, out float dv)) dayT = dv;
        }

        static string Arg(string key)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1].ToLowerInvariant();
            return "";
        }

        void Shuffle<T>(IList<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); T t = l[i]; l[i] = l[j]; l[j] = t; }
        }

        static int IndexOfRoom(string id) { for (int i = 0; i < MapData.Rooms.Length; i++) if (MapData.Rooms[i].id == id) return i; return 0; }

        void Separate()
        {
            for (int pass = 0; pass < 6; pass++)
                for (int i = 0; i < actors.Count; i++)
                    for (int j = i + 1; j < actors.Count; j++)
                    {
                        var a = actors[i]; var b = actors[j];
                        if (Dist(a, b) < 0.9f && !a.human && !b.human) { var p = FreeSpot(MapData.Rooms[IndexOfRoom(MapData.MeetingRoomId)], 0.9f); b.x = p.x; b.z = p.y; }
                    }
        }

        public static float Dist(Actor a, Actor b) { float dx = a.x - b.x, dz = a.z - b.z; return Mathf.Sqrt(dx * dx + dz * dz); }

        // จุดว่างแบบมีระยะห่างจากของตัน
        Vector2 FreeSpot(RoomDef r, float clear)
        {
            for (int t = 0; t < 60; t++)
            {
                float x = r.x + 1.2f + (float)rng.NextDouble() * (r.w - 2.4f), z = r.z + 1.2f + (float)rng.NextDouble() * (r.d - 2.4f);
                if (Clearance(x, z) >= clear) return new Vector2(x, z);
            }
            return new Vector2(r.x + r.w / 2f, r.z + r.d / 2f);
        }

        float Clearance(float x, float z)
        {
            float best = 9f;
            int cx = Mathf.FloorToInt(x), cz = Mathf.FloorToInt(z);
            for (int j = -2; j <= 2; j++)
                for (int i = -2; i <= 2; i++)
                {
                    if (!g.Grid.IsBlocked(cx + i, cz + j)) continue;
                    float nx = Mathf.Clamp(x, cx + i, cx + i + 1), nz = Mathf.Clamp(z, cz + j, cz + j + 1);
                    best = Mathf.Min(best, Mathf.Sqrt((x - nx) * (x - nx) + (z - nz) * (z - nz)));
                }
            return best;
        }

        // ═════════════════════════ ข้อความ ═════════════════════════
        public static bool Verbose;

        public void Say(string s)
        {
            if (Verbose) Debug.Log($"PZLOG t={matchT:F0} {phase} {s}");
            feed.Add(new KeyValuePair<float, string>(Time.unscaledTime, s));
            if (feed.Count > 12) feed.RemoveAt(0);
        }

        public void Clue(string s)
        {
            clues.Add(s);
            if (clues.Count > 24) clues.RemoveAt(0);
            Say(s);
        }

        public static string JobName(Job j) => j == Job.Doctor ? "หมอ" : j == Job.Scientist ? "นักวิทยาศาสตร์" : j == Job.Police ? "ตำรวจ" : "ผู้รอดชีวิต";

        // ═════════════════════════ ลูปหลัก ═════════════════════════
        // active = false เมื่อเมนูเลือกตัวละครเปิดอยู่ (หน้าเปิดเผยบทบาทต้องไม่นับเวลา)
        public void Update(float dt, bool active = true)
        {
            // ผู้เล่น: ตำแหน่งมาจากตัวควบคุม (หรือเป็นของบอทที่กำลังติดตามตอนรับชม)
            if (me.Alive && !autoPilot) { me.x = g.PosX; me.z = g.PosZ; me.facing = g.Facing; }

            if (phase == Phase.Reveal && !active) { SyncAvatars(dt); return; }
            phaseT += dt;
            shotFlash = Mathf.Max(0f, shotFlash - dt);
            biteFlash = Mathf.Max(0f, biteFlash - dt);
            fx.Update(dt, actors);
            meetCd = Mathf.Max(0f, meetCd - dt);
            if (phase != Phase.Reveal && phase != Phase.GameOver) matchT += dt;

            switch (phase)
            {
                case Phase.Reveal:
                    if (phaseT > RevealLen || (phaseT > 1.2f && (GameInput.Pressed(KeyCode.Return) || GameInput.Pressed(KeyCode.Space)))) { phase = Phase.Day; phaseT = 0f; if (Arg("-pz-gameover") != "") End(Arg("-pz-gameover") == "infected" ? "ผู้ติดเชื้อ" : "ผู้รอดชีวิต", "(ทดสอบหน้าสรุปผล)"); }
                    break;
                case Phase.Day:
                    dayT -= dt;
                    TickWorld(dt);
                    if (!autoPilot) HumanActions(dt);
                    if (pendingCaller != null) { pendingT -= dt; if (pendingT <= 0f) { var c = pendingCaller; pendingCaller = null; StartMeeting(c, pendingReason, false); } }
                    if (dayT <= 0f) StartNight();
                    break;
                case Phase.Night:
                    nightT -= dt;
                    TickWorld(dt);
                    if (!autoPilot) HumanActions(dt);
                    if (nightT <= 0f) { EndNight(); }
                    break;
                case Phase.Meeting:
                    TickMeeting(dt);
                    break;
                case Phase.GameOver:
                    if (GameInput.Pressed(KeyCode.R)) SkeldGame.Restart();
                    break;
            }

            float nightTarget = phase == Phase.Night ? 1f : 0f;
            NightAmount = Mathf.MoveTowards(NightAmount, nightTarget, dt * 0.6f);

            UpdateSymptoms(dt);
            if (Spectating) Spectate();
            if (phase != Phase.Reveal && phase != Phase.GameOver) CheckWin();
            SyncAvatars(dt);
        }

        // ═════════════════════════ โลก (บอท/ซอมบี้/การติดเชื้อ) ═════════════════════════
        void TickWorld(float dt)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                if (!a.OnMap) continue;
                a.biteCd = Mathf.Max(0f, a.biteCd - dt);
                a.graceT = Mathf.Max(0f, a.graceT - dt);
                a.bubbleT = Mathf.Max(0f, a.bubbleT - dt);
                TickHealth(a, dt);
                if (a.human && !autoPilot) continue;
                if (a.health == Health.Zombie) ZombieAI(a, dt); else BotAI(a, dt);
            }
        }

        void TickHealth(Actor a, float dt)
        {
            if (a.health == Health.Incubating)
            {
                a.incT -= dt;
                a.coughT -= dt;
                if (a.coughT <= 0f && !(a.human && !autoPilot)) { Cough(a); a.coughT = 6f + (float)rng.NextDouble() * 9f; }
                if (a.incT <= 0f) BecomeZombie(a);
            }
            else if (a.health == Health.Healthy && a.role == Role.Survivor && !(a.human && !autoPilot) && !a.npc && phase != Phase.Meeting)
            {
                // ผู้รอดชีวิตที่เครียดก็ไอหลอกได้ (ไม่ให้รู้ง่ายเกินไป)
                if (rng.NextDouble() < dt * 0.006) Cough(a);
            }
        }

        void Cough(Actor a)
        {
            a.bubble = "*ไอ*"; a.bubbleT = 1.4f;
            if (a.human) return;
            float d = Dist(a, me);
            if (me.OnMap && d < 13f) g.Cough(Mathf.Clamp01(1f - d / 13f) * 0.9f);
            foreach (var w in actors)
            {
                if (w == a || !w.Alive || Dist(w, a) > (phase == Phase.Night ? 4f : 7.5f)) continue;
                if (w.human) { if (Sees(w, a)) Clue($"คุณเห็น {a.name} ไอ"); }
                else if (w.role == Role.Survivor) AddSus(w, a, 0.3f);
            }
        }

        bool Sees(Actor w, Actor a) => Dist(w, a) < 3.5f || MapData.RoomAt(w.x, w.z) == MapData.RoomAt(a.x, a.z);

        static void AddSus(Actor w, Actor a, float v) { w.sus.TryGetValue(a.id, out float cur); w.sus[a.id] = cur + v; }

        // ผู้รอดชีวิตที่หลุดจากเกม (ซอมบี้/กักกัน/ตาย): ภารกิจที่ยังค้างของเขาไม่ต้องทำอีก (ตัดออกจากยอดวัคซีน)
        void DropTasks(Actor a)
        {
            if (a.role != Role.Survivor) return;
            int left = a.human && !autoPilot ? Mathf.Max(0, g.AssignedCount - g.DoneCount) : Mathf.Max(0, a.tasks.Count - a.taskDone);
            needTotal = Mathf.Max(doneTotal, needTotal - left);
        }

        void BecomeZombie(Actor a)
        {
            DropTasks(a);
            if (Verbose) Debug.Log($"PZLOG t={matchT:F0} convert {a.name} companions={a.companions.Count}");
            a.health = Health.Zombie; a.path = null; a.workT = 0f; a.goalTask = -1;
            // ผู้รอดชีวิตทุกคน "นึกย้อน" ว่าใครอยู่ใกล้คนนี้ก่อนป่วย → สงสัยเล็กน้อย
            foreach (var w in actors)
            {
                if (!w.Alive || w.role != Role.Survivor || w == a || (w.human && !autoPilot)) continue;
                foreach (int cid in a.companions) { var c = actors.Find(x => x.id == cid); if (c != null && c != w) AddSus(w, c, 0.25f); }
            }
            Say(a.human ? "คุณกลายเป็นซอมบี้…" : $"{a.name} กลายเป็นซอมบี้!");
            if (a.av != null) { a.av.Dispose(); }
            if (!a.human)
            {
                a.av = Avatar.Create(zombieId);
                a.av.Root.transform.SetParent(g.transform, false);
                // ทุกคนเห็นซอมบี้ → ขอประชุมด่วน (บอทที่ใกล้ที่สุดที่ไม่ใช่ผู้ติดเชื้อต้นตอ)
                Actor caller = null; float bd = 9999f;
                foreach (var w in actors) if (w.Alive && !w.human && w.role == Role.Survivor && w.meetUsed < 1) { float d = Dist(w, a); if (d < bd) { bd = d; caller = w; } }
                if (caller != null) RequestMeeting(caller, $"พบซอมบี้: {a.name}!");
            }
            else g.OnHumanOut("คุณกลายเป็นซอมบี้");
        }

        public int statBites, statZombieInfect;     // สถิติ (ใช้ดูสมดุลตอนจำลอง)

        public void Infect(Actor victim, Actor source)
        {
            if (victim.health != Health.Healthy || victim.role != Role.Survivor) return;
            if (source.health == Health.Zombie) { statZombieInfect++; fx.Play(source, victim); } else statBites++;
            victim.health = Health.Incubating;
            victim.incT = IncubMin + (float)rng.NextDouble() * (IncubMax - IncubMin);
            victim.coughT = 6f + (float)rng.NextDouble() * 6f;
            if (victim.human) { meCoughT = 4f; }
        }

        // ผู้ติดเชื้อกัด (ผู้เล่นกด B / บอท) → มีพยานเห็นได้
        public void Bite(Actor biter, Actor victim)
        {
            if (Verbose) Debug.Log($"PZLOG t={matchT:F0} bite {biter.name} -> {victim.name}");
            Infect(victim, biter);
            fx.Play(biter, victim);
            victim.companions.Clear();
            foreach (var c in actors) if (c != victim && c.Alive && Dist(c, victim) < 7f) victim.companions.Add(c.id);
            biter.biteCd = biter.human ? BiteCooldown : (34f + (float)rng.NextDouble() * 20f) * biteMul;
            float wr = phase == Phase.Night ? 3.8f : 7f;
            foreach (var w in actors)
            {
                if (w == biter || w == victim || !w.Alive || Dist(w, victim) > wr || w.role == Role.Infected) continue;
                if (!Sees(w, biter) && Dist(w, biter) > wr) continue;
                if (w.human) Clue($"คุณเห็น {biter.name} กัด {victim.name}!");
                else { AddSus(w, biter, 1.5f); RequestMeeting(w, $"{w.name} เห็นการกัด!"); }
            }
        }

        // ═════════════════════════ บอท ═════════════════════════
        void BotAI(Actor a, float dt)
        {
            a.thinkT -= dt; a.fleeT -= dt; a.repathT -= dt; a.shootCd -= dt;

            // ตำรวจฝั่งผู้รอดชีวิต: ยิงซอมบี้/ผู้ต้องสงสัยที่เห็นกับตา
            if (a.job == Job.Police && abilitiesOn && a.ammo > 0 && a.shootCd <= 0f && a.role == Role.Survivor && a.Alive) BotShoot(a);

            // หนีซอมบี้ที่อยู่ใกล้
            Actor z = NearestZombie(a, 8f);
            if (z != null && a.fleeT <= 0f && a.chase == null)
            {
                a.fleeT = 3f; a.workT = 0f;
                var far = FarRoomPoint(z);
                a.path = FindPath(new Vector2(a.x, a.z), far); a.pathI = 0;
            }

            // ผู้ติดเชื้อต้นตอ: มองหาเหยื่อที่อยู่ลำพัง
            if (a.role == Role.Infected && a.Alive && a.fleeT <= 0f) InfectedHunt(a, dt);

            bool moving = false;
            float speed = a.fleeT > 0f ? FleeSpeed : BotSpeed;
            if (a.workT > 0f)
            {
                a.workT -= dt;
                a.bubble = "…"; a.bubbleT = 0.3f;
                if (a.workT <= 0f) FinishBotTask(a);
            }
            else if (a.path != null && a.pathI < a.path.Count)
            {
                moving = Follow(a, speed, dt);
            }
            else if (a.chase == null && a.fleeT <= 0f)
            {
                if (a.goalTask >= 0)
                {
                    a.workT = BotWorkMin + (float)rng.NextDouble() * (BotWorkMax - BotWorkMin);   // ถึงจุดภารกิจ → ทำงาน
                }
                else if (a.thinkT <= 0f) PickGoal(a);
            }
            a.walkT = moving ? a.walkT + dt * 12f : 0f;
            a.movingNow = moving;
        }

        void PickGoal(Actor a)
        {
            if (a.taskDone < a.tasks.Count)
            {
                a.goalTask = a.tasks[a.taskDone];
                var def = g.Tasks[a.goalTask].def;
                a.path = FindPath(new Vector2(a.x, a.z), new Vector2(def.x, def.z)); a.pathI = 0;
            }
            else
            {
                a.goalTask = -1;
                var room = MapData.Rooms[rng.Next(MapData.Rooms.Length)];
                var p = FreeSpot(room, 0.8f);
                a.path = FindPath(new Vector2(a.x, a.z), p); a.pathI = 0;
                a.thinkT = 3f + (float)rng.NextDouble() * 6f;
            }
        }

        void FinishBotTask(Actor a)
        {
            a.taskDone++;
            a.goalTask = -1;
            a.path = null;
            a.thinkT = 1f + (float)rng.NextDouble() * 2f;
            if (a.role == Role.Survivor && a.Alive) doneTotal += 1f;     // ผู้ติดเชื้อทำหลอก ไม่นับ
        }

        void InfectedHunt(Actor a, float dt)
        {
            if (a.chase != null)
            {
                var v = a.chase;
                if (v.health != Health.Healthy || a.biteCd > 0f || !(phase == Phase.Day || phase == Phase.Night)) { a.chase = null; return; }
                if (Dist(a, v) <= BiteRange) { Bite(a, v); a.chase = null; a.path = null; a.thinkT = 2f; return; }
                if (a.repathT <= 0f)
                {
                    a.repathT = 0.45f;
                    if (Witnesses(v, a) > 0 && rng.NextDouble() < 0.6) { a.chase = null; a.path = null; return; }
                    a.path = FindPath(new Vector2(a.x, a.z), new Vector2(v.x, v.z)); a.pathI = 0;
                }
                return;
            }
            if (a.biteCd > 0f || a.graceT > 0f || a.repathT > 0f) return;
            a.repathT = 0.6f;
            Actor best = null; float bd = 9.5f;
            foreach (var v in actors)
            {
                if (v == a || v.health != Health.Healthy || v.role != Role.Survivor) continue;
                float d = Dist(a, v);
                if (d < bd && Witnesses(v, a) == 0) { bd = d; best = v; }
            }
            if (best != null && rng.NextDouble() < huntP) { a.chase = best; a.workT = 0f; a.goalTask = -1; }
        }

        // จำนวนคนที่จะเห็นการกัด (ไม่นับพวกผู้ติดเชื้อด้วยกันและซอมบี้)
        int Witnesses(Actor victim, Actor biter)
        {
            float wr = phase == Phase.Night ? 3.8f : 7f;
            int n = 0;
            foreach (var w in actors)
                if (w != victim && w != biter && w.Alive && w.role == Role.Survivor && Dist(w, victim) < wr) n++;
            return n;
        }

        Actor NearestZombie(Actor a, float range)
        {
            Actor best = null; float bd = range;
            foreach (var z in actors) if (z.health == Health.Zombie && z != a) { float d = Dist(a, z); if (d < bd) { bd = d; best = z; } }
            return best;
        }

        Vector2 FarRoomPoint(Actor from)
        {
            Vector2 best = new Vector2(from.x, from.z); float bd = -1f;
            for (int i = 0; i < 4; i++)
            {
                var p = FreeSpot(MapData.Rooms[rng.Next(MapData.Rooms.Length)], 0.8f);
                float d = (p - new Vector2(from.x, from.z)).magnitude;
                if (d > bd) { bd = d; best = p; }
            }
            return best;
        }

        void ZombieAI(Actor a, float dt)
        {
            a.repathT -= dt;
            Actor best = null; float bd = a.npc ? 16f : 13f;
            foreach (var v in actors)
            {
                if (v.health != Health.Healthy || v.role != Role.Survivor) continue;
                float d = Dist(a, v);
                if (d < bd) { bd = d; best = v; }
            }
            bool moving = false;
            if (best != null)
            {
                if (bd < 0.95f) { Infect(best, a); a.repathT = 1.5f; a.path = null; }
                else if (a.repathT <= 0f || a.path == null || a.pathI >= a.path.Count)
                {
                    a.repathT = 0.7f;
                    a.path = FindPath(new Vector2(a.x, a.z), new Vector2(best.x, best.z)); a.pathI = 0;
                }
            }
            else if (a.path == null || a.pathI >= a.path.Count)
            {
                var p = FreeSpot(MapData.Rooms[rng.Next(MapData.Rooms.Length)], 0.8f);
                a.path = FindPath(new Vector2(a.x, a.z), p); a.pathI = 0;
            }
            if (a.path != null && a.pathI < a.path.Count) moving = Follow(a, (a.npc ? NpcZombieSpeed : ConvZombieSpeed) * zMul, dt);
            a.walkT = moving ? a.walkT + dt * 9f : 0f;
            a.movingNow = moving;
        }

        // เดินตามเส้นทาง · คืน true ถ้าขยับ
        bool Follow(Actor a, float speed, float dt)
        {
            if (a.path == null || a.pathI >= a.path.Count) return false;
            Vector2 target = a.path[a.pathI];
            float dx = target.x - a.x, dz = target.y - a.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < 0.25f) { a.pathI++; return a.pathI < a.path.Count; }
            float step = Mathf.Min(speed * dt, d);
            a.x += dx / d * step; a.z += dz / d * step;
            PushWorld.PushOutOfCellsPublic(ref a.x, ref a.z, 0.34f, g.Grid);
            float tgt = Mathf.Atan2(dx, dz), diff = Mathf.Atan2(Mathf.Sin(tgt - a.facing), Mathf.Cos(tgt - a.facing));
            a.facing += diff * (1f - Mathf.Exp(-12f * dt));
            return true;
        }

        // ═════════════════════════ กลางคืน ═════════════════════════
        void StartNight()
        {
            phase = Phase.Night; phaseT = 0f; nightT = NightLen;
            Say("ค่ำแล้ว… ไฟดับ! ซอมบี้กำลังบุกเข้ามา");
            int n = room.nightZombies;
            for (int i = 0; i < n; i++)
            {
                RoomDef room = null;
                for (int t = 0; t < 12; t++)
                {
                    var r = MapData.Rooms[rng.Next(MapData.Rooms.Length)];
                    bool near = false;
                    foreach (var a in actors) if (a.OnMap && MapData.RoomAt(a.x, a.z) == r) near = true;
                    if (!near) { room = r; break; }
                }
                if (room == null) room = MapData.Rooms[rng.Next(MapData.Rooms.Length)];
                var p = FreeSpot(room, 0.8f);
                var z = new Actor { id = 100 + i, name = "ซอมบี้", npc = true, health = Health.Zombie, x = p.x, z = p.y, color = Color.green };
                z.av = Avatar.Create(zombieId);
                z.av.Root.transform.SetParent(g.transform, false);
                actors.Add(z);
            }
        }

        void EndNight()
        {
            for (int i = actors.Count - 1; i >= 0; i--)
                if (actors[i].npc) { actors[i].av?.Dispose(); actors.RemoveAt(i); }
            day++;
            StartMeeting(null, "รุ่งเช้า — ประชุมโหวตกักกันผู้ต้องสงสัย", true);
        }

        // ═════════════════════════ ประชุม / โหวต ═════════════════════════
        public void RequestMeeting(Actor caller, string reason)
        {
            if (phase != Phase.Day || meetCd > 0f || pendingCaller != null || caller.meetUsed >= 1) return;
            pendingCaller = caller; pendingReason = reason; pendingT = 1.2f + (float)rng.NextDouble() * 1.5f;
        }

        public void HumanCallMeeting()
        {
            if (phase != Phase.Day || meetCd > 0f || me.meetUsed >= 1 || !me.Alive) return;
            StartMeeting(me, "คุณเรียกประชุมฉุกเฉิน", false);
        }

        public bool CanCallMeeting => phase == Phase.Day && meetCd <= 0f && me.meetUsed < 1 && me.Alive;

        void StartMeeting(Actor caller, string reason, bool dawn)
        {
            if (caller != null) caller.meetUsed++;
            phase = Phase.Meeting; phaseT = 0f; meetT = 0f; resultT = 0f; resultShown = false;
            meetReason = reason; dawnMeeting = dawn; ejected = null; ejectText = ""; tally.Clear(); skipVotes = 0;
            pendingCaller = null;
            foreach (var a in actors)
            {
                a.vote = -2; a.path = null; a.workT = 0f; a.chase = null; a.fleeT = 0f; a.movingNow = false;
                a.voteAt = 5f + (float)rng.NextDouble() * 17f;
            }
            Say("ประชุม: " + reason);
        }

        public bool HumanCanVote => phase == Phase.Meeting && !resultShown && me.Alive;
        public void HumanVote(int id) { if (HumanCanVote) me.vote = id; }

        void TickMeeting(float dt)
        {
            if (!resultShown)
            {
                meetT += dt;
                bool all = true;
                foreach (var a in actors)
                {
                    if (!a.Alive || (a.human && !autoPilot)) continue;
                    if (a.vote == -2 && meetT >= a.voteAt) a.vote = BotVote(a);
                    if (a.vote == -2) all = false;
                }
                if (me.Alive && !autoPilot && me.vote == -2) all = false;
                if (meetT >= MeetLen || (all && meetT >= 8f)) ResolveVote();
            }
            else
            {
                resultT += dt;
                if (resultT >= ResultLen) EndMeeting();
            }
        }

        int BotVote(Actor a)
        {
            // ผู้ติดเชื้อต้นตอ: เทคะแนนใส่ผู้รอดชีวิตที่คนอื่นสงสัยอยู่ (เล่นตามน้ำ) ไม่โหวตพวกเดียวกัน
            Actor best = null; float bs = 0.35f;
            foreach (var c in actors)
            {
                if (c == a || !c.OnMap || c.npc) continue;
                if (a.role == Role.Infected && c.role == Role.Infected) continue;
                float s = 0f; a.sus.TryGetValue(c.id, out s);
                if (c.health == Health.Zombie) s += 2.2f;
                s += (float)rng.NextDouble() * 0.25f;
                if (a.role == Role.Infected && c.role == Role.Survivor)
                    foreach (var o in actors) { if (o.sus.TryGetValue(c.id, out float v)) s += v * 0.15f; }
                if (s > bs) { bs = s; best = c; }
            }
            if (a.role == Role.Infected && best == null && rng.NextDouble() < 0.5)
            {
                var pool = actors.FindAll(c => c.Alive && c.role == Role.Survivor && c != a);
                if (pool.Count > 0) return pool[rng.Next(pool.Count)].id;
            }
            if (best == null) return -1;
            return best.id;
        }

        void ResolveVote()
        {
            tally.Clear(); skipVotes = 0;
            foreach (var a in actors)
            {
                if (!a.Alive) continue;
                if (a.vote == -2 || a.vote == -1) { skipVotes++; continue; }
                tally.TryGetValue(a.vote, out int n); tally[a.vote] = n + 1;
            }
            int max = 0, top = -1; bool tie = false;
            foreach (var kv in tally)
            {
                if (kv.Value > max) { max = kv.Value; top = kv.Key; tie = false; }
                else if (kv.Value == max) tie = true;
            }
            resultShown = true; resultT = 0f; ejected = null;
            if (top >= 0 && !tie && max > skipVotes)
            {
                var v = actors.Find(x => x.id == top);
                if (v != null)
                {
                    ejected = v;
                    if (Verbose) Debug.Log($"PZLOG vote -> {v.name} role={v.role} health={v.health} votes={max} skip={skipVotes}");
                    string what = v.health == Health.Zombie ? "ซอมบี้ (ติดเชื้อจากการกัด)" : v.role == Role.Infected ? "ผู้ติดเชื้อต้นตอ!" : "ผู้รอดชีวิต (ไม่ได้ติดเชื้อ…)";
                    ejectText = $"{v.name} ถูกกักกัน — เป็น {what}";
                    Quarantine(v);
                }
            }
            else ejectText = tie ? "คะแนนเสมอ — ไม่มีใครถูกกักกัน" : "ส่วนใหญ่โหวตข้าม — ไม่มีใครถูกกักกัน";
            Say(ejectText);
        }

        void Quarantine(Actor v, string humanMsg = "คุณถูกกักกัน")
        {
            bool wasHuman = v.human;
            DropTasks(v);
            v.health = Health.Quarantined; v.path = null; v.workT = 0f;
            var vav = v.av; v.av = null;
            if (vav != null) fx.Later(0.4f, () => vav.Dispose());      // รออีกนิดให้เห็นเอฟเฟกต์โดนยิง/ถูกกักก่อนหายไป
            if (wasHuman) g.OnHumanOut(humanMsg);
        }

        void EndMeeting()
        {
            // ทุกคนกลับไปรวมตัวที่โรงอาหาร (เหมือน Among Us)
            var caf = MapData.Rooms[IndexOfRoom(MapData.MeetingRoomId)];
            foreach (var a in actors)
            {
                if (!a.OnMap) continue;
                a.graceT = 12f; a.biteCd = Mathf.Max(a.biteCd, 8f);
                var p = a.human ? new Vector2(MapData.Spawn.x, MapData.Spawn.y) : FreeSpot(caf, 0.9f);
                a.x = p.x; a.z = p.y; a.path = null; a.goalTask = -1; a.workT = 0f; a.thinkT = 1f + (float)rng.NextDouble() * 2f;
                if (a.human) g.DebugTeleport(a.x, a.z);
            }
            meetCd = 25f;
            phase = Phase.Day; phaseT = 0f;
            if (dawnMeeting) dayT = DayLen;
            CheckWin();
        }

        // ═════════════════════════ ผู้เล่น: ความสามารถ ═════════════════════════
        void HumanActions(float dt)
        {
            if (!me.Alive) return;
            // ผู้ติดเชื้อต้นตอ: กัด (B)
            if (me.role == Role.Infected && GameInput.Pressed(KeyCode.B)) HumanBite();
            // หมอ: ตรวจเลือด (T) ใช้เวลา 20 วินาที
            if (abilitiesOn && me.job == Job.Doctor)
            {
                if (testTarget != null)
                {
                    testT -= dt;
                    if (testT <= 0f)
                    {
                        bool pos = testTarget.role == Role.Infected || testTarget.health == Health.Incubating || testTarget.health == Health.Zombie;
                        Clue($"ผลตรวจเลือด {testTarget.name}: {(pos ? "พบเชื้อ!" : "ปกติ")}");
                        testTarget = null;
                    }
                }
                else if (GameInput.Pressed(KeyCode.T) && bloodLeft > 0)
                {
                    Actor best = null; float bd = 2.2f;
                    foreach (var v in actors) if (v != me && v.Alive && Dist(me, v) < bd) { bd = Dist(me, v); best = v; }
                    if (best != null) { bloodLeft--; testTarget = best; testT = 20f; Say($"เก็บเลือด {best.name} … ผลออกใน 20 วินาที"); g.Blip(1.1f, 0.5f); }
                }
            }
            // ตำรวจ: ยิง (G หรือปุ่ม "ยิง" บนจอ) · มีกระสุน 1 นัด · ยิงผิดคน (ผู้รอดชีวิตที่ไม่ติดเชื้อ) ตัวเองตาย
            if (abilitiesOn && me.job == Job.Police && GameInput.Pressed(KeyCode.G)) HumanShoot();
        }

        // ผู้เล่นที่เป็นผู้ติดเชื้อกัด (ปุ่ม B หรือปุ่ม "กัด" บนจอ) · คืน true ถ้ากัดสำเร็จ
        public float biteFlash;
        public bool HumanBite()
        {
            if (!CanAct || me.role != Role.Infected) return false;
            if (me.biteCd > 0f) { Say($"ยังกัดไม่ได้ — รออีก {Mathf.CeilToInt(me.biteCd)} วินาที"); g.Blip(0.5f, 0.35f); return false; }
            var v = BiteCandidate();
            if (v == null) { Say("ไม่มีผู้รอดชีวิตใกล้พอให้กัด — เข้าไปใกล้ ๆ ก่อน"); g.Blip(0.5f, 0.35f); return false; }
            Bite(me, v);
            biteFlash = 0.4f;
            Say($"คุณกัด {v.name} … เขาจะเริ่มมีอาการใน 30–60 วินาที");
            g.Blip(0.35f, 0.8f);
            return true;
        }

        // ───── ตำรวจยิง ─────
        // เป้าหมายในแนวเล็ง: ใกล้สุดภายใน 8 หน่วย และอยู่ในมุม ±28° จากทิศที่ตัวละครหันหน้า
        public Actor ShootCandidate(Actor s)
        {
            Actor best = null; float bd = 8f;
            foreach (var v in actors)
            {
                if (v == s || !v.OnMap) continue;
                float d = Dist(s, v);
                float ang = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(v.x - s.x, v.z - s.z) * Mathf.Rad2Deg, s.facing * Mathf.Rad2Deg));
                if (d < bd && ang < 28f) { bd = d; best = v; }
            }
            return best;
        }

        public bool HumanShoot()
        {
            if (!CanAct || !abilitiesOn || me.job != Job.Police) return false;
            if (me.ammo <= 0) { Say("กระสุนหมดแล้ว"); g.Blip(0.5f, 0.35f); return false; }
            var t = ShootCandidate(me);
            if (t == null) { Say("ไม่มีเป้าหมายในแนวเล็ง — หันหน้าไปทางเป้าหมายก่อน (ระยะ 8)"); g.Blip(0.5f, 0.35f); return false; }
            Shoot(me, t);
            return true;
        }

        void Shoot(Actor shooter, Actor target)
        {
            shooter.ammo--; shooter.shootCd = 1.5f;
            fx.Shoot(shooter, target);
            string who = shooter.human ? "คุณ" : shooter.name;
            bool bad = target.role == Role.Infected || target.health == Health.Zombie || target.npc;
            if (bad)
            {
                if (target.npc) { Say($"{who}ยิงซอมบี้ล้ม!"); KillNpc(target); }
                else
                {
                    Say($"{who}ยิง {target.name} — {(target.health == Health.Zombie ? "เป็นซอมบี้" : "เป็นผู้ติดเชื้อต้นตอ!")} (ถูกกำจัด)");
                    Quarantine(target, "คุณถูกยิง");
                    target.health = Health.Dead;
                }
            }
            else
            {
                Say($"{who}ยิง {target.name} — เป็นผู้บริสุทธิ์! {who}เสียชีวิตจากความรู้สึกผิด");
                DropTasks(shooter);
                shooter.health = Health.Dead; shooter.path = null;
                if (shooter.human) g.OnHumanOut("คุณเสียชีวิต");
                else fx.Later(0.6f, () => { shooter.av?.Dispose(); shooter.av = null; });
            }
        }

        void KillNpc(Actor z)
        {
            z.health = Health.Dead; z.path = null;
            var av = z.av; z.av = null;
            fx.Later(0.45f, () => av?.Dispose());
        }

        // บอทตำรวจ (ฝั่งผู้รอดชีวิต): ยิงซอมบี้ที่ใกล้ หรือคนที่ตัวเองเห็นกับตาว่ากัด (ความสงสัย ≥ 1.4) — ไม่เล่นเสี่ยงยิงมั่ว
        void BotShoot(Actor a)
        {
            Actor t = null; float bd = 9999f;
            foreach (var z in actors)
                if (z.health == Health.Zombie && z != a) { float d = Dist(a, z); if (d < 6f && d < bd) { bd = d; t = z; } }
            if (t == null)
                foreach (var c in actors)
                {
                    if (c == a || !c.Alive) continue;
                    if (a.sus.TryGetValue(c.id, out float s) && s >= 1.4f) { float d = Dist(a, c); if (d < 7f && d < bd) { bd = d; t = c; } }
                }
            if (t == null) return;
            a.facing = Mathf.Atan2(t.x - a.x, t.z - a.z);
            a.path = null; a.workT = 0f; a.thinkT = 1f;
            Shoot(a, t);
        }

        public Actor BiteCandidate()
        {
            Actor best = null; float bd = BiteRange;
            foreach (var v in actors)
                if (v != me && v.health == Health.Healthy && v.role == Role.Survivor && !v.npc) { float d = Dist(me, v); if (d < bd) { bd = d; best = v; } }
            return best;
        }

        public void OnHumanTaskDone()
        {
            if (me.role == Role.Survivor && me.Alive) doneTotal += 1f;
        }

        // ═════════════════════════ อาการของผู้เล่น ═════════════════════════
        void UpdateSymptoms(float dt)
        {
            float target = 0f;
            if (me.health == Health.Incubating)
            {
                meCoughT -= dt;
                float k = 1f - Mathf.Clamp01(me.incT / IncubMax);        // ยิ่งใกล้กลายเป็นซอมบี้ยิ่งหนัก
                target = 0.25f + 0.75f * k;
                if (meCoughT <= 0f)
                {
                    meCoughT = Mathf.Lerp(11f, 4f, k) + (float)rng.NextDouble() * 3f;
                    me.bubble = "*ไอ*"; me.bubbleT = 1.2f; g.Cough(1f); symptomPulse = 1f;
                    if (clues.Count == 0 || !clues[clues.Count - 1].StartsWith("คุณรู้สึก")) Clue("คุณรู้สึกไม่สบาย…");
                }
            }
            symptomPulse = Mathf.MoveTowards(symptomPulse, 0f, dt * 1.5f);
            SymptomLevel = Mathf.MoveTowards(SymptomLevel, Mathf.Clamp01(target * 0.6f + symptomPulse * 0.7f), dt * 2f);
            g.Shake = symptomPulse * 0.25f * (GameSettings.S.camShake ? 1f : 0f);
        }

        void Spectate()
        {
            var alive = actors.FindAll(a => a.Alive && !a.human);
            if (alive.Count == 0) return;
            if (GameInput.Pressed(KeyCode.Space)) specIndex++;
            var t = alive[Mathf.Abs(specIndex) % alive.Count];
            g.DebugTeleport(t.x, t.z);
        }

        // ═════════════════════════ ชนะ/แพ้ ═════════════════════════
        void CheckWin()
        {
            if (phase == Phase.GameOver) return;
            // ผู้ติดเชื้อชนะเมื่อ "ผู้ติดเชื้อต้นตอ" ≥ ผู้รอดชีวิตที่ยังเป็นคน (ซอมบี้ที่แปลงแล้วถือว่าสูญเสียไปแล้ว ไม่นับทั้งสองฝั่ง)
            // ผู้รอดชีวิตชนะเมื่อวัคซีนเสร็จ หรือกักกันต้นตอและซอมบี้หมดแล้ว
            int surv = 0, src = 0, zombies = 0;
            foreach (var a in actors)
            {
                if (a.npc) continue;
                if (a.role == Role.Survivor && a.Alive) surv++;
                if (a.role == Role.Infected && a.Alive) src++;
                if (a.health == Health.Zombie) zombies++;
            }
            if (doneTotal >= needTotal) End("ผู้รอดชีวิต", "ผลิตวัคซีนสำเร็จ!");
            else if (src == 0 && zombies == 0) End("ผู้รอดชีวิต", "กักกันผู้ติดเชื้อได้ครบแล้ว!");
            else if (src >= surv) End("ผู้ติดเชื้อ", "ผู้ติดเชื้อมีจำนวนมากกว่าหรือเท่ากับผู้รอดชีวิต");
            else if (matchT >= MatchLimit) End("ผู้ติดเชื้อ", "หมดเวลา — วัคซีนไม่เสร็จทัน");
        }

        void End(string who, string why)
        {
            winner = who; winReason = why; phase = Phase.GameOver; phaseT = 0f;
            pendingCaller = null;
            foreach (var a in actors) { a.path = null; a.movingNow = false; }
        }

        // ═════════════════════════ แสดงตัวละครบอท ═════════════════════════
        void SyncAvatars(float dt)
        {
            foreach (var a in actors)
            {
                if (a.human || a.av == null) continue;
                bool show = a.OnMap;
                if (a.av.Root.activeSelf != show) a.av.Root.SetActive(show);
                if (!show) continue;
                var t = a.av.Root.transform;
                t.position = MapData.ToWorld(a.x + a.fxOff.x, a.av.BobHeight(a.walkT), a.z + a.fxOff.y);
                t.rotation = Quaternion.Euler(0f, (Mathf.PI - a.facing) * Mathf.Rad2Deg, 0f) * Quaternion.Euler(a.fxPitch, 0f, a.fxRoll);
                float sy = a.fxScaleY, sxz = 1f / Mathf.Sqrt(Mathf.Max(0.5f, sy));
                t.localScale = new Vector3(sxz, sy, sxz);
                a.av.Animate(a.movingNow, dt, a.walkT);
            }
        }

        // ═════════════════════════ ค้นหาเส้นทาง A* ═════════════════════════
        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 }, DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };
        float[] gCost; int[] parent; bool[] closed; List<KeyValuePair<float, int>> heap = new List<KeyValuePair<float, int>>();

        bool Free(int x, int z) => !g.Grid.IsBlocked(x, z);

        Vector2Int NearestFree(int x, int z)
        {
            for (int r = 0; r < 6; r++)
                for (int j = -r; j <= r; j++)
                    for (int i = -r; i <= r; i++)
                        if (Free(x + i, z + j)) return new Vector2Int(x + i, z + j);
            return new Vector2Int(x, z);
        }

        void Push(float f, int i)
        {
            heap.Add(new KeyValuePair<float, int>(f, i));
            int c = heap.Count - 1;
            while (c > 0) { int p = (c - 1) / 2; if (heap[p].Key <= heap[c].Key) break; var t = heap[p]; heap[p] = heap[c]; heap[c] = t; c = p; }
        }

        KeyValuePair<float, int> Pop()
        {
            var top = heap[0]; var last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
            {
                heap[0] = last; int c = 0;
                while (true)
                {
                    int l = c * 2 + 1, r = l + 1, m = c;
                    if (l < heap.Count && heap[l].Key < heap[m].Key) m = l;
                    if (r < heap.Count && heap[r].Key < heap[m].Key) m = r;
                    if (m == c) break;
                    var t = heap[m]; heap[m] = heap[c]; heap[c] = t; c = m;
                }
            }
            return top;
        }

        public List<Vector2> FindPath(Vector2 from, Vector2 to)
        {
            int W = MapData.GridW, H = MapData.GridH;
            if (gCost == null) { gCost = new float[W * H]; parent = new int[W * H]; closed = new bool[W * H]; }
            System.Array.Fill(gCost, float.MaxValue); System.Array.Fill(closed, false); heap.Clear();
            var s = NearestFree(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.y));
            var e = NearestFree(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.y));
            int si = s.y * W + s.x, ei = e.y * W + e.x;
            gCost[si] = 0f; parent[si] = -1; Push(0f, si);
            bool found = false;
            int guard = 0;
            while (heap.Count > 0 && guard++ < 20000)
            {
                var cur = Pop(); int ci = cur.Value;
                if (closed[ci]) continue;
                closed[ci] = true;
                if (ci == ei) { found = true; break; }
                int cx = ci % W, cz = ci / W;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + DX[d], nz = cz + DZ[d];
                    if (nx < 0 || nz < 0 || nx >= W || nz >= H || !Free(nx, nz)) continue;
                    if (d >= 4 && (!Free(cx + DX[d], cz) || !Free(cx, cz + DZ[d]))) continue;
                    int ni = nz * W + nx;
                    if (closed[ni]) continue;
                    float cost = (d >= 4 ? 1.414f : 1f);
                    // เลี่ยงชิดกำแพง (ให้เดินกลางทาง ลดการเสียดสี)
                    if (!Free(nx + 1, nz) || !Free(nx - 1, nz) || !Free(nx, nz + 1) || !Free(nx, nz - 1)) cost += 0.6f;
                    float ng = gCost[ci] + cost;
                    if (ng >= gCost[ni]) continue;
                    gCost[ni] = ng; parent[ni] = ci;
                    float dx = Mathf.Abs(nx - e.x), dz = Mathf.Abs(nz - e.y);
                    Push(ng + (dx + dz) + (1.414f - 2f) * Mathf.Min(dx, dz), ni);
                }
            }
            var path = new List<Vector2>();
            if (!found) { path.Add(to); return path; }
            for (int i = ei; i != -1; i = parent[i]) path.Add(new Vector2(i % W + 0.5f, i / W + 0.5f));
            path.Reverse();
            if (path.Count > 0 && Free(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.y))) path[path.Count - 1] = to;
            // ตัดจุดที่เกือบเป็นเส้นตรงเดียวกัน
            for (int i = path.Count - 2; i > 0; i--)
            {
                Vector2 a = path[i] - path[i - 1], b = path[i + 1] - path[i];
                if (Mathf.Abs(a.x * b.y - a.y * b.x) < 0.001f) path.RemoveAt(i);
            }
            if (path.Count > 0) path.RemoveAt(0);
            return path;
        }
    }
}
