using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    public sealed class TaskState
    {
        public TaskDef def;
        public bool done;
        public Transform group, ring, gem;
        public Material ringMat, gemMat, beamMat;
        public float gemAngle;
        public bool assigned = true;   // เป็นภารกิจของผู้เล่นหรือไม่ (แต่ละคนได้ไม่เท่ากัน)
    }

    // ตัวควบคุมเกมทั้งหมด (port จาก js/main.js)
    //  • กด Play ใน scene ไหนก็ได้ → สร้างเกมให้เองผ่าน [RuntimeInitializeOnLoadMethod] (ไม่ต้องตั้งค่าอะไร)
    //  • พิกัดภายใน (PosX, PosZ) เป็น "พิกัดกริด" เหมือนเว็บ · แปลงเป็น world ของ Unity ตอนวาง transform ด้วย z → -z
    [DefaultExecutionOrder(-100)]
    public sealed class SkeldGame : MonoBehaviour
    {
        // ───── ค่าคงที่ (เหมือน main.js) ─────
        const float PlayerR = 0.42f;
        const float PlayerSpeed = 5.6f;
        const float BlockedFraction = 0.3f; // ขยับได้จริงน้อยกว่า 30% ของที่ตั้งใจ = ถูกขวาง
        const float TaskRange = 1.6f;
        const float TaskRangeExit = 0.5f;   // รัศมีขาออกกว้างกว่าขาเข้า 0.5 (hysteresis)
        const float FogInner = 5.5f;
        const float FogOuter = 11f;
        const float FogDark = 0.03f;
        static readonly Color TaskColor = Mats.Hex(0xffd23f);
        static readonly Color TaskDoneColor = Mats.Hex(0x4cff8a);

        // ───── state ที่ HUD อ่าน ─────
        public MapGrid Grid { get; private set; }
        public float PosX, PosZ, Facing;
        public readonly List<TaskState> Tasks = new List<TaskState>();
        public string LocationName = "CAFETERIA";
        public bool PromptVisible;
        public string PromptText = "";
        public float HoldProgress;           // 0..1
        public float PromptAlpha;            // 0..1 เฟดของแผง prompt
        public bool AllDone;
        public float BannerAlpha;
        public bool Overview;
        public int DoneCount
        {
            get { int n = 0; foreach (var t in Tasks) if (t.assigned && t.done) n++; return n; }
        }
        public int AssignedCount
        {
            get { int n = 0; foreach (var t in Tasks) if (t.assigned) n++; return n; }
        }

        [Header("Character")]
        [Tooltip("ตัวละครเริ่มต้น (ใช้เมื่อผู้เล่นยังไม่เคยเลือกในหน้าเลือกตัวละคร): ชื่อไฟล์ใน Assets/Resources/Characters เช่น character-alien, character-robot, character-knight\n" +
                 "หรือ crewmate-red / crewmate-blue … · random = สุ่ม · crewmate = สุ่มสี Crewmate")]
        public string mascotModel = "character-alien";

        [Tooltip("เปิดหน้าเลือกตัวละครทุกครั้งที่เริ่มเกม (กด C เปิดซ้ำได้ตลอด)")]
        public bool showCharacterSelectOnStart = false;

        [Header("Map")]
        [Tooltip("เปิด = พื้น/กำแพงจาก Kenney Modular Space Kit · ปิด = แมปเดิมที่สร้างด้วยโค้ดล้วน")]
        public bool useKenneyKit = true;

        [Tooltip("เปิด = นอกแมปเป็นพื้นหญ้า รั้ว ป่า ลานเฮลิคอปเตอร์ เต็นท์ (ตามภาพ Blueprint) · ปิด = อวกาศดำ + ดวงดาวแบบเดิม")]
        public bool useSurroundings = true;

        // ───── ภายใน ─────
        Camera cam;
        Light sun, playerLight;
        Avatar avatar;               // ตัวละครที่ผู้เล่นคุม (โมเดล Kenney หรือ Crewmate)
        Hud hud;
        public TaskGames Games { get; private set; }   // มินิเกมประจำภารกิจ
        public PZ PZ { get; private set; }             // กติกา Patient Zero (บทบาท/ติดเชื้อ/ประชุม/กลางวัน-กลางคืน)
        PZHud pzHud;
        public float Shake;                            // กล้องสั่น (อาการป่วย)
        public float Jolt;                             // กล้องกระตุกครั้งเดียว (ถูกกัด) — ค่อย ๆ จางเอง
        public string HumanOutText = "";
        public static bool SkipSelectOnce;
        public static bool SkipMenuOnce;                // รีสตาร์ทแมตช์ → ข้ามเมนูหลัก
        public bool enablePatientZero = true;
        public bool InMenu { get; private set; }
        public bool Paused { get; private set; }
        public CharacterSelect Select => select;
        public Transform PlayerRoot => avatar != null ? avatar.Root.transform : null;
        AudioClip gunClip;
        MenuUI menu;
        Transform taskRoot;
        AudioSource musicSrc;
        float fps, fpsAcc; int fpsN;
        GUIStyle fpsStyle;
        CharacterSelect select;
        AudioSource audioSrc;
        AudioClip[] stepClips;
        AudioClip chimeClip, blipClip, coughClip;

        float walkT, yawTarget, zoom = 17f, elapsed;
        float camYaw, camPitch = 58f * Mathf.Deg2Rad, camDist = 17f, camTx, camTz;
        bool fogOn = true, built;
        bool muted { get => GameSettings.Mute; set => GameSettings.Mute = value; }
        float fogAmount = 1f;
        TaskState holdTask;
        float standY;   // ยกตัวละครขึ้นเมื่อยืนบนแท่น (แท่นสแกน)
        float holdT;
        int lastStepIdx;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindFirstObjectByType<SkeldGame>() == null)
                new GameObject("SkeldGame").AddComponent<SkeldGame>();
        }

        void Awake() => Init();

        // แยกออกมาเพื่อให้ editor tool เรียกสร้างฉากนอก Play mode ได้
        public void Init()
        {
            if (built) return;
            built = true;

            Grid = new MapGrid();
            SetupRenderSettings();
            SetupCamera();
            SetupLights();
            MapBuilder.UseKit = useKenneyKit;
            MapBuilder.UseSurroundings = useSurroundings;
            if (cam != null && useSurroundings) cam.backgroundColor = Mats.Hex(0x050a07); // ไกลสุดขอบพื้นหญ้า = เขียวเกือบดำ ไม่ใช่น้ำเงินอวกาศ
            MapBuilder.Build(Grid).transform.SetParent(transform, false);

            PosX = MapData.Spawn.x; PosZ = MapData.Spawn.y;
            camTx = PosX; camTz = PosZ;

            // ตัวละครเริ่มต้น: ค่าที่ผู้เล่นเลือกไว้ครั้งก่อน → ไม่มีก็ใช้ค่าจาก Inspector
            string saved = PlayerPrefs.GetString(PrefKey, "");
            SetCharacter(saved != "" ? saved : CharacterCatalog.Resolve(mascotModel), save: false);

            BuildTaskMarkers();
            SetupAudio();
            hud = new Hud(this);
            Games = new TaskGames(this);
            select = new CharacterSelect(this);
            // (เลือกตัวละครทำจากเมนูหลัก/ห้อง หรือกด C ระหว่างเล่น — ไม่เปิดเองตอนเริ่มอีกแล้ว)
            SkipSelectOnce = false;

            // เปิดเกม: หน้าชื่อเกม/เมนูหลัก (ฉากหมุนอยู่ด้านหลัง) · รีสตาร์ทแมตช์/ใช้จากเครื่องมือ editor → เริ่มแมตช์ทันที
            menu = new MenuUI(this);
            if (!Application.isPlaying || SkipMenuOnce)
            {
                SkipMenuOnce = false;
                StartMatch(GameSettings.Room, silent: true);
            }
            else EnterMenu();

            ApplyTransforms(0f, false);
            UpdateCamera(0.016f);
            UpdateFog(1f);
        }

        // ───────────────────────── Setup ─────────────────────────
        const string PrefKey = "Skeld.Character";

        // ตัวละครที่ใช้อยู่ตอนนี้ (id จาก CharacterCatalog) · ครั้งแรกใช้ค่าที่จำไว้ ถ้าไม่มีใช้ค่าจาก Inspector
        public string CharacterId { get; private set; } = "";

        // เปลี่ยนตัวละครทันที: ตัวใหม่ยืนตำแหน่งเดิม หันทิศเดิม (ทำได้ทั้งตอนเดินและตอนเปิดหน้าเลือกตัวละคร)
        public void SetCharacter(string id, bool save = true)
        {
            if (avatar != null && CharacterId == id) return;
            var next = Avatar.Create(id);
            next.Root.transform.SetParent(transform, false);
            avatar?.Dispose();
            avatar = next;
            CharacterId = id;
            if (save)
            {
                PlayerPrefs.SetString(PrefKey, id);
                PlayerPrefs.Save();
            }
            RefreshGun();
            if (playerLight != null) ApplyTransforms(0f, false);
        }

        // เปลี่ยนไปตัวก่อนหน้า/ถัดไปในรายการ (วนรอบ)
        public void CycleCharacter(int dir)
        {
            int n = CharacterCatalog.All.Count;
            int i = CharacterCatalog.IndexOf(CharacterId);
            SetCharacter(CharacterCatalog.All[(i + dir + n * 2) % n].id);
        }

        void OnDestroy() => avatar?.Dispose();

        void SetupRenderSettings()
        {
            // HemisphereLight(0xcfe0ff, 0x2a3144) → Trilight ambient
            Color sky = Mats.Hex(0xcfe0ff), ground = Mats.Hex(0x2a3144);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, 0.5f);
            RenderSettings.ambientGroundColor = ground;
            // แสงของ three.js (non-legacy) เป็นหน่วยฟิสิกส์ — Lambert หาร π ส่วน Unity ไม่หาร จึงแปลง Unity = three / π
            // Hemisphere 1.25 → 0.4 · Directional 1.7 → 0.54 (ดูใน SetupLights)
            RenderSettings.ambientIntensity = 1.25f / Mathf.PI;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.35f;

            GameSettings.ApplyQuality();   // เงา/ลดรอยหยัก/aniso ตามที่ตั้งไว้ (พื้นเอียง 58° ต้องใช้ aniso ไม่งั้นเส้นกระเบื้องไกล ๆ กะพริบ)
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.SetParent(null);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Mats.Hex(0x03050a);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1500f;
            cam.renderingPath = RenderingPath.Forward; // finalcolor ของ shader หมอกใช้ได้เฉพาะ Forward
            cam.cullingMask &= ~(1 << CharacterSelect.PreviewLayer); // ไม่เห็นฉากถ่ายตัวอย่างของหน้าเลือกตัวละคร
            cam.allowHDR = true; // ต้องเป็น HDR เพื่อให้ tone mapping ได้ค่าแสงจริง (emissive เกิน 1.0)
            if (cam.GetComponent<ToneMapper>() == null) cam.gameObject.AddComponent<ToneMapper>();
        }

        void SetupLights()
        {
            // ปิดไฟเดิมของ scene (เช่น Directional Light ใน SampleScene) กันแสงซ้อน
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.enabled = false;

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.white;
            sun.intensity = 1.7f / Mathf.PI;
            sun.shadows = LightShadows.Soft;
            sun.cullingMask = ~(1 << CharacterSelect.PreviewLayer); // ไม่ส่องฉากถ่ายตัวอย่าง (มีไฟของตัวเอง)
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.4f;
            // three: ดวงอาทิตย์อยู่ที่ (+x-12, 28, z+14) มองมาที่ผู้เล่น ⇒ ทิศแสง = (12, -28, -14) ในพิกัดกริด → (12, -28, +14) ใน Unity
            sunGo.transform.rotation = Quaternion.LookRotation(new Vector3(12f, -28f, 14f));

            var plGo = new GameObject("PlayerLight");
            plGo.transform.SetParent(transform, false);
            playerLight = plGo.AddComponent<Light>();
            playerLight.type = LightType.Point;
            playerLight.color = Mats.Hex(0xfff0d0);
            // three: 22 cd, decay 2 · Unity ใช้ attenuation แบบ 1/(1+25(d/r)²) ⇒ จูนให้ความสว่างใต้เท้าเท่ากัน (≈0.68×albedo)
            playerLight.intensity = 1.4f;
            playerLight.range = 16f;
            playerLight.shadows = LightShadows.None;
        }

        void SetupAudio()
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;
            stepClips = ProceduralAudio.MakeFootsteps(6);
            chimeClip = ProceduralAudio.MakeChime();
            blipClip = ProceduralAudio.MakeBlip();
            coughClip = ProceduralAudio.MakeCough();
            gunClip = ProceduralAudio.MakeGunshot();

            // เสียงบรรยากาศวนลูป (ปรับระดับด้วย "เสียงบรรยากาศ" ในการตั้งค่า)
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.clip = ProceduralAudio.MakeAmbient();
            musicSrc.loop = true; musicSrc.spatialBlend = 0f; musicSrc.playOnAwake = false;
            if (Application.isPlaying) musicSrc.Play();
        }

        void UpdateMusic()
        {
            if (musicSrc == null) return;
            float night = PZ != null ? PZ.NightAmount : 0f;
            musicSrc.volume = GameSettings.Music * 0.3f * Mathf.Lerp(1f, 1.6f, night);
            musicSrc.pitch = Mathf.Lerp(1f, 0.86f, night);
        }

        void PlayStep()
        {
            if (muted || stepClips == null) return;
            int i = Random.Range(0, stepClips.Length);
            if (i == lastStepIdx) i = (i + 1) % stepClips.Length;
            lastStepIdx = i;
            audioSrc.pitch = 0.85f + Random.value * 0.3f;
            audioSrc.PlayOneShot(stepClips[i], 0.9f * GameSettings.Sfx);
        }

        // เสียงปุ่ม/ผิดพลาดในมินิเกม (pitch สูง = ถูก · ต่ำ = ผิด)
        public void Blip(float pitch, float vol)
        {
            if (muted || blipClip == null) return;
            audioSrc.pitch = pitch;
            audioSrc.PlayOneShot(blipClip, vol * GameSettings.Sfx);
        }

        public void Gunshot(float vol)
        {
            if (muted || gunClip == null) return;
            audioSrc.pitch = 0.95f + Random.value * 0.1f;
            audioSrc.PlayOneShot(gunClip, Mathf.Clamp01(vol) * GameSettings.Sfx);
        }

        // ผู้เล่นเป็นตำรวจ → ติดปืนที่ตัวละคร (เรียกใหม่ทุกครั้งที่เปลี่ยนตัวละคร)
        public void RefreshGun()
        {
            if (PZ != null && PZ.me.job == Job.Police && avatar != null) GunProp.Attach(avatar);
        }

        public void Cough(float vol)
        {
            if (muted || coughClip == null || vol <= 0f) return;
            audioSrc.pitch = 0.9f + Random.value * 0.25f;
            audioSrc.PlayOneShot(coughClip, vol * GameSettings.Sfx);
        }

        // ผู้เล่นออกจากเกม (กลายเป็นซอมบี้/ถูกกักกัน/ตาย) → ซ่อนตัวละคร ปิดมินิเกม แล้วรับชมต่อ
        public void OnHumanOut(string reason)
        {
            HumanOutText = reason;
            if (avatar != null) avatar.Root.SetActive(false);
            Games.Close();
            PromptVisible = false; PromptAlpha = 0f;
        }

        public static void Restart()
        {
            SkipMenuOnce = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // แจกภารกิจให้ผู้เล่น: เฉพาะที่ได้รับจะเห็นจุดเหลือง/ทำได้
        public void ApplyAssignment(List<int> mine)
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                Tasks[i].assigned = mine.Contains(i);
                Tasks[i].group.gameObject.SetActive(Tasks[i].assigned);
            }
        }

        void PlayChime()
        {
            if (muted || chimeClip == null) return;
            audioSrc.pitch = 1f;
            audioSrc.PlayOneShot(chimeClip, 0.8f * GameSettings.Sfx);
        }

        // ───────────────────────── Task markers ─────────────────────────
        void BuildTaskMarkers()
        {
            var root = new GameObject("TaskMarkers").transform;
            root.SetParent(transform, false);
            taskRoot = root;

            foreach (var def in MapData.Tasks)
            {
                var t = new TaskState { def = def };
                t.ringMat = Mats.Unlit(new Color(TaskColor.r, TaskColor.g, TaskColor.b, 0.85f), transparent: true, doubleSided: true);
                t.gemMat = Mats.Unlit(TaskColor);
                t.beamMat = Mats.Unlit(new Color(TaskColor.r, TaskColor.g, TaskColor.b, 0.14f), transparent: true, doubleSided: true, zWrite: false);

                var g = new GameObject("Task_" + def.id);
                g.transform.SetParent(root, false);
                g.transform.position = MapData.ToWorld(def.x, def.y, def.z);
                t.group = g.transform;

                t.ring = AddMarkerPart(g.transform, "ring", MeshBuilder.Ring(0.7f, 0.9f, 40), t.ringMat, new Vector3(0, 0.06f, 0));
                t.gem = AddMarkerPart(g.transform, "gem", MeshBuilder.Octahedron(0.28f), t.gemMat, new Vector3(0, 1.2f, 0));
                AddMarkerPart(g.transform, "beam", MeshBuilder.Cylinder(0.55f, 0.55f, 2.6f, 24, openEnded: true), t.beamMat, new Vector3(0, 1.3f, 0));
                Tasks.Add(t);
            }
        }

        static Transform AddMarkerPart(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        // มินิเกมเล่นสำเร็จ → นับภารกิจ
        public void FinishTask(TaskState t)
        {
            if (t == null || t.done) return;
            CompleteTask(t);
            holdTask = null; PromptVisible = false; PromptAlpha = 0f;
            PZ?.OnHumanTaskDone();
        }

        void CompleteTask(TaskState t)
        {
            t.done = true;
            SetAlphaColor(t.ringMat, TaskDoneColor, 0.85f);
            SetAlphaColor(t.gemMat, TaskDoneColor, 1f);
            SetAlphaColor(t.beamMat, TaskDoneColor, 0.05f);
            t.gem.gameObject.SetActive(false);
            if (DoneCount == AssignedCount) AllDone = true;
            PlayChime();
        }

        static void SetAlphaColor(Material m, Color c, float a) => m.SetColor("_Color", new Color(c.r, c.g, c.b, a));

        // ───────────────────────── Update ─────────────────────────
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            fpsAcc += Time.unscaledDeltaTime; fpsN++;
            if (fpsAcc >= 0.5f) { fps = fpsN / fpsAcc; fpsAcc = 0f; fpsN = 0; }

            // F11 สลับเต็มจอ/หน้าต่าง
            if (GameInput.Pressed(KeyCode.F11))
            {
                var S = GameSettings.S;
                S.displayMode = S.displayMode == 2 ? 0 : 2;
                if (S.displayMode == 2 && S.resW == 0) { S.resW = 1920; S.resH = 1080; }
                GameSettings.ApplyDisplay(); GameSettings.Save();
            }

            UpdateMusic();
            bool wasPaused = Paused;
            menu.Tick(Time.unscaledDeltaTime);

            // เมนูหลัก: ฉากหมุนเป็นฉากหลัง
            if (InMenu)
            {
                StepMenu(dt);
                select.Tick(dt);
                if (select.Open) select.HandleInput();
                return;
            }
            if (wasPaused || Paused) { select.Tick(dt); return; }   // พักเกม: โลกหยุด

            // Esc = พักเกม (ถ้าไม่ได้เล่นมินิเกม/เลือกตัวละครอยู่)
            if (GameInput.Pressed(KeyCode.Escape) && !Games.Open && !select.Open) { Pause(); return; }

            // หน้าเลือกตัวละคร (C) และสลับตัวละครด่วนระหว่างเดิน ([ และ ])
            if (GameInput.Pressed(KeyCode.C)) select.Toggle();
            if (select.Open)
            {
                select.HandleInput();
            }
            else
            {
                if (GameInput.Pressed(KeyCode.LeftBracket)) CycleCharacter(-1);
                if (GameInput.Pressed(KeyCode.RightBracket)) CycleCharacter(1);
            }

            // เปิดเมนูอยู่ → หยุดรับปุ่มเดิน/เควส (โลกยังเดินต่อ เช่น ตัวละครยังเล่นท่ายืน)
            Step(dt, !select.Open);
            select.Tick(dt);
        }

        // ───────────────────────── เมนู / เริ่มแมตช์ / พักเกม ─────────────────────────
        void EnterMenu()
        {
            InMenu = true; Paused = false;
            if (taskRoot != null) taskRoot.gameObject.SetActive(false);
            if (avatar != null) avatar.Root.SetActive(false);
            if (playerLight != null) playerLight.enabled = false;
            menu.ShowTitle();
        }

        void StepMenu(float dt)
        {
            elapsed += dt;
            UpdateCamera(dt);
            UpdateFog(dt);
        }

        // เริ่มแมตช์ตามการตั้งค่าห้อง
        public void StartMatch(RoomSettings rs, bool silent = false)
        {
            rs.Clamp();
            GameSettings.Room = rs;
            GameSettings.Save();
            InMenu = false; Paused = false;
            menu?.Hide();
            if (taskRoot != null) taskRoot.gameObject.SetActive(true);
            if (avatar != null) avatar.Root.SetActive(true);
            if (playerLight != null) playerLight.enabled = true;
            PosX = MapData.Spawn.x; PosZ = MapData.Spawn.y; Facing = 0f;
            Overview = false; HumanOutText = ""; Shake = 0f;
            PZ = new PZ(this, rs);
            pzHud = new PZHud(this);
            RefreshGun();
            ApplyTransforms(0f, false);
            UpdateCamera(0.016f);
            UpdateFog(1f);
        }

        public void Pause() { Paused = true; Games.Close(); menu.ShowPause(); }
        public void Resume() { Paused = false; menu.Hide(); }

        public void RestartMatch() => Restart();
        public void BackToMenu()
        {
            SkipMenuOnce = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // dt = วินาที · readInput=false ใช้ตอน editor tool จำลองเฟรมโดยไม่มีการกดปุ่ม
        public void Step(float dt, bool readInput)
        {
            elapsed += dt;

            Games.Update(dt);
            PZ?.Update(dt, readInput);
            bool ctrl = readInput && !Games.Open;                          // ระหว่างเล่นมินิเกมตัวละคร/กล้องหยุดรับปุ่ม
            bool canMove = ctrl && (PZ == null || !PZ.Frozen);              // ประชุม/เปิดเผยบทบาท/ถูกกักกัน = เดินไม่ได้

            if (ctrl)
            {
                if (GameInput.Pressed(KeyCode.N)) muted = !muted;
                if (GameInput.Pressed(KeyCode.V)) fogOn = !fogOn;
                if (GameInput.Pressed(KeyCode.M) || GameInput.Pressed(KeyCode.Tab)) Overview = !Overview;
                if (PZ == null && GameInput.Pressed(KeyCode.R)) { PosX = MapData.Spawn.x; PosZ = MapData.Spawn.y; PushWorld.ResetAll(); }

                float wheel = GameInput.Scroll();
                if (wheel != 0f) zoom = Mathf.Clamp(zoom - Mathf.Sign(wheel) * 1.5f, 8f, 32f); // เลื่อนลง = ซูมออก เหมือนเว็บ

                if (GameInput.Held(KeyCode.Q)) yawTarget += 1.8f * GameSettings.S.camSpeed * dt;
                if (GameInput.Held(KeyCode.E)) yawTarget -= 1.8f * GameSettings.S.camSpeed * dt;
            }

            float fwd = 0f, str = 0f;
            if (canMove)
            {
                fwd = (GameInput.Held(KeyCode.W) || GameInput.Held(KeyCode.UpArrow) ? 1f : 0f)
                    - (GameInput.Held(KeyCode.S) || GameInput.Held(KeyCode.DownArrow) ? 1f : 0f);
                str = (GameInput.Held(KeyCode.D) || GameInput.Held(KeyCode.RightArrow) ? 1f : 0f)
                    - (GameInput.Held(KeyCode.A) || GameInput.Held(KeyCode.LeftArrow) ? 1f : 0f);
            }

            float sy = Mathf.Sin(camYaw), cy = Mathf.Cos(camYaw);
            float dx = cy * str - sy * fwd;
            float dz = -sy * str - cy * fwd;
            float len = Mathf.Sqrt(dx * dx + dz * dz);
            bool moving = len > 0f && MoveStep(dx / len, dz / len, dt);   // ขยับตัวได้จริง (ใช้ตัดสินท่าเดิน/เสียงเดิน)

            if (moving)
            {
                // เท้าแตะพื้นทุกครั้งที่ walkT ข้ามผลคูณของ π + ก้าวแรกทันทีที่เริ่มเดิน
                float prev = walkT;
                walkT += dt * 12f;
                if (prev == 0f || Mathf.Floor(walkT / Mathf.PI) > Mathf.Floor(prev / Mathf.PI)) PlayStep();
            }
            else
            {
                walkT = 0f;
            }

            standY = Mathf.MoveTowards(standY, StandHeightAt(PosX, PosZ), dt * 2.5f);
            ApplyTransforms(dt, moving);
            UpdateCamera(dt);
            UpdateTasks(dt, readInput);
            UpdateFog(dt);

            var room = MapData.RoomAt(PosX, PosZ);
            LocationName = room != null ? room.name : "HALLWAY";
            BannerAlpha = Mathf.MoveTowards(BannerAlpha, AllDone ? 1f : 0f, dt * 2f);
        }

        // เดินไปทิศ (dx, dz) (หน่วยความยาว, พิกัดกริด) หนึ่งเฟรม พร้อมชนกำแพง/ของ · คืน true ถ้าขยับตัวได้จริง
        // เดินชนกำแพง/ของจนแทบไม่ขยับ → false: ไม่เล่นท่าวิ่งและไม่มีเสียงฝีเท้า (เดิมวิ่งอยู่กับที่ + เสียงดังทั้งที่ไม่ขยับ)
        // เดินเฉียดกำแพงแล้วไถไปตามผนังยังได้ true เพราะระยะที่ขยับได้เกินเกณฑ์
        bool MoveStep(float dx, float dz, float dt)
        {
            float dist = PlayerSpeed * dt;
            float x0 = PosX, z0 = PosZ;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.15f));
            for (int s = 0; s < steps; s++)
            {
                PosX += dx * dist / steps;
                PosZ += dz * dist / steps;
                ResolveCollision();
            }

            // ยังหันหน้าไปทางที่กดเสมอ แม้ถูกขวางอยู่ (ตอบสนองดี)
            float target = Mathf.Atan2(dx, dz);
            float diff = target - Facing;
            diff = Mathf.Atan2(Mathf.Sin(diff), Mathf.Cos(diff));
            Facing += diff * (1f - Mathf.Exp(-16f * dt));

            float moved = Mathf.Sqrt((PosX - x0) * (PosX - x0) + (PosZ - z0) * (PosZ - z0));
            return dist > 1e-5f && moved > dist * BlockedFraction;
        }

        // ความสูงพื้นที่ยืน: บนแท่นสแกน (กึ่งกลางระหว่างเสา) ยกขึ้นเท่าความสูงเตียงสแกน
        static float StandHeightAt(float x, float z)
        {
            foreach (var p in MapData.Props)
            {
                if (p.kind != "scanner") continue;
                float hw = (p.w ?? 2.4f) / 2f - 0.5f, hd = (p.d ?? 1.4f) / 2f;
                if (Mathf.Abs(x - p.x) < hw && Mathf.Abs(z - p.z) < hd) return 0.3f;
            }
            return 0f;
        }

        void ApplyTransforms(float dt, bool moving)
        {
            Transform p = avatar.Root.transform;
            var me = PZ != null ? PZ.me : null;
            Vector2 fo = me != null ? me.fxOff : Vector2.zero;
            float fsy = me != null ? me.fxScaleY : 1f;
            p.position = MapData.ToWorld(PosX + fo.x, avatar.BobHeight(walkT) + standY, PosZ + fo.y);
            // three: rotation.y = facing (หน้า = +z กริด) · Unity z กลับด้าน ⇒ yaw = π − facing
            p.rotation = Quaternion.Euler(0f, (Mathf.PI - Facing) * Mathf.Rad2Deg, 0f) * (me != null ? Quaternion.Euler(me.fxPitch, 0f, me.fxRoll) : Quaternion.identity);
            float fsxz = 1f / Mathf.Sqrt(Mathf.Max(0.5f, fsy));
            p.localScale = new Vector3(fsxz, fsy, fsxz);
            avatar.Animate(moving, dt, walkT);

            playerLight.transform.position = MapData.ToWorld(PosX, 3.2f, PosZ);
        }

        // ───────────────────────── Collision ─────────────────────────
        // วงกลมผู้เล่น (รัศมี PlayerR) ชนกับช่องกริดที่ตัน — ดันออกตามจุดที่ใกล้ที่สุด
        void ResolveCollision()
        {
            ResolveGrid();
            if (PushWorld.Count > 0)
            {
                // ผลักของชิ้นเล็ก (ถัง/รถเข็น/ลังเล็ก) แล้วเช็กกำแพงอีกรอบกันผู้เล่นโดนดันทะลุ
                PushWorld.ResolvePlayer(ref PosX, ref PosZ, PlayerR, Grid);
                ResolveGrid();
            }
        }

        void ResolveGrid()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                int cx = Mathf.FloorToInt(PosX), cz = Mathf.FloorToInt(PosZ);
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int gx = cx + i, gz = cz + j;
                        if (!Grid.IsBlocked(gx, gz)) continue;
                        float nx = Mathf.Clamp(PosX, gx, gx + 1);
                        float nz = Mathf.Clamp(PosZ, gz, gz + 1);
                        float dx = PosX - nx, dz = PosZ - nz;
                        float d2 = dx * dx + dz * dz;
                        if (d2 >= PlayerR * PlayerR) continue;
                        if (d2 > 1e-8f)
                        {
                            float d = Mathf.Sqrt(d2);
                            PosX += dx / d * (PlayerR - d);
                            PosZ += dz / d * (PlayerR - d);
                        }
                        else
                        {
                            // จุดศูนย์กลางอยู่ในช่องที่ตัน → ดันออกทางขอบที่ใกล้สุด
                            float l = PosX - gx, r = gx + 1 - PosX, t = PosZ - gz, b = gz + 1 - PosZ;
                            float m = Mathf.Min(Mathf.Min(l, r), Mathf.Min(t, b));
                            if (m == l) PosX = gx - PlayerR;
                            else if (m == r) PosX = gx + 1 + PlayerR;
                            else if (m == t) PosZ = gz - PlayerR;
                            else PosZ = gz + 1 + PlayerR;
                        }
                    }
                }
            }
        }

        // ───────────────────────── Camera ─────────────────────────
        void UpdateCamera(float dt)
        {
            float vHalf = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2f);
            float fit = Mathf.Max((MapData.GridH + 8) / 2f / vHalf, (MapData.GridW + 8) / 2f / vHalf / cam.aspect);

            float gYaw, gPitch, gDist, gTx, gTz;
            if (InMenu) { gYaw = elapsed * 0.07f; gPitch = 52f * Mathf.Deg2Rad; gDist = fit * 0.74f; gTx = MapData.GridW / 2f; gTz = MapData.GridH / 2f; }
            else if (Overview) { gYaw = 0f; gPitch = 89f * Mathf.Deg2Rad; gDist = fit; gTx = MapData.GridW / 2f; gTz = MapData.GridH / 2f; }
            else { gYaw = yawTarget; gPitch = 58f * Mathf.Deg2Rad; gDist = zoom; gTx = PosX; gTz = PosZ; }

            float k = 1f - Mathf.Exp(-7f * dt);
            camYaw += (gYaw - camYaw) * k;
            camPitch += (gPitch - camPitch) * k;
            camDist += (gDist - camDist) * k;
            // ตามผู้เล่นแน่นกว่า ส่วน overview เลื่อนนุ่ม ๆ
            float kt = Overview ? k : 1f - Mathf.Exp(-14f * dt);
            camTx += (gTx - camTx) * kt;
            camTz += (gTz - camTz) * kt;

            float cp = Mathf.Cos(camPitch);
            Vector3 pos = new Vector3(
                camTx + Mathf.Sin(camYaw) * cp * camDist,
                Mathf.Sin(camPitch) * camDist,
                camTz + Mathf.Cos(camYaw) * cp * camDist);
            Jolt = Mathf.MoveTowards(Jolt, 0f, dt * 1.2f);
            float shake = Shake + Jolt;
            if (shake > 0.001f) pos += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shake;
            cam.transform.position = MapData.ToWorld(pos.x, pos.y, pos.z);
            cam.transform.LookAt(MapData.ToWorld(camTx, 0f, camTz), Vector3.up);
        }

        // ───────────────────────── Fog ─────────────────────────
        // มืดลงตามระยะจากตัวละคร · Overview ปิดหมอกอัตโนมัติ · V = เปิด/ปิด
        void UpdateFog(float dt)
        {
            float night = PZ != null ? PZ.NightAmount : 0f;
            float target = (fogOn || night > 0.02f) && !Overview && !InMenu ? 1f : 0f;
            fogAmount += (target - fogAmount) * (1f - Mathf.Exp(-6f * dt));
            Vector3 c = MapData.ToWorld(PosX, 0f, PosZ);
            Shader.SetGlobalVector("_SkeldFogCenter", new Vector4(c.x, 0f, c.z, 0f));
            float rs = Mathf.Lerp(1f, 0.5f, night);
            Shader.SetGlobalVector("_SkeldFogRange", new Vector4(FogInner * rs, FogOuter * rs, FogDark, fogAmount));
            if (sun != null) sun.intensity = Mathf.Lerp(1.7f / Mathf.PI, 0.18f / Mathf.PI, night);
            RenderSettings.ambientIntensity = Mathf.Lerp(1.25f / Mathf.PI, 0.28f / Mathf.PI, night);
        }

        // ───────────────────────── Quests ─────────────────────────
        // ยืนในรัศมี TaskRange แล้วกด F/Space = เปิดมินิเกมของภารกิจ (TaskGames.cs) · เล่นสำเร็จจึงนับว่าเสร็จ
        void UpdateTasks(float dt, bool readInput)
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                if (!t.assigned) continue;
                t.gemAngle += dt * 1.6f * Mathf.Rad2Deg;
                t.gem.localPosition = new Vector3(0f, 1.2f + Mathf.Sin(elapsed * 2f + i) * 0.12f, 0f);
                t.gem.localRotation = Quaternion.Euler(0f, t.gemAngle, 0f);
                if (!t.done) t.ring.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 3f + i) * 0.06f);
            }

            if (Games.Open || (PZ != null && !PZ.CanAct)) { PromptVisible = false; PromptAlpha = 0f; return; }

            TaskState nearest = null;
            float nd = TaskRange;

            // hysteresis: เควสที่กำลังจับอยู่ยังนับว่า "อยู่ในรัศมี" จนกว่าจะออกเกินรัศมีขาออก (กว้างกว่าขาเข้า)
            // กันแผง prompt ติด/ดับรัว ๆ เวลาวิ่งผ่านขอบรัศมี
            if (holdTask != null && !holdTask.done)
            {
                float hd = Mathf.Sqrt((holdTask.def.x - PosX) * (holdTask.def.x - PosX) + (holdTask.def.z - PosZ) * (holdTask.def.z - PosZ));
                if (hd < TaskRange + TaskRangeExit) { nearest = holdTask; nd = hd; }
            }
            if (nearest == null)
            {
                foreach (var t in Tasks)
                {
                    if (t.done || !t.assigned) continue;
                    float d = Mathf.Sqrt((t.def.x - PosX) * (t.def.x - PosX) + (t.def.z - PosZ) * (t.def.z - PosZ));
                    if (d < nd) { nd = d; nearest = t; }
                }
            }
            if (nearest != holdTask) { holdTask = nearest; holdT = 0f; }

            // แผง prompt เฟดเข้า/ออกแทนการโผล่-หายทันที (วิ่งผ่านขอบรัศมีแค่เฟรมสองเฟรมจะเห็นเป็นเงาจาง ๆ ไม่ใช่การกะพริบ)
            PromptAlpha = Mathf.MoveTowards(PromptAlpha, nearest != null ? 1f : 0f, dt * (nearest != null ? 8f : 5f));
            if (nearest == null)
            {
                // ไม่มีภารกิจใกล้ตัว: โต๊ะโรงอาหารมีปุ่มเรียกประชุมฉุกเฉิน (ใช้ได้คนละ 1 ครั้ง)
                if (PZ != null && PZ.CanCallMeeting)
                {
                    float bd = Mathf.Sqrt((PZ.ButtonPos.x - PosX) * (PZ.ButtonPos.x - PosX) + (PZ.ButtonPos.y - PosZ) * (PZ.ButtonPos.y - PosZ));
                    if (bd < 2.6f)
                    {
                        if (readInput && (GameInput.Pressed(KeyCode.F) || GameInput.Pressed(KeyCode.Space))) { PZ.HumanCallMeeting(); PromptVisible = false; PromptAlpha = 0f; return; }
                        PromptAlpha = Mathf.MoveTowards(PromptAlpha, 1f, dt * 8f);
                        PromptText = "กด F เพื่อเรียกประชุมฉุกเฉิน";
                        HoldProgress = 0f; PromptVisible = true;
                        return;
                    }
                }
                PromptVisible = PromptAlpha > 0.01f; return;
            }

            // กด F / Space ครั้งเดียว → เปิดมินิเกมของภารกิจนั้น (ต้องเล่นจริงจนสำเร็จ จึงนับว่าทำเสร็จ)
            if (readInput && (GameInput.Pressed(KeyCode.F) || GameInput.Pressed(KeyCode.Space)))
            {
                Games.Begin(nearest);
                PromptVisible = false; PromptAlpha = 0f; HoldProgress = 0f;
                return;
            }
            PromptText = $"กด F เพื่อทำเควส: {nearest.def.name}";
            HoldProgress = 0f;
            PromptVisible = true;
        }

        void OnGUI()
        {
            if (!InMenu)
            {
                hud?.Draw();
                pzHud?.OnGUI();   // UI Patient Zero (ป้ายชื่อ/บทบาท/ประชุม)
                Games?.OnGUI();   // มินิเกมภารกิจ (วาดทับ HUD)
                DrawFps();
            }
            menu?.OnGUI();       // เมนูหลัก/พักเกม/ตั้งค่า
            select?.OnGUI();     // เลือกตัวละคร (วาดทับทุกอย่าง)
        }

        void DrawFps()
        {
            if (!GameSettings.S.showFps || Event.current.type != EventType.Repaint) return;
            fpsStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            fpsStyle.normal.textColor = fps >= 55f ? new Color(0.4f, 1f, 0.55f) : fps >= 30f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(Screen.width - 150, Screen.height - 28, 140, 22), $"{fps:F0} FPS", fpsStyle);
        }

        // ───────────────────────── editor tooling ─────────────────────────
        public void DebugTeleport(float x, float z) { PosX = x; PosZ = z; }
        public void DebugSetOverview(bool on) => Overview = on;
        public Camera DebugCamera => cam;
    }
}
