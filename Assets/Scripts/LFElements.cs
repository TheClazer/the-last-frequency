using UnityEngine;

namespace LF
{
    public enum LFEl { Ice = 0, Water = 1, Earth = 2, Fire = 3 }

    /// <summary>Element looks: the material the world is built from. Each element changes the palette, the particle glyph and motion, the building shapes and the sky/floor.
    /// (Palette + shapes + particle behaviour; not a fluid or rigid-body simulation.)</summary>
    public static class LFElements
    {
        public static LFEl Cur = LFEl.Ice;
        public static readonly string[] Names = { "ICE", "WATER", "EARTH", "FIRE" };
        public static Color A1 = LFUtil.Hex("#b8e6ff"), A2 = LFUtil.Hex("#c9b8ff"), A3 = LFUtil.Hex("#9fdcff");

        public class Look
        {
            public string[] pal; public string floorA = "#06193a", floorB = "#a9dcff"; public string sky1, sky2, skyVoid, floorDeep, floorGlow, p0, p1, p2, accent1, accent2;
            public float feetY0, feetY1, aboveY0, aboveY1, swirl, noise, kind;
        }

        static readonly Look[] _l = {
            // ICE: the original look
            new Look { pal = new[] { "#2fa8ff", "#d9f6ff", "#a064ff", "#ffc9f2", "#ffcf7a", "#fff4d0", "#4458ff", "#bfc9ff", "#2fe0cc", "#cffff5", "#8fd0ff", "#ffffff" },
                sky1 = "#33e6ff", sky2 = "#9959ff", skyVoid = "#03051a", floorDeep = "#050d33", floorGlow = "#73d9ff", p0 = "#c8f2ff", p1 = "#7fc4ff", p2 = "#cdbcff", accent1 = "#b8e6ff", accent2 = "#c9b8ff",
                feetY0 = 0.35f, feetY1 = 0.9f, aboveY0 = -1.4f, aboveY1 = -0.5f, swirl = 0.7f, noise = 0.25f, kind = 0 },
            // WATER: teal and deep blue, rain-fast droplets, slow rising bubbles, ripple glyphs
            new Look { pal = new[] { "#1fb5c9", "#c8fff4", "#3a7dff", "#b8e0ff", "#7fe6d0", "#eafff8", "#2a4fd8", "#a9c2ff", "#26d4a8", "#ccfff0", "#5fc4ff", "#f0ffff" },
                sky1 = "#19d6c8", sky2 = "#1a52ff", skyVoid = "#00080f", floorDeep = "#00122e", floorGlow = "#33e0d0", p0 = "#c8fff4", p1 = "#4fd2ff", p2 = "#2a6bff", accent1 = "#9ff3ff", accent2 = "#7ab2ff",
                feetY0 = 0.15f, feetY1 = 0.45f, aboveY0 = -3.4f, aboveY1 = -1.8f, swirl = 0.5f, noise = 0.55f, kind = 1, floorA = "#02223a", floorB = "#7fe6ea" },
            // EARTH: sand, moss and gold, heavy slow dust, rising pebbles, hex facets
            new Look { pal = new[] { "#9a7340", "#e8c98c", "#6b8f3a", "#cfe69a", "#d9a441", "#ffe9a6", "#7a5a48", "#dcbfa6", "#4f8a4a", "#c4eaa6", "#b09a7c", "#f6ead6" },
                sky1 = "#e6892a", sky2 = "#7fb33a", skyVoid = "#0d0804", floorDeep = "#1a0f06", floorGlow = "#bf8033", p0 = "#ffe0a0", p1 = "#d9a441", p2 = "#8a6a3c", accent1 = "#ffd98a", accent2 = "#b8d878",
                feetY0 = 0.1f, feetY1 = 0.35f, aboveY0 = -0.55f, aboveY1 = -0.15f, swirl = 0.22f, noise = 0.8f, kind = 2, floorA = "#5a4630", floorB = "#b89a6a" },
            // FIRE: orange to crimson, upward sparks, ember glyphs
            new Look { pal = new[] { "#ff7a1f", "#fff0b0", "#ff2f4a", "#ffc0a0", "#ffc21f", "#fff7c0", "#b3122e", "#ff9a7a", "#ff9a2f", "#ffe0a0", "#ffd27a", "#ffffff" },
                sky1 = "#ff5a0d", sky2 = "#e60d33", skyVoid = "#0a0203", floorDeep = "#1f0303", floorGlow = "#ff7326", p0 = "#fff3b0", p1 = "#ff9a2f", p2 = "#ff3a1f", accent1 = "#ffc86b", accent2 = "#ff7a5a",
                feetY0 = 1.0f, feetY1 = 2.4f, aboveY0 = -0.2f, aboveY1 = 0.35f, swirl = 1.0f, noise = 0.9f, kind = 3, floorA = "#140805", floorB = "#3a1a10" },
        };
        public static Look Get(LFEl e) { return _l[(int)e]; }

