using System.Collections;
using UnityEngine;

namespace LF
{
    /// <summary>Measures the "feel" of whatever music is playing near the headset, from the microphone only: loudness, bass, brightness and tempo.
    /// It does not identify songs. It reads energy, tempo and tone and maps them onto the element library.</summary>
    public class LFFeelTracker
    {
        public float EnergyFast, EnergySlow, Bass, Bright, Tempo = 100f; public int Section;   // 0 calm, 1 build, 2 peak
        readonly float[] _iv = new float[8]; int _ivN, _ivI; float _lastBeat = -9f, _secT;
        public void Beat(float now)
        {
            float d = now - _lastBeat; _lastBeat = now;
            if (d > 0.28f && d < 1.1f) { _iv[_ivI++ % _iv.Length] = d; if (_ivN < _iv.Length) _ivN++; }
            if (_ivN >= 4) { var a = new float[_ivN]; System.Array.Copy(_iv, a, _ivN); System.Array.Sort(a); float med = a[_ivN / 2]; float bpm = 60f / med; while (bpm < 70f) bpm *= 2f; while (bpm > 170f) bpm *= 0.5f; Tempo = Mathf.Lerp(Tempo, bpm, 0.35f); }
        }
        public void Tick(float dt, float level, float bass, float bright)
        {
            EnergyFast += (level - EnergyFast) * Mathf.Min(1f, dt * 2f); EnergySlow += (level - EnergySlow) * Mathf.Min(1f, dt * 0.12f);
            Bass += (bass - Bass) * Mathf.Min(1f, dt * 1.5f); Bright += (bright - Bright) * Mathf.Min(1f, dt * 1.5f);
            _secT += dt; int s = EnergyFast > 0.55f ? 2 : (EnergyFast < 0.22f ? 0 : 1); if (s != Section && _secT > 2.5f) { Section = s; _secT = 0f; SectionChanged = true; }
        }
        public bool SectionChanged;
        /// <summary>Heuristic element scores. Fire = loud, fast, bright. Ice = bright and airy. Water = slow, soft, dark. Earth = bass-heavy and grounded.</summary>
        public LFEl Suggest(out float score)
        {
            float bassHeavy = Bass / (Bass + Bright * 0.6f + 0.05f);
            float fire = 0.9f * EnergyFast + 0.5f * Mathf.Clamp01((Tempo - 105f) / 50f) + 0.3f * Bright;
            float ice = 0.8f * Bright + 0.35f * (1f - EnergyFast) + 0.2f * Mathf.Clamp01(1f - Mathf.Abs(Tempo - 105f) / 45f);
            float water = 0.7f * (1f - Mathf.Clamp01((Tempo - 70f) / 60f)) + 0.45f * (1f - Bright) + 0.25f * (1f - EnergyFast);
            float earth = 1.15f * bassHeavy + 0.35f * EnergyFast;
            float best = fire; var e = LFEl.Fire;
            if (ice > best) { best = ice; e = LFEl.Ice; }
            if (water > best) { best = water; e = LFEl.Water; }
            if (earth > best) { best = earth; e = LFEl.Earth; }
            score = best; return e;
        }
    }

    public partial class LFApp
    {
        public LFEl Element = LFEl.Ice; public bool ElementAuto; public bool ListenSim, ListenCycle, ListenFixed;
        LFFeelTracker _feel; float _listenT, _lastSwitch; int _lastSec = -1; Transform _listenPanel; LFButton[] _bEl = new LFButton[5]; int _listenPeaks;

