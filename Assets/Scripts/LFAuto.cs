using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>"SHOW ME THE FLOW" guided tour (a ghost fingertip presses the real buttons while captions explain each step) and the headless autopilot
    /// used for the simulation video (-lf-auto). Both drive the same code paths a real hand would: the ghost is just pointer #2.</summary>
    public partial class LFApp
    {
        Pointer Ghost { get { return Pointers[2]; } }
        bool _tourRunning;

        void StartTour() { if (_tourRunning) return; StartCoroutine(TourRoutine()); }

        IEnumerator GhostMove(Vector3 to, float dur)
        {
            var g = Ghost; Vector3 a = g.pos; float t = 0;
            while (t < dur) { t += Time.deltaTime; float u = Mathf.SmoothStep(0, 1, t / dur); g.pos = Vector3.Lerp(a, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.03f; yield return null; }
            g.pos = to;
        }
        IEnumerator GhostPress(LFButton b)
        {
            if (b == null || b.tf == null) yield break; var g = Ghost; Vector3 tgt = b.tf.position; Vector3 fwd = b.tf.parent.forward;
            if (!g.active) { g.active = true; g.pos = _head.position + _head.forward * 0.3f + Vector3.down * 0.35f + _head.right * 0.25f; g.prev = g.pos; }
            yield return GhostMove(tgt - fwd * 0.17f, 0.7f); yield return GhostMove(tgt + fwd * 0.025f, 0.22f); yield return new WaitForSeconds(0.15f); yield return GhostMove(tgt - fwd * 0.16f, 0.25f);
        }
        IEnumerator GhostPressText(string text) { yield return GhostPress(UI.FindButton(text)); }
        IEnumerator GhostTap(Vector3 world, float dur) { var g = Ghost; if (!g.active) { g.active = true; g.pos = world + Vector3.down * 0.3f; } yield return GhostMove(world, dur); }
        void GhostOff() { Ghost.active = false; }

        IEnumerator TourRoutine()
        {
            _tourRunning = true; _tourDone = false;
            UI.Hint("1. Hands only. Poke any button with your fingertip."); yield return new WaitForSeconds(3f);
            UI.Hint("2. VISUAL: VOID BUILD grows the world out of darkness around you."); yield return GhostPress(_bVisual); yield return new WaitForSeconds(0.8f); yield return GhostPress(_bVisual); yield return new WaitForSeconds(0.5f);
            UI.Hint("3. RIVAL DUEL: you and a rival colour the world. Your voice claims it."); yield return GhostPress(_bMode); yield return new WaitForSeconds(0.8f); yield return GhostPress(_bMode);
            UI.Hint("4. SING takes you to the studio."); yield return GhostPressText("SING: LIGHT THE SILENCE");
            yield return new WaitForSeconds(1.2f); UI.Hint("5. Choose how your voice sounds: natural, dream, arena..."); yield return GhostPress(_bPreset[2]); yield return new WaitForSeconds(1.4f);
            UI.Hint("6. START THE SHOW. Magic flows around you; your voice builds the world."); yield return GhostPressText("START THE SHOW");
            GhostOff();
            // performance preview (song section 3) runs; PreviewEnd() returns to the menu and sets _tourDone
            while (!_tourDone) yield return null;
            UI.Hint("7. Prefer to make music? COMPOSE: type a word, play the objects, send it to the studio."); yield return new WaitForSeconds(4.5f); UI.Hint("");
            _tourRunning = false;
        }

        // ------------------------------------------------------------------ autopilot (the simulation video)
        IEnumerator AutoFlow()
        {
            yield return StartCoroutine(BootIntro()); BuildMenu(); State = LFState.Menu; yield return new WaitForSeconds(1.5f);
            { bool quick = false; foreach (var qa in System.Environment.GetCommandLineArgs()) if (qa == "-lf-quick") quick = true; if (quick) { UI.Remove(_menu); _menu = null; StartPerformanceAt(46f, -1f); yield return new WaitForSeconds(8f); foreach (var dk in new[] { "sweep", "rect", "circle", "triangle" }) yield return RunDemo(dk); while (true) yield return null; } }
            // 1) the guided flow
            _previewOverride = true; yield return StartCoroutine(TourRoutine()); GhostOff(); yield return new WaitForSeconds(1.5f);
            // 2) the real performance, sung by the simulated voice
            UI.Hint("Now the real thing."); yield return new WaitForSeconds(1.2f);
            yield return GhostPressText("SING: LIGHT THE SILENCE"); yield return new WaitForSeconds(1.0f);
            UI.Hint("Studio: sing a note, the floor answers."); yield return new WaitForSeconds(2.2f); yield return GhostPress(_bPreset[2]); GuideOn = false;
            yield return GhostPressText("START THE SHOW"); GhostOff();
            yield return new WaitForSeconds(5f);
            float tBud = 3f, tHands = 20f; int nd = 0; float[] dT = { 16f, 25f, 34f, 56f, 66f, 90f, 108f, 120f }; string[] dK = { "sweep", "rect", "circle", "triangle", "sweep", "circle", "rect", "triangle" };
            while (State != LFState.Results || _results == null)
            {
                float dt = Time.deltaTime; tBud -= dt; tHands -= dt;
                if (State == LFState.Perform && nd < dT.Length && _songT >= dT[nd]) { yield return RunDemo(dK[nd]); nd++; }
                if (State == LFState.Perform && tBud <= 0f && World.BudCount() > 0 && _songT > 6f)
                {
                    tBud = 5.5f; var p = World.NearestBud(_head.position); yield return GhostTap(p + (_head.position - p).normalized * 0.12f + Vector3.up * 0.02f, 0.45f); yield return GhostTap(p, 0.2f); yield return GhostTap(p + Vector3.up * 0.2f + _head.right * 0.15f, 0.35f);
                }
                if (State == LFState.Perform && tHands <= 0f && _song.SectionAt(_songT) >= 0 && _song.sections[_song.SectionAt(_songT)].kind == "peak")
                {
                    tHands = 24f; Pointers[0].active = Pointers[1].active = true; Vector3 hc = _head.position + _head.forward * 0.45f;
                    for (float t = 0; t < 1.4f; t += Time.deltaTime) { float u = Mathf.SmoothStep(0, 1, t / 1.4f); Pointers[0].pos = hc + new Vector3(-0.3f, Mathf.Lerp(-0.25f, 0.28f, u), 0); Pointers[1].pos = hc + new Vector3(0.3f, Mathf.Lerp(-0.25f, 0.28f, u), 0); yield return null; }
                    yield return new WaitForSeconds(0.4f); Pointers[0].active = Pointers[1].active = false;
                }
                yield return null;
            }
            GhostOff(); yield return new WaitForSeconds(2.5f);
            yield return GhostPressText("SAVE MY SONG"); yield return new WaitForSeconds(1.5f); while (_bounceTask != null) yield return null; yield return new WaitForSeconds(4f);
            // 3) compose
            yield return GhostPressText("MENU"); yield return new WaitForSeconds(1.5f);
            UI.Hint("Compose your own: type a word."); yield return new WaitForSeconds(1.5f); yield return GhostPressText("COMPOSE YOUR OWN"); yield return new WaitForSeconds(1.2f);
            foreach (char ch in "AURORA") { yield return GhostPressText(ch.ToString()); yield return new WaitForSeconds(0.1f); }
            yield return GhostPressText("GO"); GhostOff();
            while (_orbs.Count < 12 || !_loopRun) yield return null; yield return new WaitForSeconds(2.5f);
            var rb = UI.FindButton("REC: OFF"); yield return GhostPress(rb); yield return new WaitForSeconds(0.2f);
            int[] pattern = { 0, 3, 6, 9, 2, 5, 8, 11, 1, 4, 7, 10, 0, 6, 3, 9 }; var wait = new WaitForSeconds(_beatLen * 0.5f);
            for (int k = 0; k < pattern.Length; k++) { var o = _orbs[pattern[k] % _orbs.Count]; yield return GhostTap(o.tf.position - Vector3.forward * 0.0f + (_head.position - o.tf.position).normalized * 0.01f, 0.18f); yield return wait; }
            GhostOff(); yield return new WaitForSeconds(_beatLen * 8f);
            yield return GhostPressText("SEND TO STUDIO"); GhostOff(); while (_bounceTask != null) yield return null; yield return new WaitForSeconds(9f);
            UI.Hint("Your song is saved. Share it to your phone."); yield return new WaitForSeconds(5f);
            LFLog.Flush(@"E:\Winter\metavr\LastFrequency\logs\video\events.tsv"); Debug.Log("[LF] AUTOPILOT DONE frames=" + LFRecorder.Count + " story=" + LFLog.Story.ToString("F1"));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }
        bool _previewOverride;
        IEnumerator AutoJungle()
        {
            World.SetVisualTheme(true); World.SetFlow(0f); State = LFState.Boot; World.ClearAll(); Jungle.ShowAll(true); Jungle.SetStatics(false); Jungle.SetLive(false); World.SetVisualTheme(true);
            bool quick = false; foreach (var qa in System.Environment.GetCommandLineArgs()) if (qa == "-lf-quick") quick = true;
            LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); UI.Hint("Listen. Then push your hand outward."); yield return new WaitForSeconds(1.5f); yield return VoidReach(6f);
            _skipOpening = true; StartPerformanceAt(0f, -1f); yield return new WaitForSeconds(4.5f);
            float tBud = 4f; int nd = 0; float f = quick ? 0.42f : 1f;
            float[] dT0 = { 16f, 24f, 32f, 40f, 49f, 55f, 62f, 70f, 78f, 90f, 97f, 104f, 111f, 121f, 127f, 133f, 139f }; string[] dK = { "push", "push", "push", "push", "lift", "push", "both", "both", "both", "climb", "climb", "climb", "climb", "sky:RAIN +", "sky:RAIN -", "sky:TIME +", "sky:TIME -" };
            var dT = new float[dT0.Length]; for (int q = 0; q < dT.Length; q++) dT[q] = dT0[q] * f;
            while (State != LFState.Results || _results == null)
            {
                float dt = Time.deltaTime; tBud -= dt;
                if (nd < dT.Length && (quick || (State == LFState.Perform && _songT >= dT[nd]))) { yield return RunDemo(dK[nd]); nd++; }
                if (false && State == LFState.Perform && tBud <= 0f && World.BudCount() > 0) { tBud = 6f; var p = World.NearestBud(_head.position); yield return GhostTap(p + (_head.position - p).normalized * 0.12f, 0.45f); yield return GhostTap(p, 0.2f); }
                yield return null;
            }
            LFLog.Flush(@"E:\Winter\metavr\LastFrequency\logs\video\events.tsv"); Debug.Log("[LF] AUTOPILOT DONE frames=" + LFRecorder.Count);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
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
            if (kind.StartsWith("sky:")) { yield return GhostPressText(kind.Substring(4)); g.active = false; yield return new WaitForSeconds(0.4f); yield break; }
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
            {
                Vector3 s0 = _head.position + f * 0.22f + Vector3.down * 0.3f + r * 0.12f; yield return GhostMove(s0, 0.4f); g.pinch = false; yield return GhostMove(s0 + f * 0.62f + Vector3.up * 0.1f, 0.22f); yield return new WaitForSeconds(0.9f); g.active = false; yield break;
            }
            Vector3[] pts;
            if (kind == "rect") pts = new[] { c + r * -0.16f + Vector3.up * 0.11f, c + r * 0.16f + Vector3.up * 0.11f, c + r * 0.16f - Vector3.up * 0.11f, c - r * 0.16f - Vector3.up * 0.11f, c + r * -0.16f + Vector3.up * 0.11f };
            else if (kind == "triangle") pts = new[] { c + Vector3.up * 0.15f, c + r * 0.15f - Vector3.up * 0.1f, c - r * 0.15f - Vector3.up * 0.1f, c + Vector3.up * 0.15f };
            else { pts = new Vector3[25]; for (int i = 0; i < 25; i++) { float a = i / 24f * 6.2832f; pts[i] = c + r * Mathf.Cos(a) * 0.14f + Vector3.up * Mathf.Sin(a) * 0.14f; } }
            yield return GhostMove(pts[0], 0.5f); g.pinch = true;
            for (int i = 1; i < pts.Length; i++) yield return GhostMove(pts[i], kind == "circle" ? 0.07f : 0.32f);
            g.pinch = false; yield return new WaitForSeconds(0.3f); g.active = false; yield return new WaitForSeconds(2.6f);
        }
    }
}