        /// <summary>Building shapes per element, all baked in Blender with real normals: ice crystal clusters and chunks, rising water columns and drops,
        /// rock spires, basalt columns and boulders, flame tongues.</summary>
        static int _c; public static string Mesh(string n)
        {
            _c++; bool tall = n == "fan", small = n == "shard", ball = n == "orb";
            switch (Cur)
            {
                case LFEl.Ice: if (tall || small) return _c % 3 == 0 ? "IceCrystal_A" : (_c % 3 == 1 ? "IceCrystal_B" : "IceCrystal_C"); if (ball) return _c % 2 == 0 ? "IceChunk_A" : "IceChunk_B"; return n;
                case LFEl.Water: if (tall) return _c % 2 == 0 ? "WaterColumn_A" : "WaterColumn_B"; if (small) return "WaterDrop"; if (ball) return "ElemSphere"; return n;
                case LFEl.Earth: if (tall) { int k = _c % 4; return k == 0 ? "RockSpire_A" : (k == 1 ? "Basalt_A" : (k == 2 ? "RockSpire_B" : "Basalt_B")); } if (small || ball) return _c % 2 == 0 ? "RockBoulder_A" : "RockBoulder_B"; return n;
                case LFEl.Fire: if (tall) return _c % 2 == 0 ? "Flame_A" : "Flame_B"; if (small) return "Flame_B"; if (ball) return "ElemSphere"; return n;
            }
            return n;
        }
        /// <summary>Proportions so each element reads differently: ice wide clusters, water fat columns, earth broad and low, fire slim and tall.</summary>
        public static Vector3 Shape(string mesh, Vector3 s)
        {
            bool tall = mesh == "fan", small = mesh == "shard";
            switch (Cur)
            {
                case LFEl.Ice: return (tall || small) ? new Vector3(s.x * 1.9f, s.y, s.z * 1.9f) : s;
                case LFEl.Water: return tall ? new Vector3(s.x * 1.25f, s.y * 1.1f, s.z * 1.25f) : (small ? new Vector3(s.x * 1.5f, s.y * 0.7f, s.z * 1.5f) : s);
                case LFEl.Earth: return tall ? new Vector3(s.x * 1.4f, s.y * 0.75f, s.z * 1.4f) : (small ? new Vector3(s.x * 1.2f, s.y * 0.42f, s.z * 1.2f) : s);
                case LFEl.Fire: return tall ? new Vector3(s.x * 2.0f, s.y * 1.05f, s.z * 2.0f) : (small ? new Vector3(s.x * 1.4f, s.y * 0.8f, s.z * 1.4f) : s * 0.8f);
            }
            return s;
        }
        public static void SetAccents(Look l) { A1 = LFUtil.Hex(l.accent1); A2 = LFUtil.Hex(l.accent2); A3 = Color.Lerp(A1, Color.white, 0.25f); }
    }
}
