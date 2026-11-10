using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // ตัวละครจากโมเดล 3D (Kenney Mini Characters, CC0) — โหลดจาก Assets/Resources/Characters/<ชื่อ>.fbx
    // ใช้แอนิเมชัน idle ตอนยืน และ sprint ตอนเดิน ผสมกันด้วย PlayableGraph (ไม่ต้องสร้าง Animator Controller)
    // วัสดุถูกสลับเป็น Skeld/Lit เพื่อให้ได้เงาและหมอกจากระยะตัวละครเหมือนฉากส่วนอื่น
    public sealed class ModelMascot : System.IDisposable
    {
        // โมเดลสูง ~0.7 หน่วย · Crewmate เดิมสูง ~1.2 ⇒ ขยายให้ขนาดบนจอใกล้เคียงเดิม
        public const float Scale = 1.7f;

        // วงจร sprint = 0.5 วินาที ส่วนจังหวะเสียงฝีเท้าของเกมคือ 2π/12 ≈ 0.524 วินาที/วงจร → ปรับความเร็วเล่นให้ตรงกัน
        const float MoveClipRate = 0.5f / 0.524f;

        public readonly GameObject Root;

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idle, move, hold;
        AnimationLayerMixerPlayable layers;
        float idleLen = 1f, moveLen = 1f, holdLen = 1f, idleT, moveT, holdT, blend, holdBlend;
        bool hasAnim, hasHold, holding;

        // กระดูกแขนขวา (ไว้ให้ปืนตามมือ) · null ถ้าโมเดลไม่มี
        public Transform RightArm { get; private set; }

        public static ModelMascot TryCreate(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            string path = "Characters/" + modelName.Trim();
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[Skeld] ไม่พบโมเดล Resources/{path}.fbx — ใช้ Crewmate เดิมแทน");
                return null;
            }

            AnimationClip idleClip = null, moveClip = null, holdClip = null;
            foreach (var c in Resources.LoadAll<AnimationClip>(path))
            {
                if (c.name == "idle") idleClip = c;
                else if (c.name == "sprint") moveClip = c;
                else if (c.name == "holding-right") holdClip = c;   // ท่าชูแขนขวาถือของ (ใช้ซ้อนบนท่าเดินเฉพาะแขนขวา)
            }
            return new ModelMascot(prefab, idleClip, moveClip, holdClip);
        }

        ModelMascot(GameObject prefab, AnimationClip idleClip, AnimationClip moveClip, AnimationClip holdClip)
        {
            Root = new GameObject("Mascot");
            var inst = Object.Instantiate(prefab, Root.transform);
            inst.name = prefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one * Scale;

            var fallbackTex = Resources.Load<Texture2D>("Characters/Textures/colormap");
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    Texture tex = src[i] != null ? src[i].mainTexture : null;
                    dst[i] = Mats.Lit(0xffffff, 0.9f, 0f, 0, 1f, tex != null ? tex : fallbackTex);
                }
                r.sharedMaterials = dst;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            foreach (var t in inst.GetComponentsInChildren<Transform>())
                if (t.name == "arm-right") { RightArm = t; break; }

            if (idleClip == null || moveClip == null)
            {
                Debug.LogWarning("[Skeld] โมเดลนี้ไม่มีแอนิเมชัน idle/sprint — แสดงเป็นท่าค้าง");
                return;
            }

            var animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            graph = PlayableGraph.Create("Mascot");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "out", animator);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            idle = AnimationClipPlayable.Create(graph, idleClip);
            move = AnimationClipPlayable.Create(graph, moveClip);
            graph.Connect(idle, 0, mixer, 0);
            graph.Connect(move, 0, mixer, 1);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            if (holdClip != null && RightArm != null)
            {
                // เลเยอร์ที่ 2 = ท่าถือปืน ใช้เฉพาะกระดูกแขนขวา (ขาและลำตัวยังเดิน/ยืนตามปกติ)
                var mask = new AvatarMask();
                var all = inst.GetComponentsInChildren<Transform>();
                mask.transformCount = all.Length;
                for (int i = 0; i < all.Length; i++)
                {
                    mask.SetTransformPath(i, PathFrom(inst.transform, all[i]));
                    mask.SetTransformActive(i, all[i] == RightArm || all[i].IsChildOf(RightArm));
                }
                hold = AnimationClipPlayable.Create(graph, holdClip);
                holdLen = Mathf.Max(holdClip.length, 0.01f);
                layers = AnimationLayerMixerPlayable.Create(graph, 2);
                graph.Connect(mixer, 0, layers, 0);
                graph.Connect(hold, 0, layers, 1);
                layers.SetInputWeight(0, 1f);
                layers.SetInputWeight(1, 0f);
                layers.SetLayerMaskFromAvatarMask(1, mask);
                output.SetSourcePlayable(layers);
                hasHold = true;
            }
            else output.SetSourcePlayable(mixer);
            idleLen = Mathf.Max(idleClip.length, 0.01f);
            moveLen = Mathf.Max(moveClip.length, 0.01f);
            graph.Play();
            hasAnim = true;
            Animate(false, 0f);
        }

        // เรียกทุกเฟรม — moving = กำลังเดินอยู่หรือไม่ (ผสม idle ↔ sprint แบบนุ่ม ๆ)
        public void Animate(bool moving, float dt)
        {
            if (!hasAnim || !graph.IsValid()) return;
            blend += ((moving ? 1f : 0f) - blend) * (1f - Mathf.Exp(-12f * dt));
            idleT += dt;
            moveT += dt * MoveClipRate;
            idle.SetTime(idleT % idleLen); // คลิปจาก Kenney ไม่ได้ติ๊ก loop → วนเวลาเอง
            move.SetTime(moveT % moveLen);
            mixer.SetInputWeight(0, 1f - blend);
            mixer.SetInputWeight(1, blend);
            if (hasHold)
            {
                holdBlend += ((holding ? 1f : 0f) - holdBlend) * (1f - Mathf.Exp(-14f * dt));
                holdT += dt;
                hold.SetTime(holdT % holdLen);
                layers.SetInputWeight(1, holdBlend);
            }
            graph.Evaluate(0f);
        }

        // ชูแขนขวาถือของ (ปืน) หรือปล่อยแขนตามท่าปกติ — คืน true ถ้าโมเดลมีท่านี้
        public bool SetHolding(bool on) { holding = on; return hasHold; }

        static string PathFrom(Transform top, Transform t)
        {
            if (t == top) return "";
            string p = t.name;
            for (var c = t.parent; c != null && c != top; c = c.parent) p = c.name + "/" + p;
            return p;
        }

        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
