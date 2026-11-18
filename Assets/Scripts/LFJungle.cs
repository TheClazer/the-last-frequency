using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    [Serializable] public class JPath { public float[] p; }
    [Serializable] public class JAnchors { public float[] sun, statue, temple, courtyard; public JPath[] falls, rivers; public float floor; }
    [Serializable] public class JInst { public float[] TTree_A, TTree_B, TRock_A, TCliff_A; }

    public static class LFMeshIO
    {
        public static Mesh Load(string res)
        {
            var ta = Resources.Load<TextAsset>(res); if (ta == null) { Debug.LogError("[LF] missing mesh " + res); return null; }
            var b = ta.bytes; int nv = BitConverter.ToInt32(b, 4), nf = BitConverter.ToInt32(b, 8), o = 12;
            var f = new float[nv * 3]; Buffer.BlockCopy(b, o, f, 0, nv * 12); o += nv * 12; var P = new Vector3[nv]; for (int i = 0; i < nv; i++) P[i] = new Vector3(f[i * 3], f[i * 3 + 1], f[i * 3 + 2]);
            Buffer.BlockCopy(b, o, f, 0, nv * 12); o += nv * 12; var N = new Vector3[nv]; for (int i = 0; i < nv; i++) N[i] = new Vector3(f[i * 3], f[i * 3 + 1], f[i * 3 + 2]);
            var C = new Color32[nv]; for (int i = 0; i < nv; i++) C[i] = new Color32(b[o + i * 4], b[o + i * 4 + 1], b[o + i * 4 + 2], b[o + i * 4 + 3]); o += nv * 4;
            var u = new float[nv * 2]; Buffer.BlockCopy(b, o, u, 0, nv * 8); o += nv * 8; var U0 = new Vector2[nv]; for (int i = 0; i < nv; i++) U0[i] = new Vector2(u[i * 2], u[i * 2 + 1]);
            Buffer.BlockCopy(b, o, u, 0, nv * 8); o += nv * 8; var U1 = new Vector2[nv]; for (int i = 0; i < nv; i++) U1[i] = new Vector2(u[i * 2], u[i * 2 + 1]);
            var idx = new int[nf * 3]; var ui = new uint[nf * 3]; Buffer.BlockCopy(b, o, ui, 0, nf * 12); for (int i = 0; i < idx.Length; i++) idx[i] = (int)ui[i];
            var m = new Mesh { name = res }; m.indexFormat = nv > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(P); m.SetNormals(N); m.SetColors(C); m.SetUVs(0, U0); m.SetUVs(1, U1); m.SetIndices(idx, MeshTopology.Triangles, 0); m.RecalculateBounds(); m.RecalculateTangents(); m.UploadMeshData(true);
            return m;
        }
    }

    /// <summary>THE JUNGLE: baked-lit terrain with cliffs, waterfalls and a river; a colossal guardian statue, temple arches, a ruined bridge, trees and rocks (Tripo assets cleaned in Blender);
    /// golden-hour sky, mist, light shafts. The world builds itself: void, then a drawing, then paint, then depth, then the real lit place (Opening).</summary>
    public class LFJungle : MonoBehaviour
    {
        public static LFJungle I;
        Transform _root; JAnchors _a; Vector3 _sun; Material _vcMat; readonly List<Renderer> _statics = new List<Renderer>(); readonly List<GameObject> _live = new List<GameObject>();
        class Shaft { public Transform tf; public float s; } readonly List<Shaft> _shafts = new List<Shaft>(); ParticleSystem _mist; Transform _sky; Camera _cam; public Transform Rig; Vector3 _rigBase;
        readonly List<Transform> _grown = new List<Transform>(); float _fogBase = 0.0021f; public bool Built; float _driftT; public LFLife Life;
        readonly float[][] _rev = { new float[12], new float[12], new float[12], new float[12] }, _tgt = { new float[12], new float[12], new float[12], new float[12] }; readonly float[] _spd = { 0.07f, 0.06f, 0.10f, 0.06f };
        bool _revOn, _allLife, _flyDone; readonly List<ParticleSystem> _mistPS = new List<ParticleSystem>(); public int Pushes; float _stair, _stairT; float _climb, _climbT; public float Climbed { get { return _climb; } }
        public float TimeOfDay = 0.72f, RainAmt, FogAmt; ParticleSystem _rain; public bool MixedReality; public Vector3 Hand0, Hand1; public float HandRadius = 0.45f;
        public float StairProgress { get { return _stair; } } public bool Climbing { get { return _stairT > 0.3; } }
        public static int SectorOf(float az) { return Mathf.RoundToInt((az / (2f * Mathf.PI) + 0.5f) * 12f) % 12; }
        public void StartVoid() { _revOn = true; for (int l = 0; l < 4; l++) for (int k = 0; k < 12; k++) { _rev[l][k] = 0f; _tgt[l][k] = 0f; } _stair = _stairT = _climb = _climbT = 0f; _allLife = _flyDone = false; Pushes = 0; ShowAll(true); SetStatics(true); SetLive(true); UpdateReveal(0f, 0f); }
        void Add(int layer, int s, float amt) { for (int off = -2; off <= 2; off++) { int j = (s + off + 12) % 12; float w = off == 0 ? 1f : (Mathf.Abs(off) == 1 ? 0.6f : 0.3f); _tgt[layer][j] = Mathf.Min(1f, _tgt[layer][j] + amt * w); } }
        /// <summary>one hand pushed outward at azimuth az (0 = +Z): first the ground and rocks rise around that direction, a little at a time; once those are there the trees grow</summary>
        public int PushOne(float az) { int s = SectorOf(az); Pushes++; int layer = _tgt[0][s] < 0.8f ? 0 : 1; Add(layer, s, 0.2f); if (layer == 1 && _tgt[1][s] > 0.6f && Life != null) StartCoroutine(ArriveLater(s, 6f)); return layer; }
        /// <summary>both hands pushed together: the stairs appear, a few steps at a time, toward the statue</summary>
        public void PushBoth() { _stairT = Mathf.Min(1f, _stairT + 0.26f); Pushes++; }
        /// <summary>a hand lifted skyward: water pours from the top of the cliffs in that direction</summary>
        public void Lift(float az) { Add(2, SectorOf(az), 0.5f); }
        public void Climb(float meters) { float maxM = Mathf.Max(0f, (_stair * 1.12f - 0.04f) * 62f + 6f - 4f); _climbT = Mathf.Clamp(_climbT + meters, 0f, Mathf.Min(maxM, 49f)); }
        static float DeckY(float z) { return z <= 6.4f ? 0f : (z <= 40f ? 5f * Mathf.Pow((z - 6.4f) / 33.6f, 2f) : 5f); }
        static IEnumerator Nothing() { yield break; }
        IEnumerator ArriveLater(int s, float d) { yield return new WaitForSeconds(d); if (Life != null) Life.Arrive(s, false); }
        /// <summary>a hesitant singer still gets the world, but slowly: the song keeps drawing in the background</summary>
        public void AutoAssist(float t)
        {
            if (!_revOn) return; float u = Mathf.Clamp01((t - 30f) / 60f) * 0.55f; for (int k = 0; k < 12; k++) _tgt[0][k] = Mathf.Max(_tgt[0][k], u); u = Mathf.Clamp01((t - 60f) / 60f) * 0.5f; for (int k = 0; k < 12; k++) _tgt[1][k] = Mathf.Max(_tgt[1][k], u);
            if (t > 95f) _stairT = Mathf.Max(_stairT, Mathf.Clamp01((t - 95f) / 20f)); if (t > 80f) for (int k = 0; k < 12; k++) _tgt[2][k] = Mathf.Max(_tgt[2][k], Mathf.Clamp01((t - 80f) / 40f) * 0.7f);
            if (_climb > 30f && !_allLife && Life != null) { _allLife = true; Life.Arrive(0, true); } if (_climb > 38f && !_flyDone && Life != null) { _flyDone = true; Life.Butterflies(Rig != null ? Rig.position : Vector3.zero); }
        }
        void UpdateReveal(float dt, float boost)
        {
            float mean = 0f; for (int l = 0; l < 3; l++) for (int k = 0; k < 12; k++) { _rev[l][k] = Mathf.MoveTowards(_rev[l][k], _tgt[l][k], dt * _spd[l] * (1f + boost * 1.2f)); if (l == 2) mean += _rev[2][k] / 12f; }
            _stair = Mathf.MoveTowards(_stair, _stairT, dt * _spd[3]); for (int k = 0; k < 12; k++) _rev[3][k] = _stair;
            Shader.SetGlobalFloatArray("_LFRev0", _rev[0]); Shader.SetGlobalFloatArray("_LFRev1", _rev[1]); Shader.SetGlobalFloatArray("_LFRev2", _rev[2]); Shader.SetGlobalFloatArray("_LFRev3", _rev[3]);
            Shader.SetGlobalVector("_LFRevOrigin", Vector4.zero); Shader.SetGlobalVector("_LFRevCfg", new Vector4(_revOn ? 1f : 0f, mean, MixedReality ? 1f : 0f, 0f));
            Shader.SetGlobalVector("_LFHand0", new Vector4(Hand0.x, Hand0.y, Hand0.z, MixedReality ? HandRadius : 0f)); Shader.SetGlobalVector("_LFHand1", new Vector4(Hand1.x, Hand1.y, Hand1.z, MixedReality ? HandRadius : 0f));
            foreach (var ps in _mistPS) { var em = ps.emission; em.rateOverTime = 6f * mean * (1f + FogAmt * 3f); }
            _climb = Mathf.MoveTowards(_climb, _climbT, dt * 1.6f);
        }
        public float Revealed { get { float m = 0; for (int k = 0; k < 12; k++) m += (_rev[0][k] + _rev[1][k] + _rev[2][k]) / 36f; return m; } }
        public Vector3 SunDir { get { return _sun; } }

        public void Build(Camera cam)
        {
            I = this; _cam = cam; Rig = cam.transform.root; _rigBase = Rig.position; _root = new GameObject("Jungle").transform; _root.SetParent(transform, false);
            _a = JsonUtility.FromJson<JAnchors>(Resources.Load<TextAsset>("Jungle/anchors").text); _sun = new Vector3(_a.sun[0], _a.sun[1], _a.sun[2]).normalized;
            ApplyGlobals(0.5f);
            var lit = Shader.Find("LF/Lit");
            _vcMat = new Material(lit) { name = "JVC" }; _vcMat.SetFloat("_Gloss", 0.06f); _vcMat.SetFloat("_Layer", 0f);
            var bridgeMat = new Material(lit) { name = "JBridge" }; bridgeMat.SetFloat("_Layer", 3f); bridgeMat.SetFloat("_Gloss", 0.05f); var templeMat = new Material(lit) { name = "JTemple" }; templeMat.SetFloat("_Layer", 1f); AddMesh("terrain", _vcMat); AddMesh("bridge", bridgeMat); AddMesh("temple", templeMat);
            AddMesh("statue", TexMat("GiantGuardian", 0.9f)); AddMesh("arches", TexMat("TempleArch", 0.9f));
            var inst = JsonUtility.FromJson<JInst>(Resources.Load<TextAsset>("Jungle/instances").text);
            Scatter("TTree_A", inst.TTree_A, 0.55f); Scatter("TTree_B", inst.TTree_B, 0.55f); Scatter("TRock_A", inst.TRock_A, 0f); Scatter("TCliff_A", inst.TCliff_A, 0f);
            // sky dome
            var sky = new GameObject("Sky"); sky.transform.SetParent(_root, false); sky.AddComponent<MeshFilter>().sharedMesh = SphereMesh(); var smr = sky.AddComponent<MeshRenderer>(); smr.sharedMaterial = new Material(Shader.Find("LF/SkyDay")); smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sky.transform.localScale = Vector3.one * 280f; _sky = sky.transform; _statics.Add(smr);
            // water: two falls, two river arms
            foreach (var f in _a.falls) MakeRibbon(f.p, 5.5f, 11f, true); foreach (var r in _a.rivers) MakeRibbon(r.p, 9f, 9f, false);
            MakeShafts(); MakeMist(); Life = new GameObject("Life").AddComponent<LFLife>(); Life.transform.SetParent(_root, false); Life.Init(); foreach (var g in _live) { var ps = g.GetComponent<ParticleSystem>(); if (ps != null) _mistPS.Add(ps); } Built = true; SetLive(false);
        }

        static Mesh _sph; static Mesh SphereMesh() { if (_sph != null) return _sph; var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); _sph = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); return _sph; }

        Material TexMat(string name, float gloss)
        {
            var m = new Material(Shader.Find("LF/Lit")) { name = name }; m.SetFloat("_Layer", name.StartsWith("TTree") ? 1f : ((name == "GiantGuardian" || name == "TempleArch") ? 3f : 0f)); m.SetFloat("_UseTex", 1f); m.SetTexture("_Albedo", Resources.Load<Texture2D>("Jungle/" + name + "_a")); m.SetTexture("_Normal", Resources.Load<Texture2D>("Jungle/" + name + "_n")); m.SetFloat("_Gloss", 0.08f); m.SetFloat("_NStr", 1.2f); return m;
        }
        void AddMesh(string res, Material mat)
        {
            var m = LFMeshIO.Load("Jungle/" + res); if (m == null) return; var g = new GameObject(res); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; _statics.Add(mr);
        }
        void Scatter(string res, float[] flat, float sway)
        {
            if (flat == null) return; var mesh = LFMeshIO.Load("Jungle/" + res); var mat = TexMat(res, 0.05f); mat.SetFloat("_Sway", sway * 0.9f); if (res.StartsWith("TTree")) mat.SetColor("_Tint", new Color(1.05f, 1.1f, 0.95f));
            for (int i = 0; i + 4 < flat.Length; i += 5)
            {
                var g = new GameObject(res); g.transform.SetParent(_root, false); g.transform.position = new Vector3(flat[i], flat[i + 1], flat[i + 2]); g.transform.rotation = Quaternion.Euler(0f, -flat[i + 3] * Mathf.Rad2Deg, 0f); g.transform.localScale = Vector3.one * flat[i + 4];
                g.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; _statics.Add(mr);
            }
        }
        void MakeRibbon(float[] p, float w0, float w1, bool fall)
        {
            int n = p.Length / 3; var V = new Vector3[n * 2]; var UV = new Vector2[n * 2]; var T = new List<int>(); float len = 0; Vector3 prev = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 c = new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]); Vector3 nx = new Vector3(p[Mathf.Min(i + 1, n - 1) * 3], p[Mathf.Min(i + 1, n - 1) * 3 + 1], p[Mathf.Min(i + 1, n - 1) * 3 + 2]); Vector3 d = (nx - new Vector3(p[Mathf.Max(i - 1, 0) * 3], p[Mathf.Max(i - 1, 0) * 3 + 1], p[Mathf.Max(i - 1, 0) * 3 + 2])); d.y = 0; if (d.sqrMagnitude < 1e-4f) d = Vector3.forward; d.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, d); float w = Mathf.Lerp(w0, w1, i / (float)(n - 1)) * 0.5f; if (i > 0) len += (c - prev).magnitude; prev = c;
                V[i * 2] = c - side * w; V[i * 2 + 1] = c + side * w; UV[i * 2] = new Vector2(0, len / 8f); UV[i * 2 + 1] = new Vector2(1, len / 8f);
                if (i < n - 1) { int a = i * 2; T.Add(a); T.Add(a + 2); T.Add(a + 1); T.Add(a + 1); T.Add(a + 2); T.Add(a + 3); }
            }
            var m = new Mesh(); m.SetVertices(V); m.SetUVs(0, UV); m.SetTriangles(T, 0); m.RecalculateBounds();
            var g = new GameObject(fall ? "Fall" : "River"); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("LF/Water")); mat.SetFloat("_Fall", fall ? 1f : 0f); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _live.Add(g);
            if (fall) { var bp = new Vector3(p[(n - 1) * 3], p[(n - 1) * 3 + 1], p[(n - 1) * 3 + 2]); var ps = MakeMistPS("FallMist", bp + Vector3.up * 3f, 7f, 14f, 0.5f); _live.Add(ps.gameObject); }
        }
        ParticleSystem MakeMistPS(string name, Vector3 pos, float rad, float size, float alpha)
        {
            var g = new GameObject(name); g.transform.SetParent(_root, false); g.transform.position = pos; var ps = g.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = true; m.maxParticles = 60; m.startLifetime = 9f; m.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size); m.startSpeed = 0.5f; m.startColor = new Color(0.95f, 0.97f, 0.95f, 0.07f * alpha); m.simulationSpace = ParticleSystemSimulationSpace.World; m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            var e = ps.emission; e.rateOverTime = 6f; var s = ps.shape; s.shapeType = ParticleSystemShapeType.Circle; s.radius = rad; s.rotation = new Vector3(-90, 0, 0);
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(0.6f, 1.6f); v.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = new Material(Shader.Find("LF/Mist")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ps.Play(); return ps;
        }
        void MakeMist()
        {
            _mist = MakeMistPS("ValleyMist", new Vector3(0, -12f, 55f), 70f, 40f, 0.55f); _live.Add(_mist.gameObject);
            var m2 = MakeMistPS("ValleyMist2", new Vector3(-40, -10f, 10f), 60f, 36f, 0.4f); _live.Add(m2.gameObject);
        }
        void MakeShafts()
        {
            var shMat = new Material(Shader.Find("LF/Shaft")); var q = new Mesh(); q.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0) }; q.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) }; q.triangles = new[] { 0, 2, 1, 1, 2, 3 }; q.bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            var rnd = new System.Random(5);
            for (int i = 0; i < 14; i++)
            {
                var g = new GameObject("Shaft" + i); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>().sharedMesh = q; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = shMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                float x = (float)(rnd.NextDouble() * 2 - 1) * 55f, z = 25f + (float)rnd.NextDouble() * 85f; g.transform.position = new Vector3(x, -8f, z); float w = 5f + (float)rnd.NextDouble() * 9f; _shafts.Add(new Shaft { tf = g.transform, s = w }); _live.Add(g);
            }
        }

        // ------------------------------------------------------------------------------------ lighting globals
        public void ApplyGlobals(float intensity)
        {
            float tod = TimeOfDay; float elev = Mathf.Sin((tod - 0.25f) * Mathf.PI * 1.4f); float night = Mathf.Clamp01(-elev * 3f + 0.0f) * (tod > 0.95f || tod < 0.1f ? 1f : 0f); night = Mathf.Max(night, Mathf.Clamp01((tod - 0.9f) * 10f));
            Vector3 az = new Vector3(_a.sun[0], 0, _a.sun[2]).normalized; float el = Mathf.Lerp(0.12f, 0.95f, Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(tod / 0.9f) * Mathf.PI))); _sun = (az * Mathf.Cos(el) + Vector3.up * Mathf.Sin(el)).normalized; float warm = 1f - Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(tod / 0.9f) * Mathf.PI) * 1.4f);
            Color sunC = Color.Lerp(new Color(1f, 0.95f, 0.85f), new Color(1f, 0.55f, 0.28f), warm) * (1f - night * 0.92f) * (1f - RainAmt * 0.6f); Color ambT = Color.Lerp(new Color(0.34f, 0.5f, 0.74f), new Color(0.07f, 0.1f, 0.22f), night) * (1f - RainAmt * 0.35f);
            Shader.SetGlobalVector("_LFSunCol", new Vector4(sunC.r, sunC.g, sunC.b, 1) * 1.7f); Shader.SetGlobalVector("_LFAmbTop", new Vector4(ambT.r, ambT.g, ambT.b, 1) * 0.95f);
            Shader.SetGlobalVector("_LFSunDir", new Vector4(_sun.x, _sun.y, _sun.z, 0));
            Shader.SetGlobalVector("_LFAmbBot", new Vector4(0.24f, 0.30f, 0.13f, 1) * (1f - night * 0.85f));
            Color fogC = Color.Lerp(new Color(0.66f, 0.72f, 0.70f), new Color(0.05f, 0.07f, 0.14f), night); fogC = Color.Lerp(fogC, new Color(0.45f, 0.5f, 0.52f) * (1f - night * 0.8f), RainAmt * 0.6f); Shader.SetGlobalVector("_LFFogCol", new Vector4(fogC.r, fogC.g, fogC.b, night)); Shader.SetGlobalVector("_LFFog", new Vector4(_fogBase * Mathf.Lerp(1.2f, 0.6f, intensity) * (1f + FogAmt * 3.5f + RainAmt * 1.2f), 0.05f, 0.7f * (1f - night), 1.12f));
        }
        public void SetLive(bool on) { foreach (var g in _live) if (g != null) g.SetActive(on); }
        public List<Renderer> Statics { get { return _statics; } }
        public void SetStatics(bool on) { foreach (var r in _statics) if (r != null) r.enabled = on; }
        public void ShowAll(bool on) { _root.gameObject.SetActive(on); }

        /// <summary>every frame: sun/fog follow the song, shafts face the camera, the rig glides slowly so the world reads as 3D (not a backdrop)</summary>
        public void Tick(float intensity, float dt, bool drift)
        {
            UpdateReveal(dt, intensity);
            ApplyGlobals(intensity); if (_sky != null && _cam != null) _sky.position = _cam.transform.position;
            Vector3 cp = _cam.transform.position;
            for (int i = 0; i < _shafts.Count; i++)
            {
                var sh = _shafts[i]; Vector3 toCam = cp - sh.tf.position; Vector3 f = toCam - Vector3.Dot(toCam, _sun) * _sun; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
                sh.tf.rotation = Quaternion.LookRotation(f, _sun); sh.tf.localScale = new Vector3(sh.s, 95f, 1f);
            }
            if (_rain == null) MakeRain(); { var em = _rain.emission; em.rateOverTime = RainAmt * 900f; _rain.transform.position = cp + Vector3.up * 9f; if (RainAmt > 0.02f && !_rain.isPlaying) _rain.Play(); }
            if (Rig != null) { _driftT += dt; Vector3 sway = new Vector3(Mathf.Sin(_driftT * 0.09f) * 0.12f, Mathf.Sin(_driftT * 0.13f) * 0.04f, Mathf.Cos(_driftT * 0.06f) * 0.1f); Rig.position = _rigBase + new Vector3(0f, DeckY(_climb), _climb) + sway * (LFComfort.Calm ? 0.2f : 1f); }
            if (false && drift && Rig != null) { _driftT += dt; Rig.position = _rigBase + new Vector3(Mathf.Sin(_driftT * 0.09f) * 1.6f, Mathf.Sin(_driftT * 0.13f) * 0.22f, Mathf.Cos(_driftT * 0.06f) * 1.2f + Mathf.Min(_driftT * 0.035f, 2.2f)); }
        }

        void MakeRain()
        {
            var g = new GameObject("Rain"); g.transform.SetParent(_root, false); _rain = g.AddComponent<ParticleSystem>(); _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var m = _rain.main; m.loop = true; m.maxParticles = 4000; m.startLifetime = 1.0f; m.startSpeed = 0f; m.startSize = 0.05f; m.startColor = new Color(0.8f, 0.88f, 1f, 0.5f); m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = _rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(24f, 0.1f, 24f); var v = _rain.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(-16f, -12f); v.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.8f);
            var r = g.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 5f; r.velocityScale = 0.04f; r.sharedMaterial = new Material(Shader.Find("LF/Mote")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void ClimbFinale() { if (Life != null) { Life.Arrive(0, true); Life.Butterflies(Rig.position); } }
        // ------------------------------------------------------------------------------------ gesture-grown trees (triangle)
        readonly Mesh[] _treeMesh = new Mesh[2]; Material _treeMat;
        public void GrowTree(Vector3 floorPos)
        {
            // meshes and the material are made once: parsing a tree mesh (and computing tangents) on every drawn shape caused a hitch per gesture
            int which = UnityEngine.Random.value < 0.5f ? 0 : 1; if (_treeMesh[which] == null) _treeMesh[which] = LFMeshIO.Load("Jungle/" + (which == 0 ? "TTree_A" : "TTree_B")); if (_treeMat == null) _treeMat = TexMat("TTree_A", 0.05f);
            var g = new GameObject("GrownTree"); g.transform.SetParent(_root, false); g.transform.position = floorPos; g.AddComponent<MeshFilter>().sharedMesh = _treeMesh[which];
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = _treeMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; StartCoroutine(Grow(g.transform, UnityEngine.Random.Range(0.5f, 0.9f)));
        }
        IEnumerator Grow(Transform t, float target) { float u = 0; while (u < 1f) { u += Time.deltaTime / 2.2f; t.localScale = Vector3.one * target * LFUtil.EaseOutBack(u); yield return null; } }

        // ------------------------------------------------------------------------------------ THE OPENING: void -> pencil drawing -> paint -> depth -> real
        public IEnumerator Opening(LFApp app, float length)
        {
            var cam = _cam; var uiOff = app.SetOverlays(false); SetLive(false); SetStatics(true); ShowAll(true); ApplyGlobals(0.35f);
            Color paperCol = new Color(0.95f, 0.91f, 0.82f), voidCol = new Color(0.015f, 0.014f, 0.02f);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; yield return null; yield return null;
            var painter = new Material(Shader.Find("Hidden/RoomIsLying/V2Painter")); var proj = new Material(Shader.Find("RoomIsLying/V2/Projected"));
            var raw = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); var smooth = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32); var paint = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32); paint.filterMode = FilterMode.Bilinear;
            painter.SetColor("_Paper", paperCol); painter.SetFloat("_Sat", 1.5f); painter.SetFloat("_Warm", 0.5f);
            var capGo = new GameObject("PaintCapture"); var cap = capGo.AddComponent<Camera>(); cap.fieldOfView = 90f; cap.aspect = 16f / 9f; cap.nearClipPlane = 0.05f; cap.farClipPlane = 3000f; cap.clearFlags = CameraClearFlags.SolidColor; cap.backgroundColor = Color.gray;
            capGo.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation); cap.targetTexture = raw; var cad = capGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); cad.renderPostProcessing = false;
            yield return null; cap.Render(); Matrix4x4 vp = cap.projectionMatrix * cap.worldToCameraMatrix; Vector3 capPos = capGo.transform.position; Destroy(capGo);
            Graphics.Blit(raw, smooth, painter, 0); Shader.SetGlobalMatrix("_V2ProjVP", vp); Shader.SetGlobalVector("_V2ProjPos", capPos); proj.SetTexture("_Paint", paint); painter.SetFloat("_Draw", 0); painter.SetFloat("_Tone", 0); painter.SetFloat("_Color", 0); Graphics.Blit(smooth, paint, painter, 1);
            var copies = new List<GameObject>();
            foreach (var r in _statics) { var mr0 = r as MeshRenderer; if (mr0 == null) continue; var mf = r.GetComponent<MeshFilter>(); if (mf == null) continue; var c = new GameObject("_Paint"); c.transform.SetParent(r.transform, false); c.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh; var mr = c.AddComponent<MeshRenderer>(); mr.sharedMaterial = proj; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; copies.Add(c); }
            SetStatics(false); cam.backgroundColor = paperCol; var rigStart = Rig.position; bool dissolving = false; float t = 0f; LFLog.Ev("music", "opening", 0f);
            while (t < length)
            {
                t += Time.deltaTime; float k = t / 46f * (46f / length); float tt = t * 46f / length;
                float draw = Mathf.Clamp01((tt - 3f) / 15f), tone = Mathf.Clamp01((tt - 12f) / 12f), colr = Mathf.Clamp01((tt - 20f) / 11f);
                painter.SetFloat("_Draw", draw); painter.SetFloat("_Tone", tone); painter.SetFloat("_Color", colr); Graphics.Blit(smooth, paint, painter, 1);
                float fade = Mathf.SmoothStep(0, 1, tt / 3.5f); cam.backgroundColor = Color.Lerp(Color.black, Color.Lerp(paperCol, voidCol, Mathf.Clamp01((tt - 30f) / 6f)), fade); proj.SetColor("_Void", Color.Lerp(paperCol, voidCol, Mathf.Clamp01((tt - 30f) / 6f)) * fade);
                float m = Mathf.SmoothStep(0, 1, (tt - 28f) / 11f); Rig.position = rigStart + new Vector3(Mathf.Sin(m * 2.4f) * 1.4f, m * 0.5f, m * 3.0f);
                proj.SetFloat("_Dissolve", Mathf.Clamp01((tt - 37f) / 9f));
                if (tt >= 37f && !dissolving) { dissolving = true; SetStatics(true); cam.clearFlags = CameraClearFlags.Skybox; LFLog.Ev("fx", "dissolve", 9f); }
                yield return null;
            }
            foreach (var c in copies) if (c != null) Destroy(c); raw.Release(); smooth.Release(); paint.Release(); SetStatics(true); SetLive(true); cam.clearFlags = CameraClearFlags.Skybox; app.SetOverlays(true); _rigBase = Rig.position;
        }
    }
}
