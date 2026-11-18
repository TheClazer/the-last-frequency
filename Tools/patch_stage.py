import os
S = r'E:\Winter\metavr\LastFrequency\Assets'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# ---------------------------------------------------------------- reveal include: layer 3 (stairs/statue outward growth), hand passthrough holes
sub('Resources/Shaders/LFReveal.hlsl', [
('float _LFRev0[12]; float _LFRev1[12]; float _LFRev2[12]; float4 _LFRevCfg; float4 _LFRevOrigin;', 'float _LFRev0[12]; float _LFRev1[12]; float _LFRev2[12]; float _LFRev3[12]; float4 _LFRevCfg; float4 _LFRevOrigin; float4 _LFHand0; float4 _LFHand1;'),
('    if (layer == 0) { p0 = _LFRev0[i0]; p1 = _LFRev0[i1]; } else if (layer == 1) { p0 = _LFRev1[i0]; p1 = _LFRev1[i1]; } else { p0 = _LFRev2[i0]; p1 = _LFRev2[i1]; }',
 '    if (layer == 0) { p0 = _LFRev0[i0]; p1 = _LFRev0[i1]; } else if (layer == 1) { p0 = _LFRev1[i0]; p1 = _LFRev1[i1]; } else if (layer == 2) { p0 = _LFRev2[i0]; p1 = _LFRev2[i1]; } else { p0 = _LFRev3[i0]; p1 = _LFRev3[i1]; }'),
('    float key = (dir > 0.0 ? h : 1.0 - h) * 0.72 + far * 0.28;', '    float key = (dir > 0.0 ? h : 1.0 - h) * 0.72 + far * 0.28;\n    if (layer == 3) key = saturate((dist - 6.0) / 62.0);   // stairs and statue grow outward, one step after another'),
('    float t = prog * 1.12 - 0.04 - n; float vis = step(key, t); float edge = vis * (1.0 - step(key + 0.028, t));', '    float t = prog * 1.12 - 0.04 - n; float vis = step(key, t); float edge = vis * (1.0 - step(key + 0.028, t));\n    if (_LFRevCfg.z > 0.5) { float dh = min(distance(wp, _LFHand0.xyz), distance(wp, _LFHand1.xyz)); if (dh < _LFHand0.w) { vis = 0.0; edge = 0.0; } }   // proximity passthrough: real hands and what is near them show through'),
])
sub('Resources/Shaders/LFSkyDay.shader', [
('half3 voidc = half3(0.004, 0.006, 0.02) + st * half3(0.9, 0.95, 1.0); c = lerp(voidc, c, v);', 'half3 voidc = half3(0.004, 0.006, 0.02) + st * half3(0.9, 0.95, 1.0); c = lerp(voidc, c, v); if (_LFRevCfg.z > 0.5) clip(v - 0.5);'),
('                c *= 1.0 + _LFIntensity * 0.25;\n', '                c *= 1.0 + _LFIntensity * 0.25;\n                float night = _LFFogCol.a; { float2 sc2 = floor(d.xz / (abs(d.y) + 0.3) * 150.0); float st2 = step(0.985, h21(sc2)) * (0.5 + 0.5 * sin(_Time.y * 2.0 + h21(sc2 + 3.0) * 30.0)) * smoothstep(-0.05, 0.25, d.y); half3 nc = lerp(half3(0.02, 0.03, 0.09), half3(0.06, 0.08, 0.2), saturate(d.y)) + st2 * half3(0.9, 0.95, 1.0) + half3(0.8, 0.85, 1.0) * pow(sd, 400.0) * 0.0; c = lerp(c, nc, night); }\n'),
])

# ---------------------------------------------------------------- materials per layer
sub('Scripts/LFJungle.cs', [
('AddMesh("terrain", _vcMat); AddMesh("bridge", _vcMat); AddMesh("temple", _vcMat);', 'var bridgeMat = new Material(lit) { name = "JBridge" }; bridgeMat.SetFloat("_Layer", 3f); bridgeMat.SetFloat("_Gloss", 0.05f); var templeMat = new Material(lit) { name = "JTemple" }; templeMat.SetFloat("_Layer", 1f); AddMesh("terrain", _vcMat); AddMesh("bridge", bridgeMat); AddMesh("temple", templeMat);'),
('m.SetFloat("_Layer", (name.StartsWith("TTree") || name == "GiantGuardian" || name == "TempleArch") ? 1f : 0f);', 'm.SetFloat("_Layer", name.StartsWith("TTree") ? 1f : ((name == "GiantGuardian" || name == "TempleArch") ? 3f : 0f));'),
])

