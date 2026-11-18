import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('Assets/Scripts/LFApp.cs', [
('public int Theme; public LFJungle Jungle; LFButton _bTheme;', 'public int Theme; public LFJungle Jungle; LFButton _bTheme, _bMR; public bool MixedReality; Transform _skyPanel;'),
('foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-auto") Auto = true;', 'foreach (var a in Environment.GetCommandLineArgs()) { if (a == "-lf-mr") MixedReality = true; if (a == "-lf-auto") Auto = true;'),
('            _bTheme = UI.Button(_menu, "", new Vector3(0f, -0.31f, 0), new Vector2(0.9f, 0.12f), () => Q(() => { SetTheme(1 - Theme); RefreshToggles(); }), LFMats.Gold);', '            _bTheme = UI.Button(_menu, "", new Vector3(0f, -0.31f, 0), new Vector2(0.9f, 0.12f), () => Q(() => { SetTheme(1 - Theme); RefreshToggles(); }), LFMats.Gold);\n            _bMR = UI.Button(_menu, "", new Vector3(0f, -0.45f, 0), new Vector2(0.9f, 0.12f), () => Q(() => { SetMixedReality(!MixedReality); RefreshToggles(); }), LFMats.Mint);'),
('UI.SetText(_bGuide, GuideOn ? "GUIDE VOCAL: ON" : "GUIDE VOCAL: OFF");', 'UI.SetText(_bGuide, GuideOn ? "GUIDE VOCAL: ON" : "GUIDE VOCAL: OFF"); UI.SetText(_bMR, MixedReality ? "MIXED REALITY: ON  (your room opens into the world)" : "MIXED REALITY: OFF  (fully virtual)");'),
('        public void EnsureJungle()', '''        /// <summary>Passthrough: the void becomes your real room, the world is drawn into it, and your hands keep a ring of the real room around them.</summary>
        public void SetMixedReality(bool on)
        {
            MixedReality = on; if (Jungle != null) Jungle.MixedReality = on;
            if (Application.isMobilePlatform)
            {
                var mgr = FindFirstObjectByType<OVRManager>(); if (mgr != null)
                {
                    mgr.isInsightPassthroughEnabled = on; var pt = mgr.gameObject.GetComponent<OVRPassthroughLayer>(); if (on && pt == null) { pt = mgr.gameObject.AddComponent<OVRPassthroughLayer>(); pt.overlayType = OVROverlay.OverlayType.Underlay; } if (pt != null) pt.enabled = on;
                }
            }
            Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = on ? new Color(0, 0, 0, 0) : new Color(0.01f, 0.02f, 0.08f);
        }
        void BuildSkyPanel()
        {
            if (_skyPanel != null) return; var fwd = Quaternion.Euler(0, -38f, 0) * new Vector3(_head.forward.x, 0, _head.forward.z).normalized; _skyPanel = UI.Panel("Sky", _head.position + fwd * 0.58f + Vector3.down * 0.4f);
            string[] rows = { "TIME", "RAIN", "FOG" }; for (int i = 0; i < 3; i++) { string r = rows[i]; float y = 0.1f - i * 0.1f; UI.Button(_skyPanel, r + " -", new Vector3(-0.09f, y, 0), new Vector2(0.16f, 0.085f), () => Q(() => SkyAdj(r, -1)), LFMats.Ice); UI.Button(_skyPanel, r + " +", new Vector3(0.09f, y, 0), new Vector2(0.16f, 0.085f), () => Q(() => SkyAdj(r, 1)), LFMats.Gold); }
        }
        void SkyAdj(string what, int dir)
        {
            if (Jungle == null) return; LFLog.Ev("sfx", "tick");
            if (what == "TIME") Jungle.TimeOfDay = Mathf.Clamp01(Jungle.TimeOfDay + dir * 0.14f); else if (what == "RAIN") Jungle.RainAmt = Mathf.Clamp01(Jungle.RainAmt + dir * 0.25f); else Jungle.FogAmt = Mathf.Clamp01(Jungle.FogAmt + dir * 0.25f);
        }
        public void EnsureJungle()'''),
('            if (Jungle == null) { Jungle = new GameObject("LFJungle").AddComponent<LFJungle>(); Jungle.Build(Cam); }', '            if (Jungle == null) { Jungle = new GameObject("LFJungle").AddComponent<LFJungle>(); Jungle.Build(Cam); Jungle.MixedReality = MixedReality; if (MixedReality) SetMixedReality(true); }'),
('            if (_sim) Cam.transform.root.Rotate(0f, 3.5f * dt, 0f, Space.World);', '            if (_sim) { if (Theme == 1 && Jungle != null && Jungle.Climbing) { float y = Cam.transform.root.eulerAngles.y; float d = Mathf.DeltaAngle(y, 0f); Cam.transform.root.Rotate(0f, Mathf.Clamp(d, -25f * dt, 25f * dt) * (Mathf.Abs(d) > 0.5f ? 1f : 0f), 0f, Space.World); } else Cam.transform.root.Rotate(0f, 3.5f * dt, 0f, Space.World); }'),
('            Conduct(dt);\n', '            if (Theme == 0) Conduct(dt);\n            if (Theme == 1 && Jungle != null) { Jungle.Hand0 = Pointers[0].active ? Pointers[0].pos : new Vector3(0, -999, 0); Jungle.Hand1 = Pointers[1].active ? Pointers[1].pos : new Vector3(0, -999, 0); if (_skyPanel == null && _songT > 4f) BuildSkyPanel(); }\n'),
('UI.Hint("Push your hand outward. The world draws itself where you point."); }', 'UI.Hint("Raise a hand and push outward. Listen."); }'),
])
sub('Assets/Scripts/LFAuto.cs', [
('            float tBud = 4f; int nd = 0; float[] dT = { 1f, 7f, 13f, 19f, 25f, 31f, 37f, 43f, 49f, 55f, 61f, 67f, 73f, 79f, 85f, 91f, 100f, 112f, 124f }; string[] dK = { "push", "push", "push", "push", "push", "push", "push", "push", "circle", "push", "push", "rect", "push", "triangle", "push", "push", "sweep", "push", "circle" };',
 '''            float tBud = 4f; int nd = 0; float f = quick ? 0.42f : 1f;
            float[] dT0 = { 7f, 15f, 23f, 31f, 39f, 47f, 55f, 64f, 73f, 82f, 94f, 106f, 112f, 118f, 126f, 133f }; string[] dK = { "push", "push", "lift", "push", "push", "both", "both", "both", "climb", "climb", "climb", "sky:RAIN +", "sky:RAIN +", "sky:TIME +", "sky:TIME +", "sky:RAIN -" };
            var dT = new float[dT0.Length]; for (int q = 0; q < dT.Length; q++) dT[q] = dT0[q] * f;'''),
('                if (State == LFState.Perform && tBud <= 0f && World.BudCount() > 0)', '                if (false && State == LFState.Perform && tBud <= 0f && World.BudCount() > 0)'),
('            if (kind == "push")\n            {', '''            if (kind.StartsWith("sky:")) { yield return GhostPressText(kind.Substring(4)); g.active = false; yield return new WaitForSeconds(0.4f); yield break; }
            if (kind == "lift")
            {
                Vector3 s1 = _head.position + f * 0.22f + Vector3.down * 0.3f; yield return GhostMove(s1, 0.4f); g.pinch = false; yield return GhostMove(_head.position + f * 0.28f + Vector3.up * 0.38f, 0.22f); yield return new WaitForSeconds(1.2f); g.active = false; yield break;
            }
            if (kind == "both")
            {
                var L = Pointers[0]; var R = Pointers[1]; Vector3 lp = _head.position + f * 0.22f - r * 0.2f + Vector3.down * 0.3f, rp = _head.position + f * 0.22f + r * 0.2f + Vector3.down * 0.3f; L.active = R.active = true; L.pos = lp; R.pos = rp; L.prev = lp; R.prev = rp; yield return new WaitForSeconds(0.3f);
                for (float t = 0; t < 0.22f; t += Time.deltaTime) { float u = t / 0.22f; L.pos = lp + f * 0.55f * u - r * 0.05f * u; R.pos = rp + f * 0.55f * u + r * 0.05f * u; yield return null; }
                yield return new WaitForSeconds(1.8f); L.active = R.active = false; yield break;
            }
            if (kind == "climb")
            {
                var L = Pointers[0]; var R = Pointers[1]; Vector3 lb = _head.position + f * 0.3f - r * 0.18f + Vector3.down * 0.5f, rb = _head.position + f * 0.3f + r * 0.18f + Vector3.down * 0.5f; L.active = R.active = true; L.pos = lb; R.pos = rb; L.prev = lb; R.prev = rb; yield return new WaitForSeconds(0.2f);
                for (int i = 0; i < 12; i++) { var h = (i % 2 == 0) ? L : R; Vector3 b0 = (i % 2 == 0) ? lb : rb; float dirv = (i / 2 % 2 == 0) ? 1f : -1f; dirv = (i % 2 == 0) ? 1f : -1f; for (float t = 0; t < 0.3f; t += Time.deltaTime) { float u = t / 0.3f; h.pos = b0 + Vector3.up * 0.34f * dirv * u; yield return null; } if (i % 2 == 1) { } }
                L.active = R.active = false; yield return new WaitForSeconds(0.5f); yield break;
            }
            if (kind == "push")
            {'''),
])
print("patched stage2")
