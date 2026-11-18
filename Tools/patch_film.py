import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('Assets/Scripts/LFMagic.cs', [
('public static bool JungleMode; float _pushCool,', 'public static bool JungleMode; public static float LastPushT = -99f, LastPushAz; float _pushCool,'),
('float az = Mathf.Atan2(dir.x, dir.z); int layer = LFJungle.I != null ? LFJungle.I.PushOne(az) : 0;', 'float az = Mathf.Atan2(dir.x, dir.z); LastPushT = Time.time; LastPushAz = az; int layer = LFJungle.I != null ? LFJungle.I.PushOne(az) : 0;'),
])
sub('Assets/Scripts/LFRecorder.cs', [
('            float tt = Time.time; Vector3 sway', '            if (LFApp.I != null && LFApp.I.Film && LFApp.I.Dir != null) { _cam.transform.SetPositionAndRotation(LFApp.I.Dir.Pos, LFApp.I.Dir.Rot); _cam.fieldOfView = LFApp.I.Dir.Fov; _cam.Render(); var pv = RenderTexture.active; RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); _tex.Apply(); RenderTexture.active = pv; File.WriteAllBytes(Dir + @"\\f" + (_n++).ToString("D5") + ".jpg", _tex.EncodeToJPG(90)); Count = _n; LFLog.Frame = _n; if (_n % 300 == 0) Debug.Log("[LFVideo] film " + _n); return; }\n            float tt = Time.time; Vector3 sway'),
])
sub('Assets/Scripts/LFApp.cs', [
('public int Theme; public LFJungle Jungle;', 'public bool Film; public LFDirector Dir; LFAvatar _avatar; public int Outfit; LFReplay _replay; string _filmPath; public float SongTime { get { return _songT; } }\n        public int Theme; public LFJungle Jungle;'),
('foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-mr") MixedReality = true;', 'var cl = Environment.GetCommandLineArgs(); for (int q = 0; q < cl.Length - 1; q++) if (cl[q] == "-lf-film") { Film = true; _filmPath = cl[q + 1]; }\n            foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-mr") MixedReality = true;'),
('            Auto |= Video;', '            Auto |= Video | Film; if (Auto) Time.captureFramerate = 30;'),
('            _song = _provider.Load("last_frequency");', '            _song = _provider.Load("last_frequency"); _replay = gameObject.AddComponent<LFReplay>();\n            if (Film) { if (!_replay.Load(_filmPath)) { Debug.LogError("[LF] film: cannot load replay"); Application.Quit(); yield break; } Theme = _replay.Playback.theme; Outfit = _replay.Playback.outfit; _avatar = new GameObject("LFAvatar").AddComponent<LFAvatar>(); _avatar.SetOutfit(Outfit); Dir = new GameObject("LFDirector").AddComponent<LFDirector>(); LFMagic.JungleMode = Theme == 1; }'),
('            StartCoroutine(Theme == 1 && Auto ? AutoJungle() : (Auto ? AutoFlow() : BootFlow()));', '            StartCoroutine(Film ? FilmFlow() : (Theme == 1 && Auto ? AutoJungle() : (Auto ? AutoFlow() : BootFlow())));'),
('            if (_sim) { if (Theme == 1 && Jungle != null && Jungle.Climbing)', '            if (_sim && !Film) { if (Theme == 1 && Jungle != null && Jungle.Climbing)'),
('            for (int i = 0; i < 3; i++) Pointers[i].prev = Pointers[i].pos;', '            for (int i = 0; i < 3; i++) Pointers[i].prev = Pointers[i].pos;\n            if (!Film && State == LFState.Perform && _songOn) _replay.Sample(Cam, Pointers, Voice);\n            if (Film && State == LFState.Perform && _songOn) ApplyReplay(dt);\n            if (State == LFState.Studio && _avatar != null) StudioAvatar(dt);'),
('            if (Voice.Simulated) { float lv = mel > 0', '            if (Film && _replay.Playback != null) { var fr0 = _replay.Playback.f[Mathf.Clamp(Mathf.RoundToInt(_songT * 30f), 0, _replay.Playback.f.Count - 1)]; Voice.Feed(fr0.lvl, fr0.midi); }\n            else if (Voice.Simulated) { float lv = mel > 0'),
('            _songT = offset; _songOn = true;', '            _songT = offset; _songOn = true; if (!Film) _replay.BeginRecord(_song.id, Theme, Outfit);'),
('            _songOn = false; _music.Stop(); _guide.Stop();', '            _songOn = false; if (!Film) _replay.EndRecord(ExportDir); _music.Stop(); _guide.Stop();'),
('        public void EnsureJungle()', '''        // ------------------------------------------------------------ the film: replay the recorded performance through the real app
        IEnumerator FilmFlow()
        {
            World.SetVisualTheme(Theme == 1); UI.gameObject.SetActive(false);
            if (Theme == 1) { EnsureJungle(); LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); }
            _skipOpening = true; StartPerformanceAt(0f, -1f);
            while (State != LFState.Results) yield return null;
            LFLog.Flush(@"E:\\Winter\\metavr\\LastFrequency\\logs\\video\\events.tsv"); Debug.Log("[LF] FILM DONE frames=" + LFRecorder.Count);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        void ApplyReplay(float dt)
        {
            var f = _replay.Playback.f; int i = Mathf.Clamp(Mathf.RoundToInt(_songT * 30f), 0, f.Count - 1); var fr = f[i]; var root = Cam.transform.root;
            root.rotation = fr.hr * Quaternion.Inverse(Cam.transform.localRotation); root.position = fr.hp - root.rotation * Cam.transform.localPosition;
            for (int k = 0; k < 3; k++) { var p = Pointers[k]; p.active = (fr.fl & (1 << k)) != 0; p.pinch = (fr.fl & (1 << (k + 3))) != 0; p.pos = k == 0 ? fr.p0 : (k == 1 ? fr.p1 : fr.p2); }
            Vector3? left = Pointers[0].active ? Pointers[0].pos : (Vector3?)null; Vector3? right = Pointers[1].active ? Pointers[1].pos : (Pointers[2].active ? Pointers[2].pos : (Vector3?)null);
            _avatar.Pose(Cam.transform.position, Cam.transform.rotation, left, right, Voice.Level, World.Pitch01, dt);
        }
        void StudioAvatar(float dt)
        {
            Vector3 f = new Vector3(_head.forward.x, 0, _head.forward.z).normalized; Vector3 r = Vector3.Cross(Vector3.up, f); Vector3 pos = _head.position + f * 1.8f - r * 0.85f + Vector3.up * 0.1f;
            _avatar.Pose(pos, Quaternion.LookRotation(-f), null, null, Voice.Level, 0.5f, dt);
        }
        public void EnsureJungle()'''),
('            UI.Button(_studio, "BACK", new Vector3(0.42f, -0.1f, 0), new Vector2(0.18f, 0.1f), () => Q(BackToMenu), LFMats.Violet);', '''            UI.Button(_studio, "BACK", new Vector3(0.42f, -0.1f, 0), new Vector2(0.18f, 0.1f), () => Q(BackToMenu), LFMats.Violet);
            for (int i = 0; i < 3; i++) { int k = i; _bOutfit[i] = UI.Button(_studio, LFAvatar.OutfitNames[i], new Vector3(-0.2f + i * 0.2f, -0.26f, 0), new Vector2(0.19f, 0.085f), () => Q(() => SetOutfit(k)), i == 0 ? LFMats.Mint : (i == 1 ? LFMats.Gold : LFMats.Violet)); }
            if (_avatar == null) _avatar = new GameObject("LFAvatar").AddComponent<LFAvatar>(); _avatar.Show(true); SetOutfit(Outfit);'''),
('        public void SetTheme(int t)', '''        LFButton[] _bOutfit = new LFButton[3];
        void SetOutfit(int k) { Outfit = k; if (_avatar != null) _avatar.SetOutfit(k); for (int i = 0; i < 3; i++) if (_bOutfit[i] != null) _bOutfit[i].toggled = i == k; }
        public void SetTheme(int t)'''),
('            if (_studio != null) { UI.Remove(_studio); _studio = null; }\n            if (_song == null', '            if (_studio != null) { UI.Remove(_studio); _studio = null; } if (_avatar != null && !Film) _avatar.Show(false);\n            if (_song == null'),
('            if (_studio != null) { UI.Remove(_studio); _studio = null; } if (_results != null)', '            if (_avatar != null && !Film) _avatar.Show(false); if (_studio != null) { UI.Remove(_studio); _studio = null; } if (_results != null)'),
])
sub('Assets/Scripts/LFHandVis.cs', [('bool show = p.active && !_anyTracked;', 'bool show = p.active && !_anyTracked && !Film;')])

mix = os.path.join(S, 'Tools/mix_video.py'); s = open(mix, encoding='utf8').read()
s = s.replace("guide = load(os.path.join(SD, 'guide.ogg'), 'aecho=0.8:0.6:60|120:0.2|0.1')", "guide = load(os.environ.get('TAKE') or os.path.join(SD, 'guide.ogg'), 'aecho=0.8:0.6:60|120:0.2|0.1')")
open(mix, 'w', encoding='utf8').write(s)
print("patched film")
