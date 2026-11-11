using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SkeldMap
{
    // ผู้เล่นออนไลน์ 1 คน · เจ้าของเขียนตำแหน่งจาก SkeldGame · คนอื่นสร้าง Avatar แล้วสอดแทรกตำแหน่งตาม
    public sealed class NetPlayer : NetworkBehaviour
    {
        readonly NetworkVariable<float> posX = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> posZ = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<float> facing = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<int> characterId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        static SkeldGame game;
        Avatar av;
        int builtId = -1;
        float cx, cz, cf, walkT;
        bool placed, moving;
        GUIStyle style;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                var g = Game();
                if (g != null) { posX.Value = g.PosX; posZ.Value = g.PosZ; facing.Value = g.Facing; }
                characterId.Value = Mathf.Max(0, CharacterCatalog.IndexOf(g != null ? g.CharacterId : ""));
                playerName.Value = new FixedString32Bytes(Trunc(GameSettings.PlayerName));
            }
            else
            {
                characterId.OnValueChanged += OnCharChanged;
                Build(characterId.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            characterId.OnValueChanged -= OnCharChanged;
            DestroyAvatar();
        }

        public override void OnDestroy()
        {
            DestroyAvatar();
            base.OnDestroy();
        }

        static string Trunc(string s) => s.Length > 12 ? s.Substring(0, 12) : s;

        static SkeldGame Game()
        {
            if (game == null) game = FindFirstObjectByType<SkeldGame>();
            return game;
        }

        void OnCharChanged(int prev, int now) => Build(now);

        void Build(int idx)
        {
            if (idx == builtId && av != null) return;
            DestroyAvatar();
            var all = CharacterCatalog.All;
            string id = all.Count > 0 ? all[Mathf.Clamp(idx, 0, all.Count - 1)].id : "";
            av = Avatar.Create(id);
            var g = Game();
            if (g != null) av.Root.transform.SetParent(g.transform, false);
            builtId = idx;
            placed = false;
        }

        void DestroyAvatar()
        {
            av?.Dispose();
            av = null; builtId = -1;
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner)
            {
                var g = Game();
                if (g == null) return;
                posX.Value = g.PosX; posZ.Value = g.PosZ; facing.Value = g.Facing;
                return;
            }
            if (av == null) return;
            var gm = Game();
            bool vis = gm == null || !gm.InMenu;   // ซ่อนตอนอยู่หน้าเมนูหลัก (ฉากหมุนเป็นฉากหลัง)
            if (av.Root.activeSelf != vis) av.Root.SetActive(vis);
            float dt = Time.deltaTime;
            float tx = posX.Value, tz = posZ.Value;
            if (!placed) { cx = tx; cz = tz; cf = facing.Value; placed = true; }
            float k = Mathf.Clamp01(10f * dt);
            float ox = cx, oz = cz;
            cx = Mathf.Lerp(cx, tx, k); cz = Mathf.Lerp(cz, tz, k);
            cf = Mathf.LerpAngle(cf * Mathf.Rad2Deg, facing.Value * Mathf.Rad2Deg, k) * Mathf.Deg2Rad;
            float speed = dt > 0f ? Mathf.Sqrt((cx - ox) * (cx - ox) + (cz - oz) * (cz - oz)) / dt : 0f;
            moving = speed > 0.4f;
            walkT = moving ? walkT + dt * 12f : 0f;

            var t = av.Root.transform;
            t.position = MapData.ToWorld(cx, av.BobHeight(walkT), cz);
            t.rotation = Quaternion.Euler(0f, (Mathf.PI - cf) * Mathf.Rad2Deg, 0f);
            av.Animate(moving, dt, walkT);
        }

        void OnGUI()
        {
            if (!IsSpawned || IsOwner || av == null || Event.current.type != EventType.Repaint) return;
            var g = Game();
            if (g == null || g.InMenu || g.Overview || !GameSettings.S.nameTags) return;
            var cam = g.DebugCamera;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(MapData.ToWorld(cx, 2.1f, cz));
            if (sp.z <= 0f) return;
            float s = GameSettings.GuiScale;
            float gx = sp.x / s, gy = (Screen.height - sp.y) / s;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            var bg = new Rect(gx - 62, gy - 24, 124, 22);
            TaskGames.Fill(bg, new Color(0, 0, 0, 0.45f), 8f);
            style.normal.textColor = Color.white;
            GUI.Label(bg, playerName.Value.ToString(), style);
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
