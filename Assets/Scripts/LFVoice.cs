using System;
using UnityEngine;

namespace LF
{
    /// <summary>Live voice analysis: level, pitch (autocorrelation), voiced flag, sustained-note timer. Zero allocation after Start.
    /// In Simulated mode (editor autopilot / no microphone) the values are fed by LFApp from the song's melody.</summary>
    public class LFVoice : MonoBehaviour
    {
        public bool Simulated;
        public float Level;          // smoothed 0..1 (loudness)
        public float Midi;           // smoothed midi note (0 when unvoiced)
        public bool Voiced;
        public float Sustain;        // seconds the current note has been held
        public int SustainSteps;     // increments once per second of sustain (each one grows a stair)
        public event Action<float> OnLoud;      // level peak
        public event Action OnPhrase;           // voiced after silence
        public bool MicOk { get { return _mic != null; } }

        // Pitch range ~73 Hz (D2) .. 1333 Hz (E6). The old range (lags 40..200 = 80..400 Hz) lost every sung note above G4: those came back an octave or more low.
        public const int Rate = 16000, Win = 1024, MinLag = 12, MaxLag = 220;
        AudioClip _mic; readonly float[] _buf = new float[Win]; readonly float[] _nsdf = new float[MaxLag + 2]; float[] _take; int _takeLen, _lastPos;
        float _simLevel, _simMidi; float _lastMidi, _silence, _holdT, _peakHold; bool _was; float _tick;
        public float[] Take { get { return _take; } } public int TakeLength { get { return _takeLen; } } public int TakeRate { get { return Rate; } }
        public bool Recording;
        // Listen mode: react to music playing in the room (loudness, bass beats, brightness) instead of tracking a sung pitch
        public bool Listen; public float Bass, Bright; public event Action<float> OnBeat;
        float _lp, _bassAvg, _beatCool;
        public void FakeBeat(float s) { OnBeat?.Invoke(s); }
        public void SetNoMic(bool on)
        {
            Simulated = on;
            // never start the microphone before the permission is granted: a clip started without it stays silent for the whole session (OpenMic starts it on the grant)
            if (!on && _mic == null && Microphone.devices.Length > 0 && HasMicPermission()) { _mic = Microphone.Start(null, true, 10, Rate); if (_take == null) _take = new float[Rate * 200]; }
        }
        public static bool HasMicPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
            return true;
#endif
        }
        void ListenAnalyze(float dt)
        {
            double eb = 0; int zc = 0; float prev = _buf[0]; float lp = _lp;
            for (int i = 0; i < Win; i++) { float x = _buf[i]; lp += 0.04f * (x - lp); eb += lp * lp; if (x * prev < 0f) zc++; prev = x; }
            _lp = lp; Bass = Mathf.Clamp01(Mathf.Sqrt((float)(eb / Win)) * 14f); Bright = Mathf.Clamp01(zc / 220f);
            _bassAvg += (Bass - _bassAvg) * Mathf.Min(1f, dt * 2f); _beatCool -= dt;
            if (Bass > _bassAvg * 1.45f + 0.06f && _beatCool <= 0f) { _beatCool = 0.26f; OnBeat?.Invoke(Bass); }
        }

        void Start()
        {
            if (Simulated) return;
            StartCoroutine(OpenMic());
        }

        /// <summary>On first launch the headset shows the microphone permission dialog. Starting the microphone before the answer gives a dead (silent) clip
        /// for the whole session, so wait for the grant, then start. Until then (or if it is refused) the voice is simulated and LFApp lets the song lead.</summary>
        System.Collections.IEnumerator OpenMic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)) yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForSecondsRealtime(0.3f);
            }
