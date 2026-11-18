import os, re
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets\Scripts')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# --- LFJungle: clean shaft orientation
s = open('LFJungle.cs', encoding='utf8').read()
a = s.index("            for (int i = 0; i < _shafts.Count; i++)\n            {\n                var s = _shafts[i];"); b = s.index("            if (drift && Rig != null)")
s = s[:a] + '''            for (int i = 0; i < _shafts.Count; i++)
            {
                var sh = _shafts[i]; Vector3 toCam = cp - sh.tf.position; Vector3 f = toCam - Vector3.Dot(toCam, _sun) * _sun; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
                sh.tf.rotation = Quaternion.LookRotation(f, _sun); sh.tf.localScale = new Vector3(sh.s, 95f, 1f);
            }
''' + s[b:]
open('LFJungle.cs', 'w', encoding='utf8').write(s)

# --- LFWorld: jungle visual theme
sub('LFWorld.cs', [
('public int ClaimedPlayer, ClaimedRival;', 'public int ClaimedPlayer, ClaimedRival; bool _jungle; public bool IsJungle { get { return _jungle; } }'),
('        public void Intensity(float v)', '''        Color Col(Color c) { return _jungle ? Color.Lerp(c, new Color(1f, 0.82f, 0.45f), 0.62f) : c; }
        public void SetVisualTheme(bool jungle)
        {
            _jungle = jungle; if (_sky != null) _sky.gameObject.SetActive(!jungle); if (jungle) { _floorOn = false; _stairTarget = 0; }
            var r = _flakes.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = jungle ? LFMats.Flower() : LFMats.Flake();
            var gr = new Gradient(); if (jungle) gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex("#fff0b8"), 0f), new GradientColorKey(LFUtil.Hex("#ffc860"), 0.5f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            else gr.SetKeys(new[] { new GradientColorKey(LFUtil.Hex("#c8f2ff"), 0f), new GradientColorKey(LFUtil.Hex("#7fc4ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#cdbcff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            foreach (var ps in new[] { _feet, _above, _swirl }) { var m = ps.main; m.startColor = new ParticleSystem.MinMaxGradient(gr) { mode = ParticleSystemGradientMode.RandomColor }; }
        }
        public void Intensity(float v)'''),
('        public void EmitDust(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = c;', '        public void EmitDust(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = Col(c);'),
('public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = c;', 'public void EmitFlake(Vector3 p, Vector3 v, Color c, float size, float life) { _ep.position = p; _ep.applyShapeToPosition = true; _ep.velocity = v; _ep.startColor = Col(c);'),
('public void GrowFan(Vector3 floorPos, float h, float w, int mat) { var p = NextPiece();', 'public void GrowFan(Vector3 floorPos, float h, float w, int mat) { if (_jungle && LFJungle.I != null) { var fp = floorPos; fp.y = 0f; LFJungle.I.GrowTree(fp * 0.55f + new Vector3(0, 0.0f, 0)); return; } var p = NextPiece();'),
('_floor.localScale = Vector3.one * 3.4f * LFUtil.EaseOutBack(_floorGrow); _floor.gameObject.SetActive(_floorGrow > 0.01f);', '_floor.localScale = Vector3.one * 3.4f * LFUtil.EaseOutBack(_floorGrow); _floor.gameObject.SetActive(_floorGrow > 0.01f && !_jungle);'),
('_stairs.gameObject.SetActive(_stair > 0.001f);', '_stairs.gameObject.SetActive(_stair > 0.001f && !_jungle);'),
('public void Burst(Vector3 pos, Color c, int n, float speed)\n        {', 'public void Burst(Vector3 pos, Color c, int n, float speed)\n        {\n            c = Col(c);'),
])
sub('LFMeshes.cs', [('public static Material Trail()', 'static Material _flower; public static Material Flower() { if (_flower == null) _flower = new Material(Shader.Find("LF/Flower")); return _flower; }\n        public static Material Trail()')])

