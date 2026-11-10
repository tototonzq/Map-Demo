using UnityEngine;

namespace SkeldMap
{
    // ตัวละครหนึ่งตัว ไม่ว่าจะเป็นโมเดล Kenney (ModelMascot) หรือ Crewmate ที่สร้างจากโค้ด — ห่อให้ใช้ได้เหมือนกัน
    // ผู้เล่นและหน้าเลือกตัวละคร (ตัวอย่างหมุน/รูปย่อ) ใช้คลาสนี้ร่วมกัน
    public sealed class Avatar : System.IDisposable
    {
        public readonly string Id;
        public readonly GameObject Root;

        readonly ModelMascot mascot;
        readonly Crewmate crewmate;

        Avatar(string id, ModelMascot m, Crewmate c)
        {
            Id = id; mascot = m; crewmate = c;
            Root = m != null ? m.Root : c.Root;
        }

        // หาโมเดลไม่เจอ → ใช้ Crewmate แทน (id เดิมยังคงไว้ เพื่อให้ UI จำได้ว่าผู้ใช้เลือกอะไร)
        public static Avatar Create(string id)
        {
            ModelMascot m = CharacterCatalog.IsCrewmate(id) ? null : ModelMascot.TryCreate(id);
            if (m != null) return new Avatar(id, m, null);
            return new Avatar(id, null, new Crewmate(CharacterCatalog.CrewmateColor(id)));
        }

        // Crewmate เดิมเด้งตัวตอนเดิน · โมเดลจริงมีแอนิเมชันของตัวเองจึงไม่ต้องเด้ง
        public float BobHeight(float walkT) => crewmate != null ? Mathf.Abs(Mathf.Sin(walkT)) * 0.06f : 0f;

        // moving = กำลังเดินหรือไม่ · walkT = ตัวนับจังหวะก้าวของเกม (ใช้แกว่งขา Crewmate)
        public void Animate(bool moving, float dt, float walkT)
        {
            if (crewmate != null)
            {
                float swing = moving ? Mathf.Sin(walkT) * 0.8f : 0f;
                crewmate.Legs[0].localRotation = Quaternion.Euler(swing * Mathf.Rad2Deg, 0, 0);
                crewmate.Legs[1].localRotation = Quaternion.Euler(-swing * Mathf.Rad2Deg, 0, 0);
            }
            else
            {
                mascot.Animate(moving, dt);
            }
        }

        // กระดูกแขนขวา (ให้ปืนตามมือ) · null ถ้าเป็น Crewmate หรือโมเดลไม่มีกระดูกนี้
        public Transform RightArm => mascot != null ? mascot.RightArm : null;

        // ชูแขนขวาถือปืน (เฉพาะโมเดลที่มีท่า holding-right)
        public bool SetArmed(bool on) => mascot != null && mascot.SetHolding(on);

        public void Dispose() => Dispose(false);

        // immediate = ลบทันที (ใช้กับฉากถ่ายตัวอย่างที่เรนเดอร์ในเฟรมเดียวกัน ไม่งั้นตัวเก่าจะค้างซ้อนอยู่จนจบเฟรม)
        public void Dispose(bool immediate)
        {
            mascot?.Dispose();
            if (Root == null) return;
            if (Application.isPlaying && !immediate) Object.Destroy(Root); else Object.DestroyImmediate(Root);
        }
    }
}