#endif
            if (Microphone.devices.Length == 0) { Simulated = true; Debug.Log("[LF] voice: no microphone"); yield break; }
            if (_mic == null) _mic = Microphone.Start(null, true, 10, Rate);
            if (_take == null) _take = new float[Rate * 200];
            if (_mic != null && !LFComfort.NoMic) Simulated = false;
            Debug.Log("[LF] voice: microphone " + (_mic != null ? "started" : "failed to start"));
        }

        public void Feed(float level, float midi) { _simLevel = level; _simMidi = midi; }
        public void BeginTake() { _takeLen = 0; Recording = true; if (_take == null) _take = new float[Rate * 200]; }
        public void EndTake() { Recording = false; }

        void Update()
        {
            float dt = Time.deltaTime, lvl, midi = 0f;
            if (Simulated || _mic == null)
            {
                lvl = _simLevel; midi = _simMidi;
            }
            else
            {
                int pos = Microphone.GetPosition(null);
                if (pos <= 0 && _lastPos <= 0) return;
                if (Recording && _take != null && pos != _lastPos)
                {
                    int n = pos >= _lastPos ? pos - _lastPos : (_mic.samples - _lastPos) + pos;
                    n = Mathf.Min(n, _take.Length - _takeLen);
                    if (n > 0 && n <= _mic.samples) { _tmp = _tmp != null && _tmp.Length >= n ? _tmp : new float[Mathf.Max(n, 4096)]; ReadRange(_lastPos, n); }
                }
                _lastPos = pos;
                int start = pos - Win; if (start < 0) start += _mic.samples;
                _mic.GetData(_buf, start);
                double e = 0; for (int i = 0; i < Win; i++) e += _buf[i] * _buf[i];
                lvl = Mathf.Clamp01(Mathf.Sqrt((float)(e / Win)) * 6f);
                if (Listen) { ListenAnalyze(dt); if (lvl > 0.06f) midi = 50f + Bright * 26f; }
                else if (lvl > 0.06f) { _odd = !_odd; if (_odd || _held <= 0f) _held = Pitch(); midi = _held; }   // pitch on every other frame (~36-45 Hz, the window is 64 ms anyway): halves the cost
                else _held = 0f;
            }
            Level = LFUtil.Damp(Level, lvl, lvl > Level ? 40f : 7f, dt);
            bool voiced = midi > 0f && lvl > 0.06f;
            if (voiced)
            {
                if (!_was && _silence > 0.25f) OnPhrase?.Invoke();
                if (Mathf.Abs(midi - _lastMidi) < 1.4f) { _holdT += dt; } else { _holdT = 0f; }
                _lastMidi = midi; Midi = LFUtil.Damp(Midi, midi, 25f, dt); _silence = 0f;
                Sustain = _holdT;
                int steps = (int)Sustain; if (steps > SustainSteps) SustainSteps = steps;
            }
            else { _silence += dt; if (_silence > 0.3f) { Midi = 0f; Sustain = 0f; _holdT = 0f; SustainSteps = 0; } }
            Voiced = voiced; _was = voiced;
            if (Level > 0.7f && _peakHold <= 0f) { _peakHold = 0.5f; OnLoud?.Invoke(Level); }
            _peakHold -= dt;
            Shader.SetGlobalFloat(LFShaderIds.Voice, Level);
        }

        float[] _tmp;
        void ReadRange(int from, int n)
        {
            int total = _mic.samples; if (from + n <= total) { if (_tmp.Length < n) _tmp = new float[n]; _mic.GetData(_tmp, from); Append(n); }
            else { int a = total - from; if (_tmp.Length < n) _tmp = new float[n]; var t1 = new float[a]; _mic.GetData(t1, from); Array.Copy(t1, 0, _tmp, 0, a); var t2 = new float[n - a]; _mic.GetData(t2, 0); Array.Copy(t2, 0, _tmp, a, n - a); Append(n); }
        }
        void Append(int n) { Array.Copy(_tmp, 0, _take, _takeLen, n); _takeLen += n; }

        bool _odd; float _held;
        float Pitch() { return PitchOf(_buf, _nsdf); }

        /// <summary>Sung pitch of the last Win samples at Rate, as a MIDI note (0 = unvoiced). McLeod normalised square difference (NSDF):
        /// one value per lag 1..MaxLag, then the first "key maximum" (one per positive lobe after the first zero crossing) within 95% of the best
        /// one is taken as the period, refined by a parabola. Taking the first strong peak instead of the highest one avoids octave-low errors,
        /// and starting at lag 1 lets high voices (up to ~1.3 kHz) through. No allocation; nsdf must hold MaxLag + 2 floats.
        /// LF.Editor.LFPitchTest feeds synthetic and recorded sung notes through this exact function.</summary>
        public static float PitchOf(float[] buf, float[] nsdf)
        {
            const int n = Win - MaxLag; const float K = 0.95f, Clarity = 0.6f;   // K: 0.95 beat 0.80/0.85/0.90 on sung vocals (fewer octave-high picks) and equals them on synthetic notes (Tools/pitch)
            double e0 = 0; for (int i = 0; i < n; i++) e0 += buf[i] * buf[i];
            if (e0 < 1e-6) return 0f;
            double e1 = 0; for (int i = 1; i <= n; i++) e1 += buf[i] * buf[i];
            nsdf[0] = 1f; nsdf[MaxLag + 1] = 0f;
            for (int lag = 1; lag <= MaxLag; lag++)
            {
                if (lag > 1) { float add = buf[lag + n - 1], sub = buf[lag - 1]; e1 += add * add - sub * sub; }
                float s0 = 0f, s1 = 0f, s2 = 0f, s3 = 0f;   // n = 804 is a multiple of 4: four running sums let IL2CPP pipeline the loop
                for (int i = 0; i < n; i += 4) { s0 += buf[i] * buf[i + lag]; s1 += buf[i + 1] * buf[i + lag + 1]; s2 += buf[i + 2] * buf[i + lag + 2]; s3 += buf[i + 3] * buf[i + lag + 3]; }
                float s = (s0 + s1) + (s2 + s3);
                nsdf[lag] = (float)(2.0 * s / (e0 + e1 + 1e-9));
            }
            // key maxima after the zero-lag lobe
            int l = 1; while (l <= MaxLag && nsdf[l] > 0f) l++;
            float gmax = 0f; int first = -1;
            for (int pass = 0; pass < 2; pass++)
            {
                int k = l;
                while (k <= MaxLag)
                {
                    while (k <= MaxLag && nsdf[k] <= 0f) k++;
                    if (k > MaxLag) break;
                    int bi = k; float bv = nsdf[k];
                    while (k <= MaxLag && nsdf[k] > 0f) { if (nsdf[k] > bv) { bv = nsdf[k]; bi = k; } k++; }
                    if (bi < MinLag || bi >= MaxLag) continue;
                    if (pass == 0) { if (bv > gmax) gmax = bv; }
                    else if (bv >= K * gmax) { first = bi; break; }
                }
                if (pass == 0 && gmax < Clarity) return 0f;
            }
            if (first < 0) return 0f;
            float y0 = nsdf[first - 1], y1 = nsdf[first], y2 = nsdf[first + 1], d = y0 - 2f * y1 + y2;
            float off = Mathf.Abs(d) > 1e-9f ? Mathf.Clamp(0.5f * (y0 - y2) / d, -0.5f, 0.5f) : 0f;
            float hz = Rate / (first + off);
            return 69f + 12f * Mathf.Log(hz / 440f, 2f);
        }
    }
}
