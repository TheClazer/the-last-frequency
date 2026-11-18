using System;
using UnityEngine;

namespace LF
{
    /// <summary>The Void world. Everything starts as darkness; waves of structures spiral in from below, above and the sides; magic motes flow through the
    /// player's feet, overhead and all around; wonder-buds float within reach and burst into blooms when touched. Fixed pools, no per-frame allocation.</summary>
    public class LFWorld : MonoBehaviour
    {
        public const int MaxPieces = 72, MaxBuds = 14, MaxRings = 10;
        class Piece
        {
            public Transform tf; public MeshRenderer mr; public bool alive; public float t, dur, delay, swirl, bob, spin; public Vector3 from, to, scale; public int owner; public bool flat;
            public float rise; public bool hazed; public string kind; public Vector3 baseScale;
        }
        class Bud { public Transform tf; public MeshRenderer mr; public bool alive; public float phase, born; public Vector3 basePos; public int kind; public float pop; }
        class RingFx { public Transform tf; public Material mat; public float t, dur, size; public bool alive; }

        public event Action<Vector3, int> OnPop;        // world pos, bud kind
        public event Action<int, int> OnClaim;          // owner, total claimed by that owner
        public Pointer[] pointers;
        public Transform head;
        public int ClaimedPlayer, ClaimedRival; bool _jungle; public bool IsJungle { get { return _jungle; } } ParticleSystem _dust, _flakes, _haze; class Ribbon { public Transform tf; public TrailRenderer tr; public float ph, sp, r, hy; } Ribbon[] _rib = new Ribbon[8]; float _flakeT; int _stairShown;

        Piece[] _p = new Piece[MaxPieces]; Bud[] _b = new Bud[MaxBuds]; RingFx[] _r = new RingFx[MaxRings];
        ParticleSystem _burst, _feet, _above, _swirl; ParticleSystem.EmitParams _ep;
        Transform _floor, _stairs, _sky; MeshRenderer _floorMr; float _floorGrow; float _stairTarget, _stair; float _pulse, _intensity, _intensityT;
        System.Random _rnd = new System.Random(7); int _pieceCursor, _ringCursor; public bool VoidBuild = true; bool _floorOn;
        Transform _root;

        float R() { return (float)_rnd.NextDouble(); }
        float AvoidFront(float az) { float a = Mathf.Repeat(az + Mathf.PI, 6.2832f) - Mathf.PI; if (Mathf.Abs(a) < 0.55f) a = (a >= 0 ? 0.55f : -0.55f) + a * 0.2f; return a; }
        float R(float a, float b) { return a + (b - a) * R(); }

        public void Init(Transform headT)
        {
            head = headT; _root = new GameObject("World").transform; _root.SetParent(transform, false);
            var sky = new GameObject("Sky"); sky.transform.SetParent(_root, false); sky.transform.localScale = Vector3.one * 180f;
            sky.AddComponent<MeshFilter>().sharedMesh = SphereMesh(); sky.AddComponent<MeshRenderer>().sharedMaterial = LFMats.Sky(); _sky = sky.transform;
            var fl = new GameObject("Floor"); fl.transform.SetParent(_root, false); fl.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("disc"); _floorMr = fl.AddComponent<MeshRenderer>(); _floorMr.sharedMaterial = LFMats.Floor(); _floor = fl.transform; _floor.localScale = Vector3.zero;
            var st = new GameObject("Stairs"); st.transform.SetParent(_root, false); st.transform.rotation = Quaternion.Euler(0, 200f, 0); st.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("stairs"); st.AddComponent<MeshRenderer>().sharedMaterial = LFMats.Stair(); _stairs = st.transform;
            Shader.SetGlobalFloat(LFShaderIds.Stair, 0f);
            for (int i = 0; i < MaxPieces; i++) { var g = new GameObject("P" + i); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>(); var mr = g.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; g.SetActive(false); _p[i] = new Piece { tf = g.transform, mr = mr }; }
            for (int i = 0; i < MaxBuds; i++) { var g = new GameObject("Bud" + i); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("ElemSphere"); var mr = g.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; g.SetActive(false); _b[i] = new Bud { tf = g.transform, mr = mr }; }
            for (int i = 0; i < MaxRings; i++) { var g = new GameObject("Ring" + i); g.transform.SetParent(_root, false); g.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad"); var mr = g.AddComponent<MeshRenderer>(); var mat = LFMats.Ring(Color.white); mr.sharedMaterial = mat; g.SetActive(false); _r[i] = new RingFx { tf = g.transform, mat = mat }; }
            _feet = MakeFlow("MotesFeet", 700, 0.075f, 3.6f); _above = MakeFlow("MotesAbove", 600, 0.085f, 5f); _swirl = MakeFlow("MotesSwirl", 800, 0.07f, 6f); _burst = MakeFlow("Burst", 700, 0.09f, 1.6f); _dust = MakeFlow("HandDust", 2600, 0.045f, 1.5f); _flakes = MakeFlow("Flakes", 300, 0.22f, 3f, true); _haze = MakeFlow("Haze", 60, 2.2f, 5f);
            ConfigureFeet(_feet); ConfigureAbove(_above); ConfigureSwirl(_swirl); ConfigureBurst(_burst); ConfigureBurst(_dust); ConfigureFlakes(_flakes); ConfigureBurst(_haze);
            for (int i = 0; i < _rib.Length; i++)
            {
                var g = new GameObject("Ribbon" + i); g.transform.SetParent(_root, false); var tr = g.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 2.8f; tr.minVertexDistance = 0.012f; tr.numCapVertices = 3;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.09f), new Keyframe(0.2f, 0.055f), new Keyframe(1, 0f)); var gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#8fd6ff"), 0.35f), new GradientColorKey(LFUtil.Hex("#8a78ff"), 1f) }, new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0.22f, 0.4f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; tr.emitting = false;
                _rib[i] = new Ribbon { tf = g.transform, tr = tr, ph = i * 0.785f, sp = 0.32f + 0.1f * (i % 3), r = 1.2f + 0.34f * i, hy = 0.2f + 0.25f * (i % 4) };
            }
            SetFlow(0.0f);
        }

