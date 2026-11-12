using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SkeldMap.EditorTools
{
    // ตรวจความถูกต้องของแมปทุกแมป: Unity -batchmode -executeMethod SkeldMap.EditorTools.MapValidator.Run
    public static class MapValidator
    {
        static readonly HashSet<string> TaskIds = new HashSet<string>
        {
            "alignUpper", "alignLower", "scan", "download", "comms", "upload", "asteroids", "reactor", "swipe", "filter", "course", "wiring", "fuel", "shields",
        };

        // ห้องที่ตั้งใจให้ติดกัน (ไม่มีกำแพงคั่น)
        static readonly HashSet<string> AllowedTouch = new HashSet<string> { "aurora:admin|observatory", "aurora:admin|science" };

        static int errors;

        static void Err(string m) { errors++; Debug.LogError("[MapValidator] FAIL " + m); }
        static void Info(string m) { Debug.Log("[MapValidator] " + m); }

        [MenuItem("Skeld/Validate Maps")]
        public static void Run()
        {
            errors = 0;
            string prev = MapData.CurrentId;
            foreach (var id in MapData.MapIds)
            {
                MapData.Select(id);
                Validate(MapData.Current);
            }
            MapData.Select(prev);
            Info(errors == 0 ? "ALL MAPS PASSED" : "FAILED with " + errors + " error(s)");
            if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
        }

        static void Validate(MapDef m)
        {
            Info($"=== {m.id} ({m.name}) : {m.Rooms.Length} rooms, {m.Corridors.Length} corridors, {m.Tasks.Length} tasks, {m.Props.Length} props, {m.Vents.Length} vents ===");
            int W = MapData.GridW, H = MapData.GridH;

            // 1) ขอบเขต + การซ้อนทับ/ติดกันของห้อง
            var ids = new HashSet<string>();
            foreach (var r in m.Rooms)
            {
                if (!ids.Add(r.id)) Err($"{m.id}: room id ซ้ำ {r.id}");
                if (r.x < 0 || r.z < 0 || r.x + r.w > W || r.z + r.d > H) Err($"{m.id}: ห้อง {r.id} เกินกริด {W}x{H}");
            }
            foreach (var c in m.Corridors)
                if (c[0] < 0 || c[1] < 0 || c[0] + c[2] > W || c[1] + c[3] > H) Err($"{m.id}: ทางเดิน {{{c[0]},{c[1]},{c[2]},{c[3]}}} เกินกริด");
            for (int i = 0; i < m.Rooms.Length; i++)
                for (int j = i + 1; j < m.Rooms.Length; j++)
                {
                    var a = m.Rooms[i]; var b = m.Rooms[j];
                    int ox = Mathf.Min(a.x + a.w, b.x + b.w) - Mathf.Max(a.x, b.x);
                    int oz = Mathf.Min(a.z + a.d, b.z + b.d) - Mathf.Max(a.z, b.z);
                    if (ox > 0 && oz > 0) Err($"{m.id}: ห้องซ้อนทับ {a.id} / {b.id}");
                    else if ((ox == 0 && oz > 0) || (oz == 0 && ox > 0))
                    {
                        string key = m.id + ":" + (string.CompareOrdinal(a.id, b.id) < 0 ? a.id + "|" + b.id : b.id + "|" + a.id);
                        bool ok = AllowedTouch.Contains(key);
                        Info($"{(ok ? "touch (อนุญาต)" : "WARNING ห้องติดกัน")}: {a.id} / {b.id} (ยาว {Mathf.Max(ox, oz)})");
                    }
                }

            // 2) กริด + flood fill จากจุดเกิด
            var grid = new MapGrid();
            int sx = Mathf.FloorToInt(m.Spawn.x), sz = Mathf.FloorToInt(m.Spawn.y);
            if (!grid.IsFloor(sx, sz) || grid.IsSolid(sx, sz)) Err($"{m.id}: จุดเกิด ({sx},{sz}) ไม่ใช่พื้นที่เดินได้");
            var seen = new bool[W * H];
            var q = new Queue<int>();
            if (grid.IsFloor(sx, sz) && !grid.IsSolid(sx, sz)) { seen[sz * W + sx] = true; q.Enqueue(sz * W + sx); }
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (q.Count > 0)
            {
                int cur = q.Dequeue(); int cx = cur % W, cz = cur / W;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], nz = cz + dz[k];
                    if (nx < 0 || nz < 0 || nx >= W || nz >= H) continue;
                    int ni = nz * W + nx;
                    if (seen[ni] || grid.IsBlocked(nx, nz)) continue;
                    seen[ni] = true; q.Enqueue(ni);
                }
            }
            foreach (var r in m.Rooms)
            {
                int n = 0;
                for (int z = r.z; z < r.z + r.d; z++) for (int x = r.x; x < r.x + r.w; x++) if (seen[z * W + x]) n++;
                if (n == 0) Err($"{m.id}: ห้อง {r.id} เข้าถึงไม่ได้จากจุดเกิด");
                else Info($"  room {r.id}: reachable cells {n}/{r.w * r.d}");
            }
            // ช่องพื้นที่ไม่ตันแต่เข้าไม่ถึง (ถ้ามีมาก = ของบังทางเดิน)
            var stranded = new List<string>();
            for (int z = 0; z < H; z++) for (int x = 0; x < W; x++)
                    if (grid.IsFloor(x, z) && !grid.IsSolid(x, z) && !seen[z * W + x]) stranded.Add($"({x},{z})");
            if (stranded.Count > 0) Info($"WARNING {m.id}: พื้นที่ไม่ตันแต่เข้าไม่ถึง {stranded.Count} ช่อง: {string.Join(" ", stranded.GetRange(0, Mathf.Min(40, stranded.Count)))}");

            // 3) ภารกิจ
            if (m.Tasks.Length < 8) Err($"{m.id}: ภารกิจน้อยกว่า 8 ({m.Tasks.Length})");
            foreach (var t in m.Tasks)
            {
                if (!TaskIds.Contains(t.id)) Err($"{m.id}: task id ไม่รู้จัก {t.id}");
                if (!ids.Contains(t.room)) Err($"{m.id}: task {t.id} อ้างห้อง {t.room} ที่ไม่มี");
                int tx = Mathf.FloorToInt(t.x), tz = Mathf.FloorToInt(t.z);
                if (!grid.IsFloor(tx, tz)) Err($"{m.id}: task {t.id} ({t.x},{t.z}) ไม่อยู่บนพื้น");
                else if (grid.IsSolid(tx, tz)) Err($"{m.id}: task {t.id} ({t.x},{t.z}) อยู่บนช่องตัน");
                else if (!seen[tz * W + tx]) Err($"{m.id}: task {t.id} ({t.x},{t.z}) เข้าไม่ถึง");
                var ra = MapData.RoomAt(t.x, t.z);
                if (ra == null || ra.id != t.room) Err($"{m.id}: task {t.id} ไม่ได้อยู่ในห้อง {t.room}");
            }

            // 4) ปุ่มประชุม
            var mr = System.Array.Find(m.Rooms, r => r.id == m.MeetingRoomId);
            if (mr == null) Err($"{m.id}: MeetingRoomId {m.MeetingRoomId} ไม่มีในห้อง");
            else
            {
                var ra = MapData.RoomAt(m.ButtonPos.x, m.ButtonPos.y);
                if (ra == null || ra.id != mr.id) Err($"{m.id}: ButtonPos ไม่อยู่ในห้องประชุม");
            }
            bool tableOk = false;
            foreach (var p in m.Props)
                if (p.kind == "table" && p.button && Vector2.Distance(new Vector2(p.x, p.z), m.ButtonPos) <= 2f) tableOk = true;
            if (!tableOk) Err($"{m.id}: ไม่มี table button:true ภายใน 2.0 จาก ButtonPos");

            // 5) prop ต้องมีพารามิเตอร์ครบ + อยู่บนพื้น
            var needR = new HashSet<string> { "engine", "reactor", "table", "tank", "dish", "turret", "barrel", "holotable", "cablereel", "antenna", "shieldgen", "holoemitter", "ammotower" };
            foreach (var p in m.Props)
            {
                if (needR.Contains(p.kind) && !p.r.HasValue) Err($"{m.id}: prop {p.kind} ({p.x},{p.z}) ต้องมี r");
                if ((p.kind == "crate" || p.kind == "console") && (!p.w.HasValue || !p.d.HasValue)) Err($"{m.id}: prop {p.kind} ({p.x},{p.z}) ต้องมี w/d");
                if (!grid.IsFloor(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)) && p.kind != "pipe" && p.kind != "lightstrip")
                    Err($"{m.id}: prop {p.kind} ({p.x},{p.z}) อยู่นอกพื้น");
            }
            foreach (var v in m.Vents)
                if (!grid.IsFloor(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y))) Err($"{m.id}: vent ({v.x},{v.y}) อยู่นอกพื้น");

            Dump(m, grid, seen);
            Info($"=== {m.id}: {(errors == 0 ? "OK" : "มีข้อผิดพลาดสะสม " + errors)} ===");
        }

        // ASCII map: # = ตัน, . = เดินได้, ' ' = ว่าง, T = task, S = spawn, B = ปุ่ม, ? = เข้าไม่ถึง
        static void Dump(MapDef m, MapGrid grid, bool[] seen)
        {
            try
            {
                var sb = new StringBuilder();
                int W = MapData.GridW, H = MapData.GridH;
                for (int z = 0; z < H; z++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        char c = !grid.IsFloor(x, z) ? ' ' : grid.IsSolid(x, z) ? '#' : seen[z * W + x] ? '.' : '?';
                        foreach (var t in m.Tasks) if (Mathf.FloorToInt(t.x) == x && Mathf.FloorToInt(t.z) == z) c = 'T';
                        if (Mathf.FloorToInt(m.Spawn.x) == x && Mathf.FloorToInt(m.Spawn.y) == z) c = 'S';
                        if (Mathf.FloorToInt(m.ButtonPos.x) == x && Mathf.FloorToInt(m.ButtonPos.y) == z) c = 'B';
                        sb.Append(c);
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "MapDump_" + m.id + ".txt"), sb.ToString());
            }
            catch (System.Exception ex) { Debug.LogWarning("[MapValidator] dump failed: " + ex.Message); }
        }
    }
}
