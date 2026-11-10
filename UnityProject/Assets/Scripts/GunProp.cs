using UnityEngine;

namespace SkeldMap
{
    // ปืนพกของตำรวจ (โมเดลโพรซีเดอร์) — ถืออยู่ในมือขวา: ปืนตามกระดูกแขนขวาของโมเดล (ชูแขนด้วยท่า holding-right)
    // ไม่มีกระดูกแขน (Crewmate) → วางข้างลำตัวด้านขวาเหมือนเดิม
    // ลูกที่ BiteFx ใช้: "Muzzle" (จุดปลายกระบอก) และ "Flash" (แฟลชตอนยิง ปิดไว้ตามปกติ)
    public static class GunProp
    {
        // ตำแหน่งปืนในพิกัดของกระดูกแขนขวา (หน่วยโลก ไม่ผ่านสเกลของโมเดล) · มุม = เทียบทิศที่ตัวละครหัน
        public static Vector3 HandOffset = new Vector3(0.278f, 0.0f, 0.16f);
        public static Vector3 HandEuler = new Vector3(-2f, 0f, 0f);

        public static Transform Attach(Avatar av)
        {
            if (av == null) return null;
            var root = av.Root.transform;
            var old = root.Find("Gun");
            if (old != null) { if (Application.isPlaying) Object.Destroy(old.gameObject); else Object.DestroyImmediate(old.gameObject); }

            var g = new GameObject("Gun").transform;
            g.SetParent(root, false);
            var arm = av.RightArm;
            if (arm != null && av.SetArmed(true))
            {
                var f = g.gameObject.AddComponent<GunFollow>();
                f.arm = arm;
            }
            else
            {
                g.localPosition = new Vector3(0.27f, 0.62f, 0.24f);
                g.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            }

            var dark = PropFactory.Std(0x232837, 0.35f, 0.7f);
            var steel = PropFactory.Std(0x8d97ae, 0.3f, 0.8f);
            var grip = PropFactory.Std(0x3a2a22, 0.7f, 0.1f);

            PropFactory.Part(g, "slide", MeshBuilder.Box(0.075f, 0.085f, 0.36f), dark, new Vector3(0f, 0.05f, 0.14f), false);
            PropFactory.Part(g, "barrel", MeshBuilder.Box(0.045f, 0.045f, 0.1f), steel, new Vector3(0f, 0.045f, 0.36f), false);
            PropFactory.Part(g, "frame", MeshBuilder.Box(0.07f, 0.05f, 0.26f), steel, new Vector3(0f, 0.0f, 0.1f), false);
            PropFactory.Part(g, "grip", MeshBuilder.Box(0.065f, 0.16f, 0.075f), grip, new Vector3(0f, -0.08f, -0.02f), false, Quaternion.Euler(-14f, 0f, 0f));
            PropFactory.Part(g, "guard", MeshBuilder.Box(0.03f, 0.03f, 0.09f), dark, new Vector3(0f, -0.035f, 0.13f), false);
            PropFactory.Part(g, "sight", MeshBuilder.Box(0.02f, 0.02f, 0.02f), steel, new Vector3(0f, 0.105f, 0.3f), false);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(g, false);
            muzzle.localPosition = new Vector3(0f, 0.045f, 0.44f);

            var flash = PropFactory.Part(g, "Flash", MeshBuilder.Sphere(0.11f, 10, 8), Mats.Unlit(new Color(1f, 0.85f, 0.35f), fog: false), new Vector3(0f, 0.045f, 0.5f), false);
            flash.transform.localScale = new Vector3(1f, 1f, 1.6f);
            flash.SetActive(false);
            return g;
        }
    }

    // ทุกเฟรมหลังแอนิเมชันอัปเดต: พาปืนไปอยู่ที่มือขวา (ปรับ HandOffset/HandEuler ที่ GunProp)
    public sealed class GunFollow : MonoBehaviour
    {
        public Transform arm;
        void LateUpdate() => Follow();
        public void Follow()
        {
            if (arm == null) return;
            transform.position = arm.TransformPoint(GunProp.HandOffset / Mathf.Max(arm.lossyScale.x, 0.0001f));
            // ตำแหน่งตามมือ แต่ปากกระบอกหันตามทิศที่ตัวละครหันเสมอ (ตอนวิ่งลำตัวโน้มไปข้างหน้า ปืนจะได้ไม่ก้มตาม)
            transform.rotation = transform.parent.rotation * Quaternion.Euler(GunProp.HandEuler);
        }
    }
}
