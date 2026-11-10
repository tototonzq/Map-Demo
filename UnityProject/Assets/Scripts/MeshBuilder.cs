using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkeldMap
{
    // ตัวสร้างเมชแบบ procedural — แทน THREE.BoxGeometry / CylinderGeometry / SphereGeometry / CapsuleGeometry ฯลฯ
    // Triangle() จะกลับด้านหน้าให้อัตโนมัติให้ตรงกับ normal ที่ส่งมา จึงไม่ต้องสนใจ winding ของ Unity ทีละหน้า
    public sealed class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color> col = new List<Color>();
        readonly List<int> t = new List<int>();

        public int VertexCount => v.Count;

        public int Vertex(Vector3 p, Vector3 normal, Vector2 uv0, Color? color = null)
        {
            v.Add(p); n.Add(normal); uv.Add(uv0); col.Add(color ?? Color.white);
            return v.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            Vector3 geo = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            Vector3 want = n[a] + n[b] + n[c];
            if (Vector3.Dot(geo, want) < 0f) { int tmp = b; b = c; c = tmp; }
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        // ทำให้เห็นทั้งสองด้าน (เทียบเท่า THREE.DoubleSide) โดยก๊อปปี้ทุกจุดกลับด้าน
        public MeshBuilder MakeDoubleSided()
        {
            int vc = v.Count, tc = t.Count;
            for (int i = 0; i < vc; i++) { v.Add(v[i]); n.Add(-n[i]); uv.Add(uv[i]); col.Add(col[i]); }
            for (int i = 0; i < tc; i += 3) { t.Add(t[i] + vc); t.Add(t[i + 2] + vc); t.Add(t[i + 1] + vc); }
            return this;
        }

        public Mesh ToMesh(string name = "mesh")
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetColors(col);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ───────────── รวมเมชสำเร็จรูป (เช่นชิ้นส่วนจาก FBX) ─────────────

        // เมชต้นฉบับที่อ่านข้อมูลออกมาครั้งเดียวและใส่ transform เริ่มต้นไว้แล้ว — ใช้วางซ้ำหลายพันครั้งโดยไม่ต้องอ่านเมชซ้ำ
        public sealed class MeshSource
        {
            public Vector3[] v, n;
            public Vector2[] uv;
            public int[] t;

            public static MeshSource From(Mesh m, Matrix4x4 pre)
            {
                var s = new MeshSource { v = m.vertices, n = m.normals, uv = m.uv, t = m.triangles };
                Matrix4x4 nm = pre.inverse.transpose;
                for (int i = 0; i < s.v.Length; i++)
                {
                    s.v[i] = pre.MultiplyPoint3x4(s.v[i]);
                    if (i < s.n.Length) s.n[i] = nm.MultiplyVector(s.n[i]).normalized;
                }
                return s;
            }
        }

        // เพิ่มเมชหนึ่งชิ้นพร้อม transform (รองรับ scale ไม่เท่ากันทุกแกน — normal ใช้ inverse-transpose)
        // ต้องเป็น matrix ที่ determinant > 0 (หมุน + scale บวก) ไม่เช่นนั้นด้านหน้าจะกลับ
        public MeshBuilder AddMesh(MeshSource src, Matrix4x4 mat, Color? color = null)
        {
            Matrix4x4 nm = mat.inverse.transpose;
            int baseIdx = v.Count;
            Color c = color ?? Color.white;
            for (int i = 0; i < src.v.Length; i++)
            {
                v.Add(mat.MultiplyPoint3x4(src.v[i]));
                n.Add(i < src.n.Length ? nm.MultiplyVector(src.n[i]).normalized : Vector3.up);
                uv.Add(i < src.uv.Length ? src.uv[i] : Vector2.zero);
                col.Add(c);
            }
            for (int i = 0; i < src.t.Length; i++) t.Add(baseIdx + src.t[i]);
            return this;
        }

        // ───────────── primitives ─────────────

        // กล่องกึ่งกลางที่ center ขนาด size (ใช้ต่อกันหลายกล่องในเมชเดียว เช่นกำแพง)
        public MeshBuilder AddBox(Vector3 center, Vector3 size, Color? color = null)
        {
            Vector3 h = size * 0.5f;
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            for (int ax = 0; ax < 3; ax++)
            {
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    int i1 = (ax + 1) % 3, i2 = (ax + 2) % 3;
                    Vector3 nrm = axes[ax] * sgn;
                    Vector3 a1 = axes[i1], a2 = axes[i2];
                    Vector3 c = center + nrm * h[ax];
                    int q0 = Vertex(c - a1 * h[i1] - a2 * h[i2], nrm, new Vector2(0, 0), color);
                    int q1 = Vertex(c + a1 * h[i1] - a2 * h[i2], nrm, new Vector2(1, 0), color);
                    int q2 = Vertex(c + a1 * h[i1] + a2 * h[i2], nrm, new Vector2(1, 1), color);
                    int q3 = Vertex(c - a1 * h[i1] + a2 * h[i2], nrm, new Vector2(0, 1), color);
                    Quad(q0, q1, q2, q3);
                }
            }
            return this;
        }

        public static Mesh Box(float w, float h, float d)
            => new MeshBuilder().AddBox(Vector3.zero, new Vector3(w, h, d)).ToMesh("box");

        // ตัวคูณความละเอียดของผิวโค้ง (ทรงกระบอก/ทรงกลม/แคปซูล) · 1 = เดิม · 2 = ละเอียดขึ้นเท่าตัว (ค่าเริ่มต้น) · 3 = ละเอียดมาก
        // ชิ้นที่ตั้งใจให้เป็นเหลี่ยม (< 10 ด้าน เช่น ฐานหกเหลี่ยม) จะไม่ถูกคูณ
        public static int Detail = 2;
        const int MaxSeg = 96;
        static int Fine(int n) => n >= 10 ? Mathf.Min(n * Mathf.Max(1, Detail), MaxSeg) : n;

        // เทียบเท่า CylinderGeometry(radiusTop, radiusBottom, height, segments) — กึ่งกลางที่ origin
        public static Mesh Cylinder(float rTop, float rBottom, float height, int seg = 24, bool openEnded = false)
        {
            seg = Fine(seg);
            var mb = new MeshBuilder();
            float hh = height * 0.5f;
            float slope = height > 0 ? (rBottom - rTop) / height : 0f;
            var top = new int[seg + 1];
            var bot = new int[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float th = (float)i / seg * Mathf.PI * 2f;
                float s = Mathf.Sin(th), c = Mathf.Cos(th);
                Vector3 nrm = new Vector3(s, slope, c).normalized;
                top[i] = mb.Vertex(new Vector3(rTop * s, hh, rTop * c), nrm, new Vector2((float)i / seg, 1));
                bot[i] = mb.Vertex(new Vector3(rBottom * s, -hh, rBottom * c), nrm, new Vector2((float)i / seg, 0));
            }
            for (int i = 0; i < seg; i++) mb.Quad(top[i], bot[i], bot[i + 1], top[i + 1]);

            if (!openEnded)
            {
                for (int side = 0; side < 2; side++)
                {
                    float r = side == 0 ? rTop : rBottom;
                    if (r <= 0f) continue;
                    float y = side == 0 ? hh : -hh;
                    Vector3 nrm = side == 0 ? Vector3.up : Vector3.down;
                    int centre = mb.Vertex(new Vector3(0, y, 0), nrm, new Vector2(0.5f, 0.5f));
                    var ring = new int[seg + 1];
                    for (int i = 0; i <= seg; i++)
                    {
                        float th = (float)i / seg * Mathf.PI * 2f;
                        float s = Mathf.Sin(th), c = Mathf.Cos(th);
                        ring[i] = mb.Vertex(new Vector3(r * s, y, r * c), nrm, new Vector2(0.5f + 0.5f * s, 0.5f + 0.5f * c));
                    }
                    for (int i = 0; i < seg; i++) mb.Triangle(centre, ring[i], ring[i + 1]);
                }
            }
            return mb.ToMesh("cylinder");
        }

        // เทียบเท่า SphereGeometry(r, wSeg, hSeg, 0, 2π, 0, thetaLength) — thetaLength < π = ฝาครอบ (โดมเปิดด้านล่าง)
        public static Mesh Sphere(float r, int wSeg, int hSeg, float thetaLength = Mathf.PI, bool doubleSided = false)
        {
            wSeg = Fine(wSeg); hSeg = Fine(hSeg);
            var mb = new MeshBuilder();
            var grid = new int[hSeg + 1, wSeg + 1];
            for (int iy = 0; iy <= hSeg; iy++)
            {
                float theta = thetaLength * iy / hSeg;
                float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
                for (int ix = 0; ix <= wSeg; ix++)
                {
                    float phi = (float)ix / wSeg * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(st * Mathf.Cos(phi), ct, st * Mathf.Sin(phi));
                    grid[iy, ix] = mb.Vertex(dir * r, dir, new Vector2((float)ix / wSeg, 1f - (float)iy / hSeg));
                }
            }
            for (int iy = 0; iy < hSeg; iy++)
                for (int ix = 0; ix < wSeg; ix++)
                    mb.Quad(grid[iy, ix], grid[iy + 1, ix], grid[iy + 1, ix + 1], grid[iy, ix + 1]);
            if (doubleSided) mb.MakeDoubleSided();
            return mb.ToMesh("sphere");
        }

        // เทียบเท่า CapsuleGeometry(radius, length) — สูงรวม = length + 2*radius
        public static Mesh Capsule(float radius, float length, int capSeg = 8, int radSeg = 16)
        {
            capSeg = Fine(capSeg); radSeg = Fine(radSeg);
            var mb = new MeshBuilder();
            int rings = capSeg * 2 + 2;
            var grid = new int[rings, radSeg + 1];
            int row = 0;
            for (int hemi = 0; hemi < 2; hemi++)
            {
                for (int i = 0; i <= capSeg; i++)
                {
                    float a = (hemi == 0 ? 0f : Mathf.PI / 2f) + (Mathf.PI / 2f) * i / capSeg;
                    float yc = hemi == 0 ? length * 0.5f : -length * 0.5f;
                    float sa = Mathf.Sin(a), ca = Mathf.Cos(a);
                    for (int ix = 0; ix <= radSeg; ix++)
                    {
                        float phi = (float)ix / radSeg * Mathf.PI * 2f;
                        Vector3 dir = new Vector3(sa * Mathf.Cos(phi), ca, sa * Mathf.Sin(phi));
                        grid[row, ix] = mb.Vertex(new Vector3(dir.x * radius, yc + dir.y * radius, dir.z * radius), dir,
                                                  new Vector2((float)ix / radSeg, 1f - (float)row / (rings - 1)));
                    }
                    row++;
                }
            }
            for (int iy = 0; iy < rings - 1; iy++)
                for (int ix = 0; ix < radSeg; ix++)
                    mb.Quad(grid[iy, ix], grid[iy + 1, ix], grid[iy + 1, ix + 1], grid[iy, ix + 1]);
            return mb.ToMesh("capsule");
        }

        // เทียบเท่า OctahedronGeometry(r)
        public static Mesh Octahedron(float r)
        {
            var mb = new MeshBuilder();
            Vector3[] p =
            {
                new Vector3(r, 0, 0), new Vector3(-r, 0, 0), new Vector3(0, r, 0),
                new Vector3(0, -r, 0), new Vector3(0, 0, r), new Vector3(0, 0, -r),
            };
            int[,] f =
            {
                { 0, 2, 4 }, { 4, 2, 1 }, { 1, 2, 5 }, { 5, 2, 0 },
                { 0, 4, 3 }, { 4, 1, 3 }, { 1, 5, 3 }, { 5, 0, 3 },
            };
            for (int i = 0; i < 8; i++) mb.FlatTri(p[f[i, 0]], p[f[i, 1]], p[f[i, 2]]);
            return mb.ToMesh("octahedron");
        }

        // เทียบเท่า IcosahedronGeometry(r, 1) — shading แบบเหลี่ยม (flat) เหมือน three.js
        // subdivide=false → 20 หน้าแบบหยาบ (ใช้กับต้นไม้/หินจำนวนมาก)
        public static Mesh Icosahedron(float r, bool subdivide = true)
        {
            float tt = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] p =
            {
                new Vector3(-1, tt, 0), new Vector3(1, tt, 0), new Vector3(-1, -tt, 0), new Vector3(1, -tt, 0),
                new Vector3(0, -1, tt), new Vector3(0, 1, tt), new Vector3(0, -1, -tt), new Vector3(0, 1, -tt),
                new Vector3(tt, 0, -1), new Vector3(tt, 0, 1), new Vector3(-tt, 0, -1), new Vector3(-tt, 0, 1),
            };
            for (int i = 0; i < p.Length; i++) p[i] = p[i].normalized * r;
            int[,] f =
            {
                { 0, 11, 5 }, { 0, 5, 1 }, { 0, 1, 7 }, { 0, 7, 10 }, { 0, 10, 11 },
                { 1, 5, 9 }, { 5, 11, 4 }, { 11, 10, 2 }, { 10, 7, 6 }, { 7, 1, 8 },
                { 3, 9, 4 }, { 3, 4, 2 }, { 3, 2, 6 }, { 3, 6, 8 }, { 3, 8, 9 },
                { 4, 9, 5 }, { 2, 4, 11 }, { 6, 2, 10 }, { 8, 6, 7 }, { 9, 8, 1 },
            };
            var mb = new MeshBuilder();
            for (int i = 0; i < f.GetLength(0); i++)
            {
                Vector3 a = p[f[i, 0]], b = p[f[i, 1]], c = p[f[i, 2]];
                if (!subdivide) { mb.FlatTri(a, b, c); continue; }
                Vector3 ab = (a + b).normalized * r, bc = (b + c).normalized * r, ca = (c + a).normalized * r;
                mb.FlatTri(a, ab, ca);
                mb.FlatTri(ab, b, bc);
                mb.FlatTri(ca, bc, c);
                mb.FlatTri(ab, bc, ca);
            }
            return mb.ToMesh("icosahedron");
        }

        // วงแหวนแบนบนระนาบ XZ หันขึ้น (เทียบเท่า RingGeometry ที่หมุน -90° รอบ X) — ใช้กับวงแหวนจุดเควส
        public static Mesh Ring(float inner, float outer, int seg = 40)
        {
            seg = Fine(seg);
            var mb = new MeshBuilder();
            var a = new int[seg + 1];
            var b = new int[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float th = (float)i / seg * Mathf.PI * 2f;
                float s = Mathf.Sin(th), c = Mathf.Cos(th);
                a[i] = mb.Vertex(new Vector3(inner * s, 0, inner * c), Vector3.up, new Vector2(0, 0));
                b[i] = mb.Vertex(new Vector3(outer * s, 0, outer * c), Vector3.up, new Vector2(1, 1));
            }
            for (int i = 0; i < seg; i++) mb.Quad(a[i], b[i], b[i + 1], a[i + 1]);
            return mb.ToMesh("ring");
        }

        // ระนาบพื้นสี่เหลี่ยมบน XZ หันขึ้น · UV ขยายตามขนาด (1 หน่วย = 1 กระเบื้อง) เหมือน addFloor ใน JS
        public static Mesh FloorQuad(float w, float d)
        {
            var mb = new MeshBuilder();
            int a = mb.Vertex(new Vector3(0, 0, 0), Vector3.up, new Vector2(0, 0));
            int b = mb.Vertex(new Vector3(0, 0, d), Vector3.up, new Vector2(0, d));
            int c = mb.Vertex(new Vector3(w, 0, d), Vector3.up, new Vector2(w, d));
            int e = mb.Vertex(new Vector3(w, 0, 0), Vector3.up, new Vector2(w, 0));
            mb.Quad(a, b, c, e);
            return mb.ToMesh("floor");
        }

        void FlatTri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 nrm = ((a + b + c) / 3f).normalized;
            int i0 = Vertex(a, nrm, Vector2.zero);
            int i1 = Vertex(b, nrm, Vector2.right);
            int i2 = Vertex(c, nrm, Vector2.up);
            Triangle(i0, i1, i2);
        }
    }
}
