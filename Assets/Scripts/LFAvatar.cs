using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>The singer as a faceless spirit: a floating figure whose head, torso and hands follow the recorded head and fingertips (two-bone arms), whose gown and ribbons glow with the voice, and who wears one of three outfits.
    /// No face, no likeness: it moves like you and sings like you.</summary>
    public class LFAvatar : MonoBehaviour
    {
        public static readonly string[] OutfitNames = { "EXPLORER", "PRIESTESS", "STAR ROBE" };
        static readonly Color[][] Pal = {
            new[] { LFUtil.Hex("#1f5a2c"), LFUtil.Hex("#e9c36a"), LFUtil.Hex("#ffd27a") },      // explorer: moss gown, gold
            new[] { LFUtil.Hex("#f4e9d0"), LFUtil.Hex("#c7372f"), LFUtil.Hex("#f2c14e") },      // priestess: ivory, crimson, gold
            new[] { LFUtil.Hex("#141a55"), LFUtil.Hex("#7d8bff"), LFUtil.Hex("#e6ecff") } };    // star robe: indigo, periwinkle, silver
        public const int ShadowLayer = 30; public static bool ShadowLook;   // the film: a black shadow figure with white glowing ribbons, instead of the coloured spirit
        Transform _head, _torso; Transform[] _arm = new Transform[4], _hand = new Transform[2], _sleeveGlow = new Transform[2]; Material _cloth, _skin; Transform _mantle; TrailRenderer[] _rib = new TrailRenderer[4]; Transform[] _crown;
        Vector3 _hp, _hv; float _yaw; Quaternion _hr = Quaternion.identity; int _outfit = -1; bool _built;
        const float L1 = 0.33f, L2 = 0.35f;

        static Mesh _limb;
        static Mesh Limb()
        {
            if (_limb != null) return _limb; int n = 10; var V = new List<Vector3>(); var T = new List<int>(); var C = new List<Color>(); var UV = new List<Vector2>();
            for (int k = 0; k <= 1; k++) for (int i = 0; i < n; i++) { float a = i / (float)n * Mathf.PI * 2f; float r = k == 0 ? 1f : 0.8f; V.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, k)); C.Add(new Color(k * 0.8f + 0.1f, 0f, 0f, 1f)); UV.Add(new Vector2(i / (float)n, k)); }
            for (int i = 0; i < n; i++) { int a = i, b = (i + 1) % n; T.AddRange(new[] { a, a + n, b, b, a + n, b + n }); }
            _limb = new Mesh(); _limb.SetVertices(V); _limb.SetColors(C); _limb.SetUVs(0, UV); _limb.SetTriangles(T, 0); _limb.RecalculateNormals(); _limb.RecalculateBounds(); return _limb;
        }
        Mesh Gown() { return GownMesh(new float[,] { { 0.07f, 0f }, { 0.13f, -0.07f }, { 0.20f, -0.2f }, { 0.17f, -0.38f }, { 0.20f, -0.55f }, { 0.28f, -0.75f }, { 0.4f, -0.95f }, { 0.55f, -1.1f } }, true); }
        Mesh GownMesh(float[,] prof, bool hemRuffle)
        {
            int rings = prof.GetLength(0), seg = 28; var V = new List<Vector3>(); var C = new List<Color>(); var UV = new List<Vector2>(); var T = new List<int>();
            for (int i = 0; i < rings; i++) for (int s = 0; s < seg; s++) { float a = s / (float)seg * Mathf.PI * 2f; float r = prof[i, 0] * (1f + (hemRuffle && i == rings - 1 ? 0.12f * Mathf.Sin(a * 7f) : 0f)); V.Add(new Vector3(Mathf.Cos(a) * r, prof[i, 1] - (hemRuffle && i == rings - 1 ? 0.06f * Mathf.Sin(a * 7f) : 0f), Mathf.Sin(a) * r * 0.9f)); C.Add(new Color(1f - i / (float)(rings - 1) * 0.9f, i / (float)(rings - 1), 0, 1)); UV.Add(new Vector2(s / (float)seg, i / (float)(rings - 1))); }
            for (int i = 0; i < rings - 1; i++) for (int s = 0; s < seg; s++) { int a = i * seg + s, b = i * seg + (s + 1) % seg, c = a + seg, d = b + seg; T.AddRange(new[] { a, c, b, b, c, d }); }
            var m = new Mesh(); m.SetVertices(V); m.SetColors(C); m.SetUVs(0, UV); m.SetTriangles(T, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        void Build()
        {
            _cloth = new Material(Shader.Find("LF/Avatar")); var gm = new GameObject("Gown"); gm.transform.SetParent(transform, false); gm.AddComponent<MeshFilter>().sharedMesh = Gown(); var gr = gm.AddComponent<MeshRenderer>(); gr.sharedMaterial = _cloth; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _torso = gm.transform;
            _skin = new Material(Shader.Find("LF/Avatar")); _skin.SetColor("_ColA", new Color(0.96f, 0.88f, 0.76f)); _skin.SetColor("_ColB", new Color(1f, 0.95f, 0.85f)); _skin.SetFloat("_Pattern", 0f); _skin.SetFloat("_Glow", 0.9f);
            var mt = new GameObject("Mantle"); mt.transform.SetParent(transform, false); mt.AddComponent<MeshFilter>().sharedMesh = GownMesh(new float[,] { { 0.1f, 0.0f }, { 0.28f, -0.04f }, { 0.4f, -0.16f }, { 0.38f, -0.3f }, { 0.3f, -0.36f } }, false); var mr2 = mt.AddComponent<MeshRenderer>(); mr2.sharedMaterial = _cloth; mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _mantle = mt.transform;
            var h = new GameObject("Head"); h.transform.SetParent(transform, false); h.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("ElemSphere"); var hr = h.AddComponent<MeshRenderer>(); hr.sharedMaterial = _skin; hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; h.transform.localScale = Vector3.one * 0.3f; _head = h.transform;
            for (int i = 0; i < 4; i++) { var a = new GameObject("Arm" + i); a.transform.SetParent(transform, false); a.AddComponent<MeshFilter>().sharedMesh = Limb(); var ar = a.AddComponent<MeshRenderer>(); ar.sharedMaterial = _cloth; ar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _arm[i] = a.transform; }
            for (int i = 0; i < 2; i++) { var o = new GameObject("Hand" + i); o.transform.SetParent(transform, false); o.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("ElemSphere"); var orr = o.AddComponent<MeshRenderer>(); orr.sharedMaterial = LFMats.Toon(LFMats.Gold); orr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; o.transform.localScale = Vector3.one * 0.085f; _hand[i] = o.transform; }
            for (int i = 0; i < 4; i++) { var g = new GameObject("Ribbon" + i); g.transform.SetParent(transform, false); var tr = g.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.3f; tr.minVertexDistance = 0.03f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.09f), new Keyframe(1, 0f)); _rib[i] = tr; }
            _crown = new Transform[9]; for (int i = 0; i < _crown.Length; i++) { var c = new GameObject("Crown" + i); c.transform.SetParent(transform, false); c.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("spire3"); var cr = c.AddComponent<MeshRenderer>(); cr.sharedMaterial = LFMats.Toon(LFMats.Gold); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _crown[i] = c.transform; }
            _built = true;
            if (ShadowLook)
            {
                var sm = new Material(Shader.Find("LF/Shadow")) { name = "LFShadow" };
                var smc = new Material(sm) { name = "LFShadowCloth" }; smc.SetFloat("_Flutter", 1f);
                foreach (var mr in GetComponentsInChildren<MeshRenderer>(true)) mr.sharedMaterial = (mr.transform == _torso || mr.transform == _mantle) ? smc : sm;
                foreach (var c in _crown) c.gameObject.SetActive(false);
                foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ShadowLayer;   // drawn only by the corner camera
            }
        }
        public void SetOutfit(int o)
        {
            if (!_built) Build(); o = Mathf.Clamp(o, 0, 2); if (o == _outfit && !ShadowLook) return; _outfit = o; var p = Pal[o];
            _cloth.SetColor("_ColA", p[0]); _cloth.SetColor("_ColB", p[1]); _cloth.SetColor("_Trim", p[2]); _cloth.SetFloat("_Pattern", o == 2 ? 0.4f : 1f);
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.Lerp(p[1], Color.white, 0.5f), 0f), new GradientColorKey(p[1], 0.5f), new GradientColorKey(p[0] * 1.5f, 1f) }, new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) }); foreach (var r in _rib) r.colorGradient = gr;
            if (ShadowLook) { var wg = new Gradient(); wg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }); foreach (var r in _rib) { r.colorGradient = wg; r.time = 2.2f; } }
            int cm = o == 0 ? LFMats.Gold : (o == 1 ? LFMats.Magenta : LFMats.Cyan); foreach (var c in _crown) c.GetComponent<MeshRenderer>().sharedMaterial = LFMats.Toon(cm);
            _skin.SetColor("_Trim", p[2]);
        }
        public void Show(bool on) { gameObject.SetActive(on); }

        /// <summary>head world pose, each hand's world position (or null for a resting hand), pinch states, voice level</summary>
        public void Pose(Vector3 headPos, Quaternion headRot, Vector3? left, Vector3? right, float level, float pitch01, float dt)
        {
            if (!_built) SetOutfit(0);
            float ty = headRot.eulerAngles.y; _yaw = Mathf.LerpAngle(_yaw, ty, 1f - Mathf.Exp(-4f * dt)); var yawQ = Quaternion.Euler(0f, _yaw, 0f); Vector3 fwd = yawQ * Vector3.forward, rt = yawQ * Vector3.right;
            _hp = headPos; float bob = Mathf.Sin(Time.time * 1.7f) * 0.012f * (1f + level * 3f);
            _head.position = headPos + Vector3.up * (0.04f + bob); _head.localScale = Vector3.one * (0.3f + level * 0.03f); _head.rotation = headRot;
            Vector3 neck = headPos + Vector3.down * 0.17f; _torso.position = neck; _mantle.position = neck + Vector3.up * 0.0f; _mantle.rotation = yawQ; _mantle.localScale = new Vector3(1f + level * 0.08f, 1f, 1f + level * 0.08f); _torso.rotation = yawQ * Quaternion.Euler(Mathf.Sin(Time.time * 1.3f) * 2.2f * (1f + level * 2.5f), 0f, Mathf.Sin(Time.time * 0.9f) * 2.8f * (1f + level * 2.5f)); float breathe = 1f + 0.03f * Mathf.Sin(Time.time * 1.6f) + level * 0.06f; _torso.localScale = new Vector3(breathe, 1f, breathe);
            for (int i = 0; i < _crown.Length; i++) { float a = i / (float)_crown.Length * Mathf.PI * 2f; _crown[i].position = headPos + new Vector3(0, 0.19f, 0) + yawQ * new Vector3(Mathf.Sin(a) * 0.1f, 0f, Mathf.Cos(a) * 0.1f); _crown[i].localScale = new Vector3(0.05f, 0.14f + (i % 2) * 0.06f + level * 0.04f, 0.05f); _crown[i].rotation = Quaternion.Euler(Mathf.Cos(a) * 14f, 0, -Mathf.Sin(a) * 14f) * yawQ; _crown[i].gameObject.SetActive(!ShadowLook && (_outfit != 2 || i % 3 == 0)); }
            Vector3 lsh = neck + Vector3.down * 0.1f - rt * 0.2f, rsh = neck + Vector3.down * 0.1f + rt * 0.2f;
            Arm(0, lsh, left ?? (neck + fwd * 0.28f - rt * 0.22f + Vector3.down * 0.42f), -1f, rt); Arm(1, rsh, right ?? (neck + fwd * 0.28f + rt * 0.22f + Vector3.down * 0.42f), 1f, rt);
            _hand[0].position = (left ?? (neck + fwd * 0.28f - rt * 0.22f + Vector3.down * 0.42f)); _hand[1].position = (right ?? (neck + fwd * 0.28f + rt * 0.22f + Vector3.down * 0.42f));
            _rib[0].transform.position = lsh + Vector3.down * 0.05f - fwd * 0.06f; _rib[1].transform.position = rsh + Vector3.down * 0.05f - fwd * 0.06f; _rib[2].transform.position = neck + Vector3.down * 0.95f - rt * 0.3f - fwd * 0.12f; _rib[3].transform.position = neck + Vector3.down * 0.95f + rt * 0.3f - fwd * 0.12f;
            float wm = 0.6f + level * 1.6f; foreach (var r in _rib) r.widthMultiplier = wm;
        }
        void Arm(int idx, Vector3 sh, Vector3 target, float side, Vector3 rt)
        {
            Vector3 d = target - sh; float dist = Mathf.Clamp(d.magnitude, 0.08f, L1 + L2 - 0.01f); Vector3 dir = d.sqrMagnitude < 1e-6f ? Vector3.down : d.normalized; float a = (L1 * L1 - L2 * L2 + dist * dist) / (2f * dist); float h = Mathf.Sqrt(Mathf.Max(L1 * L1 - a * a, 0f));
            Vector3 hint = (Vector3.down * 0.8f + rt * side * 0.6f - dir * Vector3.Dot(Vector3.down * 0.8f + rt * side * 0.6f, dir)); if (hint.sqrMagnitude < 1e-5f) hint = rt * side; hint.Normalize(); Vector3 el = sh + dir * a + hint * h; Vector3 hand = sh + dir * dist;
            Place(_arm[idx * 2], sh, el, 0.085f); Place(_arm[idx * 2 + 1], el, hand, 0.12f);
        }
        static void Place(Transform t, Vector3 a, Vector3 b, float r) { Vector3 d = b - a; float l = d.magnitude; if (l < 1e-4f) return; t.position = a; t.rotation = Quaternion.LookRotation(d / l); t.localScale = new Vector3(r, r, l); }
    }
}