        void BuildElementRow()
        {
            string[] n = { "ICE", "WATER", "EARTH", "FIRE", "AUTO" }; int[] pal = { LFMats.Ice, LFMats.Cyan, LFMats.Gold, LFMats.Magenta, LFMats.Mint };
            for (int i = 0; i < 5; i++) { int k = i; _bEl[i] = UI.Button(_menuOpt, n[i], new Vector3(-0.36f + i * 0.18f, 0.33f, 0), new Vector2(0.165f, 0.1f), () => Q(() => ChooseElement(k)), pal[i]); }
            UI.Button(_menuMain, "LISTEN AND BUILD", new Vector3(-0.22f, 0.17f, 0), new Vector2(0.52f, 0.13f), () => Q(StartListenBuild), LFMats.Violet);   // SING took the top row (first action)
            RefreshElementToggles();
        }
        void RefreshElementToggles() { for (int i = 0; i < 5; i++) if (_bEl[i] != null) _bEl[i].toggled = i < 4 ? (!ElementAuto && (int)Element == i) : ElementAuto; }
        void ChooseElement(int k)
        {
            if (k == 4) { ElementAuto = true; UI.Hint("AUTO: the music chooses the element"); }
            else { ElementAuto = false; SetElement((LFEl)k); UI.Hint("ELEMENT: " + LFElements.Names[k]); }
            RefreshElementToggles(); LFLog.Ev("sfx", "tick");
        }
        public void SetElement(LFEl e)
        {
            Element = e; if (Theme == 1) SetTheme(0); World.SetElement(e);
            World.RingAt(new Vector3(0, 0.03f, 0), LFMats.Tint(LFMats.Cyan), 3.8f, true); World.Burst(new Vector3(0, 1.4f, 0), LFElements.A1, 70, 2.2f); World.Beat(0.9f); LFLog.Ev("element", LFElements.Names[(int)e], (float)e);
        }

        void StartListenBuild()
        {
            if (Theme == 1) SetTheme(0);
            State = LFState.Listen; if (_menu != null) { UI.Remove(_menu); _menu = null; }
            World.ClearAll(); World.VoidBuild = true; World.FloorOn(true); World.SetElement(Element); World.Intensity(0.1f); World.SetFlow(0.4f); World.Frost(new Vector3(_head.position.x, 0f, _head.position.z), 8.5f, true);
            if (Voice != null) { Voice.Listen = true; Voice.OnBeat -= OnListenBeat; Voice.OnBeat += OnListenBeat; }
            _feel = new LFFeelTracker(); _listenT = 0f; _lastSwitch = 0f; _lastSec = -1; _listenPeaks = 0;
            UI.Hint(_sim ? "Listening to the room..." : "Play any music near the headset. The world builds from how it feels.");
            _listenPanel = UI.Panel("ListenStop", PanelPos(0.66f, -0.4f)); UI.Button(_listenPanel, "STOP LISTENING", new Vector3(0f, 0f, 0), new Vector2(0.4f, 0.1f), () => Q(StopListenBuild), LFMats.Violet);
            LFLog.Ev("listen", "start", 0);
        }
        void StopListenBuild()
        {
            if (_listenPanel != null) { UI.Remove(_listenPanel); _listenPanel = null; }
            if (Voice != null) { Voice.Listen = false; Voice.OnBeat -= OnListenBeat; }
            World.ClearAll(); World.FloorOn(true); World.SetStairs(0f); BackToMenu();
        }
        void OnListenBeat(float s)
        {
            if (State != LFState.Listen || _feel == null) return;
            _feel.Beat(_listenT); World.Beat(0.3f + s * 0.5f); if (s > 0.35f) World.RingAt(new Vector3(0, 0.03f, 0), LFMats.Tint(LFMats.Cyan), 2f + s * 3f, true);
        }

