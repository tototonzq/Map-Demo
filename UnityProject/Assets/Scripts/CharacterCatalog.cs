using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // รายการตัวละครทั้งหมดที่เลือกได้ในหน้าเลือกตัวละคร
    //  • โมเดล Kenney Mini Characters 20 ตัว  → id = ชื่อไฟล์ใน Resources/Characters (เช่น "character-alien")
    //  • Crewmate ที่สร้างจากโค้ด 9 สี         → id = "crewmate-<สี>"
    public static class CharacterCatalog
    {
        public sealed class Entry
        {
            public readonly string id, name;
            public Entry(string id, string name) { this.id = id; this.name = name; }
        }

        // ชื่อสี Crewmate เรียงตรงกับ Crewmate.Palette
        static readonly string[] CrewColors = { "red", "blue", "green", "pink", "orange", "yellow", "black", "purple", "cyan" };

        public static readonly IReadOnlyList<Entry> All = Build();

        static List<Entry> Build()
        {
            var list = new List<Entry>
            {
                new Entry("character-alien", "Alien"),
                new Entry("character-robot", "Robot"),
                new Entry("character-knight", "Knight"),
                new Entry("character-zombie", "Zombie"),
                new Entry("character-soldier", "Soldier"),
                new Entry("character-raider", "Raider"),
                new Entry("character-worker", "Worker"),
            };
            foreach (char c in "abcdefg") list.Add(new Entry("character-male-" + c, "Male " + char.ToUpper(c)));
            foreach (char c in "abcdef") list.Add(new Entry("character-female-" + c, "Female " + char.ToUpper(c)));
            foreach (var col in CrewColors) list.Add(new Entry("crewmate-" + col, "Crew " + char.ToUpper(col[0]) + col.Substring(1)));
            return list;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].id == id) return i;
            return -1;
        }

        public static bool IsCrewmate(string id) => id != null && id.StartsWith("crewmate");

        // id ของ Crewmate → สีจาก Crewmate.Palette ("crewmate" เฉย ๆ = สุ่มสี)
        public static uint CrewmateColor(string id)
        {
            if (id != null && id.StartsWith("crewmate-"))
            {
                int i = System.Array.IndexOf(CrewColors, id.Substring("crewmate-".Length));
                if (i >= 0) return Crewmate.Palette[i];
            }
            return Crewmate.Palette[Random.Range(0, Crewmate.Palette.Length)];
        }

        // ค่าที่ผู้ใช้ตั้งไว้ใน Inspector ("random" / "crewmate" / ชื่อโมเดลตรง ๆ) → id ที่ใช้งานจริง
        public static string Resolve(string requested)
        {
            requested = (requested ?? "").Trim();
            if (requested == "random") return All[Random.Range(0, All.Count)].id;
            if (requested == "crewmate") return "crewmate-" + CrewColors[Random.Range(0, CrewColors.Length)];
            return requested == "" ? All[0].id : requested;
        }
    }
}
