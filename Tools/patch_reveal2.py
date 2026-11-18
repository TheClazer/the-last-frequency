import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets\Scripts')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('LFLife.cs', [('Mathf.Abs(((d.sector - sector + 18) % 12) - 6) >= 4', 'Mathf.Abs(((d.sector - sector + 18) % 12) - 6) <= 2')])

sub('LFJungle.cs', [
('public bool Built; float _driftT;', '''public bool Built; float _driftT; public LFLife Life;
        readonly float[][] _rev = { new float[12], new float[12], new float[12] }, _tgt = { new float[12], new float[12], new float[12] }; readonly int[] _stage = new int[12]; readonly float[] _spd = { 0.12f, 0.10f, 0.14f };
        bool _revOn, _allLife, _flyDone; readonly List<ParticleSystem> _mistPS = new List<ParticleSystem>(); public int Pushes;
        public static int SectorOf(float az) { return Mathf.RoundToInt((az / (2f * Mathf.PI) + 0.5f) * 12f) % 12; }
        public void StartVoid() { _revOn = true; for (int l = 0; l < 3; l++) for (int k = 0; k < 12; k++) { _rev[l][k] = 0f; _tgt[l][k] = 0f; } for (int k = 0; k < 12; k++) _stage[k] = 0; _allLife = _flyDone = false; Pushes = 0; ShowAll(true); SetStatics(true); SetLive(true); UpdateReveal(0f, 0f); }
        /// <summary>The user pushed a hand outward toward world azimuth az (radians, 0 = +Z): the next layer of the world draws itself across a 150-degree sector.</summary>
        public int Push(float az)
        {
            int s = SectorOf(az); int st = _stage[s]; Pushes++; if (st >= 3) { if (Life != null) { Life.Arrive(s, false); Life.Butterflies(_a != null ? new Vector3(0, 0.2f, 3f) : Vector3.zero); } return 3; }
            for (int off = -2; off <= 2; off++) { int j = (s + off + 12) % 12; _tgt[st][j] = 1f; if (Mathf.Abs(off) <= 1) _stage[j] = Mathf.Max(_stage[j], st + 1); }
            if (st == 1 && Life != null) StartCoroutine(ArriveLater(s, 5.5f)); return st;
        }
        IEnumerator ArriveLater(int s, float d) { yield return new WaitForSeconds(d); if (Life != null) Life.Arrive(s, false); }
        /// <summary>safety net so a hesitant singer still gets the whole world by the chorus: the song itself keeps drawing</summary>
        public void AutoAssist(float t)
        {
            if (!_revOn) return; if (t > 28f) for (int k = 0; k < 12; k++) _tgt[0][k] = Mathf.Max(_tgt[0][k], Mathf.Clamp01((t - 28f) / 40f)); if (t > 50f) for (int k = 0; k < 12; k++) _tgt[1][k] = Mathf.Max(_tgt[1][k], Mathf.Clamp01((t - 50f) / 40f)); if (t > 70f) for (int k = 0; k < 12; k++) _tgt[2][k] = Mathf.Max(_tgt[2][k], Mathf.Clamp01((t - 70f) / 35f));
            if (t > 84f && !_allLife && Life != null) { _allLife = true; Life.Arrive(0, true); } if (t > 108f && !_flyDone && Life != null) { _flyDone = true; Life.Butterflies(new Vector3(0, 0.2f, 3f)); }
        }
        void UpdateReveal(float dt, float boost)
        {
            float mean = 0f; for (int l = 0; l < 3; l++) for (int k = 0; k < 12; k++) { _rev[l][k] = Mathf.MoveTowards(_rev[l][k], _tgt[l][k], dt * _spd[l] * (1f + boost * 1.6f)); if (l == 2) mean += _rev[2][k] / 12f; }
            Shader.SetGlobalFloatArray("_LFRev0", _rev[0]); Shader.SetGlobalFloatArray("_LFRev1", _rev[1]); Shader.SetGlobalFloatArray("_LFRev2", _rev[2]);
            var o = _cam != null ? _cam.transform.position : Vector3.zero; Shader.SetGlobalVector("_LFRevOrigin", new Vector4(0f, 0f, 0f, 0f)); Shader.SetGlobalVector("_LFRevCfg", new Vector4(_revOn ? 1f : 0f, mean, 0f, 0f));
            foreach (var ps in _mistPS) { var em = ps.emission; em.rateOverTime = 6f * mean; }
        }
        public float Revealed { get { float m = 0; for (int k = 0; k < 12; k++) m += (_rev[0][k] + _rev[1][k] + _rev[2][k]) / 36f; return m; } }'''),
('            MakeShafts(); MakeMist(); Built = true; SetLive(false);', '            MakeShafts(); MakeMist(); Life = new GameObject("Life").AddComponent<LFLife>(); Life.transform.SetParent(_root, false); Life.Init(); foreach (var g in _live) { var ps = g.GetComponent<ParticleSystem>(); if (ps != null) _mistPS.Add(ps); } Built = true; SetLive(false);'),
('        public void Tick(float intensity, float dt, bool drift)\n        {', '        public void Tick(float intensity, float dt, bool drift)\n        {\n            UpdateReveal(dt, intensity);'),
('var m = new Material(Shader.Find("LF/Lit")) { name = name }; m.SetFloat("_UseTex", 1f);', 'var m = new Material(Shader.Find("LF/Lit")) { name = name }; m.SetFloat("_Layer", (name.StartsWith("TTree") || name == "GiantGuardian" || name == "TempleArch") ? 1f : 0f); m.SetFloat("_UseTex", 1f);'),
('_vcMat = new Material(lit) { name = "JVC" }; _vcMat.SetFloat("_Gloss", 0.06f);', '_vcMat = new Material(lit) { name = "JVC" }; _vcMat.SetFloat("_Gloss", 0.06f); _vcMat.SetFloat("_Layer", 0f);'),
])

