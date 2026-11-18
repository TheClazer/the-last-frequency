import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('Assets/Scripts/LFComfort.cs', [('            if (NoMic && app.State == LFState.Perform && app.Voice != null) { float t = Time.time; app.Voice.Feed(0.32f + 0.12f * Mathf.Sin(t * 0.8f), 57f + 3f * Mathf.Sin(t * 0.3f)); }\n', '')])

sub('Assets/Scripts/LFVoice.cs', [
 ('        public bool Recording;\n', '''        public bool Recording;
        // Listen mode: react to music playing in the room (loudness, bass beats, brightness) instead of tracking a sung pitch
        public bool Listen; public float Bass, Bright; public event Action<float> OnBeat;
        float _lp, _bassAvg, _beatCool;
        public void SetNoMic(bool on)
        {
            Simulated = on; if (!on && _mic == null && Microphone.devices.Length > 0) { _mic = Microphone.Start(null, true, 10, Rate); if (_take == null) _take = new float[Rate * 200]; }
        }
        void ListenAnalyze(float dt)
        {
            double eb = 0; int zc = 0; float prev = _buf[0]; float lp = _lp;
            for (int i = 0; i < Win; i++) { float x = _buf[i]; lp += 0.04f * (x - lp); eb += lp * lp; if (x * prev < 0f) zc++; prev = x; }
            _lp = lp; Bass = Mathf.Clamp01(Mathf.Sqrt((float)(eb / Win)) * 14f); Bright = Mathf.Clamp01(zc / 220f);
            _bassAvg += (Bass - _bassAvg) * Mathf.Min(1f, dt * 2f); _beatCool -= dt;
            if (Bass > _bassAvg * 1.45f + 0.06f && _beatCool <= 0f) { _beatCool = 0.26f; OnBeat?.Invoke(Bass); }
        }
'''),
 ('                if (lvl > 0.06f) midi = Pitch();', '                if (Listen) { ListenAnalyze(dt); if (lvl > 0.06f) midi = 50f + Bright * 26f; }\n                else if (lvl > 0.06f) midi = Pitch();'),
])
sub('Assets/Scripts/LFWorld.cs', [('public void Beat(float strength) { _pulse = Mathf.Max(_pulse, strength); }', 'public void Beat(float strength) { _pulse = Mathf.Max(_pulse, strength * (LFComfort.Calm ? 0.4f : 1f)); }')])
sub('Assets/Scripts/LFJungle.cs', [('Rig.position = _rigBase + new Vector3(0f, DeckY(_climb), _climb) + sway;', 'Rig.position = _rigBase + new Vector3(0f, DeckY(_climb), _climb) + sway * (LFComfort.Calm ? 0.2f : 1f);')])
sub('Assets/Scripts/LFApp.cs', [
 ('Voice = new GameObject("LFVoice").AddComponent<LFVoice>(); Voice.Simulated = _sim;', 'Voice = new GameObject("LFVoice").AddComponent<LFVoice>(); Voice.Simulated = _sim; Voice.OnBeat += s => { if (Voice.Listen) World.Beat(0.25f + s * 0.45f); }; gameObject.AddComponent<LFComfort>();'),
 ('            RefreshToggles(); UI.Hint("Poke a button with your fingertip");', '''            _bListen = UI.Button(_menu, "", new Vector3(-0.31f, -0.6f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { if (Voice != null) Voice.Listen = !Voice.Listen; RefreshToggles(); }), LFMats.Cyan);
            _bCalm = UI.Button(_menu, "", new Vector3(0f, -0.6f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { LFComfort.Calm = !LFComfort.Calm; RefreshToggles(); }), LFMats.Mint);
            _bNoMic = UI.Button(_menu, "", new Vector3(0.31f, -0.6f, 0), new Vector2(0.29f, 0.11f), () => Q(() => { LFComfort.NoMic = !LFComfort.NoMic; if (Voice != null) Voice.SetNoMic(LFComfort.NoMic || _sim); RefreshToggles(); }), LFMats.Violet);
            RefreshToggles(); UI.Hint("Poke a button with your fingertip");'''),
 ('        void RefreshToggles()\n        {\n', '        LFButton _bListen, _bCalm, _bNoMic;\n        void RefreshToggles()\n        {\n            UI.SetText(_bListen, Voice != null && Voice.Listen ? "LISTEN TO ROOM: ON" : "LISTEN TO ROOM: OFF"); UI.SetText(_bCalm, LFComfort.Calm ? "CALM MODE: ON" : "CALM MODE: OFF"); UI.SetText(_bNoMic, LFComfort.NoMic ? "NO MIC: ON" : "NO MIC: OFF");\n'),
])
print("patched comfort")