        void ListenUpdate(float dt)
        {
            _listenT += dt; if (_feel == null) return;
            if (_sim) SimFeatures(dt);
            _feel.Tick(dt, Voice.Level, Voice.Bass, Voice.Bright); float e = _feel.EnergyFast;
            World.Intensity(Mathf.Max(e, 0.12f)); World.SetFlow(0.3f + 0.7f * e); World.SetTimbre(_feel.Bright, Voice.Level); World.SetStairs(Mathf.Clamp01(_listenT / 150f));
            if (_feel.SectionChanged) { _feel.SectionChanged = false; OnFeelSection(_feel.Section); }
            if (ElementAuto && _listenT - _lastSwitch > (_sim ? 9f : 14f) && _listenT > 3f)
            {
                float sc; var cand = _feel.Suggest(out sc);
                if (cand != Element) { _lastSwitch = _listenT; SetElement(cand); UI.Hint("It feels like " + LFElements.Names[(int)cand] + "  (tempo ~" + Mathf.RoundToInt(_feel.Tempo) + ")"); RefreshElementToggles(); }
            }
            if (_sim) { _simYaw += 3.5f * dt; Cam.transform.root.rotation = Quaternion.Euler(0f, _simYaw, 0f); }
        }
        void OnFeelSection(int s)
        {
            LFLog.Ev("feel", s == 2 ? "peak" : (s == 1 ? "build" : "calm"), _feel.Tempo);
            if (s == 0) { World.SpawnBuds(2); World.Beat(0.3f); }
            else if (s == 1) { World.Wave("shards", 6, 1, false); World.Wave("spires", 5, 0, false); World.SpawnBuds(1); }
            else { _listenPeaks++; World.Wave("arches", 4, _listenPeaks, false); World.Wave("skyorbs", 9, _listenPeaks + 1, false); World.Wave(_listenPeaks >= 2 ? "tallspires" : "shards", 8, _listenPeaks + 3, false); if (_listenPeaks >= 2) World.Wave("crown", 8, 5, false); World.Beat(1.1f); World.Frost(new Vector3(_head.position.x, 0f, _head.position.z), 11f, true); }
        }

        // Simulation only: a synthetic "music" whose character moves through four moods, so the whole listen -> feel -> element pipeline can be rendered without a microphone.
        float _simBeatT;
        void SimFeatures(float dt)
        {
            int ph = Mathf.Min(3, (int)(_listenT / 12.5f)); float u = (_listenT % 12.5f) / 12.5f;
            float[] lvl = { 0.26f, 0.5f, 0.46f, 0.9f }, bass = { 0.5f, 0.85f, 0.3f, 0.75f }, br = { 0.22f, 0.28f, 0.78f, 0.62f }, tempo = { 72f, 96f, 112f, 140f };
            float l = lvl[ph] * (0.85f + 0.15f * Mathf.Sin(_listenT * 3f)) * Mathf.Clamp01(_listenT / 2f);
            Voice.Feed(l, 60f); Voice.Bass = bass[ph] * (0.8f + 0.2f * Mathf.Sin(_listenT * 5f)); Voice.Bright = br[ph];
            _simBeatT -= dt; if (_simBeatT <= 0f) { _simBeatT = 60f / tempo[ph]; Voice.FakeBeat(Voice.Bass); }
        }

        /// <summary>Simulator only: the ghost hand reaches slowly through the dark so its light can be seen sliding over the floor and wall.</summary>
        IEnumerator VoidReach(float seconds)
        {
            var g = Pointers[2]; Vector3 f = _head.forward; f.y = 0; f.Normalize(); Vector3 r = Vector3.Cross(Vector3.up, f); Vector3 c = _head.position + f * 0.6f;
            g.active = true; g.pinch = false; float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime; float u = t / seconds; float a = u * Mathf.PI * 1.6f - 0.9f;
                g.pos = c + r * Mathf.Sin(a) * 0.55f + f * (0.1f + 0.35f * Mathf.Sin(u * Mathf.PI)) + Vector3.down * (0.55f - 0.5f * Mathf.Sin(u * Mathf.PI * 1.2f));
                yield return null;
            }
            g.active = false;
        }

        IEnumerator ListenAuto()
        {
            ElementAuto = !ListenCycle && !ListenFixed; if (!ListenFixed) Element = LFEl.Ice; World.SetElement(Element); yield return new WaitForSeconds(0.5f); UI.Hint("");
            StartListenBuild(); float t0 = Time.time; yield return VoidReach(5f);
            string[] kinds = { "sweep", "circle", "rect", "triangle" }; int k = 0; float nextGesture = 5f;
            while (_listenT < 52f)
            {
                if (ListenCycle && _listenT > 12f * (k + 1) - 0.01f && k < 3) { k++; SetElement((LFEl)k); }
                if (_listenT > nextGesture) { nextGesture += 9f; yield return RunDemo(kinds[(int)(nextGesture / 9f) % 4]); }
                yield return null;
            }
            LFLog.Flush(@"E:\Winter\metavr\LastFrequency\logs\video\events.tsv"); Debug.Log("[LF] LISTEN AUTO DONE frames=" + LFRecorder.Count);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
