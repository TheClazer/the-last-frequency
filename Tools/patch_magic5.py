import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:80])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('Scripts/LFCore.cs', [('Pulse = Shader.PropertyToID("_LFPulse"),', 'Pitch = Shader.PropertyToID("_LFPitch"), Pulse = Shader.PropertyToID("_LFPulse"),')])
sub('Scripts/LFWorld.cs', [
('        public void Intensity(float v)', '''        float _pitch01 = 0.5f, _lvl; public float Pitch01 { get { return _pitch01; } }
        public void EmitDust(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = c; _ep.startSize = size * Mathf.Lerp(1.5f, 0.7f, _pitch01); _ep.startLifetime = life; _dust.Emit(_ep, 1); }
        public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = c; _ep.startSize = size; _ep.startLifetime = life; _flakes.Emit(_ep, 1); }
        public void GrowFan(Vector3 floorPos, float h, float w, int mat) { var p = NextPiece(); float wd = w * Mathf.Lerp(1.5f, 0.8f, _pitch01); Place(p, "fan", mat, floorPos + Vector3.down * (h + 1f), floorPos, new Vector3(wd, h, wd), 0f, 1.5f, 0.25f, false); }
        /// <summary>Voice character: high soprano = fine, bright, fast and thin; low = thick, slow, heavy and wide.</summary>
        public void SetTimbre(float pitch01, float level)
        {
            _pitch01 = Mathf.Lerp(_pitch01, Mathf.Clamp01(pitch01), 0.08f); _lvl = level; Shader.SetGlobalFloat(LFShaderIds.Pitch, _pitch01);
            float wm = Mathf.Lerp(1.9f, 0.55f, _pitch01) * (0.8f + level * 0.5f);
            for (int i = 0; i < _rib.Length; i++) _rib[i].tr.widthMultiplier = wm;
            float sz = Mathf.Lerp(1.6f, 0.6f, _pitch01); var m1 = _swirl.main; m1.startSizeMultiplier = sz; var m2 = _feet.main; m2.startSizeMultiplier = sz;
        }
        public void Intensity(float v)'''),
('float sp0 = 0.5f + _intensity * 0.55f;', 'float sp0 = (0.5f + _intensity * 0.55f) * Mathf.Lerp(0.6f, 1.5f, _pitch01);'),
('var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.25f; lim.dampen = 0.06f;', 'var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.25f; lim.dampen = 0.06f; var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0.85f)));'),
])
# shaders: pitch tints sky and floor
sub('Resources/Shaders/LFSky.shader', [
('float _LFPulse, _LFIntensity;', 'float _LFPulse, _LFIntensity, _LFPitch;'),
('half3 aur = lerp(_AurA.rgb, _AurB.rgb, saturate(el * 1.3 + w2 * 0.4));', 'half3 aur = lerp(_AurA.rgb, _AurB.rgb, saturate(el * 1.3 + w2 * 0.4)); aur = lerp(aur * half3(0.55, 0.35, 1.2), aur * half3(0.9, 1.25, 1.1), _LFPitch);'),
])
sub('Resources/Shaders/LFFloor.shader', [
('float _LFPulse, _LFIntensity, _LFVoice;', 'float _LFPulse, _LFIntensity, _LFVoice, _LFPitch;'),
('half3 glow = _ColorB.rgb * (spokes', 'half3 glow = lerp(_ColorB.rgb * half3(0.6, 0.45, 1.2), _ColorB.rgb * half3(1.0, 1.2, 1.05), _LFPitch) * (spokes'),
])
sub('Scripts/LFApp.cs', [
('World.OnPop += OnPop;', 'Magic = gameObject.AddComponent<LFMagic>(); World.OnPop += OnPop;'),
('public LFWorld World;', 'public LFMagic Magic; public LFWorld World;'),
('_notes = new GameObject("LFNotes").AddComponent<LFNotePlayer>(); _notes.transform.position = _head.position;', '_notes = new GameObject("LFNotes").AddComponent<LFNotePlayer>(); _notes.transform.position = _head.position; Magic.Init(World, Pointers, _head, _notes);'),
('            Conduct(dt);', '            { float mm = Voice.Midi > 0f ? Voice.Midi : 62f; World.SetTimbre(Mathf.InverseLerp(52f, 84f, mm), Voice.Level); }\n            Conduct(dt);'),
])
# autopilot: the ghost hand performs the gestures
sub('Scripts/LFAuto.cs', [
('float tBud = 3f, tHands = 20f;', 'float tBud = 3f, tHands = 20f; int nd = 0; float[] dT = { 16f, 25f, 34f, 56f, 66f, 90f, 108f, 120f }; string[] dK = { "sweep", "rect", "circle", "triangle", "sweep", "circle", "rect", "triangle" };'),
('                if (State == LFState.Perform && tBud <= 0f', '                if (State == LFState.Perform && nd < dT.Length && _songT >= dT[nd]) { yield return RunDemo(dK[nd]); nd++; }\n                if (State == LFState.Perform && tBud <= 0f'),
('        bool _previewOverride;', '''        bool _previewOverride;
        IEnumerator RunDemo(string kind)
        {
            var g = Ghost; Vector3 f = _head.forward; f.y = 0; f.Normalize(); Vector3 r = Vector3.Cross(Vector3.up, f); Vector3 c = _head.position + f * 0.55f + Vector3.up * 0.02f;
            g.active = true; if ((g.pos - c).sqrMagnitude > 1.5f) g.pos = c + Vector3.down * 0.4f;
            if (kind == "sweep")
            {
                yield return GhostMove(c - r * 0.35f + Vector3.down * 0.15f, 0.5f); g.pinch = false;
                float t = 0; while (t < 0.5f) { t += Time.deltaTime; float u = t / 0.5f; g.pos = c + r * Mathf.Lerp(-0.35f, 0.35f, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.22f - 0.12f); yield return null; }
                yield return new WaitForSeconds(1.8f); g.active = false; yield break;
            }
            Vector3[] pts;
            if (kind == "rect") pts = new[] { c + r * -0.16f + Vector3.up * 0.11f, c + r * 0.16f + Vector3.up * 0.11f, c + r * 0.16f - Vector3.up * 0.11f, c - r * 0.16f - Vector3.up * 0.11f, c + r * -0.16f + Vector3.up * 0.11f };
            else if (kind == "triangle") pts = new[] { c + Vector3.up * 0.15f, c + r * 0.15f - Vector3.up * 0.1f, c - r * 0.15f - Vector3.up * 0.1f, c + Vector3.up * 0.15f };
            else { pts = new Vector3[25]; for (int i = 0; i < 25; i++) { float a = i / 24f * 6.2832f; pts[i] = c + r * Mathf.Cos(a) * 0.14f + Vector3.up * Mathf.Sin(a) * 0.14f; } }
            yield return GhostMove(pts[0], 0.5f); g.pinch = true;
            for (int i = 1; i < pts.Length; i++) yield return GhostMove(pts[i], kind == "circle" ? 0.07f : 0.32f);
            g.pinch = false; yield return new WaitForSeconds(0.3f); g.active = false; yield return new WaitForSeconds(2.6f);
        }'''),
])
print('patched5')
