using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LF
{
    public enum LFState { Boot, Menu, Studio, Perform, Results, Compose, Listen }

    /// <summary>Main controller: Boot (void) -> Menu -> Studio -> Perform -> Results, and Compose. Hands-only input through poke buttons and fingertip proximity.</summary>
    public partial class LFApp : MonoBehaviour
    {
        public static LFApp I;
        public LFState State = LFState.Boot;
        public bool Auto, Video;               // -lf-auto / -lf-video command-line flags (simulation video)
        public Camera Cam; Transform _head;
        public LFMagic Magic; public LFWorld World; public LFUI UI; public LFVoice Voice; LFNotePlayer _notes;
        public readonly Pointer[] Pointers = { new Pointer { id = 0 }, new Pointer { id = 1 }, new Pointer { id = 2 } };   // 0,1 = real fingertips, 2 = ghost (tour/autopilot)
        AudioSource _music, _guide; SongData _song; ISongProvider _provider = new LocalPackProvider();
        // settings (all changeable by hand-poke)
        public bool VoidBuild = true, Duel, GuideOn; public bool Film; public LFDirector Dir; LFAvatar _avatar; public int Outfit; LFReplay _replay; string _filmPath; public float SongTime { get { return _songT; } }
        public LFVoidRoom Room; public int Theme; public LFJungle Jungle; LFButton _bTheme, _bMR; public bool MixedReality; Transform _skyPanel; public int Preset;
        static readonly string[] PresetNames = { "NATURAL", "INTIMATE", "DREAM", "ARENA", "HYPER" };
        static readonly float[] PresetSize = { 0.1f, 0.15f, 0.8f, 1.0f, 0.5f }, PresetWet = { 0.0f, 0.18f, 0.5f, 0.62f, 0.35f };
        // performance state
        float _songT, _lastBeatT; int _beatIdx, _secIdx = -1; bool _songOn; float _voiceBonus, _budTimer, _accTimer, _rivalTimer, _comicCool;
        int _pops, _accHit, _accTot; float _maxStairs; bool _sim; float _previewEnd = -1f; int _bar;
        readonly Queue<Action> _q = new Queue<Action>(); Transform _menu, _studio, _results; LFButton _bVisual, _bMode, _bGuide; LFButton[] _bPreset = new LFButton[5];
        TextMesh _status; Transform _meter; Transform[] _tipVis = new Transform[3];
        string _resultNote = "";
        System.Threading.Tasks.Task<string> _bounceTask;
        public bool _skipOpening; float _simYaw, _simPitch;
        public static string ExportDir { get { return Application.persistentDataPath + "/LastFrequency"; } }

        void Awake()
        {
            I = this; Application.targetFrameRate = 90; QualitySettings.vSyncCount = 0; LFNoise.Init();
            var cl = Environment.GetCommandLineArgs(); for (int q = 0; q < cl.Length - 1; q++) if (cl[q] == "-lf-film") { Film = true; _filmPath = cl[q + 1]; }
            foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-mr") MixedReality = true; if (a == "-lf-probe") gameObject.AddComponent<LFProbe>(); if (a == "-lf-auto") Auto = true; if (a == "-lf-video") Video = true; if (a == "-lf-jungle") Theme = 1; if (a == "-lf-duel") Duel = true; if (a == "-lf-listen") ListenSim = true; if (a == "-lf-cycle") ListenCycle = true; if (a == "-lf-fixed") ListenFixed = true; }
            for (int q2 = 0; q2 < cl.Length - 1; q2++) if (cl[q2] == "-lf-element") { int ei; if (int.TryParse(cl[q2 + 1], out ei)) Element = (LFEl)Mathf.Clamp(ei, 0, 3); }
            if (ListenSim) Auto = true;
            Auto |= Video | Film; if (Auto) Time.captureFramerate = 30;
        }

        IEnumerator Start()
        {
            Cam = Camera.main; if (Cam == null) { var g = new GameObject("Cam"); Cam = g.AddComponent<Camera>(); g.tag = "MainCamera"; g.transform.position = new Vector3(0, 1.35f, 0); }
            _head = Cam.transform; Cam.backgroundColor = new Color(0.01f, 0.02f, 0.08f); Cam.clearFlags = CameraClearFlags.SolidColor; Cam.nearClipPlane = 0.05f; Cam.farClipPlane = 300f;
            if (!Application.isMobilePlatform) { Cam.transform.root.position = new Vector3(0, 1.3f, 0); }
            _sim = Auto; if (Auto) foreach (var hh in FindObjectsByType<OVRHand>(FindObjectsSortMode.None)) hh.gameObject.SetActive(false); BuildHandVis();
            Room = new GameObject("LFVoidRoom").AddComponent<LFVoidRoom>(); Room.Init(_head, Film ? 1.9f : 1f);   // the film camera orbits wider than the player sits, so its room is larger
            World = new GameObject("LFWorld").AddComponent<LFWorld>(); World.pointers = Pointers; World.Init(_head); Magic = gameObject.AddComponent<LFMagic>(); World.OnPop += OnPop; World.OnClaim += OnClaim;
            UI = new GameObject("LFUI").AddComponent<LFUI>(); UI.pointers = Pointers; UI.Init(_head); UI.followPos = () => PanelPos(0.64f, -0.13f);
            Voice = new GameObject("LFVoice").AddComponent<LFVoice>(); Voice.Simulated = _sim; Voice.OnBeat += s => { if (Voice.Listen) World.Beat(0.25f + s * 0.45f); }; gameObject.AddComponent<LFComfort>();
            _notes = new GameObject("LFNotes").AddComponent<LFNotePlayer>(); _notes.transform.position = _head.position; Magic.Init(World, Pointers, _head, _notes);
            _music = new GameObject("Music").AddComponent<AudioSource>(); _music.spatialBlend = 0; _music.playOnAwake = false; _guide = new GameObject("Guide").AddComponent<AudioSource>(); _guide.spatialBlend = 0; _guide.playOnAwake = false;
            for (int i = 0; i < 3; i++) { var o = new GameObject("Tip" + i); o.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("orb"); var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = LFMats.Toon(LFMats.Gold); o.transform.localScale = Vector3.one * 0.016f; var tr = o.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 0.95f; tr.minVertexDistance = 0.008f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.05f), new Keyframe(1, 0f)); tr.numCapVertices = 4; var tg = new Gradient(); tg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#7fd0ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#7a6cff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = tg; tr.emitting = false; o.SetActive(false); _tipVis[i] = o.transform; }
            _song = _provider.Load("last_frequency"); _replay = gameObject.AddComponent<LFReplay>();
            if (Film) { if (!_replay.Load(_filmPath)) { Debug.LogError("[LF] film: cannot load replay"); Application.Quit(); yield break; } Theme = _replay.Playback.theme; Outfit = _replay.Playback.outfit; LFAvatar.ShadowLook = true; _avatar = new GameObject("LFAvatar").AddComponent<LFAvatar>(); _avatar.SetOutfit(Outfit); Dir = new GameObject("LFDirector").AddComponent<LFDirector>(); LFMagic.JungleMode = Theme == 1; }
            if (Video) { var rec = new GameObject("LFRecorder").AddComponent<LFRecorder>(); rec.Init(Cam); }
            yield return null; EnsureAudio(); LFNoise.HeadsetPerf();
            if (Theme == 1) EnsureJungle();
            if (!Film && Theme == 0) World.SetElement(Element);
            StartCoroutine(ListenSim ? ListenAuto() : Film ? FilmFlow() : (Theme == 1 && Auto ? AutoJungle() : (Auto ? AutoFlow() : BootFlow())));
        }

        /// <summary>Passthrough: the void becomes your real room, the world is drawn into it, and your hands keep a ring of the real room around them.</summary>
        public void SetMixedReality(bool on)
        {
            MixedReality = on; if (Jungle != null) Jungle.MixedReality = on;
            if (Application.isMobilePlatform)
            {
                var mgr = FindFirstObjectByType<OVRManager>(); if (mgr != null)
                {
                    mgr.isInsightPassthroughEnabled = on; var pt = mgr.gameObject.GetComponent<OVRPassthroughLayer>(); if (on && pt == null) { pt = mgr.gameObject.AddComponent<OVRPassthroughLayer>(); } if (pt != null) pt.enabled = on;
                }
            }
            Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = on ? new Color(0, 0, 0, 0) : new Color(0.01f, 0.02f, 0.08f);
            // Aurora Ice: the sky dome and the dark void room are opaque and would cover the passthrough underlay, so they step aside (the jungle clips its own sky)
            if (World != null) World.SetPassthrough(on); if (Room != null) Room.Hidden = on;
        }
        void BuildSkyPanel()
        {
            // optional mood controls: kept 38 deg to the left, out of the forward push path, but raised so the lowest row is ~32 deg down (was 41, below a desk top)
            if (_skyPanel != null) return; var fwd = Quaternion.Euler(0, -38f, 0) * new Vector3(_head.forward.x, 0, _head.forward.z).normalized; _skyPanel = UI.Panel("Sky", _head.position + fwd * 0.58f + Vector3.down * 0.27f);
            string[] rows = { "TIME", "RAIN", "FOG" }; for (int i = 0; i < 3; i++) { string r = rows[i]; float y = 0.1f - i * 0.1f; UI.Button(_skyPanel, r + " -", new Vector3(-0.09f, y, 0), new Vector2(0.16f, 0.085f), () => Q(() => SkyAdj(r, -1)), LFMats.Ice); UI.Button(_skyPanel, r + " +", new Vector3(0.09f, y, 0), new Vector2(0.16f, 0.085f), () => Q(() => SkyAdj(r, 1)), LFMats.Gold); }
        }
        void SkyAdj(string what, int dir)
        {
            if (Jungle == null) return; LFLog.Ev("sfx", "tick");
            if (what == "TIME") Jungle.TimeOfDay = Mathf.Clamp01(Jungle.TimeOfDay + dir * 0.14f); else if (what == "RAIN") Jungle.RainAmt = Mathf.Clamp01(Jungle.RainAmt + dir * 0.25f); else Jungle.FogAmt = Mathf.Clamp01(Jungle.FogAmt + dir * 0.25f);
        }
        // ------------------------------------------------------------ the film: replay the recorded performance through the real app
        IEnumerator FilmFlow()
        {
            World.SetVisualTheme(Theme == 1); UI.gameObject.SetActive(false);
            if (Theme == 1) { EnsureJungle(); LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); }
            _skipOpening = true; StartPerformanceAt(0f, -1f);
            while (State != LFState.Results) yield return null;
            LFLog.Flush(@"E:\Winter\metavr\LastFrequency\logs\video\events.tsv"); Debug.Log("[LF] FILM DONE frames=" + LFRecorder.Count);
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
        public void EnsureJungle()
        {
            if (Jungle == null) { Jungle = new GameObject("LFJungle").AddComponent<LFJungle>(); Jungle.Build(Cam); Jungle.MixedReality = MixedReality; if (MixedReality) SetMixedReality(true); }
        }
        LFButton[] _bOutfit = new LFButton[3];
        void SetOutfit(int k) { Outfit = k; if (_avatar != null) _avatar.SetOutfit(k); for (int i = 0; i < 3; i++) if (_bOutfit[i] != null) _bOutfit[i].toggled = i == k; }
        public void SetTheme(int t)
        {
            Theme = t; LFMagic.JungleMode = t == 1; World.SetVisualTheme(t == 1); if (t == 1) { EnsureJungle(); Jungle.ShowAll(true); Jungle.SetStatics(false); Jungle.SetLive(false); World.ClearAll(); } else if (Jungle != null) { Jungle.ShowAll(false); World.ClearAll(); World.FloorOn(true); World.SetElement(Element); }
        }
        public bool SetOverlays(bool on)
        {
            UI.gameObject.SetActive(on); World.gameObject.SetActive(on); for (int i = 0; i < 3; i++) { if (_tipVis[i] != null && !on) _tipVis[i].gameObject.SetActive(false); if (_hv[i] != null && !on) _hv[i].root.gameObject.SetActive(false); }
            return on;
        }
        public void Q(Action a) { _q.Enqueue(a); }

        void Update()
        {
            float dt = Time.deltaTime; LFLog.Story += dt;
            if (!Auto) ReadHands();
            for (int i = 0; i < 3; i++) { var p = Pointers[i]; p.vel = (p.pos - p.prev) / Mathf.Max(dt, 1e-4f); { bool was = _tipVis[i].gameObject.activeSelf; _tipVis[i].gameObject.SetActive(p.active); if (p.active) { _tipVis[i].position = p.pos; var trr = _tipVis[i].GetComponent<TrailRenderer>(); if (!was) trr.Clear(); trr.emitting = true; } } }
            if (State == LFState.Studio) StudioUpdate(dt);
            if (State == LFState.Perform) PerformUpdate(dt);
            if (State == LFState.Compose) ComposeUpdate(dt);
            if (State == LFState.Listen) ListenUpdate(dt);
            if (Room != null)
            {
                float tg = 0f;
                if (State == LFState.Perform) tg = Theme == 1 ? (Jungle != null ? Mathf.Clamp01(Jungle.Pushes / 4f) : 0f) : Mathf.Clamp01(_songT / 16f);
                else if (State == LFState.Listen) tg = Mathf.Clamp01(_listenT / 14f);
                else if (State == LFState.Results || State == LFState.Compose) tg = 1f;
                Room.Target = tg;
            }
            for (int i = 0; i < 3; i++) Pointers[i].prev = Pointers[i].pos;
            if (!Film && State == LFState.Perform && _songOn) _replay.Sample(Cam, Pointers, Voice);
            if (Film && State == LFState.Perform && _songOn) ApplyReplay(dt);
            if (State == LFState.Studio && _avatar != null) StudioAvatar(dt);
            if (_bounceTask != null && _bounceTask.IsCompleted) { var r = _bounceTask.Result; _bounceTask = null; OnBounceDone(r); }
        }

        // ----------------------------------------------------------------- real hands (Quest)
        OVRHand[] _hands = new OVRHand[0]; Transform[] _idx = new Transform[0]; float _refresh; bool _skelLogged;
        void ReadHands()
        {
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh <= 0f) { _refresh = 1f; _hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None); _idx = new Transform[_hands.Length]; }
            Pointers[0].active = Pointers[1].active = false; int n = 0;
            for (int i = 0; i < _hands.Length && n < 2; i++)
            {
                var h = _hands[i]; if (h == null || !h.IsTracked) continue; var sk = h.GetComponent<OVRSkeleton>(); if (sk == null || !sk.IsDataValid || sk.Bones == null) continue;
                if (_idx[i] == null)
                {
                    // The project uses the OpenXR hand skeleton (OculusRuntimeSettings handSkeletonVersion 1): its index tip is XRHand_IndexTip (10).
                    // The legacy id Hand_IndexTip (20) is XRHand_RingTip in that skeleton, so the pointer would have been the ring finger. Pick by the type the runtime reports.
                    var st = sk.GetSkeletonType(); bool xr = st == OVRSkeleton.SkeletonType.XRHandLeft || st == OVRSkeleton.SkeletonType.XRHandRight;
                    var want = xr ? OVRSkeleton.BoneId.XRHand_IndexTip : OVRSkeleton.BoneId.Hand_IndexTip;
                    foreach (var b in sk.Bones) if (b.Id == want) _idx[i] = b.Transform;
                    if (!_skelLogged) { _skelLogged = true; Debug.Log("[LF] hands: skeleton " + st + ", " + sk.Bones.Count + " bones, pointer bone " + want + (_idx[i] != null ? " found" : " MISSING")); }
                }
                if (_idx[i] == null) continue; var p = Pointers[n++]; p.active = true; p.pos = _idx[i].position; p.pinch = h.GetFingerIsPinching(OVRHand.HandFinger.Index);
            }
        }

        // ----------------------------------------------------------------- flows
        IEnumerator BootFlow()
        {
            yield return StartCoroutine(BootIntro()); BuildMenu(); State = LFState.Menu;
            // first launch: one obvious path. Skip the full menu and open the studio for the song; the menu (compose, worlds, options) comes after the first song.
            if (PlayerPrefs.GetInt("lf_first_done", 0) == 0) { PlayerPrefs.SetInt("lf_first_done", 1); PlayerPrefs.Save(); Q(OpenStudio); }
        }
        IEnumerator BootIntro()
        {
            State = LFState.Boot; World.ClearAll(); World.VoidBuild = VoidBuild; World.Intensity(0.05f); World.SetFlow(0f); World.FloorOn(false);
            yield return new WaitForSeconds(1.6f); LFLog.Ev("boot", "hum");
            { int[] bs = LFSynth.ScaleNotes(74, true, 10); for (int i = 0; i < bs.Length; i++) LFSynth.Samples(Instr.Bell, bs[i]); }   // pre-render the pop notes so the first pop never hitches
            UI.Big("THE LAST FREQUENCY", 3.8f); yield return new WaitForSeconds(3.4f);
        }

        // ----------------------------------------------------------------- menu
        public Vector3 PanelPos(float dist = 0.62f, float dy = -0.2f) { var f = _head.forward; f.y = 0; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize(); return _head.position + f * dist + Vector3.up * dy; }

        void BuildMenu()
        {
            if (_menu != null) UI.Remove(_menu);
            _menu = UI.Panel("Menu", PanelPos(0.64f, -0.13f)); _menu.localScale = Vector3.one * 0.82f; UI.follow.Add(_menu);
            _menuMain = new GameObject("Main").transform; _menuMain.SetParent(_menu, false); _menuOpt = new GameObject("Options").transform; _menuOpt.SetParent(_menu, false);
            // page 1: what you can do. SING is the first action: the widest, glowing button on top, with a one-line instruction under the title.
            // (seated eye at the origin: title ~19 degrees up, SING ~10 degrees up, bottom row ~31 degrees down, all 0.64 m away)
            UI.Label(_menuMain, "THE LAST FREQUENCY", new Vector3(0f, 0.435f, 0), 0.0055f, Color.white);
            UI.Label(_menuMain, "Start here: poke SING, then sing or hum along.", new Vector3(0f, 0.395f, 0), 0.0036f, LFUtil.Hex("#fff3b0"));
            var bSing = UI.Button(_menuMain, "SING: LIGHT THE SILENCE", new Vector3(0f, 0.31f, 0), new Vector2(0.9f, 0.12f), () => Q(OpenStudio), LFMats.Cyan); bSing.toggled = true;
            UI.Button(_menuMain, "COMPOSE YOUR OWN", new Vector3(-0.22f, 0.01f, 0), new Vector2(0.52f, 0.13f), () => Q(OpenCompose), LFMats.Gold);
            UI.Button(_menuMain, "SHOW ME THE FLOW", new Vector3(-0.22f, -0.15f, 0), new Vector2(0.52f, 0.13f), () => Q(StartTour), LFMats.Violet);
            _bVisual = UI.Button(_menuMain, "", new Vector3(0.24f, 0.17f, 0), new Vector2(0.4f, 0.13f), () => Q(() => { VoidBuild = !VoidBuild; RefreshToggles(); }), LFMats.Mint);
            _bMode = UI.Button(_menuMain, "", new Vector3(0.24f, 0.01f, 0), new Vector2(0.4f, 0.13f), () => Q(() => { Duel = !Duel; RefreshToggles(); }), LFMats.Magenta);
            _bGuide = UI.Button(_menuMain, "", new Vector3(0.24f, -0.15f, 0), new Vector2(0.4f, 0.13f), () => Q(() => { GuideOn = !GuideOn; RefreshToggles(); }), LFMats.Ice);
            _bTheme = UI.Button(_menuMain, "", new Vector3(-0.15f, -0.31f, 0), new Vector2(0.6f, 0.12f), () => Q(() => { SetTheme(1 - Theme); RefreshToggles(); }), LFMats.Gold);
            UI.Button(_menuMain, "OPTIONS", new Vector3(0.31f, -0.31f, 0), new Vector2(0.28f, 0.12f), () => Q(() => ShowOptions(true)), LFMats.Violet);
            // page 2: elements, mixed reality, comfort and access
            _bMR = UI.Button(_menuOpt, "", new Vector3(0f, 0.17f, 0), new Vector2(0.9f, 0.12f), () => Q(() => { SetMixedReality(!MixedReality); RefreshToggles(); }), LFMats.Mint);
            _bListen = UI.Button(_menuOpt, "", new Vector3(-0.31f, 0.01f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { if (Voice != null) Voice.Listen = !Voice.Listen; RefreshToggles(); }), LFMats.Cyan);
            _bCalm = UI.Button(_menuOpt, "", new Vector3(0f, 0.01f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { LFComfort.Calm = !LFComfort.Calm; RefreshToggles(); }), LFMats.Mint);
            _bNoMic = UI.Button(_menuOpt, "", new Vector3(0.31f, 0.01f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { LFComfort.NoMic = !LFComfort.NoMic; if (Voice != null) Voice.SetNoMic(LFComfort.NoMic || _sim); RefreshToggles(); }), LFMats.Violet);
            UI.Button(_menuOpt, "BACK", new Vector3(0f, -0.15f, 0), new Vector2(0.4f, 0.12f), () => Q(() => ShowOptions(false)), LFMats.Violet);
            BuildElementRow(); RefreshToggles(); ShowOptions(false);
        }
        Transform _menuMain, _menuOpt;
        void ShowOptions(bool on)
        {
            if (_menu == null) return; UI.SetVisible(_menuMain, !on); UI.SetVisible(_menuOpt, on);
            UI.Hint(on ? "Pick the element the world is made of, mixed reality, and comfort options" : "Poke a button with your fingertip. Your voice builds the world.");
        }
        LFButton _bListen, _bCalm, _bNoMic;
        void RefreshToggles()
        {
            UI.SetText(_bListen, Voice != null && Voice.Listen ? "LISTEN TO ROOM: ON" : "LISTEN TO ROOM: OFF"); UI.SetText(_bCalm, LFComfort.Calm ? "CALM MODE: ON" : "CALM MODE: OFF"); UI.SetText(_bNoMic, LFComfort.NoMic ? "NO MIC: ON" : "NO MIC: OFF");
            UI.SetText(_bVisual, VoidBuild ? "VISUAL: VOID BUILD" : "VISUAL: CLASSIC STAGE"); UI.SetText(_bMode, Duel ? "MODE: RIVAL DUEL" : "MODE: SOLO"); UI.SetText(_bGuide, GuideOn ? "GUIDE VOCAL: ON" : "GUIDE VOCAL: OFF"); UI.SetText(_bMR, MixedReality ? "MIXED REALITY: ON  (your room opens into the world)" : "MIXED REALITY: OFF  (fully virtual)"); UI.SetText(_bTheme, Theme == 0 ? "WORLD: AURORA ICE  (CHANGE)" : "WORLD: JUNGLE TEMPLE  (CHANGE)");
        }

        // ----------------------------------------------------------------- studio
        void OpenStudio()
        {
            State = LFState.Studio; CloseShare(); if (_menu != null) UI.Remove(_menu); _menu = null; UI.Hint("");
            // seated eye at the origin, panel 0.62 m away and 0.13 m below: presets ~4 deg up, START ~20 deg down, outfits ~32 deg down (was 37, at desk height), BACK ~29 deg right (was 34)
            _studio = UI.Panel("Studio", PanelPos(0.62f, -0.13f)); UI.follow.Add(_studio);
            for (int i = 0; i < 5; i++) { int k = i; _bPreset[i] = UI.Button(_studio, PresetNames[i], new Vector3(-0.36f + i * 0.18f, 0.17f, 0), new Vector2(0.165f, 0.1f), () => Q(() => SetPreset(k)), i % 5 + 0); }
            UI.Button(_studio, "START THE SHOW", new Vector3(-0.04f, -0.1f, 0), new Vector2(0.46f, 0.14f), () => Q(StartPerformance), LFMats.Magenta);
            UI.Button(_studio, "BACK", new Vector3(0.35f, -0.1f, 0), new Vector2(0.16f, 0.1f), () => Q(BackToMenu), LFMats.Violet);
            for (int i = 0; i < 3; i++) { int k = i; _bOutfit[i] = UI.Button(_studio, LFAvatar.OutfitNames[i], new Vector3(-0.2f + i * 0.2f, -0.26f, 0), new Vector2(0.19f, 0.085f), () => Q(() => SetOutfit(k)), i == 0 ? LFMats.Mint : (i == 1 ? LFMats.Gold : LFMats.Violet)); }
            if (_avatar == null) _avatar = new GameObject("LFAvatar").AddComponent<LFAvatar>(); _avatar.Show(true); SetOutfit(Outfit);
            var mg = new GameObject("Meter"); mg.transform.SetParent(_studio, false); mg.transform.localPosition = new Vector3(-0.36f, 0.04f, -0.004f); var mq = new GameObject("MeterQuad"); mq.transform.SetParent(mg.transform, false); mq.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad");
            var mr = mq.AddComponent<MeshRenderer>(); mr.sharedMaterial = LFMats.Ring(Color.white); _meter = mq.transform; _meter.localScale = new Vector3(0.01f, 0.02f, 1);
            _meter.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("LF/Panel")) { }; _meter.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Aspect", 8f);
            SetPreset(Preset);
            // a judge who refused the microphone (or turned on NO MIC) must not be told to sing at a meter that never moves
            _studioMic = -1; StudioHint();
            World.Intensity(0.25f);
        }
        int _studioMic = -1;
        void StudioHint()
        {
            int live = _sim || (Voice.MicOk && !Voice.Simulated) ? 1 : 0; if (live == _studioMic) return; _studioMic = live;   // re-checked every frame: the permission may be granted while the studio is open
            UI.Hint(live == 1 ? "Hum a note: the floor answers your voice. Then poke START THE SHOW and sing along." : "Microphone is off, so the song will lead and your hands still build. Poke START THE SHOW.");
        }
        void SetPreset(int p)
        {
            Preset = p; for (int i = 0; i < 5; i++) if (_bPreset[i] != null) _bPreset[i].toggled = i == p; LFLog.Ev("sfx", "tick");
        }
        void StudioUpdate(float dt)
        {
            if (Auto && Voice.Simulated) Voice.Feed(0.35f + 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 1.7f)), 62f + 3f * Mathf.Sin(Time.time));
            if (!_tourRunning) StudioHint();
            float l = Voice.Level; if (_meter != null) { _meter.localScale = new Vector3(Mathf.Max(0.01f, l * 0.72f), 0.02f, 1); _meter.localPosition = new Vector3(l * 0.36f, 0, 0); }
            World.Intensity(0.25f + l * 0.6f); if (l > 0.35f && Time.frameCount % 20 == 0) { World.RingAt(new Vector3(0, 0.02f, 0), LFMats.Tint(LFMats.Cyan), 1.5f + l * 2.5f, true); World.Beat(l); }
        }
        void BackToMenu()
        {
            if (_avatar != null && !Film) _avatar.Show(false); if (_studio != null) { UI.Remove(_studio); _studio = null; } if (_results != null) { UI.Remove(_results); _results = null; }
            CloseShare(); State = LFState.Menu; BuildMenu(); World.Intensity(0.2f); World.SetStairs(0f);
        }

        // ----------------------------------------------------------------- performance
        void StartPerformance() { if (_tourRunning) StartPerformanceAt(44f, 16f); else StartPerformanceAt(0f, -1f); }
        void StartPerformanceAt(float offset, float previewSeconds)
        {
            if (_studio != null) { UI.Remove(_studio); _studio = null; } if (_avatar != null && !Film) _avatar.Show(false);
            if (_song == null || _song.inst == null) { UI.Hint("Song pack missing"); return; }
            StartCoroutine(PerformRoutine(offset, previewSeconds));
        }
        IEnumerator PerformRoutine(float offset, float previewSeconds)
        {
            _song = _provider.Load(Theme == 1 ? "wake_the_green" : "last_frequency"); State = LFState.Boot; UI.Hint(""); World.ClearAll(); World.VoidBuild = VoidBuild; World.Intensity(0.05f); World.SetFlow(0.45f); World.FloorOn(Theme == 0);
            if (Theme == 1 && !_skipOpening) { Jungle.StartVoid(); LFMagic.JungleMode = true; UI.Hint("Raise a hand and push outward. Listen."); }
            _pops = _accHit = _accTot = 0; _maxStairs = 0; _voiceBonus = 0; _budTimer = 0; _rivalTimer = 0; _bar = 0; _secIdx = -1; _beatIdx = 0;
            if (Theme == 0) { for (int i = 3; i >= 1; i--) { UI.Big(i.ToString(), 0.9f); LFLog.Ev("sfx", "count"); yield return new WaitForSeconds(1f); } UI.Big("SING!", 1.2f); } else { _music.volume = 0f; }
            _music.clip = _song.inst; _music.volume = Theme == 1 ? 0.0f : 0.9f; _music.time = offset; _music.Play(); LFLog.Ev("music", "inst", offset); if (Theme == 0) World.Frost(new Vector3(_head.position.x, 0f, _head.position.z), 8.5f, true);
            if (GuideOn || _sim) { _guide.clip = _song.guideClip; _guide.volume = GuideOn ? 0.45f : 0f; _guide.time = offset; _guide.Play(); if (GuideOn) LFLog.Ev("music", "guide", offset); }
            if (!_sim && !Voice.MicOk && !Voice.Simulated) { Voice.Simulated = true; Debug.Log("[LF] no microphone: the song leads this take"); }
            if (!_sim) Voice.BeginTake();
            _songT = offset; _songOn = true; if (!Film) _replay.BeginRecord(_song.id, Theme, Outfit); _previewEnd = previewSeconds > 0 ? offset + previewSeconds : -1f;
            while (_beatIdx < _song.beats.Length && _song.beats[_beatIdx] < offset) _beatIdx++;
            UI.ShowLyrics(true); State = LFState.Perform;
        }

        void PerformUpdate(float dt)
        {
            if (!_songOn) return;
            if (_sim) _songT += dt; else if (_music.isPlaying) { _songT = _music.time; _stall = 0f; } else if (!_focused) { _stall = 0f; } else { _stall += Mathf.Min(Time.unscaledDeltaTime, 0.05f); if (_stall > 1f) { _stall = 0f; EndPerformance(_previewEnd > 0); return; } }
            LFLog.Story = _songT;
            float dur = _song.duration;
            // voice (simulated: follow the guide melody so the world reacts exactly as a real singer would)
            float mel = _song.MelodyAt(_songT);
            if (Film && _replay.Playback != null) { var fr0 = _replay.Playback.f[Mathf.Clamp(Mathf.RoundToInt(_songT * 30f), 0, _replay.Playback.f.Count - 1)]; Voice.Feed(fr0.lvl, fr0.midi); }
            else if (Voice.Simulated) { float lv = mel > 0 ? 0.55f + 0.35f * Mathf.Sin(_songT * 5.1f) * Mathf.Sin(_songT * 1.3f) + 0.25f * _song.Intensity(_songT) : 0f; Voice.Feed(Mathf.Clamp01(lv), mel > 0 ? mel + 0.2f * Mathf.Sin(_songT * 7f) : 0f); }
            // sections
            int si = _song.SectionAt(_songT); if (si != _secIdx) { _secIdx = si; OnSection(si); }
            if (Theme == 1) _music.volume = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(_songT / 14f)); float it = _song.Intensity(_songT); World.Intensity(Mathf.Max(it, Voice.Level * 0.9f * (it + 0.25f)));
            // beats
            while (_beatIdx < _song.beats.Length && _song.beats[_beatIdx] <= _songT)
            {
                bool down = _beatIdx % 4 == 0; World.Beat(down ? 0.6f + it * 0.5f : 0.25f + it * 0.3f); if (down) { _bar++; if (Duel && _bar % 2 == 0 && UnityEngine.Random.value < 0.75f) World.Claim(2); }
                _beatIdx++;
            }
            // staircase: song progress + sustained notes
            if (Voice.SustainSteps > _lastSustain) { _voiceBonus = Mathf.Min(0.3f, _voiceBonus + 0.04f); World.RingAt(new Vector3(0, 0.03f, 0), LFMats.Tint(LFMats.Gold), 3.2f, true); ComicPop("Resonate", LFMats.Gold); if (Duel) World.Claim(1); }
            _lastSustain = Voice.SustainSteps;
            if (Theme == 0) { World.SetStairs(Mathf.Clamp01(StairBase(_secIdx) + _voiceBonus)); _maxStairs = Mathf.Max(_maxStairs, World.Stairs); } else if (Jungle != null) { _maxStairs = Mathf.Clamp01(StairBase(_secIdx) + _voiceBonus); Jungle.Tick(Mathf.Max(it, Voice.Level * 0.8f), dt, true); Jungle.AutoAssist(_songT); }
            // buds to pop
            _budTimer -= dt; if (_budTimer <= 0 && World.BudCount() < 5 && _secIdx >= 0) { World.SpawnBuds(1); _budTimer = 2.2f; }
            // accuracy (real mic only)
            _accTimer -= dt; if (_accTimer <= 0) { _accTimer = 0.25f; if (mel > 0) { _accTot++; if (Voice.Voiced) { float d = Mathf.Abs(Mathf.Repeat(Voice.Midi - mel + 6f, 12f) - 6f); if (d < 1.6f) _accHit++; } } }
            { float mm = Voice.Midi > 0f ? Voice.Midi : 62f; World.SetTimbre(Mathf.InverseLerp(52f, 84f, mm), Voice.Level); }
            if (Theme == 0) Conduct(dt);
            if (Theme == 1 && Jungle != null) { Jungle.Hand0 = Pointers[0].active ? Pointers[0].pos : new Vector3(0, -999, 0); Jungle.Hand1 = Pointers[1].active ? Pointers[1].pos : new Vector3(0, -999, 0); if (_skyPanel == null && _songT > 4f) BuildSkyPanel(); }
            if (_sim && !Film) { if (Theme == 1 && Jungle != null && Jungle.Climbing) { float d = Mathf.DeltaAngle(_simYaw, 0f); _simYaw += Mathf.Clamp(d, -25f * dt, 25f * dt) * (Mathf.Abs(d) > 0.5f ? 1f : 0f); _simPitch = Mathf.Lerp(_simPitch, -Mathf.Clamp01((Jungle.Climbed - 26f) / 14f) * 28f, 0.03f); } else _simYaw += 3.5f * dt; Cam.transform.root.rotation = Quaternion.Euler(_simPitch, _simYaw, 0f); }   // the world is 360: the simulation view slowly turns through it
            UI.Lyrics(_song, _songT);
            if ((_previewEnd > 0 && _songT >= _previewEnd) || _songT >= dur - 0.2f) EndPerformance(_previewEnd > 0 && _songT >= _previewEnd);
        }
        int _lastSustain; float _conductCool, _stall; bool _focused = true;
        // Meta button, headset off, system dialog: the song must not be ended by the stall guard while the app is in the background, and it picks up where it was.
        void OnApplicationFocus(bool f) { _focused = f; if (f) ResumeSong(); }
        void OnApplicationPause(bool p) { _focused = !p; if (!p) ResumeSong(); }
        void ResumeSong()
        {
            _stall = 0f; if (_sim || !_songOn || State != LFState.Perform || _music == null || _music.clip == null || _music.isPlaying) return;
            float t = Mathf.Clamp(_songT, 0f, _music.clip.length - 0.05f); if (t >= _song.duration - 1f) return;
            _music.time = t; _music.Play(); if (GuideOn && _guide.clip != null) { _guide.time = Mathf.Min(t, _guide.clip.length - 0.05f); _guide.Play(); }
            Debug.Log("[LF] resumed the song at " + t.ToString("F1") + " s after the app was in the background");
        }
        float StairBase(int si) { if (_song.sections == null || si < 0) return 0f; var s = _song.sections[si]; float u = Mathf.Clamp01(_songT / _song.duration); int peaks = 0; for (int i = 0; i <= si; i++) if (_song.sections[i].kind == "peak") peaks++; return s.kind == "quiet" && si == 0 ? 0f : Mathf.Clamp01(0.12f + peaks * 0.3f + u * 0.25f); }

        void OnSection(int i)
        {
            if (Theme == 1) { LFLog.Ev("cue", _song.sections[i].kind, i); if (_song.sections[i].kind == "peak") World.Beat(0.5f); return; }
            var s = _song.sections[i]; int peaks = 0, builds = 0; for (int k = 0; k <= i; k++) { if (_song.sections[k].kind == "peak") peaks++; else if (_song.sections[k].kind == "build") builds++; }
            LFLog.Ev("cue", s.kind, i);
            switch (s.kind)
            {
                case "quiet":
                    if (i == 0) { World.SetFlow(0.5f); World.SpawnBuds(2); UI.ComicText("Listen...", PanelPos(1.8f, 0.2f), LFUtil.Hex("#9fe8ff")); }
                    else { World.SetFlow(0.4f); UI.Big("HOLD THE LAST NOTE", 2.5f); }
                    break;
                case "build":
                    World.SetFlow(0.7f); World.Wave("spires", builds == 1 ? 8 : 6, builds, !VoidBuild && false); World.Wave("shards", 7, builds + 2, false); World.SpawnBuds(2); ComicPop("Rise", LFMats.Cyan);
                    break;
                case "peak":
                    World.SetFlow(1f); World.Wave("arches", 4, peaks, false); World.Wave("skyorbs", 10, peaks + 1, false); World.Wave(peaks >= 2 ? "tallspires" : "shards", peaks >= 2 ? 10 : 8, peaks + 3, false);
                    if (peaks >= 2) World.Wave("crown", 8, 5, false); World.Beat(1.2f); World.Frost(new Vector3(_head.position.x, 0f, _head.position.z), 11f, true); ComicPop(peaks >= 2 ? "Aurora" : "Shimmer", LFMats.Magenta);
                    if (Duel) UI.Big("RIVAL DUEL: CLAIM THE WORLD", 2.5f);
                    break;
            }
        }
        void ComicPop(string s, int pal)
        {
            if (Time.time < _comicCool) return; _comicCool = Time.time + 0.5f; var f = _head.forward; f.y = 0; f.Normalize();
            UI.ComicText(s, _head.position + f * 2.1f + Vector3.up * (0.15f + UnityEngine.Random.Range(-0.1f, 0.35f)) + _head.right * UnityEngine.Random.Range(-0.9f, 0.9f), LFMats.Tint(pal), 1f);
        }
        void Conduct(float dt)
        {
            _conductCool -= dt; var a = Pointers[0]; var b = Pointers[1]; if (!a.active || !b.active || _conductCool > 0f) return;
            if (a.pos.y > _head.position.y + 0.05f && b.pos.y > _head.position.y + 0.05f) { _conductCool = 1.3f; World.Burst((a.pos + b.pos) * 0.5f, LFMats.Tint(LFMats.Gold), 70, 2.2f); World.Beat(1f); World.RingAt(new Vector3(0, 0.03f, 0), LFMats.Tint(LFMats.Magenta), 4f, true); _voiceBonus = Mathf.Min(0.3f, _voiceBonus + 0.03f); ComicPop("Rise", LFMats.Gold); LFLog.Ev("sfx", "conduct"); }
        }

        void OnPop(Vector3 pos, int kind)
        {
            _pops++; int[] sc = LFSynth.ScaleNotes(62, true, 10); int midi = sc[Mathf.Abs((int)(pos.x * 7 + pos.y * 11 + kind)) % 10] + 12;
            if (LFSynth.Samples(Instr.Bell, midi) != null) { _notes.transform.position = pos; _notes.Play(Instr.Bell, midi, 0.9f, pos); }
            LFLog.Ev("pop", midi.ToString(), 0.9f); ComicPop(new[] { "Bloom", "Shimmer", "Glow", "Echo", "Frost", "Spark" }[kind % 6], kind);
            if (Duel) World.Claim(1);
        }
        void OnClaim(int owner, int total) { }

        void EndPerformance(bool previewOnly)
        {
            _songOn = false; if (!Film) _replay.EndRecord(ExportDir); StartCoroutine(FadeStop(_music, 3.5f)); _guide.Stop(); UI.ShowLyrics(false); Voice.EndTake(); LFLog.Ev("music", "stop", 0);
            if (previewOnly) { StartCoroutine(PreviewEnd()); return; }
            StartCoroutine(FinaleThenResults());
        }
        IEnumerator PreviewEnd() { UI.Big("...AND THEN YOU SING IT FOR REAL", 3f); yield return new WaitForSeconds(3.2f); World.ClearAll(); World.FloorOn(true); World.SetFlow(0.5f); BackToMenu(); _tourDone = true; }
        IEnumerator FadeStop(AudioSource s, float t)
        {
            float v0 = s.volume; for (float e = 0; e < t; e += Time.deltaTime) { s.volume = Mathf.Lerp(v0, 0f, e / t); yield return null; }
            s.Stop(); s.volume = v0;
        }
        void EnsureAudio()
        {
            if (FindFirstObjectByType<AudioListener>() == null && Cam != null) Cam.gameObject.AddComponent<AudioListener>();
            AudioListener.pause = false; if (!Video && !Film) AudioListener.volume = 1f;
        }
        IEnumerator FinaleThenResults()
        {
            State = LFState.Results; World.Beat(1.5f);
            for (int k = 0; k < 4; k++) { float a = k * 1.57f; World.Burst(new Vector3(Mathf.Sin(a) * 2.2f, 1.6f + 0.3f * k, Mathf.Cos(a) * 2.2f), LFMats.Tint(k % 2 == 0 ? LFMats.Gold : LFMats.Cyan), 90, 3f); yield return new WaitForSeconds(0.35f); }
            UI.Big("THE WORLD IS YOURS", 3f); yield return new WaitForSeconds(3.4f);
            UI.Big("You sang it into being.", 2.6f); yield return new WaitForSeconds(2.8f);
            for (int i = 0; i < 6; i++) { World.Beat(0.8f); yield return new WaitForSeconds(0.25f); }
            BuildResults();
        }

        void BuildResults()
        {
            if (_results != null) UI.Remove(_results);
            _results = UI.Panel("Results", PanelPos(0.66f, -0.18f)); UI.follow.Add(_results);
            int acc = _accTot > 6 && !_sim && Voice.MicOk && !Voice.Simulated ? Mathf.RoundToInt(100f * _accHit / _accTot) : -1;   // only a real microphone take gets an ON-KEY score; no invented number
            string duel = Duel ? ("  |  DUEL  YOU " + World.ClaimedPlayer + "  RIVAL " + World.ClaimedRival + (World.ClaimedPlayer >= World.ClaimedRival ? "  YOU WIN" : "  RIVAL WINS")) : "";
            string line = "STAIRS " + Mathf.RoundToInt(_maxStairs * 100f) + "%   POPS " + _pops + (acc >= 0 ? "   ON-KEY " + acc + "%" : "") + (!_sim && !Voice.MicOk ? "   (MICROPHONE OFF)" : "") + duel;
            UI.Hint(line);
            UI.Button(_results, "SAVE MY SONG", new Vector3(-0.22f, 0.13f, 0), new Vector2(0.4f, 0.12f), () => Q(SaveTake), LFMats.Cyan);
            UI.Button(_results, "SHARE TO PHONE", new Vector3(0.22f, 0.13f, 0), new Vector2(0.4f, 0.12f), () => Q(ShareInfo), LFMats.Gold);
            UI.Button(_results, "SING AGAIN", new Vector3(-0.22f, -0.03f, 0), new Vector2(0.4f, 0.12f), () => Q(() => { UI.Remove(_results); _results = null; OpenStudio(); }), LFMats.Magenta);
            UI.Button(_results, "MENU", new Vector3(0.22f, -0.03f, 0), new Vector2(0.4f, 0.12f), () => Q(BackToMenu), LFMats.Violet);
            State = LFState.Results;
        }

        // ----------------------------------------------------------------- save / export (mix on a worker thread; WAV to the app folder, copied to the phone via the Quest mobile app or QR upload)
        void SaveTake()
        {
            if (_bounceTask != null) return; UI.Hint("STUDIO IS MIXING YOUR TAKE...");
            float[] voiceTake; int vr; if (!_sim && Voice.TakeLength > 16000) { voiceTake = new float[Voice.TakeLength]; Array.Copy(Voice.Take, voiceTake, Voice.TakeLength); vr = Voice.TakeRate; } else { voiceTake = MonoOf(_song.guideClip); vr = LFSynth.SR; }
            float[] inst = MonoOf(_song.inst); string path = ExportDir + "/" + _song.id + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav"; float size = PresetSize[Preset], wet = PresetWet[Preset];
            Directory.CreateDirectory(ExportDir);
            _bounceTask = System.Threading.Tasks.Task.Run(() => MixTake(voiceTake, vr, inst, size, wet, path));
        }
        static float[] MonoOf(AudioClip c)
        {
            var d = new float[c.samples * c.channels]; c.GetData(d, 0); if (c.channels == 1) return d; var m = new float[c.samples]; for (int i = 0; i < m.Length; i++) { float s = 0; for (int k = 0; k < c.channels; k++) s += d[i * c.channels + k]; m[i] = s / c.channels; } return m;
        }
        static string MixTake(float[] voice, int vr, float[] inst, float size, float wet, string path)
        {
            int n = inst.Length; var v = new float[n]; float step = vr / (float)LFSynth.SR;
            for (int i = 0; i < n; i++) { float p = i * step; int a = (int)p; if (a + 1 >= voice.Length) break; float f = p - a; v[i] = voice[a] * (1 - f) + voice[a + 1] * f; }
            if (wet > 0.001f) LFSynth.Reverb(v, size, wet);
            var o = new float[n]; float pk = 1e-5f; for (int i = 0; i < n; i++) { o[i] = inst[i] * 0.75f + v[i] * 1.05f; pk = Mathf.Max(pk, Mathf.Abs(o[i])); }
            float g = 0.9f / pk; for (int i = 0; i < n; i++) o[i] *= g; LFSynth.WriteWav(path, o, LFSynth.SR);
#if UNITY_EDITOR
            try { Directory.CreateDirectory(@"E:\Winter\metavr\LastFrequency\logs\exports"); File.Copy(path, @"E:\Winter\metavr\LastFrequency\logs\exports\" + Path.GetFileName(path), true); } catch { }
#endif
            return path;
        }
        void OnBounceDone(string path)
        {
            _resultNote = path; LFLog.Ev("export", Path.GetFileName(path), 1f); UI.Big("SAVED! " + Path.GetFileName(path), 3.5f); UI.Hint("Your song is saved on the headset. Poke SHARE TO PHONE to play it on a phone on the same Wi-Fi.");
            if (State == LFState.Compose) { ComposeDone(path); }
        }
        LFShare _share; Transform _qrPanel;
        void CloseShare() { if (_qrPanel != null) { UI.Remove(_qrPanel); _qrPanel = null; if (_results != null) UI.SetVisible(_results, true); } if (_share != null) _share.End(); }
        /// <summary>SHARE TO PHONE: the headset serves its recordings on the local network and shows a QR code; the phone scans it and plays or downloads the song.</summary>
        void ShareInfo()
        {
            if (_share == null) _share = gameObject.AddComponent<LFShare>(); string url = _share.Begin(ExportDir); if (url == null) { UI.Hint("Could not start sharing. Check Wi-Fi."); return; }
            if (_qrPanel != null) { UI.Remove(_qrPanel); _qrPanel = null; }
            if (_results != null) UI.SetVisible(_results, false);   // the QR sits where the results buttons are: hide them so CLOSE cannot also press SING AGAIN underneath
            _qrPanel = UI.Panel("QR", PanelPos(0.66f, -0.05f)); var g = new GameObject("QRQuad"); g.transform.SetParent(_qrPanel, false); g.transform.localPosition = new Vector3(0f, 0.06f, -0.01f); g.transform.localScale = Vector3.one * 0.34f;
            g.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad"); var mr = g.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("LF/QR")); mat.mainTexture = LFShare.QrTexture(url); mr.sharedMaterial = mat;
            UI.Button(_qrPanel, "CLOSE", new Vector3(0f, -0.17f, 0f), new Vector2(0.2f, 0.08f), () => Q(() => { CloseShare(); UI.Hint(""); }), LFMats.Violet);
            UI.Hint("Scan with your phone camera (same Wi-Fi):  " + url); LFLog.Ev("share", url, 1f);
        }
    }
}
