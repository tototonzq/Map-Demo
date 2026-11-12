using UnityEngine;

namespace SkeldMap
{
    // นิยามแมป 1 แผนที่ (ขนาดกริดคงที่ 96×64 ใน MapData)
    public sealed class MapDef
    {
        public string id, name;
        public RoomDef[] Rooms;
        public int[][] Corridors;
        public Vector2 Spawn;
        public TaskDef[] Tasks;
        public Vector2[] Vents;
        public PropDef[] Props;
        public Vector2 ButtonPos;            // ปุ่มเรียกประชุม (ต้องตรงกับโต๊ะ button:true)
        public string MeetingRoomId;         // ห้องประชุม/รวมตัว
        public System.Func<string, int> KitVariant;   // พาเลตต์ผนัง/พื้น: 0 ส้ม · 1 ฟ้า · 2 เขียว

        public static PropDef P(string kind, float x, float z, float? r = null, float? w = null, float? d = null,
                                float? h = null, uint? color = null, uint? screen = null, uint? glow = null,
                                bool button = false, float? rot = null, float? sx = null, float? sz = null)
        {
            return new PropDef
            {
                kind = kind, x = x, z = z, r = r, w = w, d = d, h = h, rot = rot, sx = sx, sz = sz,
                color = color, screen = screen, glow = glow, button = button,
            };
        }
    }
}
