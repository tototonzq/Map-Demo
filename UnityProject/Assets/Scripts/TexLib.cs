using System.Collections.Generic;
using UnityEngine;

namespace SkeldMap
{
    // คลังเท็กซ์เจอร์หน้าจอ/ป้ายที่วาดด้วยโค้ด (ไม่ต้องมีไฟล์รูป) — ใช้กับจอ CCTV, เรดาร์, แผนที่ดาว, ตู้เซิร์ฟเวอร์ ฯลฯ
    // พิกัดพิกเซล: (0,0) = มุมซ้ายล่างของภาพ
    public static class TexLib
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color32[] px;
            public Canvas(int w, int h, Color32 bg) { W = w; H = h; px = new Color32[w * h]; Fill(bg); }

            public void Fill(Color32 c) { for (int i = 0; i < px.Length; i++) px[i] = c; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = c; }
            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c);
            }
            public void Line(int x0, int y0, int x1, int y1, Color32 c, int th = 1)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
                while (true)
                {
                    for (int a = 0; a < th; a++) for (int b = 0; b < th; b++) Set(x0 + a, y0 + b, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }
            public void Disc(int cx, int cy, int r, Color32 c)
            {
                for (int j = -r; j <= r; j++) for (int i = -r; i <= r; i++) if (i * i + j * j <= r * r) Set(cx + i, cy + j, c);
            }
            public void Ring(int cx, int cy, int r, Color32 c)
            {
                for (int j = -r - 1; j <= r + 1; j++)
                    for (int i = -r - 1; i <= r + 1; i++)
                    {
                        float d = Mathf.Sqrt(i * i + j * j);
                        if (d >= r - 0.6f && d <= r + 0.6f) Set(cx + i, cy + j, c);
                    }
            }
            public Texture2D ToTexture(string name)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = name };
                t.SetPixels32(px);
                t.Apply(false);
                return t;
            }
        }

        static Texture2D Get(string key, System.Func<Texture2D> make)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            return cache[key] = make();
        }

        static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        // ───────────── กล้องวงจรปิด (seed ต่างกัน = ภาพต่างกัน) ─────────────
        public static Texture2D CctvFeed(int seed) => Get("cctv" + seed, () =>
        {
            var rng = new System.Random(seed * 977 + 13);
            var c = new Canvas(96, 54, C(10, 28, 22));
            // ฉากหลังไล่เฉด + พื้น/ผนัง
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    int g = 24 + y * 40 / c.H + (int)(8 * Mathf.Sin(x * 0.2f + seed));
                    c.Set(x, y, C(8, g, g - 6));
                }
            c.Rect(0, 0, c.W, 14, C(12, 44, 36));                          // พื้น
            for (int i = 0; i < 4; i++)                                   // เงาตัวละคร/สิ่งของ
            {
                int x = rng.Next(8, c.W - 20), w = rng.Next(6, 14), h = rng.Next(14, 26);
                c.Rect(x, 8, w, h, C(4, 18, 14));
                c.Disc(x + w / 2, 8 + h + 3, 4, C(4, 18, 14));
            }
            for (int y = 0; y < c.H; y += 3) for (int x = 0; x < c.W; x++) c.Set(x, y, C(0, 0, 0, 255)); // สแกนไลน์
            c.Disc(c.W - 9, c.H - 8, 3, C(255, 50, 40));                  // จุด REC
            c.Rect(4, c.H - 8, 16, 4, C(160, 255, 200));                  // เวลา/ชื่อกล้อง
            return c.ToTexture("cctv" + seed);
        });

        // ───────────── เรดาร์ ─────────────
        public static Texture2D Radar() => Get("radar", () =>
        {
            var c = new Canvas(128, 128, C(6, 18, 12));
            int cx = 64, cy = 64;
            for (int r = 14; r <= 60; r += 15) c.Ring(cx, cy, r, C(30, 130, 70));
            c.Line(cx - 62, cy, cx + 62, cy, C(24, 90, 52)); c.Line(cx, cy - 62, cx, cy + 62, C(24, 90, 52));
            // ลำแสงกวาด: ลิ่มเฉดจางลง
            for (int a = 0; a < 50; a++)
            {
                float ang = 0.9f - a * 0.012f;
                int x1 = cx + (int)(Mathf.Cos(ang) * 60), y1 = cy + (int)(Mathf.Sin(ang) * 60);
                c.Line(cx, cy, x1, y1, C(40, 200 - a * 3, 90 - a));
            }
            foreach (var p in new[] { new Vector2(30, 24), new Vector2(-24, 34), new Vector2(40, -20), new Vector2(-34, -26) })
            { c.Disc(cx + (int)p.x, cy + (int)p.y, 2, C(255, 120, 60)); c.Ring(cx + (int)p.x, cy + (int)p.y, 5, C(255, 120, 60)); }
            return c.ToTexture("radar");
        });

        // ───────────── แผนที่ดาว + เส้นทาง ─────────────
        public static Texture2D StarMap() => Get("starmap", () =>
        {
            var rng = new System.Random(7);
            var c = new Canvas(192, 96, C(8, 10, 24));
            for (int i = 0; i < 220; i++) { int b = rng.Next(90, 255); c.Set(rng.Next(c.W), rng.Next(c.H), C(b, b, 255)); }
            for (int x = 0; x < c.W; x += 24) c.Line(x, 0, x, c.H - 1, C(18, 24, 52));
            for (int y = 0; y < c.H; y += 24) c.Line(0, y, c.W - 1, y, C(18, 24, 52));
            var pts = new[] { new Vector2(14, 20), new Vector2(52, 60), new Vector2(92, 36), new Vector2(128, 70), new Vector2(172, 44) };
            for (int i = 0; i < pts.Length - 1; i++) c.Line((int)pts[i].x, (int)pts[i].y, (int)pts[i + 1].x, (int)pts[i + 1].y, C(255, 160, 60), 2);
            foreach (var p in pts) { c.Disc((int)p.x, (int)p.y, 5, C(255, 210, 90)); c.Ring((int)p.x, (int)p.y, 9, C(120, 200, 255)); }
            c.Disc((int)pts[0].x, (int)pts[0].y, 3, C(80, 255, 140));
            return c.ToTexture("starmap");
        });

        // ───────────── ตู้เซิร์ฟเวอร์ (แถบไฟ LED) ─────────────
        public static Texture2D Server(int seed) => Get("server" + seed, () =>
        {
            var rng = new System.Random(seed * 31 + 5);
            var c = new Canvas(64, 128, C(14, 16, 26));
            for (int unit = 0; unit < 10; unit++)
            {
                int y = 4 + unit * 12;
                c.Rect(3, y, 58, 9, C(28, 32, 50));
                c.Rect(6, y + 3, 24, 3, C(10, 12, 20));                  // ช่องลม
                for (int i = 0; i < 6; i++)
                {
                    int k = rng.Next(5);
                    Color32 col = k == 0 ? C(255, 80, 60) : k == 1 ? C(255, 200, 60) : C(70, 240, 130);
                    c.Rect(36 + i * 4, y + 3, 2, 3, col);
                }
            }
            return c.ToTexture("server" + seed);
        });

        // ───────────── แดชบอร์ดแอดมิน (กราฟแท่ง + เส้น) ─────────────
        public static Texture2D Dashboard() => Get("dash", () =>
        {
            var rng = new System.Random(21);
            var c = new Canvas(192, 96, C(12, 14, 30));
            c.Rect(0, c.H - 12, c.W, 12, C(244, 165, 58));
            for (int i = 0; i < 12; i++) { int h = rng.Next(10, 50); c.Rect(10 + i * 8, 10, 6, h, i % 3 == 0 ? C(255, 150, 60) : C(90, 180, 255)); }
            int prev = 50;
            for (int x = 112; x < 186; x += 4)
            {
                int y = 40 + (int)(20 * Mathf.Sin(x * 0.12f)) + rng.Next(-3, 3);
                c.Line(x - 4, prev, x, y, C(80, 255, 150), 2); prev = y;
            }
            c.Ring(150, 20, 9, C(255, 255, 255)); c.Ring(150, 20, 6, C(255, 200, 80));
            return c.ToTexture("dash");
        });

        // ───────────── แผงเบรกเกอร์/สายไฟ ─────────────
        public static Texture2D Breaker(bool wires) => Get("breaker" + wires, () =>
        {
            var rng = new System.Random(wires ? 3 : 4);
            var c = new Canvas(96, 112, C(34, 38, 60));
            if (wires)
            {
                // สายไฟ 4 สี ลากจากซ้ายไปขวาสลับกัน (งานเควส Fix Wiring)
                Color32[] cols = { C(255, 70, 60), C(70, 140, 255), C(255, 210, 60), C(255, 120, 200) };
                int[] order = { 2, 0, 3, 1 };
                for (int i = 0; i < 4; i++)
                {
                    int y0 = 90 - i * 22, y1 = 90 - order[i] * 22;
                    c.Disc(8, y0, 5, cols[i]); c.Disc(88, y1, 5, cols[i]);
                    c.Line(8, y0, 48, y0, cols[i], 3); c.Line(48, y0, 48, y1, cols[i], 3); c.Line(48, y1, 88, y1, cols[i], 3);
                }
            }
            else
            {
                for (int row = 0; row < 4; row++)
                    for (int col = 0; col < 6; col++)
                    {
                        int x = 6 + col * 15, y = 8 + row * 26;
                        bool on = rng.Next(3) > 0;
                        c.Rect(x, y, 11, 20, C(16, 18, 30));
                        c.Rect(x + 2, on ? y + 10 : y + 2, 7, 8, on ? C(80, 240, 130) : C(255, 90, 70));
                    }
            }
            return c.ToTexture("breaker");
        });

        // ───────────── ตู้กดอาหาร (ช่องสินค้าหลากสี) ─────────────
        public static Texture2D Vending() => Get("vending", () =>
        {
            var rng = new System.Random(99);
            var c = new Canvas(64, 96, C(20, 24, 40));
            Color32[] pal = { C(255, 90, 80), C(255, 190, 60), C(90, 200, 255), C(120, 230, 120), C(220, 130, 255), C(255, 255, 255) };
            for (int row = 0; row < 5; row++)
            {
                c.Rect(0, 8 + row * 18 + 14, 64, 2, C(120, 130, 170));   // ชั้นวาง
                for (int col = 0; col < 4; col++)
                    c.Rect(4 + col * 15, 8 + row * 18 + 16, 11, 12 - rng.Next(0, 3), pal[rng.Next(pal.Length)]);
            }
            return c.ToTexture("vending");
        });
    }
}
