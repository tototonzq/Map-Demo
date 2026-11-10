using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // แอนิเมชัน/เอฟเฟกต์ตอนกัด (โพรซีเดอร์ ไม่ต้องมีคลิปแอนิเมชัน):
    //   ผู้กัด  = พุ่งเข้าหาเหยื่อ + โน้มตัว + เด้งตัวแบบงับ แล้วถอยกลับ
    //   ผู้ถูกกัด = สะดุ้ง กระเด็นถอย + สั่น + ตัวยุบ  (ช่วง 0.22 วินาทีหลังเริ่มพุ่ง = จังหวะงับ)
    //   จังหวะงับ: หยดเลือดกระเด็น + วงแหวนกระแทกที่พื้น + ฟองคำพูด + (ถ้าเป็นผู้เล่น) กล้องกระตุก/จอวาบแดง
    // ค่าที่ได้เขียนลง Actor.fx* แล้ว SkeldGame / PZ.SyncAvatars นำไปใช้กับ transform ของตัวละคร
    public sealed class BiteFx
    {
        const float Total = 0.95f, LungeLen = 0.5f, ChompAt = 0.22f;

        sealed class Anim
        {
            public Actor biter, victim;
            public float t;
            public Vector2 dir;      // ทิศจากผู้กัดไปเหยื่อ (กริด)
            public float reach;      // ระยะพุ่ง
            public bool burst;
            public bool shot;        // true = โดนยิง (ผู้ยิงสะท้อนแรงถอย ไม่พุ่ง)
            public float chompAt = ChompAt;
        }

        sealed class Drop
        {
            public Transform t;
            public Vector3 vel;
            public float life, max, size;
        }

        readonly SkeldGame g;
        readonly List<Anim> anims = new List<Anim>();
        readonly List<Drop> drops = new List<Drop>();
        readonly List<Drop> pool = new List<Drop>();
        readonly List<KeyValuePair<Transform, float>> rings = new List<KeyValuePair<Transform, float>>();
        Mesh dropMesh, ringMesh, tracerMesh;
        Material dropMat, ringMat, tracerMat;
        Transform root;

        public BiteFx(SkeldGame game)
        {
            g = game;
            root = new GameObject("BiteFx").transform;
            root.SetParent(g.transform, false);
        }

        public void Play(Actor biter, Actor victim)
        {
            var d = new Vector2(victim.x - biter.x, victim.z - biter.z);
            float dist = d.magnitude;
            d = dist > 0.001f ? d / dist : Vector2.up;
            // หันหน้าเข้าหากันทันที
            biter.facing = Mathf.Atan2(d.x, d.y);
            if (biter.human) g.Facing = biter.facing;
            biter.bubble = "กัด!"; biter.bubbleT = 0.9f;
            anims.Add(new Anim { biter = biter, victim = victim, dir = d, reach = Mathf.Clamp(dist - 0.5f, 0.15f, 0.8f) });
        }

        public void Update(float dt, List<Actor> actors)
        {
            // รีเซ็ตค่าท่าทางก่อนรวมจากทุกแอนิเมชัน
            foreach (var a in actors) { a.fxOff = Vector2.zero; a.fxScaleY = 1f; a.fxPitch = 0f; a.fxRoll = 0f; }

            for (int i = anims.Count - 1; i >= 0; i--)
            {
                var an = anims[i];
                an.t += dt;
                var b = an.biter; var v = an.victim;

                if (an.shot)
                {
                    // ผู้ยิง: แรงถอยสะท้อนแล้วค่อยกลับ
                    float rc = Mathf.Exp(-an.t * 9f);
                    b.fxOff -= an.dir * 0.16f * rc;
                    b.fxPitch -= 9f * rc;
                }
                else
                {
                    // ผู้กัด: พุ่ง → งับ → ถอย
                    float k = Mathf.Clamp01(an.t / LungeLen);
                    float lunge = Mathf.Sin(Mathf.Pow(k, 0.7f) * Mathf.PI);
                    b.fxOff += an.dir * an.reach * lunge;
                    b.fxPitch += 30f * lunge;
                    b.fxScaleY *= 1f + 0.12f * Mathf.Sin(k * Mathf.PI * 5f) * lunge;       // งับปากรัว ๆ
                }

                // จังหวะงับ → เอฟเฟกต์
                if (!an.burst && an.t >= an.chompAt)
                {
                    an.burst = true;
                    Burst(b, v, an.dir);
                }

                // ผู้ถูกกัด: สะดุ้งถอย
                if (an.burst)
                {
                    float kv = Mathf.Clamp01((an.t - an.chompAt) / 0.6f);
                    float knock = Mathf.Sin(kv * Mathf.PI * 0.9f);
                    v.fxOff += an.dir * 0.42f * knock;
                    v.fxRoll += 16f * Mathf.Sin(kv * Mathf.PI * 6f) * (1f - kv);
                    v.fxPitch -= 12f * knock;
                    v.fxScaleY *= 1f - 0.16f * Mathf.Sin(Mathf.Min(1f, kv * 2f) * Mathf.PI);
                }

                if (an.t >= Total) anims.RemoveAt(i);
            }

            UpdateDrops(dt);
            UpdateRings(dt);
            UpdateTemps(dt);
        }

        // ───── ยิง ─────
        sealed class Temp { public GameObject go; public float life; public System.Action done; }
        readonly List<Temp> temps = new List<Temp>();

        // เรียกฟังก์ชันหลังหน่วงเวลา (เช่น ลบตัวละครที่ถูกยิงหลังเห็นเอฟเฟกต์)
        public void Later(float delay, System.Action act) => temps.Add(new Temp { life = delay, done = act });

        void UpdateTemps(float dt)
        {
            for (int i = temps.Count - 1; i >= 0; i--)
            {
                temps[i].life -= dt;
                if (temps[i].life > 0f) continue;
                if (temps[i].go != null) Object.Destroy(temps[i].go);
                temps[i].done?.Invoke();
                temps.RemoveAt(i);
            }
        }

        Transform GunOf(Actor a)
        {
            Transform r = a.human ? g.PlayerRoot : (a.av != null ? a.av.Root.transform : null);
            return r != null ? r.Find("Gun") : null;
        }

        public void Shoot(Actor shooter, Actor target)
        {
            EnsureAssets();
            var d = new Vector2(target.x - shooter.x, target.z - shooter.z);
            float dist = d.magnitude;
            d = dist > 0.001f ? d / dist : Vector2.up;
            shooter.facing = Mathf.Atan2(d.x, d.y);
            if (shooter.human) g.Facing = shooter.facing;
            shooter.bubble = "ปัง!"; shooter.bubbleT = 0.8f;

            // แฟลชปากกระบอก + เส้นกระสุน
            var gun = GunOf(shooter);
            Vector3 from = gun != null && gun.Find("Muzzle") != null ? gun.Find("Muzzle").position : MapData.ToWorld(shooter.x, 0.8f, shooter.z);
            Vector3 to = MapData.ToWorld(target.x, 0.85f, target.z);
            if (gun != null && gun.Find("Flash") != null)
            {
                var fl = gun.Find("Flash").gameObject;
                fl.SetActive(true);
                temps.Add(new Temp { life = 0.07f, done = () => { if (fl != null) fl.SetActive(false); } });
            }
            var tr = new GameObject("Tracer");
            tr.transform.SetParent(root, false);
            Vector3 mid = (from + to) * 0.5f;
            tr.transform.position = mid;
            tr.transform.rotation = Quaternion.LookRotation((to - from).normalized);
            tr.transform.localScale = new Vector3(0.035f, 0.035f, (to - from).magnitude);
            tr.AddComponent<MeshFilter>().sharedMesh = tracerMesh;
            var mr = tr.AddComponent<MeshRenderer>();
            mr.sharedMaterial = tracerMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            temps.Add(new Temp { go = tr, life = 0.07f });

            // เสียง (ดังตามระยะจากผู้เล่น) + กล้องกระตุกถ้าผู้เล่นเป็นคนยิง
            float dd = g.PZ != null ? PZ.Dist(shooter, g.PZ.me) : 0f;
            g.Gunshot(shooter.human ? 1f : Mathf.Clamp01(1f - dd / 28f) * 0.9f + 0.1f);
            if (shooter.human) g.Jolt = 0.12f;

            anims.Add(new Anim { biter = shooter, victim = target, dir = d, reach = 0f, shot = true, chompAt = 0.06f });
        }


        // ───── หยดเลือด + วงแหวน ─────
        void Burst(Actor biter, Actor victim, Vector2 dir)
        {
            EnsureAssets();
            Vector3 c = MapData.ToWorld(victim.x + victim.fxOff.x, 1.0f, victim.z + victim.fxOff.y);
            Vector3 outDir = new Vector3(dir.x, 0f, -dir.y);          // ทิศกริด → โลก (z กลับด้าน)
            for (int i = 0; i < 14; i++)
            {
                var d = Take();
                d.max = d.life = Random.Range(0.45f, 0.8f);
                d.size = Random.Range(0.05f, 0.1f);
                Vector3 side = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 1.3f;
                d.vel = outDir * Random.Range(1.2f, 3.2f) + side + Vector3.up * Random.Range(1.6f, 3.6f);
                d.t.position = c + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.15f), Random.Range(-0.1f, 0.1f));
                d.t.localScale = Vector3.one * d.size;
                d.t.gameObject.SetActive(true);
                drops.Add(d);
            }

            var ring = new GameObject("BiteRing").transform;
            ring.SetParent(root, false);
            ring.position = MapData.ToWorld(victim.x, 0.07f, victim.z);
            ring.gameObject.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var mr = ring.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(ringMat);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            ring.localScale = Vector3.one * 0.6f;
            rings.Add(new KeyValuePair<Transform, float>(ring, 0f));

            victim.bubble = "!"; victim.bubbleT = 0.9f;
            if (victim.human)
            {
                g.Jolt = 0.3f;
                if (g.PZ != null) g.PZ.biteFlash = Mathf.Max(g.PZ.biteFlash, 0.2f);
            }
            else if (!biter.human) { /* บอทกัดบอท: ไม่มีผลต่อกล้อง */ }
        }

        void EnsureAssets()
        {
            if (dropMesh != null) return;
            dropMesh = MeshBuilder.Sphere(0.5f, 8, 6);
            tracerMesh = MeshBuilder.Box(1f, 1f, 1f);
            tracerMat = Mats.Unlit(new Color(1f, 0.92f, 0.55f), fog: false);
            ringMesh = MeshBuilder.Ring(0.7f, 0.9f, 40);
            dropMat = Mats.Unlit(new Color(0.78f, 0.04f, 0.07f), fog: false);
            ringMat = Mats.Unlit(new Color(0.9f, 0.1f, 0.1f, 0.85f), transparent: true, doubleSided: true, zWrite: false, fog: false);
        }

        Drop Take()
        {
            if (pool.Count > 0) { var d = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); return d; }
            var go = new GameObject("drop");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = dropMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = dropMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return new Drop { t = go.transform };
        }

        void UpdateDrops(float dt)
        {
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.life -= dt;
                d.vel.y -= 9.5f * dt;
                d.t.position += d.vel * dt;
                if (d.t.position.y < 0.05f) { var p = d.t.position; p.y = 0.05f; d.t.position = p; d.vel = new Vector3(d.vel.x * 0.4f, 0f, d.vel.z * 0.4f); }
                d.t.localScale = Vector3.one * (d.size * Mathf.Clamp01(d.life / d.max * 1.6f));
                if (d.life <= 0f) { d.t.gameObject.SetActive(false); drops.RemoveAt(i); pool.Add(d); }
            }
        }

        void UpdateRings(float dt)
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                float t = rings[i].Value + dt;
                var tr = rings[i].Key;
                float k = Mathf.Clamp01(t / 0.55f);
                tr.localScale = Vector3.one * Mathf.Lerp(0.6f, 3.4f, 1f - (1f - k) * (1f - k));
                var mr = tr.GetComponent<MeshRenderer>();
                mr.sharedMaterial.SetColor("_Color", new Color(0.9f, 0.08f, 0.08f, 0.85f * (1f - k)));
                if (t >= 0.55f) { Object.Destroy(tr.gameObject); rings.RemoveAt(i); }
                else rings[i] = new KeyValuePair<Transform, float>(tr, t);
            }
        }
    }
}
