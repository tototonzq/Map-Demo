using UnityEngine;

namespace SkeldMap
{
    // สังเคราะห์เสียงด้วยโค้ด (ไม่ต้องใช้ไฟล์เสียง) — port จาก playStep / playChime ที่ใช้ Web Audio ใน js/main.js
    public static class ProceduralAudio
    {
        const int SR = 44100;

        // envelope แบบ exponentialRampToValueAtTime: ไล่จาก a → b ตามสัดส่วนเวลา u (0..1) แบบเลขชี้กำลัง
        static float Exp(float a, float b, float u) => a * Mathf.Pow(b / a, Mathf.Clamp01(u));

        // เสียงเดินบนพื้นโลหะ: "แกร๊ก" (noise ผ่าน bandpass) + "ตึก" (sine ตกจาก 120→55 Hz)
        public static AudioClip[] MakeFootsteps(int variants)
        {
            var clips = new AudioClip[variants];
            var rng = new System.Random(2024);
            for (int v = 0; v < variants; v++)
            {
                int len = (int)(SR * 0.12f);
                var data = new float[len];

                // bandpass (RBJ biquad) · ความถี่สุ่ม 900–1500 Hz ไม่ให้ซ้ำซาก
                float f0 = 900f + (float)rng.NextDouble() * 600f;
                float w0 = 2f * Mathf.PI * f0 / SR;
                float alpha = Mathf.Sin(w0) / (2f * 0.9f);
                float a0 = 1f + alpha;
                float b0 = alpha / a0, b2 = -alpha / a0;
                float a1 = -2f * Mathf.Cos(w0) / a0, a2 = (1f - alpha) / a0;
                float x1 = 0, x2 = 0, y1 = 0, y2 = 0;

                float phase = 0f;
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / SR;

                    float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
                    x2 = x1; x1 = x; y2 = y1; y1 = y;
                    float nEnv = t < 0.005f ? Exp(0.0001f, 0.28f, t / 0.005f) : Exp(0.28f, 0.0001f, (t - 0.005f) / 0.085f);
                    if (t > 0.09f) nEnv = 0f;

                    float freq = Exp(120f, 55f, t / 0.08f);
                    phase += 2f * Mathf.PI * freq / SR;
                    float oEnv = t < 0.006f ? Exp(0.0001f, 0.22f, t / 0.006f) : Exp(0.22f, 0.0001f, (t - 0.006f) / 0.094f);
                    if (t > 0.1f) oEnv = 0f;

                    data[i] = Mathf.Clamp(y * 3.0f * nEnv + Mathf.Sin(phase) * oEnv, -1f, 1f);
                }
                clips[v] = AudioClip.Create("step" + v, len, 1, SR, false);
                clips[v].SetData(data, 0);
            }
            return clips;
        }

        // เสียงติ๊งตอนทำเควสเสร็จ: โน้ต 660 → 880 Hz ห่างกัน 0.11 วินาที
        public static AudioClip MakeChime()
        {
            int len = (int)(SR * 0.65f);
            var data = new float[len];
            float[] freqs = { 660f, 880f };
            for (int n = 0; n < 2; n++)
            {
                int start = (int)(SR * 0.11f * n);
                for (int i = 0; i < (int)(SR * 0.5f) && start + i < len; i++)
                {
                    float t = (float)i / SR;
                    float env = t < 0.01f ? Exp(0.0001f, 0.2f, t / 0.01f) : Exp(0.2f, 0.0001f, (t - 0.01f) / 0.44f);
                    if (t > 0.45f) env = 0f;
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env;
                }
            }
            var clip = AudioClip.Create("chime", len, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // เสียงบรรยากาศวนลูป 12 วินาที: โดรนต่ำสามโน้ต (ความถี่เป็นจำนวนรอบพอดีจึงต่อกันเนียน) + จังหวะหัวใจเบา ๆ ทุก 1.5 วินาที
        public static AudioClip MakeAmbient()
        {
            const float Len = 12f;
            int n = (int)(SR * Len);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float lfo = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * (3f / Len) * t);
                float lfo2 = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (2f / Len) * t + 1.3f);
                float v = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f * lfo
                        + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.28f * lfo2
                        + Mathf.Sin(2f * Mathf.PI * 110.5f * t) * 0.14f * lfo;
                float bt = Mathf.Repeat(t, 1.5f);
                float beat = Mathf.Exp(-bt * 22f) * Mathf.Sin(2f * Mathf.PI * 48f * bt) + 0.7f * Mathf.Exp(-(bt - 0.28f) * 24f) * (bt > 0.28f ? 1f : 0f) * Mathf.Sin(2f * Mathf.PI * 44f * (bt - 0.28f));
                data[i] = (v * 0.5f + beat * 0.35f) * 0.55f;
            }
            var clip = AudioClip.Create("ambient", n, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // เสียงปืน: แตกดังสั้น (noise) + แรงอัดต่ำ (sine ลดความถี่) + หางสะท้อนสั้น ๆ
        public static AudioClip MakeGunshot()
        {
            int len = (int)(SR * 0.45f);
            var data = new float[len];
            var rnd = new System.Random(11);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.55f;
                float crack = (noise * 0.6f + lp * 0.7f) * Mathf.Exp(-t * 38f);
                float thump = Mathf.Sin(2f * Mathf.PI * (150f - 90f * Mathf.Min(1f, t * 6f)) * t) * Mathf.Exp(-t * 16f) * 0.9f;
                float tail = lp * 0.25f * Mathf.Exp(-t * 9f) * (t > 0.03f ? 1f : 0f);
                data[i] = Mathf.Clamp((crack + thump + tail) * 0.85f, -1f, 1f);
            }
            var clip = AudioClip.Create("gunshot", len, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // เสียงไอ: ลมหายใจสั้น ๆ 2 ครั้ง (noise กรอง + โทนต่ำ) ใช้เป็นอาการของผู้ติดเชื้อ
        public static AudioClip MakeCough()
        {
            int len = (int)(SR * 0.5f);
            var data = new float[len];
            var rnd = new System.Random(7);
            float lp = 0f;
            for (int burst = 0; burst < 2; burst++)
            {
                int start = (int)(SR * (burst == 0 ? 0.02f : 0.2f));
                int n = (int)(SR * 0.13f);
                for (int i = 0; i < n && start + i < len; i++)
                {
                    float t = (float)i / n;
                    float env = Mathf.Min(1f, t * 18f) * Mathf.Exp(-t * 5f);
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    lp += (noise - lp) * 0.32f;
                    float tone = Mathf.Sin(2f * Mathf.PI * (190f - 60f * t) * i / SR) * 0.35f;
                    data[start + i] += (lp * 1.4f + tone) * env * 0.7f;
                }
            }
            var clip = AudioClip.Create("cough", len, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // เสียงปุ่มสั้น ๆ ในมินิเกม (ปรับ pitch ตอนเล่นเพื่อให้เป็นเสียงถูก/ผิด)
        public static AudioClip MakeBlip()
        {
            int len = (int)(SR * 0.14f);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 28f) * Mathf.Min(1f, t * 400f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 990f * t) * 0.3f) * env * 0.6f;
            }
            var clip = AudioClip.Create("blip", len, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