# --- LFApp: theme, jungle flow, overlays
sub('LFApp.cs', [
('public bool VoidBuild = true, Duel, GuideOn;', 'public bool VoidBuild = true, Duel, GuideOn; public int Theme; public LFJungle Jungle; LFButton _bTheme;'),
('            _bGuide = UI.Button(_menu, "", new Vector3(0.24f, -0.15f, 0), new Vector2(0.4f, 0.13f), () => Q(() => { GuideOn = !GuideOn; RefreshToggles(); }), LFMats.Ice);',
 '            _bGuide = UI.Button(_menu, "", new Vector3(0.24f, -0.15f, 0), new Vector2(0.4f, 0.13f), () => Q(() => { GuideOn = !GuideOn; RefreshToggles(); }), LFMats.Ice);\n            _bTheme = UI.Button(_menu, "", new Vector3(0f, -0.31f, 0), new Vector2(0.9f, 0.12f), () => Q(() => { SetTheme(1 - Theme); RefreshToggles(); }), LFMats.Gold);'),
('UI.SetText(_bGuide, GuideOn ? "GUIDE VOCAL: ON" : "GUIDE VOCAL: OFF");', 'UI.SetText(_bGuide, GuideOn ? "GUIDE VOCAL: ON" : "GUIDE VOCAL: OFF"); UI.SetText(_bTheme, Theme == 0 ? "WORLD: AURORA ICE   (tap for JUNGLE TEMPLE)" : "WORLD: JUNGLE TEMPLE   (tap for AURORA ICE)");'),
('foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-auto") Auto = true; if (a == "-lf-video") Video = true; }', 'foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-auto") Auto = true; if (a == "-lf-video") Video = true; if (a == "-lf-jungle") Theme = 1; }'),
('            yield return null;\n            StartCoroutine(Auto ? AutoFlow() : BootFlow());', '            yield return null;\n            if (Theme == 1) EnsureJungle();\n            StartCoroutine(Theme == 1 && Auto ? AutoJungle() : (Auto ? AutoFlow() : BootFlow()));'),
('        public void Q(Action a)', '''        public void EnsureJungle()
        {
            if (Jungle == null) { Jungle = new GameObject("LFJungle").AddComponent<LFJungle>(); Jungle.Build(Cam); }
        }
        public void SetTheme(int t)
        {
            Theme = t; World.SetVisualTheme(t == 1); if (t == 1) { EnsureJungle(); Jungle.ShowAll(true); Jungle.SetStatics(false); Jungle.SetLive(false); World.ClearAll(); } else if (Jungle != null) { Jungle.ShowAll(false); World.ClearAll(); World.FloorOn(true); }
        }
        public bool SetOverlays(bool on)
        {
            UI.gameObject.SetActive(on); World.gameObject.SetActive(on); for (int i = 0; i < 3; i++) { if (_tipVis[i] != null && !on) _tipVis[i].gameObject.SetActive(false); if (_hv[i] != null && !on) _hv[i].root.gameObject.SetActive(false); }
            return on;
        }
        public void Q(Action a)'''),
('            World.SetStairs(Mathf.Clamp01(StairBase(_secIdx) + _voiceBonus)); _maxStairs = Mathf.Max(_maxStairs, World.Stairs);', '            if (Theme == 0) { World.SetStairs(Mathf.Clamp01(StairBase(_secIdx) + _voiceBonus)); _maxStairs = Mathf.Max(_maxStairs, World.Stairs); } else if (Jungle != null) { _maxStairs = Mathf.Clamp01(StairBase(_secIdx) + _voiceBonus); Jungle.Tick(Mathf.Max(it, Voice.Level * 0.8f), dt, true); }'),
('            var s = _song.sections[i]; int peaks = 0, builds = 0;', '            if (Theme == 1) { LFLog.Ev("cue", _song.sections[i].kind, i); if (_song.sections[i].kind == "peak") { World.Beat(1.2f); ComicPop("Rise", LFMats.Gold); } else ComicPop("Listen", LFMats.Mint); return; }\n            var s = _song.sections[i]; int peaks = 0, builds = 0;'),
('            State = LFState.Boot; UI.Hint(""); World.ClearAll(); World.VoidBuild = VoidBuild; World.Intensity(0.05f); World.SetFlow(0.45f); World.FloorOn(true);', '            State = LFState.Boot; UI.Hint(""); World.ClearAll(); World.VoidBuild = VoidBuild; World.Intensity(0.05f); World.SetFlow(0.45f); World.FloorOn(Theme == 0);\n            if (Theme == 1 && !_skipOpening) { yield return StartCoroutine(Jungle.Opening(this, 46f)); }'),
])
s = open('LFApp.cs', encoding='utf8').read()
s = s.replace("        public static string ExportDir", "        public bool _skipOpening;\n        public static string ExportDir", 1)
open('LFApp.cs', 'w', encoding='utf8').write(s)

# --- LFAuto: jungle showreel
sub('LFAuto.cs', [('        bool _previewOverride;', '''        bool _previewOverride;
        IEnumerator AutoJungle()
        {
            World.SetVisualTheme(true); World.SetFlow(0f); State = LFState.Boot; World.ClearAll(); Jungle.ShowAll(true); Jungle.SetStatics(false); Jungle.SetLive(false); World.SetVisualTheme(true);
            bool quick = false; foreach (var qa in System.Environment.GetCommandLineArgs()) if (qa == "-lf-quick") quick = true;
            yield return StartCoroutine(Jungle.Opening(this, quick ? 26f : 46f)); World.SetFlow(0.5f);
            _skipOpening = true; StartPerformanceAt(quick ? 46f : 0f, -1f);
            float tBud = 4f; int nd = 0; float[] dT = quick ? new[] { 0f, 0f, 0f, 0f } : new[] { 16f, 25f, 34f, 56f, 66f, 90f, 108f, 120f }; string[] dK = quick ? new[] { "sweep", "rect", "circle", "triangle" } : new[] { "sweep", "rect", "circle", "triangle", "sweep", "circle", "rect", "triangle" };
            if (quick) yield return new WaitForSeconds(8f);
            while (State != LFState.Results || _results == null)
            {
                float dt = Time.deltaTime; tBud -= dt;
                if (nd < dT.Length && (quick || (State == LFState.Perform && _songT >= dT[nd]))) { yield return RunDemo(dK[nd]); nd++; }
                if (State == LFState.Perform && tBud <= 0f && World.BudCount() > 0) { tBud = 6f; var p = World.NearestBud(_head.position); yield return GhostTap(p + (_head.position - p).normalized * 0.12f, 0.45f); yield return GhostTap(p, 0.2f); }
                if (quick && nd >= dT.Length) { yield return new WaitForSeconds(30f); break; }
                yield return null;
            }
            LFLog.Flush(@"E:\\Winter\\metavr\\LastFrequency\\logs\\video\\events.tsv"); Debug.Log("[LF] AUTOPILOT DONE frames=" + LFRecorder.Count);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }''')])
print("patched jungle3")