        static Mesh _sph;
        static Mesh SphereMesh()
        {
            if (_sph != null) return _sph; var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); _sph = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); return _sph;
        }

        ParticleSystem MakeFlow(string name, int max, float size, float life, bool flake = false)
        {
            var g = new GameObject(name); g.transform.SetParent(_root, false); var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = true; m.playOnAwake = false; m.maxParticles = max; m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life); m.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 2.0f); m.simulationSpace = ParticleSystemSimulationSpace.World;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex("#c8f2ff"), 0f), new GradientColorKey(LFUtil.Hex("#7fc4ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#cdbcff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            m.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor };
            var col = ps.colorOverLifetime; col.enabled = true; var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }); col.color = fade;
            var rend = g.GetComponent<ParticleSystemRenderer>(); rend.sharedMaterial = flake ? LFMats.Flake() : LFMats.Mote(); rend.renderMode = ParticleSystemRenderMode.Billboard; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
        void ConfigureFeet(ParticleSystem ps)
        {
            var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = 2.8f; s.radiusThickness = 0.5f; s.rotation = new Vector3(-90, 0, 0);
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(0.35f, 0.9f); v.orbitalY = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            var n = ps.noise; n.enabled = true; n.strength = 0.25f; n.frequency = 0.5f;
        }
        void ConfigureAbove(ParticleSystem ps)
        {
            var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = 4.5f; s.radiusThickness = 1f; s.position = new Vector3(0, 6f, 0); s.rotation = new Vector3(-90, 0, 0);
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(-1.4f, -0.5f); v.orbitalY = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            var n = ps.noise; n.enabled = true; n.strength = 0.4f; n.frequency = 0.3f;
        }
        void ConfigureSwirl(ParticleSystem ps)
        {
            var s = ps.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Sphere; s.radius = 4.5f; s.radiusThickness = 0.3f;
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.orbitalY = new ParticleSystem.MinMaxCurve(0.25f, 0.7f); v.orbitalOffsetY = new ParticleSystem.MinMaxCurve(0f);
            var n = ps.noise; n.enabled = true; n.strength = 0.6f; n.frequency = 0.25f;
        }
        void ConfigureFlakes(ParticleSystem ps)
        {
            var em = ps.emission; em.enabled = false; var s = ps.shape; s.enabled = false; var m = ps.main; m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var r = ps.rotationOverLifetime; r.enabled = true; r.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.25f; lim.dampen = 0.06f; var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0.85f)));
        }
        void ConfigureBurst(ParticleSystem ps)
        {
            var em = ps.emission; em.enabled = false; var s = ps.shape; s.enabled = false;
            var m = ps.main; m.gravityModifier = 0.15f;
            var v = ps.velocityOverLifetime; v.enabled = false;
        }
        float _flowGate; float _flowK; public void SetFlow(float k) { _flowK = k; ApplyFlow(); }
        void ApplyFlow()
        {
            float k = Mathf.Clamp01(_flowK) * Mathf.Clamp01((LFVoidRoom.Fade - 0.15f) / 0.5f); float jf = _jungle ? 0f : 1f; SetRate(_feet, 150f * k * jf); SetRate(_above, 110f * k * jf * 0.5f); SetRate(_swirl, 170f * k * jf);
            bool on = k > 0.01f; if (on) { if (!_feet.isPlaying) _feet.Play(); if (!_above.isPlaying) _above.Play(); if (!_swirl.isPlaying) _swirl.Play(); }
        }
        static void SetRate(ParticleSystem ps, float r) { var e = ps.emission; e.enabled = r > 0.5f; e.rateOverTime = r; }

        // ----------------------------------------------------------------- public API
        float _pitch01 = 0.5f, _lvl; public float Pitch01 { get { return _pitch01; } }
        /// <summary>Particles shrink as they come near the eyes, so a flake made in the palm never fills the view (full size from 1.2 m out, a fifth at 0.3 m).</summary>
        float NearScale(Vector3 p) { if (head == null) return 1f; float d = (p - head.position).magnitude; return Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0.3f, 1.2f, d)); }
        public void EmitDust(Vector3 p, Vector3 v, Color c, float size, float life) { size *= NearScale(p); _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = Col(c); _ep.startSize = size * Mathf.Lerp(1.5f, 0.7f, _pitch01); _ep.startLifetime = life; _dust.Emit(_ep, 1); }
        public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { size *= NearScale(p); if (_jungle) { EmitDust(p, v * 0.4f, c, size * 0.25f, life * 0.5f); return; } _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = Col(c); _ep.startSize = size; _ep.startLifetime = life; _flakes.Emit(_ep, 1); }
        public void GrowFan(Vector3 floorPos, float h, float w, int mat) { if (_jungle && LFJungle.I != null) { var fp = floorPos; fp.y = 0f; LFJungle.I.GrowTree(fp * 0.55f + new Vector3(0, 0.0f, 0)); return; } var p = NextPiece(); float wd = w * Mathf.Lerp(1.5f, 0.8f, _pitch01); Place(p, "fan", mat, floorPos + Vector3.down * (h + 1f), floorPos, new Vector3(wd, h, wd), 0f, 1.5f, 0.25f, false); }
        /// <summary>Voice character: high soprano = fine, bright, fast and thin; low = thick, slow, heavy and wide.</summary>
        public void SetTimbre(float pitch01, float level)
        {
            _pitch01 = Mathf.Lerp(_pitch01, Mathf.Clamp01(pitch01), 0.08f); _lvl = level; Shader.SetGlobalFloat(LFShaderIds.Pitch, _pitch01);
            float wm = Mathf.Lerp(1.9f, 0.55f, _pitch01) * (0.8f + level * 0.5f);
            for (int i = 0; i < _rib.Length; i++) _rib[i].tr.widthMultiplier = wm;
            float sz = Mathf.Lerp(0.95f, 0.4f, _pitch01); var m1 = _swirl.main; m1.startSizeMultiplier = sz; var m2 = _feet.main; m2.startSizeMultiplier = sz;
        }
        Color Col(Color c) { return _jungle ? Color.Lerp(c, new Color(1f, 0.82f, 0.45f), 0.62f) : c; }
        public void SetVisualTheme(bool jungle)
        {
            _jungle = jungle; if (_sky != null) _sky.gameObject.SetActive(!jungle && !_passthrough); if (jungle) { _floorOn = false; _stairTarget = 0; }
            var r = _flakes.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = jungle ? LFMats.Flower() : LFMats.Flake();
            var gr = new Gradient(); if (jungle) gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex("#fff0b8"), 0f), new GradientColorKey(LFUtil.Hex("#ffc860"), 0.5f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            else gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex("#c8f2ff"), 0f), new GradientColorKey(LFUtil.Hex("#7fc4ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#cdbcff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            foreach (var ps in new[] { _feet, _above, _swirl }) { var m = ps.main; m.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor }; }
        }
        /// <summary>Build the world from one element: palette, sky, floor, particle colours and motion, glyph shape and building shapes.</summary>
        float _floorScale = 3.4f;
        readonly Vector4[] _frost = { new Vector4(0, 0, 0, -1), new Vector4(0, 0, 0, -1), new Vector4(0, 0, 0, -1), new Vector4(0, 0, 0, -1) }; Vector4 _frostRad; int _frostI;
        static readonly int[] FrostId = { Shader.PropertyToID("_LFFrost0"), Shader.PropertyToID("_LFFrost1"), Shader.PropertyToID("_LFFrost2"), Shader.PropertyToID("_LFFrost3") }; static readonly int FrostRadId = Shader.PropertyToID("_LFFrostRad");
        /// <summary>Plant a frost seed on the floor: a snowflake of frost grows outward from it (ice element). Slot 0 is kept for the big one under the player.</summary>
        public void Frost(Vector3 pos, float radius, bool big = false)
        {
            int k = big ? 0 : 1 + (_frostI++ % 3); _frost[k] = new Vector4(pos.x, 0f, pos.z, 0f); _frostRad[k] = radius;
        }
        void FrostTick(float dt)
        {
            for (int k = 0; k < 4; k++) { if (_frost[k].w >= 0f) _frost[k].w += dt; Shader.SetGlobalVector(FrostId[k], _frost[k]); }
            Shader.SetGlobalVector(FrostRadId, _frostRad);
        }
        public void SetElement(LFEl e)
        {
            LFElements.Cur = e; var L = LFElements.Get(e); LFElements.SetAccents(L); LFMats.SetPalette(L.pal); LFMats.SetMode((int)L.kind);
            for (int pi = 0; pi < MaxPieces; pi++) { var q = _p[pi]; if (q == null || !q.alive || q.kind == null) continue; q.scale = LFElements.Shape(q.kind, q.baseScale); q.tf.GetComponent<MeshFilter>().sharedMesh = LFMeshes.Get(LFElements.Mesh(q.kind)); }   // what is already built is rebuilt in the new element
            var sky = LFMats.Sky(); sky.SetColor("_AurA", LFUtil.Hex(L.sky1)); sky.SetColor("_AurB", LFUtil.Hex(L.sky2)); sky.SetColor("_Void", LFUtil.Hex(L.skyVoid));
            var fl = LFMats.Floor(); fl.SetColor("_ColorA", LFUtil.Hex(L.floorDeep)); fl.SetColor("_ColorB", LFUtil.Hex(L.floorGlow));
            Shader.SetGlobalFloat("_LFKind", L.kind);
            if (_floorMr != null) { _floorMr.sharedMaterial = LFMats.ElemFloor(LFUtil.Hex(L.floorA), LFUtil.Hex(L.floorB)); _floorScale = 14f; }
            if (_jungle) return;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex(L.p0), 0f), new GradientColorKey(LFUtil.Hex(L.p1), 0.5f), new GradientColorKey(LFUtil.Hex(L.p2), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            foreach (var ps in new[] { _feet, _above, _swirl }) { var m = ps.main; m.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor }; }
            var vf = _feet.velocityOverLifetime; vf.y = new ParticleSystem.MinMaxCurve(L.feetY0, L.feetY1); var va = _above.velocityOverLifetime; va.y = new ParticleSystem.MinMaxCurve(L.aboveY0, L.aboveY1);
            var vs = _swirl.velocityOverLifetime; vs.orbitalY = new ParticleSystem.MinMaxCurve(L.swirl * 0.35f, L.swirl);
            foreach (var ps in new[] { _feet, _above, _swirl }) { var n = ps.noise; n.strength = L.noise; }
            var rg = new Gradient(); rg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex(L.p1), 0.35f), new GradientColorKey(LFUtil.Hex(L.p2), 1f) }, new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0.22f, 0.4f), new GradientAlphaKey(0f, 1f) });
            for (int i = 0; i < _rib.Length; i++) _rib[i].tr.colorGradient = rg;
        }
        bool _passthrough;
        /// <summary>Mixed reality: the opaque night-sky dome would hide the passthrough underlay completely, so it is hidden while passthrough is on.</summary>
        public void SetPassthrough(bool on) { _passthrough = on; if (_sky != null) _sky.gameObject.SetActive(!_jungle && !on); }
        public void Intensity(float v) { _intensityT = Mathf.Clamp01(v); }
        public void Beat(float strength) { _pulse = Mathf.Max(_pulse, strength * (LFComfort.Calm ? 0.4f : 1f)); }
        public void FloorOn(bool on) { _floorOn = on; }
        public void SetStairs(float progress) { _stairTarget = Mathf.Clamp01(progress); }
        public float Stairs { get { return _stair; } }

        public void ClearAll()
        {
            for (int i = 0; i < MaxPieces; i++) { _p[i].alive = false; _p[i].tf.gameObject.SetActive(false); }
            for (int i = 0; i < MaxBuds; i++) { _b[i].alive = false; _b[i].tf.gameObject.SetActive(false); }
            _stairTarget = 0; _stair = 0; _stairShown = 0; Shader.SetGlobalFloat(LFShaderIds.Stair, 0f); ClaimedPlayer = ClaimedRival = 0; _floorOn = false; for (int fk = 0; fk < 4; fk++) _frost[fk].w = -1f;
            _feet.Clear(); _above.Clear(); _swirl.Clear(); _burst.Clear();
        }

        Piece NextPiece() { for (int k = 0; k < MaxPieces; k++) { var p = _p[(_pieceCursor++) % MaxPieces]; if (!p.alive) return p; } var q = _p[(_pieceCursor++) % MaxPieces]; return q; }

        void Place(Piece p, string mesh, int mat, Vector3 from, Vector3 to, Vector3 scale, float delay, float dur, float swirl, bool instant)
        {
            p.alive = true; p.t = instant ? dur : 0f; p.dur = dur; p.delay = instant ? 0 : delay; p.from = from; p.to = to; p.scale = scale; p.swirl = swirl; p.bob = R(0, 6.28f); p.spin = R(-25f, 25f); p.owner = 0; p.flat = false;
            p.kind = mesh; p.baseScale = scale; scale = LFElements.Shape(mesh, scale); p.scale = scale; p.hazed = instant; p.tf.GetComponent<MeshFilter>().sharedMesh = LFMeshes.Get(LFElements.Mesh(mesh)); p.mr.sharedMaterial = LFMats.Toon(mat); p.tf.gameObject.SetActive(true);
            p.tf.localScale = instant ? scale : Vector3.zero; p.tf.position = instant ? to : from; p.tf.rotation = Quaternion.Euler(0, R(0, 360), 0);
        }

        /// <summary>kinds: spires, shards, arches, skyorbs, crown, tallspires</summary>
        public void Wave(string kind, int count, int matStart, bool instant)
        {
            float k = 0.6f + _intensityT * 0.8f;
            for (int i = 0; i < count; i++)
            {
                var p = NextPiece(); float az = R(0, 6.283f); int mat = matStart + i;
                switch (kind)
                {
                    case "spires":
                    {
                        float rad = R(4.6f, 8f), h = R(1.6f, 4.2f) * k; az = AvoidFront(az); var to = new Vector3(Mathf.Sin(az) * rad, 0, Mathf.Cos(az) * rad);
                        float wd = R(0.9f, 1.7f); Place(p, "fan", mat, to + Vector3.down * (h + 2f), to, new Vector3(wd, h, wd), i * 0.12f, R(1.6f, 2.4f), R(0.15f, 0.5f), instant); break;
                    }
                    case "tallspires":
                    {
                        float rad = 8.5f; az = i / (float)count * 6.283f + R(-0.1f, 0.1f); float h = R(7f, 11f); var to = new Vector3(Mathf.Sin(az) * rad, 0, Mathf.Cos(az) * rad);
                        Place(p, "fan", mat, to + Vector3.down * (h + 3f), to, new Vector3(1.7f, h, 1.7f), i * 0.08f, 2.6f, 0.3f, instant); break;
                    }
                    case "shards":
                    {
                        float rad = R(3.4f, 8f), y = R(1.2f, 6.5f); if (rad < 6.5f) az = AvoidFront(az); var to = new Vector3(Mathf.Sin(az) * rad, y, Mathf.Cos(az) * rad); var from = to * 2.6f + Vector3.up * 3f;
                        float s = R(0.5f, 1.5f); Place(p, "shard", mat, from, to, new Vector3(s, s * R(1.6f, 3.2f), s), i * 0.07f, R(1.8f, 3f), R(0.4f, 1.1f), instant); break;
                    }
                    case "arches":
                    {
                        float rad = 5.8f; az = i / (float)count * 6.283f + 0.4f; var to = new Vector3(Mathf.Sin(az) * rad, 0, Mathf.Cos(az) * rad); float s = R(3.4f, 4.6f);
                        { float hh = R(4.2f, 6.5f); Place(p, "fan", mat, to + Vector3.down * (hh + 2f), to, new Vector3(1.5f, hh, 1.5f), i * 0.2f, 2.4f, 0.2f, instant); break; }
                        Place(p, "arch", mat, to + Vector3.down * 6f, to, new Vector3(s, s, s), i * 0.2f, 2.4f, 0.2f, instant); p.tf.rotation = Quaternion.LookRotation(-to.normalized) * Quaternion.Euler(0, 90, 0); p.tf.rotation = Quaternion.Euler(0, az * Mathf.Rad2Deg + 90f, 0); break;
                    }
                    case "skyorbs":
                    {
                        float rad = R(0.8f, 6f), y = R(5f, 9f); var to = new Vector3(Mathf.Sin(az) * rad, y, Mathf.Cos(az) * rad); float s = R(0.35f, 1.1f);
                        Place(p, "orb", mat, to + Vector3.up * 9f, to, new Vector3(s, s, s), i * 0.1f, R(2f, 3.4f), R(0.1f, 0.4f), instant); break;
                    }
                    case "crown":
                    {
                        az = i / (float)count * 6.283f; float rad = R(9f, 12f); float h = R(10f, 15f); var to = new Vector3(Mathf.Sin(az) * rad, 0, Mathf.Cos(az) * rad);
                        Place(p, "fan", mat, to + Vector3.down * (h + 4f), to, new Vector3(1.9f, h, 1.9f), i * 0.06f, 3.2f, 0.1f, instant); break;
                    }
                }
            }
        }

        public void SpawnBuds(int n)
        {
            if (_jungle) return;
            for (int i = 0; i < n; i++)
            {
                Bud b = null; for (int k = 0; k < MaxBuds; k++) if (!_b[k].alive) { b = _b[k]; break; }
                if (b == null) return;
                float az = R(-1.9f, 1.9f), rad = i % 3 == 0 ? R(1.15f, 1.9f) : R(0.5f, 0.88f); float y = R(0.95f, 1.85f) - (rad > 1f ? 0.2f : 0f);
                var fwd = head != null ? Quaternion.Euler(0, head.eulerAngles.y, 0) : Quaternion.identity;
                b.basePos = fwd * new Vector3(Mathf.Sin(az) * rad, y, Mathf.Cos(az) * rad); b.alive = true; b.phase = R(0, 6.28f); b.born = Time.time; b.kind = _rnd.Next(0, LFMats.Count); b.pop = 0;
                b.mr.sharedMaterial = LFMats.Toon(b.kind); b.tf.position = b.basePos; b.tf.localScale = Vector3.zero; b.tf.gameObject.SetActive(true);
            }
        }
        public int BudCount() { int c = 0; for (int i = 0; i < MaxBuds; i++) if (_b[i].alive) c++; return c; }
        public Vector3 NearestBud(Vector3 from) { float bd = 1e9f; Vector3 r = from + Vector3.forward; for (int i = 0; i < MaxBuds; i++) if (_b[i].alive) { float d = (_b[i].tf.position - from).sqrMagnitude; if (d < bd) { bd = d; r = _b[i].tf.position; } } return r; }

        public void Burst(Vector3 pos, Color c, int n, float speed)
        {
            if (_jungle) n = Mathf.Max(4, n / 4);
            c = Col(c);
            _ep.position = pos; _ep.applyShapeToPosition = true;
            for (int i = 0; i < n; i++)
            {
                _ep.velocity = UnityEngine.Random.onUnitSphere * speed * UnityEngine.Random.Range(0.4f, 1f); _ep.startColor = Color.Lerp(c, Color.white, UnityEngine.Random.value * 0.4f);
                _ep.startSize = UnityEngine.Random.Range(0.04f, 0.12f); _ep.startLifetime = UnityEngine.Random.Range(0.7f, 1.4f); _burst.Emit(_ep, 1);
            }
        }
        public void RingAt(Vector3 pos, Color c, float size, bool flat)
        {
            var r = _r[(_ringCursor++) % MaxRings]; r.alive = true; r.t = 0; r.dur = 0.9f; r.size = size; r.mat.SetColor("_Color", c); r.tf.position = pos; r.tf.gameObject.SetActive(true);
            r.tf.rotation = flat ? Quaternion.Euler(90, 0, 0) : (head != null ? Quaternion.LookRotation(pos - head.position) : Quaternion.identity); r.tf.localScale = Vector3.one * size;
        }

        /// <summary>Duel: claim the next unowned piece for an owner (1 = player cyan, 2 = rival magenta). Returns true if a piece was claimed.</summary>
        public bool Claim(int owner)
        {
            for (int k = 0; k < MaxPieces; k++)
            {
                var p = _p[(k + _pieceCursor * 3) % MaxPieces]; if (!p.alive || p.owner != 0 || p.t < p.dur) continue;
                p.owner = owner; p.mr.sharedMaterial = LFMats.Toon(owner == 1 ? LFMats.Cyan : LFMats.Magenta); RingAt(p.tf.position, LFMats.Tint(owner == 1 ? LFMats.Cyan : LFMats.Magenta), 1.6f, false); Burst(p.tf.position + Vector3.up * 0.5f, LFMats.Tint(owner == 1 ? LFMats.Cyan : LFMats.Magenta), 14, 1.2f);
                p.tf.localScale = p.scale * 1.18f;
                if (owner == 1) ClaimedPlayer++; else ClaimedRival++; OnClaim?.Invoke(owner, owner == 1 ? ClaimedPlayer : ClaimedRival); return true;
            }
            return false;
        }

        // ----------------------------------------------------------------- update
        void Update()
        {
            float dt = Time.deltaTime; _pulse = LFUtil.Damp(_pulse, 0f, 6f, dt); _intensity = LFUtil.Damp(_intensity, _intensityT, 2.5f, dt);
            Shader.SetGlobalFloat(LFShaderIds.Pulse, _pulse); Shader.SetGlobalFloat(LFShaderIds.Intensity, _intensity);
            _floorGrow = LFUtil.Damp(_floorGrow, (_floorOn && LFVoidRoom.Fade > 0.4f) ? 1f : 0f, 2.2f, dt); if (Mathf.Abs(_flowGate - LFVoidRoom.Fade) > 0.01f) { _flowGate = LFVoidRoom.Fade; ApplyFlow(); }
            _floor.localScale = Vector3.one * _floorScale * LFUtil.EaseOutBack(_floorGrow); _floor.gameObject.SetActive(_floorGrow > 0.01f && !_jungle);
            _stair = Mathf.MoveTowards(_stair, _stairTarget, dt * 0.35f); Shader.SetGlobalFloat(LFShaderIds.Stair, _stair);
            _stairs.gameObject.SetActive(_stair > 0.001f && !_jungle);
            FrostTick(dt); float tm = Time.time;
            for (int i = 0; i < MaxPieces; i++)
            {
                var p = _p[i]; if (!p.alive) continue;
                if (p.delay > 0) { p.delay -= dt; continue; }
                p.t += dt; float u = Mathf.Clamp01(p.t / p.dur); float e = 1f - Mathf.Pow(1f - u, 3f);
                Vector3 pos = Vector3.Lerp(p.from, p.to, e);
                if (u < 1f) { float ang = (1f - e) * p.swirl * 6.283f; float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang); pos = new Vector3(pos.x * cs + pos.z * sn, pos.y, -pos.x * sn + pos.z * cs); }
                float bob = p.scale.y < 3f && p.to.y > 0.8f ? Mathf.Sin(tm * 0.9f + p.bob) * 0.18f : 0f;
                p.tf.position = pos + Vector3.up * bob;
                float sc = u < 0.25f ? u / 0.25f : LFUtil.EaseOutBack((u - 0.25f) / 0.75f * 0.6f + 0.4f);
                float pls = 1f + _pulse * (p.owner != 0 ? 0.12f : 0.07f);
                float st = u < 1f ? Mathf.Sin(u * Mathf.PI) : 0f; float sb = Mathf.Min(sc, 1.12f) * pls * (p.owner != 0 ? 1.12f : 1f);
                p.tf.localScale = new Vector3(p.scale.x * sb * (1f - 0.10f * st), p.scale.y * sb * (1f + 0.16f * st), p.scale.z * sb * (1f - 0.10f * st));   // grows tall and thin, then settles
                if (p.to.y > 0.8f) p.tf.Rotate(0, p.spin * dt, 0, Space.World);
                if (u >= 1f && p.scale.y > 1.8f && ((Time.frameCount + i) & 1) == 0)
                {
                    float hh = UnityEngine.Random.value * p.scale.y * 0.95f; float ang2 = UnityEngine.Random.value * 6.2832f; float rr = UnityEngine.Random.Range(0.05f, 0.34f) * p.scale.x * (1f - hh / (p.scale.y * 1.2f));
                    _ep.position = p.tf.position + new Vector3(Mathf.Sin(ang2) * rr, hh, Mathf.Cos(ang2) * rr); _ep.applyShapeToPosition = true; _ep.velocity = new Vector3(Mathf.Cos(ang2), 0.45f, -Mathf.Sin(ang2)) * 0.18f;
                    _ep.startColor = Color.Lerp(LFElements.A1, Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.06f); _ep.startLifetime = UnityEngine.Random.Range(1.2f, 2.2f); _dust.Emit(_ep, 1);
                }
                if (u >= 1f && !p.hazed) { p.hazed = true; _ep.position = p.tf.position + Vector3.up * Mathf.Min(p.scale.y * 0.55f, 4f); _ep.velocity = Vector3.zero; _ep.startColor = new Color(0.45f, 0.75f, 1f, 0.16f); _ep.startSize = Mathf.Clamp(p.scale.y * 0.5f, 1f, 3.2f); _ep.startLifetime = 5f; _haze.Emit(_ep, 1); Burst(p.tf.position + Vector3.up * 0.2f, LFElements.A1, 16, 0.9f); }
            }
            // buds
            for (int i = 0; i < MaxBuds; i++)
            {
                var b = _b[i]; if (!b.alive) continue;
                float age = tm - b.born; float s = LFUtil.EaseOutBack(Mathf.Clamp01(age * 1.6f)) * (0.16f + 0.03f * Mathf.Sin(tm * 3f + b.phase)) * (1f + _pulse * 0.25f);
                b.tf.localScale = Vector3.one * s; b.tf.position = b.basePos + new Vector3(Mathf.Sin(tm * 0.7f + b.phase) * 0.03f, Mathf.Sin(tm * 1.1f + b.phase * 1.3f) * 0.05f, 0);
                if (pointers != null) for (int k = 0; k < pointers.Length; k++)
                {
                    var pt = pointers[k]; if (!pt.active) continue;
                    if ((pt.pos - b.tf.position).sqrMagnitude < 0.11f * 0.11f) { PopBud(b); break; }
                }
            }
            for (int i = 0; i < MaxRings; i++)
            {
                var r = _r[i]; if (!r.alive) continue; r.t += dt; float u = r.t / r.dur; if (u >= 1f) { r.alive = false; r.tf.gameObject.SetActive(false); continue; }
                r.mat.SetFloat("_T", u);
            }
            _flakeT -= dt;
            { float sp0 = (0.5f + _intensity * 0.55f) * Mathf.Lerp(0.6f, 1.5f, _pitch01); for (int i = 0; i < _rib.Length; i++)
              {
                var rb = _rib[i]; if (!_floorOn || LFVoidRoom.Fade < 0.5f) { rb.tr.emitting = false; continue; } rb.tr.emitting = true;
                float ang = tm * rb.sp * sp0 + rb.ph; float rad = rb.r + 0.5f * Mathf.Sin(tm * 0.4f + rb.ph * 2f); float amp = 0.6f + 0.7f * _intensity;
                float y = 0.12f + rb.hy * 0.5f + (0.5f + 0.5f * Mathf.Sin(tm * 0.55f + rb.ph * 1.7f)) * amp * (1f + rb.hy);
                var rp = new Vector3(Mathf.Sin(ang) * rad, y, Mathf.Cos(ang) * rad); rb.tf.position = rp;
                _ep.position = rp; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.12f; _ep.startColor = Color.Lerp(LFElements.A1, Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.06f); _ep.startLifetime = UnityEngine.Random.Range(1f, 1.8f); _dust.Emit(_ep, 1);
              } }
            if (pointers != null) for (int k = 0; k < pointers.Length; k++)
            {
                var pt = pointers[k]; if (!pt.active) continue; float sp = pt.vel.magnitude; int n = 1 + Mathf.Min(7, (int)(sp * 10f));
                for (int q = 0; q < n; q++)
                {
                    _ep.position = pt.pos + UnityEngine.Random.insideUnitSphere * 0.012f; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.18f + pt.vel * 0.12f + Vector3.up * 0.05f;
                    _ep.startColor = Color.Lerp(LFElements.A1, Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.07f); _ep.startLifetime = UnityEngine.Random.Range(0.9f, 1.7f); _dust.Emit(_ep, 1);
                }
                if (_flakeT <= 0f && sp > 0.05f) { _flakeT = 0.22f; _ep.position = pt.pos; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.2f + Vector3.up * 0.08f; _ep.startColor = Color.Lerp(LFElements.A1, Color.white, 0.5f); _ep.startSize = UnityEngine.Random.Range(0.1f, 0.2f); _ep.startLifetime = UnityEngine.Random.Range(2f, 3f); _flakes.Emit(_ep, 1); }
            }
            int shown = Mathf.FloorToInt(_stair * 24f + 0.001f);
            while (_stairShown < shown) { float a = _stairShown * 11f * Mathf.Deg2Rad; var sp2 = _stairs.TransformPoint(new Vector3(Mathf.Sin(a) * 3.7f, _stairShown * 0.22f + 0.1f, Mathf.Cos(a) * 3.7f)); Burst(sp2, LFElements.A1, 12, 0.7f); _ep.position = sp2; _ep.velocity = Vector3.up * 0.12f; _ep.startColor = Color.white; _ep.startSize = 0.2f; _ep.startLifetime = 2.2f; _flakes.Emit(_ep, 1); _stairShown++; }
            if (shown < _stairShown) _stairShown = shown;
            if (head != null) { _sky.position = head.position; }
        }

        public void FlakeBloom(Vector3 pos, Color c, int n)
        {
            if (_jungle) { for (int i = 0; i < n / 3; i++) EmitDust(pos, UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.3f, 1.0f), c, UnityEngine.Random.Range(0.03f, 0.07f), UnityEngine.Random.Range(1.2f, 2.2f)); return; }
            for (int i = 0; i < n; i++)
            {
                _ep.position = pos; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.5f, 1.3f); _ep.startColor = Color.Lerp(c, Color.white, 0.55f);
                _ep.startSize = UnityEngine.Random.Range(0.07f, 0.2f); _ep.startLifetime = UnityEngine.Random.Range(2.2f, 3.4f); _flakes.Emit(_ep, 1);
            }
            _ep.position = pos; _ep.velocity = Vector3.zero; _ep.startColor = new Color(c.r, c.g, c.b, 0.35f); _ep.startSize = 1.6f; _ep.startLifetime = 2.2f; _haze.Emit(_ep, 1);
        }

        void PopBud(Bud b)
        {
            Vector3 pos = b.tf.position; Color c = LFMats.Tint(b.kind); b.alive = false; b.tf.gameObject.SetActive(false);
            Burst(pos, c, 46, 1.9f); RingAt(pos, c, 1.1f, false); _pulse = Mathf.Max(_pulse, 0.7f);
            // the bloom: a small crown of shards + a spire grows where the bud popped
            var floorPt = new Vector3(pos.x, 0, pos.z) * 1.55f; if (floorPt.sqrMagnitude < 1.4f) floorPt = new Vector3(pos.x, 0, pos.z).normalized * 1.5f + new Vector3(0.01f, 0, 0);
            if (_jungle) { OnPop?.Invoke(pos, b.kind); FlakeBloom(pos, c, 10); return; }
            var sp = NextPiece(); Place(sp, "fan", b.kind + 1, floorPt + Vector3.down * 2f, floorPt, new Vector3(1.3f, R(1.6f, 3.0f), 1.3f), 0f, 1.2f, 0.2f, false); FlakeBloom(pos, c, 16);
            for (int i = 0; i < 3; i++) { var s2 = NextPiece(); var o = floorPt + new Vector3(R(-.5f, .5f), R(0.4f, 1.2f), R(-.5f, .5f)); Place(s2, "shard", b.kind + 2 + i, floorPt, o, new Vector3(0.3f, 0.7f, 0.3f), 0.05f * i, 1.0f, 0.3f, false); }
            RingAt(new Vector3(floorPt.x, 0.02f, floorPt.z), c, 2.4f, true); Frost(floorPt, 2.4f);
            OnPop?.Invoke(pos, b.kind);
        }
    }
}
