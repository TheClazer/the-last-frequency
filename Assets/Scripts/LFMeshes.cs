using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>Procedural low-poly meshes (flat shaded, 16-bit indices) and the shared material palette. Everything the world needs is generated here, so
    /// the build has no model dependency; Blender-made props (Art/) can replace any of these through LFWorld's prefab slots.</summary>
    public class MB
    {
        public List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
        public Color col = Color.white; public bool orient; public Vector3 ctr;
        public void Tri(Vector3 a, Vector3 b, Vector3 d)
        {
            var nr = Vector3.Cross(b - a, d - a).normalized; if (orient && Vector3.Dot(nr, (a + b + d) / 3f - ctr) < 0f) { var sw = b; b = d; d = sw; nr = -nr; } int i = v.Count;
            v.Add(a); v.Add(b); v.Add(d); n.Add(nr); n.Add(nr); n.Add(nr); c.Add(col); c.Add(col); c.Add(col); uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d) { Tri(a, b, cc); Tri(a, cc, d); }
        public void Box(Vector3 center, Vector3 size, Quaternion rot)
        {
            bool po = orient; var pc = ctr; orient = true; ctr = center; Vector3 h = size * 0.5f; var p = new Vector3[8];
            for (int i = 0; i < 8; i++) p[i] = center + rot * new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            Quad(p[0], p[2], p[3], p[1]); Quad(p[4], p[5], p[7], p[6]); Quad(p[0], p[1], p[5], p[4]); Quad(p[2], p[6], p[7], p[3]); Quad(p[0], p[4], p[6], p[2]); Quad(p[1], p[3], p[7], p[5]); orient = po; ctr = pc;
        }
        public Mesh Build(string name)
        {
            var m = new Mesh { name = name }; m.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds(); m.UploadMeshData(true); return m;
        }
    }

    public static class LFMeshes
    {
        static readonly Dictionary<string, Mesh> _m = new Dictionary<string, Mesh>();
        public static Mesh Get(string name) { Mesh m; if (_m.TryGetValue(name, out m)) return m; m = Make(name); _m[name] = m; return m; }

        /// <summary>Surface of revolution along +Y from rings (y, radius); smooth silhouette, flat shaded.</summary>
        static void Lathe(MB b, float[] ys, float[] rs, int sides)
        {
            b.orient = true; b.ctr = new Vector3(0, 0.5f, 0);
            for (int k = 0; k < ys.Length - 1; k++)
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i / (float)sides * 6.2832f, a1 = (i + 1) / (float)sides * 6.2832f;
                    var p00 = new Vector3(Mathf.Sin(a0) * rs[k], ys[k], Mathf.Cos(a0) * rs[k]); var p10 = new Vector3(Mathf.Sin(a1) * rs[k], ys[k], Mathf.Cos(a1) * rs[k]);
                    var p01 = new Vector3(Mathf.Sin(a0) * rs[k + 1], ys[k + 1], Mathf.Cos(a0) * rs[k + 1]); var p11 = new Vector3(Mathf.Sin(a1) * rs[k + 1], ys[k + 1], Mathf.Cos(a1) * rs[k + 1]);
                    b.Tri(p00, p10, p11); if (rs[k + 1] > 0.001f) b.Tri(p00, p11, p01);
                }
        }

        static Mesh Make(string name)
        {
            if (name.StartsWith("Ice") || name.StartsWith("Water") || name.StartsWith("Flame") || name.StartsWith("Rock") || name.StartsWith("Basalt") || name == "ElemSphere") { var im = LFMeshIO.Load("Ice/" + name); if (im != null) return im; }
            var b = new MB();
            switch (name)
            {
                case "spire": return Spire(b, 5, 0.22f, 1f, 0.22f);
                case "spire3": return Spire(b, 4, 0.16f, 1f, 0f);
                case "crystals": Crystals(b); return b.Build(name);
                case "fan": Fan(b); return b.Build(name);
                case "shard": Bipyramid(b, 0.16f, 0.5f, 6); return b.Build(name);
                case "orb": Orb(b, 0.5f); return b.Build(name);
                case "stairs": Stairs(b, 24); return b.Build(name);
                case "arch": Arch(b, 1f, 0.07f, 12); return b.Build(name);
                case "floor": Floor(b, 1.9f); return b.Build(name);
                case "cube": b.Box(Vector3.zero, Vector3.one, Quaternion.identity); return b.Build(name);
                case "drop": Bipyramid(b, 0.3f, 0.5f, 8); return b.Build(name);
                case "tear": Lathe(b, new[] { 0f, 0.12f, 0.34f, 0.62f, 0.86f, 1f }, new[] { 0.08f, 0.3f, 0.27f, 0.17f, 0.07f, 0f }, 10); return b.Build(name);
                case "column": Lathe(b, new[] { 0f, 1f }, new[] { 0.3f, 0.27f }, 6); b.orient = true; b.ctr = new Vector3(0, 0.5f, 0); { var top = new Vector3(0, 1f, 0); for (int i = 0; i < 6; i++) { float a0 = i / 6f * 6.2832f, a1 = (i + 1) / 6f * 6.2832f; b.Tri(top, new Vector3(Mathf.Sin(a0) * 0.27f, 1f, Mathf.Cos(a0) * 0.27f), new Vector3(Mathf.Sin(a1) * 0.27f, 1f, Mathf.Cos(a1) * 0.27f)); } } return b.Build(name);
                case "rubble": b.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.4f, 0.6f), Quaternion.Euler(0, 15, 0)); b.Box(new Vector3(0.25f, 0.55f, 0.1f), new Vector3(0.4f, 0.5f, 0.4f), Quaternion.Euler(8, 40, 6)); b.Box(new Vector3(-0.22f, 0.38f, -0.15f), new Vector3(0.32f, 0.55f, 0.34f), Quaternion.Euler(-6, -25, 10)); return b.Build(name);
                case "quad":
                {
                    var m = new Mesh { name = "quad" };
                    m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0), new Vector3(.5f, -.5f, 0) };
                    m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }; m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                    m.triangles = new[] { 0, 1, 2, 0, 2, 3 }; m.RecalculateBounds(); return m;
                }
                case "disc": { for (int i = 0; i < 24; i++) { float a0 = i / 24f * Mathf.PI * 2, a1 = (i + 1) / 24f * Mathf.PI * 2; b.Tri(Vector3.zero, new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)), new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1))); } return b.Build(name); }
            }
            return b.Build(name);
        }

        static Mesh Spire(MB b, int sides, float r, float h, float twist)
        {
            b.orient = true; b.ctr = new Vector3(0, h * 0.35f, 0);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2, a1 = (i + 1) / (float)sides * Mathf.PI * 2;
                var p0 = new Vector3(Mathf.Sin(a0) * r, 0, Mathf.Cos(a0) * r); var p1 = new Vector3(Mathf.Sin(a1) * r, 0, Mathf.Cos(a1) * r);
                var m0 = new Vector3(Mathf.Sin(a0 + twist) * r * 0.55f, h * 0.55f, Mathf.Cos(a0 + twist) * r * 0.55f); var m1 = new Vector3(Mathf.Sin(a1 + twist) * r * 0.55f, h * 0.55f, Mathf.Cos(a1 + twist) * r * 0.55f);
                var top = new Vector3(0, h, 0);
                b.Quad(p0, m0, m1, p1); b.Tri(m0, top, m1); b.Tri(p0, p1, Vector3.zero);
            }
            return b.Build("spire");
        }
        static void Fan(MB b)
        {
            var rnd = new System.Random(5);
            for (int k = 0; k < 22; k++)
            {
                float ang = (float)rnd.NextDouble() * 6.2832f; float rad = (float)rnd.NextDouble() * 0.14f; var bp = new Vector3(Mathf.Sin(ang) * rad, 0, Mathf.Cos(ang) * rad);
                float tall = k < 4 ? 1f : (k < 10 ? 0.45f + 0.35f * (float)rnd.NextDouble() : 0.12f + 0.3f * (float)rnd.NextDouble());
                float lean = (k < 4 ? 0.06f : 0.15f) + (k < 10 ? 0.55f : 1.3f) * (float)rnd.NextDouble(); float la = (float)rnd.NextDouble() * 6.2832f;
                var dir = (Vector3.up + new Vector3(Mathf.Sin(la), 0, Mathf.Cos(la)) * lean).normalized; Crystal(b, bp, dir, tall, 0.018f + 0.03f * tall * (float)rnd.NextDouble() + 0.012f);
            }
        }
        static void Crystals(MB b)
        {
            var rnd = new System.Random(11); float[] hs = { 1f, 0.74f, 0.64f, 0.52f, 0.44f, 0.36f, 0.3f };
            for (int k = 0; k < hs.Length; k++)
            {
                float ang = k * 2.4f; float rad = k == 0 ? 0f : 0.15f + 0.035f * k; var bp = new Vector3(Mathf.Sin(ang) * rad, 0, Mathf.Cos(ang) * rad);
                float lean = k == 0 ? 0.03f : 0.22f + 0.08f * (float)rnd.NextDouble(); var dir = (Vector3.up + new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang)) * lean).normalized;
                Crystal(b, bp, dir, hs[k], 0.085f + 0.03f * (1f - k / 7f));
            }
        }
        static void Crystal(MB b, Vector3 bp, Vector3 dir, float h, float r)
        {
            b.orient = true; b.ctr = bp + dir * h * 0.4f; Vector3 u = Vector3.Cross(dir, Vector3.right).normalized; if (u.sqrMagnitude < 0.1f) u = Vector3.Cross(dir, Vector3.forward).normalized; Vector3 v = Vector3.Cross(dir, u);
            const int S = 6; var r0 = new Vector3[S]; var r1 = new Vector3[S];
            for (int i = 0; i < S; i++) { float a = i / (float)S * Mathf.PI * 2f; Vector3 off = u * Mathf.Cos(a) + v * Mathf.Sin(a); r0[i] = bp + off * r * 0.8f; r1[i] = bp + dir * h * 0.72f + off * r; }
            Vector3 tip = bp + dir * h;
            for (int i = 0; i < S; i++) { int j = (i + 1) % S; b.Quad(r0[i], r1[i], r1[j], r0[j]); b.Tri(r1[i], tip, r1[j]); }
        }
        static void Bipyramid(MB b, float r, float h, int sides)
        {
            b.orient = true; b.ctr = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2, a1 = (i + 1) / (float)sides * Mathf.PI * 2;
                var p0 = new Vector3(Mathf.Sin(a0) * r, 0, Mathf.Cos(a0) * r); var p1 = new Vector3(Mathf.Sin(a1) * r, 0, Mathf.Cos(a1) * r);
                b.Tri(p0, new Vector3(0, h, 0), p1); b.Tri(p0, p1, new Vector3(0, -h, 0));
            }
        }
        static void Orb(MB b, float r)
        {
            b.orient = true; b.ctr = Vector3.zero;
            // icosahedron, one subdivision
            float t = (1f + Mathf.Sqrt(5f)) / 2f; var ic = new[] { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            for (int i = 0; i < ic.Length; i++) ic[i] = ic[i].normalized;
            int[] f = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            for (int i = 0; i < f.Length; i += 3)
            {
                Vector3 a = ic[f[i]], c = ic[f[i + 1]], d = ic[f[i + 2]]; Vector3 ab = ((a + c) * 0.5f).normalized, cd = ((c + d) * 0.5f).normalized, da = ((d + a) * 0.5f).normalized;
                b.Tri(a * r, ab * r, da * r); b.Tri(ab * r, c * r, cd * r); b.Tri(da * r, cd * r, d * r); b.Tri(ab * r, cd * r, da * r);
            }
        }
        static void Stairs(MB b, int count)
        {
            float R = 3.7f, rise = 0.22f, dA = 11f * Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                float a = i * dA; Vector3 c = new Vector3(Mathf.Sin(a) * R, i * rise, Mathf.Cos(a) * R);
                b.col = new Color(1, 1, 1, (i + 1) / (float)count); b.Box(c, new Vector3(1.15f, 0.045f, 0.62f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
            }
        }
        static void Arch(MB b, float r, float tube, int seg)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI, a1 = (i + 1) / (float)seg * Mathf.PI;
                Vector3 c0 = new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0), c1 = new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0);
                Vector3 n0 = c0.normalized * tube, n1 = c1.normalized * tube, z = Vector3.forward * tube; b.orient = true; b.ctr = (c0 + c1) * 0.5f;
                b.Quad(c0 + n0 - z, c1 + n1 - z, c1 + n1 + z, c0 + n0 + z); b.Quad(c0 - n0 + z, c1 - n1 + z, c1 - n1 - z, c0 - n0 - z);
                b.Quad(c0 + n0 + z, c1 + n1 + z, c1 - n1 + z, c0 - n0 + z); b.Quad(c0 - n0 - z, c1 - n1 - z, c1 + n1 - z, c0 + n0 - z);
            }
        }
        static void Floor(MB b, float r)
        {
            int s = 32; float inner = r * 0.82f;
            for (int i = 0; i < s; i++)
            {
                float a0 = i / (float)s * Mathf.PI * 2, a1 = (i + 1) / (float)s * Mathf.PI * 2;
                Vector3 o0 = new Vector3(Mathf.Sin(a0) * r, 0, Mathf.Cos(a0) * r), o1 = new Vector3(Mathf.Sin(a1) * r, 0, Mathf.Cos(a1) * r), i0 = o0 * (inner / r), i1 = o1 * (inner / r);
                b.orient = true; b.ctr = new Vector3(0, -10f, 0); b.Tri(Vector3.zero, i1, i0); b.Quad(i0, i1, o1, o0);
                b.ctr = new Vector3(0, -0.06f, 0); b.Quad(o0, o1, o1 + Vector3.down * 0.12f, o0 + Vector3.down * 0.12f);
            }
        }
    }

    public static class LFMats
    {
        public const int Cyan = 0, Magenta = 1, Gold = 2, Violet = 3, Mint = 4, Ice = 5, Count = 6;
        static Material[] _m; static Material _sky, _mote, _stair, _floor;
        static readonly string[][] Pal = {
            new[] { "#2fa8ff", "#d9f6ff" }, new[] { "#a064ff", "#ffc9f2" }, new[] { "#ffcf7a", "#fff4d0" },
            new[] { "#4458ff", "#bfc9ff" }, new[] { "#2fe0cc", "#cffff5" }, new[] { "#8fd0ff", "#ffffff" } };
        public static Material Toon(int i)
        {
            if (_m == null) { _m = new Material[Count]; var sh = Shader.Find("LF/Toon"); for (int k = 0; k < Count; k++) { _m[k] = new Material(sh) { name = "LFToon" + k, enableInstancing = true }; _m[k].SetColor("_ColorA", LFUtil.Hex(Pal[k][0])); _m[k].SetColor("_ColorB", LFUtil.Hex(Pal[k][1])); } }
            return _m[i % Count];
        }
        public static Material Stair() { if (_stair == null) { _stair = new Material(Toon(Ice)) { name = "LFStair" }; _stair.SetFloat("_UseReveal", 1f); _stair.SetFloat("_Height", 6f); _stair.SetFloat("_OutW", 0.03f); } return _stair; }
        public static Material Floor() { if (_floor == null) _floor = new Material(Shader.Find("LF/Floor")) { name = "LFFloor" }; return _floor; }
        static Material _flake, _trail;
        public static Material Flake() { if (_flake == null) _flake = new Material(Shader.Find("LF/Flake")); return _flake; }
        static Material _flower; public static Material Flower() { if (_flower == null) _flower = new Material(Shader.Find("LF/Flower")); return _flower; }
        public static Material Trail() { if (_trail == null) _trail = new Material(Shader.Find("LF/Trail")); return _trail; }
        public static Material Sky() { if (_sky == null) _sky = new Material(Shader.Find("LF/Sky")); return _sky; }
        public static Material Mote() { if (_mote == null) _mote = new Material(Shader.Find("LF/Mote")); return _mote; }
        public static Material Ring(Color c) { var m = new Material(Shader.Find("LF/Ring")); m.SetColor("_Color", c); return m; }
        /// <summary>Swap the whole toon palette (pairs deep, glow for each of the 6 slots) and refresh materials already created.</summary>
        public static void SetPalette(string[] pal)
        {
            for (int k = 0; k < Count; k++) { Pal[k][0] = pal[k * 2]; Pal[k][1] = pal[k * 2 + 1]; }
            if (_m != null) for (int k = 0; k < Count; k++) { _m[k].SetColor("_ColorA", LFUtil.Hex(Pal[k][0])); _m[k].SetColor("_ColorB", LFUtil.Hex(Pal[k][1])); }
            if (_stair != null) { _stair.SetColor("_ColorA", LFUtil.Hex(Pal[Ice][0])); _stair.SetColor("_ColorB", LFUtil.Hex(Pal[Ice][1])); }
        }
        /// <summary>0 = glowing glass (ice, water, fire); 1 = opaque lit stone (earth).</summary>
        /// <summary>Per-element material mode: ice, water and earth are alpha-blended with depth write (earth is opaque); fire is additive.</summary>
        public static void SetMode(int kind)
        {
            Toon(0); float dst = kind == 3 ? 1f : 10f, zw = kind == 3 ? 0f : 1f;
            float cull = (kind == 1 || kind == 2) ? 2f : 0f;
            foreach (var m in _m) { m.SetFloat("_DstBlend", dst); m.SetFloat("_ZW", zw); m.SetFloat("_Cull", cull); }
            if (_stair != null) { _stair.SetFloat("_DstBlend", 10f); _stair.SetFloat("_ZW", 1f); }
        }
        static Material _elemFloor;
        /// <summary>The ground under the player, in the current element: frozen lake, water surface, rock, cooling lava.</summary>
        public static Material ElemFloor(Color a, Color b)
        {
            if (_elemFloor == null) { _elemFloor = new Material(Shader.Find("LF/Toon")) { name = "LFElemFloor" }; _elemFloor.SetFloat("_DstBlend", 10f); _elemFloor.SetFloat("_ZW", 1f); _elemFloor.SetFloat("_Floor", 1f); }
            _elemFloor.SetColor("_ColorA", a); _elemFloor.SetColor("_ColorB", b); return _elemFloor;
        }
        public static void SetSolid(float v)
        {
            Toon(0); foreach (var m in _m) { m.SetFloat("_Solid", v); m.SetFloat("_DstBlend", v > 0.01f ? 10f : 1f); }
            if (_stair != null) { _stair.SetFloat("_Solid", v); _stair.SetFloat("_DstBlend", v > 0.01f ? 10f : 1f); }
        }
        public static Color Tint(int i) { return LFUtil.Hex(Pal[i % Count][0]); }
        public static Color Tint2(int i) { return LFUtil.Hex(Pal[i % Count][1]); }
    }
}