sub('LFMagic.cs', [
('        public static event System.Action<string> OnShape;', '        public static event System.Action<string> OnShape; public static bool JungleMode; float _pushCool;'),
('            float dt = Time.deltaTime; _cool -= dt;', '            float dt = Time.deltaTime; _cool -= dt; _pushCool -= dt;'),
('                // DRAW: pinch held', '''                // PUSH (jungle): a hand thrown outward in any direction draws the world there
                if (JungleMode && _pushCool <= 0f && !_drawing[k]) { Vector3 rel = pt.pos - _head.position; rel.y = 0f; float rl = rel.magnitude; if (rl > 0.2f && pt.vel.magnitude > 0.9f && Vector3.Dot(pt.vel, rel / rl) > 0.85f) { _pushCool = 1.1f; StartCoroutine(PushRoutine(pt.pos, (rel / rl))); } }
                // DRAW: pinch held'''),
('        // ---------------------------------------------------------------- conjure', '''        System.Collections.IEnumerator PushRoutine(Vector3 from, Vector3 dir)
        {
            var go = new GameObject("PushComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.2f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.16f), new Keyframe(1, 0f)); tr.minVertexDistance = 0.02f;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#ffd27a"), 0.4f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr;
            Vector3 target = _head.position + dir * 42f + Vector3.down * 4f; float t = 0f; float dur = 0.9f; Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (_notes != null) { _notes.Play(Instr.Pad, 55, 0.7f, from); _notes.Play(Instr.Bell, 79, 0.6f, from); }
            while (t < dur) { t += Time.deltaTime; float u = t / dur; float e = u * u * (3f - 2f * u); go.transform.position = Vector3.Lerp(from, target, e) + side * Mathf.Sin(u * 12f) * 0.25f * (1f - u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.5f; _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.3f, Color.white, 0.06f, 1.2f); yield return null; }
            float az = Mathf.Atan2(dir.x, dir.z); int layer = LFJungle.I != null ? LFJungle.I.Push(az) : 0; LFLog.Ev("push", layer.ToString(), az);
            _w.Burst(target, LFUtil.Hex("#ffd27a"), 70, 6f); _w.RingAt(target, LFUtil.Hex("#ffd27a"), 9f, false); Destroy(go, 1.5f);
        }

        // ---------------------------------------------------------------- conjure'''),
])

sub('LFApp.cs', [
('            Theme = t; World.SetVisualTheme(t == 1);', '            Theme = t; LFMagic.JungleMode = t == 1; World.SetVisualTheme(t == 1);'),
('            if (Theme == 1 && !_skipOpening) { yield return StartCoroutine(Jungle.Opening(this, 46f)); }', '            if (Theme == 1 && !_skipOpening) { Jungle.StartVoid(); LFMagic.JungleMode = true; UI.Hint("Push your hand outward. The world draws itself where you point."); }'),
('Jungle.Tick(Mathf.Max(it, Voice.Level * 0.8f), dt, true); }', 'Jungle.Tick(Mathf.Max(it, Voice.Level * 0.8f), dt, true); Jungle.AutoAssist(_songT); }'),
])
sub('LFAuto.cs', [
('            yield return StartCoroutine(Jungle.Opening(this, quick ? 26f : 46f)); World.SetFlow(0.5f);\n            _skipOpening = true; StartPerformanceAt(quick ? 46f : 0f, -1f);',
 '            LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); UI.Hint("Push your hand outward. The world draws itself where you point."); yield return new WaitForSeconds(4.5f);\n            _skipOpening = true; StartPerformanceAt(0f, -1f); yield return new WaitForSeconds(4.5f);'),
('            float tBud = 4f; int nd = 0; float[] dT = quick ? new[] { 0f, 0f, 0f, 0f } : new[] { 16f, 25f, 34f, 56f, 66f, 90f, 108f, 120f }; string[] dK = quick ? new[] { "sweep", "rect", "circle", "triangle" } : new[] { "sweep", "rect", "circle", "triangle", "sweep", "circle", "rect", "triangle" };\n            if (quick) yield return new WaitForSeconds(8f);',
 '            float tBud = 4f; int nd = 0; float[] dT = { 1f, 7f, 13f, 19f, 25f, 31f, 37f, 43f, 49f, 55f, 61f, 67f, 73f, 79f, 85f, 91f, 100f, 112f, 124f }; string[] dK = { "push", "push", "push", "push", "push", "push", "push", "push", "circle", "push", "push", "rect", "push", "triangle", "push", "push", "sweep", "push", "circle" };'),
('                if (quick && nd >= dT.Length) { yield return new WaitForSeconds(30f); break; }\n', ''),
('            Vector3[] pts;\n            if (kind == "rect")', '''            if (kind == "push")
            {
                Vector3 s0 = _head.position + f * 0.22f + Vector3.down * 0.3f + r * 0.12f; yield return GhostMove(s0, 0.4f); g.pinch = false; yield return GhostMove(s0 + f * 0.62f + Vector3.up * 0.1f, 0.22f); yield return new WaitForSeconds(0.9f); g.active = false; yield break;
            }
            Vector3[] pts;
            if (kind == "rect")'''),
])
print('patched reveal2')
