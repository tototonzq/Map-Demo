using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // ───────────────────────────────────────────────────────────────────────────
    // การตั้งค่าเกมทั่วไป (หน้าจอ/ภาพ/เสียง/การเล่น) — บันทึกลง PlayerPrefs ทุกครั้งที่เปลี่ยน แล้วใช้ค่าทันที
    // ───────────────────────────────────────────────────────────────────────────
    [Serializable]
    public sealed class SettingsData
    {
        public int displayMode = 0;          // 0 เต็มจอไร้ขอบ · 1 เต็มจอพิเศษ · 2 หน้าต่าง
        public int resW = 0, resH = 0;       // 0 = ใช้ความละเอียดหน้าจอหลัก
        public bool vsync = true;
        public int fpsCap = 0;               // 0 = ไม่จำกัด
        public int quality = 3;              // 0 ต่ำ · 1 กลาง · 2 สูง · 3 สูงมาก
        public float uiScale = 1f;
        public bool camShake = true;
        public bool showFps = false;
        public bool nameTags = true;
        public bool showHelp = true;
        public float camSpeed = 1f;
        public float master = 0.9f, sfx = 1f, music = 0.6f;
        public bool mute = false;
        public string playerName = "";
    }

    [Serializable]
    public sealed class RoomSettings
    {
        public string roomName = "ห้องของฉัน";
        public int players = 8;              // 5–10 (รวมผู้เล่น)
        public int infected = 2;             // ผู้ติดเชื้อต้นตอ
        public int difficulty = 1;           // 0 ง่าย · 1 ปกติ · 2 ยาก (บอทผู้ติดเชื้อกัดถี่/ซอมบี้ไวขึ้น)
        public float dayLen = 150f, nightLen = 45f;
        public int nightZombies = 3;
        public int tasksPerPlayer = 5;
        public int humanRole = 0;            // 0 สุ่ม · 1 ผู้รอดชีวิต · 2 ผู้ติดเชื้อต้นตอ
        public bool abilities = true;        // ความสามารถของอาชีพ (หมอ/ตำรวจ)
        public string code = "";
        public string mapId = "skeld";       // แมปที่ใช้ (skeld / aurora)

        public int MaxInfected => Mathf.Clamp((players - 1) / 3, 1, 3);
        public void Clamp()
        {
            players = Mathf.Clamp(players, 5, 10);
            infected = Mathf.Clamp(infected, 1, MaxInfected);
            difficulty = Mathf.Clamp(difficulty, 0, 2);
            dayLen = Mathf.Clamp(dayLen, 60f, 300f);
            nightLen = Mathf.Clamp(nightLen, 15f, 120f);
            nightZombies = Mathf.Clamp(nightZombies, 0, 8);
            tasksPerPlayer = Mathf.Clamp(tasksPerPlayer, 2, 8);
            if (string.IsNullOrEmpty(code)) code = NewCode();
        }

        public static string NewCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sb = new System.Text.StringBuilder();
            var r = new System.Random();
            for (int i = 0; i < 5; i++) sb.Append(chars[r.Next(chars.Length)]);
            return sb.ToString();
        }
    }

    public static class GameSettings
    {
        const string Key = "PZ.Settings", RoomKey = "PZ.Room";
        public static SettingsData S = new SettingsData();
        public static RoomSettings Room = new RoomSettings();

        public static readonly string[] DisplayModes = { "เต็มจอ (ไร้ขอบ)", "เต็มจอพิเศษ", "หน้าต่าง" };
        public static readonly string[] Qualities = { "ต่ำ", "กลาง", "สูง", "สูงมาก" };
        public static readonly int[] FpsCaps = { 0, 30, 60, 90, 120, 144, 240 };

        public static bool Mute { get => S.mute; set { S.mute = value; ApplyAudio(); Save(); } }

        // ขนาด UI รวม = สเกลตามความสูงจอ × ค่าที่ผู้เล่นตั้ง
        public static float GuiScale => Mathf.Clamp(Screen.height / 900f, 0.8f, 2.5f) * Mathf.Clamp(S.uiScale, 0.7f, 1.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Load();
            ApplyDisplay();
            ApplyPerformance();
            ApplyAudio();
        }

        public static void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(Key)) S = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(Key)) ?? new SettingsData();
                if (PlayerPrefs.HasKey(RoomKey)) Room = JsonUtility.FromJson<RoomSettings>(PlayerPrefs.GetString(RoomKey)) ?? new RoomSettings();
            }
            catch { S = new SettingsData(); Room = new RoomSettings(); }
            Room.Clamp();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(S));
            PlayerPrefs.SetString(RoomKey, JsonUtility.ToJson(Room));
            PlayerPrefs.Save();
        }

        public static void ResetDefaults()
        {
            string name = S.playerName;
            S = new SettingsData { playerName = name };
            ApplyAll();
            Save();
        }

        public static void ApplyAll() { ApplyDisplay(); ApplyPerformance(); ApplyQuality(); ApplyAudio(); }

        // ───── หน้าจอ ─────
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                if (r.width < 1024 || r.height < 576) continue;
                var v = new Vector2Int(r.width, r.height);
                if (!list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) { list.Add(new Vector2Int(1280, 720)); list.Add(new Vector2Int(1920, 1080)); }
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        public static Vector2Int NativeResolution()
        {
            var d = Display.main;
            return new Vector2Int(Mathf.Max(d.systemWidth, 1280), Mathf.Max(d.systemHeight, 720));
        }

        public static void ApplyDisplay()
        {
#if !UNITY_EDITOR
            var native = NativeResolution();
            int w = S.resW > 0 ? S.resW : (S.displayMode == 2 ? Mathf.Min(1920, native.x) : native.x);
            int h = S.resH > 0 ? S.resH : (S.displayMode == 2 ? Mathf.Min(1080, native.y) : native.y);
            if (S.displayMode == 0) { w = native.x; h = native.y; }        // ไร้ขอบ = ขนาดจอจริงเสมอ
            var mode = S.displayMode == 0 ? FullScreenMode.FullScreenWindow : S.displayMode == 1 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
            if (Screen.width != w || Screen.height != h || Screen.fullScreenMode != mode) Screen.SetResolution(w, h, mode);
#endif
        }

        public static void ApplyPerformance()
        {
            QualitySettings.vSyncCount = S.vsync ? 1 : 0;
            Application.targetFrameRate = S.vsync ? -1 : (S.fpsCap > 0 ? S.fpsCap : -1);
        }

        // ───── คุณภาพภาพ ─────
        public static void ApplyQuality()
        {
            switch (Mathf.Clamp(S.quality, 0, 3))
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.HardOnly; QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowDistance = 35f; QualitySettings.shadowCascades = 2; QualitySettings.antiAliasing = 0;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable; break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 50f; QualitySettings.shadowCascades = 2; QualitySettings.antiAliasing = 2;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable; break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowDistance = 60f; QualitySettings.shadowCascades = 4; QualitySettings.antiAliasing = 4;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; break;
                default:
                    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowDistance = 70f; QualitySettings.shadowCascades = 4; QualitySettings.antiAliasing = 8;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; break;
            }
        }

        // ───── เสียง ─────
        public static void ApplyAudio() => AudioListener.volume = S.mute ? 0f : Mathf.Clamp01(S.master);
        public static float Sfx => Mathf.Clamp01(S.sfx);
        public static float Music => Mathf.Clamp01(S.music);

        public static string PlayerName => string.IsNullOrWhiteSpace(S.playerName) ? "คุณ" : S.playerName.Trim();
    }
}