# ---------------------------------------------------------------- staged reveal, stairs, lift, climb, time of day, rain, fog
s = open(os.path.join(S, 'Scripts/LFJungle.cs'), encoding='utf8').read()
a = s.index("        readonly float[][] _rev ="); b = s.index("        public float Revealed")
new_block = '''        readonly float[][] _rev = { new float[12], new float[12], new float[12], new float[12] }, _tgt = { new float[12], new float[12], new float[12], new float[12] }; readonly float[] _spd = { 0.07f, 0.06f, 0.10f, 0.06f };
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
'''
s = s[:a] + new_block + s[b:]
# apply globals with time of day / fog / rain
s = s.replace('''        public void ApplyGlobals(float intensity)
        {''', '''        public void ApplyGlobals(float intensity)
        {
            float tod = TimeOfDay; float elev = Mathf.Sin((tod - 0.25f) * Mathf.PI * 1.4f); float night = Mathf.Clamp01(-elev * 3f + 0.0f) * (tod > 0.95f || tod < 0.1f ? 1f : 0f); night = Mathf.Max(night, Mathf.Clamp01((tod - 0.9f) * 10f));
            Vector3 az = new Vector3(_a.sun[0], 0, _a.sun[2]).normalized; float el = Mathf.Lerp(0.12f, 0.95f, Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(tod / 0.9f) * Mathf.PI))); _sun = (az * Mathf.Cos(el) + Vector3.up * Mathf.Sin(el)).normalized; float warm = 1f - Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01(tod / 0.9f) * Mathf.PI) * 1.4f);
            Color sunC = Color.Lerp(new Color(1f, 0.95f, 0.85f), new Color(1f, 0.55f, 0.28f), warm) * (1f - night * 0.92f) * (1f - RainAmt * 0.6f); Color ambT = Color.Lerp(new Color(0.34f, 0.5f, 0.74f), new Color(0.07f, 0.1f, 0.22f), night) * (1f - RainAmt * 0.35f);
            Shader.SetGlobalVector("_LFSunCol", new Vector4(sunC.r, sunC.g, sunC.b, 1) * 1.7f); Shader.SetGlobalVector("_LFAmbTop", new Vector4(ambT.r, ambT.g, ambT.b, 1) * 0.95f);''')
s = s.replace('Shader.SetGlobalVector("_LFSunDir", new Vector4(_sun.x, _sun.y, _sun.z, 0)); Shader.SetGlobalVector("_LFSunCol", new Vector4(1.0f, 0.72f, 0.42f, 1) * 1.7f);', 'Shader.SetGlobalVector("_LFSunDir", new Vector4(_sun.x, _sun.y, _sun.z, 0));')
s = s.replace('Shader.SetGlobalVector("_LFAmbTop", new Vector4(0.30f, 0.48f, 0.72f, 1) * 0.95f); Shader.SetGlobalVector("_LFAmbBot", new Vector4(0.24f, 0.30f, 0.13f, 1));', 'Shader.SetGlobalVector("_LFAmbBot", new Vector4(0.24f, 0.30f, 0.13f, 1) * (1f - night * 0.85f));')
s = s.replace('Shader.SetGlobalVector("_LFFogCol", new Vector4(0.66f, 0.72f, 0.70f, 1)); Shader.SetGlobalVector("_LFFog", new Vector4(_fogBase * Mathf.Lerp(1.2f, 0.6f, intensity), 0.05f, 0.7f, 1.12f));', 'Color fogC = Color.Lerp(new Color(0.66f, 0.72f, 0.70f), new Color(0.05f, 0.07f, 0.14f), night); fogC = Color.Lerp(fogC, new Color(0.45f, 0.5f, 0.52f) * (1f - night * 0.8f), RainAmt * 0.6f); Shader.SetGlobalVector("_LFFogCol", new Vector4(fogC.r, fogC.g, fogC.b, night)); Shader.SetGlobalVector("_LFFog", new Vector4(_fogBase * Mathf.Lerp(1.2f, 0.6f, intensity) * (1f + FogAmt * 3.5f + RainAmt * 1.2f), 0.05f, 0.7f * (1f - night), 1.12f));')
# rain particles + path/drift in Tick
s = s.replace("            if (drift && Rig != null) { _driftT += dt;", '''            if (_rain == null) MakeRain(); { var em = _rain.emission; em.rateOverTime = RainAmt * 900f; _rain.transform.position = cp + Vector3.up * 9f; if (RainAmt > 0.02f && !_rain.isPlaying) _rain.Play(); }
            if (Rig != null) { _driftT += dt; Vector3 sway = new Vector3(Mathf.Sin(_driftT * 0.09f) * 0.12f, Mathf.Sin(_driftT * 0.13f) * 0.04f, Mathf.Cos(_driftT * 0.06f) * 0.1f); Rig.position = _rigBase + new Vector3(0f, DeckY(_climb), _climb) + sway; }
            if (false && drift && Rig != null) { _driftT += dt;''')
s = s.replace("        // ------------------------------------------------------------------------------------ gesture-grown trees (triangle)", '''        void MakeRain()
        {
            var g = new GameObject("Rain"); g.transform.SetParent(_root, false); _rain = g.AddComponent<ParticleSystem>(); _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); var m = _rain.main; m.loop = true; m.maxParticles = 4000; m.startLifetime = 1.0f; m.startSpeed = 0f; m.startSize = 0.05f; m.startColor = new Color(0.8f, 0.88f, 1f, 0.5f); m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = _rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(24f, 0.1f, 24f); var v = _rain.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(-16f, -12f); v.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.8f);
            var r = g.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 5f; r.velocityScale = 0.04f; r.sharedMaterial = new Material(Shader.Find("LF/Mote")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void ClimbFinale() { if (Life != null) { Life.Arrive(0, true); Life.Butterflies(Rig.position); } }
        // ------------------------------------------------------------------------------------ gesture-grown trees (triangle)''')
open(os.path.join(S, 'Scripts/LFJungle.cs'), 'w', encoding='utf8').write(s)

# ---------------------------------------------------------------- world: no flowers in the jungle (soft pollen instead)
sub('Scripts/LFWorld.cs', [
('public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p;', 'public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { if (_jungle) { EmitDust(p, v * 0.4f, c, size * 0.25f, life * 0.5f); return; } _ep.position = p;'),
('        public void FlakeBloom(Vector3 pos, Color c, int n)\n        {', '        public void FlakeBloom(Vector3 pos, Color c, int n)\n        {\n            if (_jungle) { for (int i = 0; i < n * 2; i++) EmitDust(pos, UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.3f, 1.0f), c, UnityEngine.Random.Range(0.03f, 0.07f), UnityEngine.Random.Range(1.2f, 2.2f)); return; }'),
])

# ---------------------------------------------------------------- magic: staged gestures (one push, both push, lift, climb)
sub('Scripts/LFMagic.cs', [
('public static bool JungleMode; float _pushCool;', 'public static bool JungleMode; float _pushCool, _liftCool, _climbCool; float[] _outT = { -9f, -9f, -9f }; float _pendT = -1f; Vector3 _pendDir; float _lastStroke;'),
('if (JungleMode && _pushCool <= 0f && !_drawing[k]) { Vector3 rel = pt.pos - _head.position; rel.y = 0f; float rl = rel.magnitude; if (rl > 0.2f && pt.vel.magnitude > 0.9f && Vector3.Dot(pt.vel, rel / rl) > 0.85f) { _pushCool = 1.1f; StartCoroutine(PushRoutine(pt.pos, (rel / rl))); } }',
 '''if (JungleMode && !_drawing[k]) JungleGestures(k, pt);'''),
('        System.Collections.IEnumerator PushRoutine(Vector3 from, Vector3 dir)', '''        void JungleGestures(int k, Pointer pt)
        {
            Vector3 rel = pt.pos - _head.position; Vector3 relH = rel; relH.y = 0f; float rl = relH.magnitude; var J = LFJungle.I; if (J == null) return;
            bool outward = rl > 0.2f && pt.vel.magnitude > 0.9f && Vector3.Dot(pt.vel, relH / Mathf.Max(rl, 1e-3f)) > 0.85f && Mathf.Abs(pt.vel.y) < 0.8f;
            if (outward && Time.time - _outT[k] > 1.0f) { _outT[k] = Time.time; }
            if (outward && _pushCool <= 0f && Time.time - _outT[0] < 0.35f && Time.time - _outT[1] < 0.35f && _p[0].active && _p[1].active) { _pushCool = 1.4f; _pendT = -1f; StartCoroutine(BothRoutine()); return; }
            if (outward && _pushCool <= 0f && _pendT < 0f) { _pendT = Time.time + 0.3f; _pendDir = relH / rl; _pendFrom = pt.pos; }
            if (_pendT > 0f && Time.time >= _pendT) { _pendT = -1f; if (_pushCool <= 0f) { _pushCool = 1.1f; StartCoroutine(PushRoutine(_pendFrom, _pendDir)); } }
            if (_liftCool <= 0f && rel.y > 0.1f && pt.vel.y > 0.8f && !outward) { _liftCool = 1.6f; Vector3 d = rl > 0.12f ? relH / rl : new Vector3(_head.forward.x, 0, _head.forward.z).normalized; StartCoroutine(LiftRoutine(pt.pos, d)); }
            if (J.StairProgress > 0.4f && _climbCool <= 0f && rel.y < 0.15f && Mathf.Abs(pt.vel.y) > 0.9f) { float sg = Mathf.Sign(pt.vel.y); if (sg != _lastStroke) { _lastStroke = sg; _climbCool = 0.3f; J.Climb(1.1f); _w.EmitDust(pt.pos, Vector3.up * 0.5f, Color.white, 0.05f, 1f); LFLog.Ev("climb", "stroke", J.Climbed); } }
        }
        Vector3 _pendFrom;
        System.Collections.IEnumerator BothRoutine()
        {
            Vector3 f = new Vector3(_head.forward.x, 0, _head.forward.z).normalized; var go = new GameObject("StairComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.4f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0f)); tr.minVertexDistance = 0.03f;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#ffd27a"), 0.5f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr;
            Vector3 a = (_p[0].pos + _p[1].pos) * 0.5f; Vector3 b = new Vector3(0f, 0.4f, 8f); float t = 0f; if (_notes != null) { _notes.Play(Instr.Bass, 40, 1f, a); _notes.Play(Instr.Pad, 52, 0.8f, a); }
            while (t < 1.2f) { t += Time.deltaTime; float e = t / 1.2f; e = e * e * (3f - 2f * e); go.transform.position = Vector3.Lerp(a, b + Vector3.up * 3f * (1f - e), e); _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.4f, Color.white, 0.07f, 1.4f); yield return null; }
            LFJungle.I.PushBoth(); LFLog.Ev("stairs", "push", LFJungle.I.StairProgress); _w.Burst(b, LFUtil.Hex("#ffd27a"), 90, 7f); _w.RingAt(new Vector3(0, 0.1f, 8f), LFUtil.Hex("#ffd27a"), 10f, true); Destroy(go, 1.6f);
        }
        System.Collections.IEnumerator LiftRoutine(Vector3 from, Vector3 dir)
        {
            Vector3 top = _head.position + dir * 60f + Vector3.up * 40f; var go = new GameObject("LiftComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.0f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.12f), new Keyframe(1, 0f)); tr.colorGradient = new Gradient();
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#9fd8ff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; float t = 0f;
            if (_notes != null) _notes.Play(Instr.Bell, 84, 0.8f, from);
            while (t < 1.0f) { t += Time.deltaTime; go.transform.position = Vector3.Lerp(from, top, t * t); _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.3f, Color.white, 0.05f, 1f); yield return null; }
            LFJungle.I.Lift(Mathf.Atan2(dir.x, dir.z)); LFLog.Ev("lift", "water", 1f); Destroy(go, 1.2f);
        }
        System.Collections.IEnumerator PushRoutine(Vector3 from, Vector3 dir)'''),
('            float dt = Time.deltaTime; _cool -= dt; _pushCool -= dt;', '            float dt = Time.deltaTime; _cool -= dt; _pushCool -= dt; _liftCool -= dt; _climbCool -= dt;'),
('float az = Mathf.Atan2(dir.x, dir.z); int layer = LFJungle.I != null ? LFJungle.I.Push(az) : 0;', 'float az = Mathf.Atan2(dir.x, dir.z); int layer = LFJungle.I != null ? LFJungle.I.PushOne(az) : 0;'),
])
print("patched stage")
