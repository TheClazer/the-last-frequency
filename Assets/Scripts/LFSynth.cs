using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LF
{
    public enum Instr { Piano, Sax, Guitar, Bass, Bell, Pad }

    public struct NoteEvent { public float t; public Instr ins; public int midi; public float vel; }

    /// <summary>Procedural instruments (no sample files): piano, sax, plucked guitar, bass, bell, pad. Notes are rendered once into float[] and cached;
    /// the same renderer feeds real-time playback and the offline "studio" bounce (reverb + normalise + WAV).</summary>
    public static class LFSynth
    {
        public const int SR = 44100;
        static readonly Dictionary<int, float[]> _cache = new Dictionary<int, float[]>();
        static readonly Dictionary<int, AudioClip> _clips = new Dictionary<int, AudioClip>();
        static float Hz(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

        public static float[] Samples(Instr ins, int midi)
        {
            int key = (int)ins * 256 + midi; float[] s;
            lock (_cache)   // SEND TO STUDIO bounces on a worker thread while the main thread can still play notes (hand gestures)
            {
                if (_cache.TryGetValue(key, out s)) return s;
                s = Render(ins, midi); _cache[key] = s; return s;
            }
        }
        public static AudioClip Clip(Instr ins, int midi)
        {
            int key = (int)ins * 256 + midi; AudioClip c;
            if (_clips.TryGetValue(key, out c) && c != null) return c;
            var s = Samples(ins, midi); c = AudioClip.Create(ins + "_" + midi, s.Length, 1, SR, false); c.SetData(s, 0); _clips[key] = c; return c;
        }

        static float[] Render(Instr ins, int midi)
        {
            float f = Hz(midi); System.Random rnd = new System.Random(midi * 31 + (int)ins);
            switch (ins)
            {
                case Instr.Piano:
                {
                    int n = (int)(SR * Mathf.Lerp(2.4f, 1.0f, (midi - 40) / 60f)); var o = new float[n];
                    float tau = Mathf.Lerp(1.3f, 0.45f, (midi - 40) / 60f);
                    for (int h = 1; h <= 9; h++)
                    {
                        float a = 1f / Mathf.Pow(h, 1.25f) * (h % 2 == 0 ? 0.8f : 1f), fh = f * h * (1f + 0.0004f * h * h); if (fh > SR / 2.2f) break;
                        float d = 0.003f * h;
                        for (int i = 0; i < n; i++) { float t = i / (float)SR; float env = Mathf.Exp(-t / (tau / (1f + 0.35f * (h - 1)))); o[i] += a * env * (Mathf.Sin(2 * Mathf.PI * fh * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * fh * (1 + d * 0.01f) * t)); }
                    }
                    Env(o, 0.004f, 0.25f); Norm(o, 0.5f); return o;
                }
                case Instr.Sax:
                {
                    int n = (int)(SR * 1.6f); var o = new float[n]; float ph = 0, lp = 0;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)SR; float vib = 1f + 0.006f * Mathf.Sin(2 * Mathf.PI * 5.2f * t) * Mathf.Clamp01(t * 2f);
                        ph += 2 * Mathf.PI * f * vib / SR; float x = 0f;
                        for (int h = 1; h <= 14; h++) { if (f * h > SR / 2.3f) break; x += Mathf.Sin(ph * h) / h * (h % 2 == 1 ? 1f : 0.55f); }
                        float breath = ((float)rnd.NextDouble() * 2f - 1f) * 0.05f;
                        float cut = Mathf.Lerp(0.08f, 0.32f, Mathf.Clamp01(t * 6f)); lp += cut * (x + breath - lp); o[i] = lp;
                    }
                    Env(o, 0.05f, 0.25f); Norm(o, 0.5f); return o;
                }
                case Instr.Guitar:
                {
                    int n = (int)(SR * 2.0f); var o = new float[n]; int L = Mathf.Max(2, (int)(SR / f)); var d = new float[L];
                    for (int i = 0; i < L; i++) d[i] = (float)rnd.NextDouble() * 2f - 1f;
                    int p = 0; float prev = 0;
                    for (int i = 0; i < n; i++) { float cur = d[p]; float nx = 0.5f * (cur + prev) * 0.9965f; d[p] = nx; prev = cur; p++; if (p >= L) p = 0; o[i] = cur; }
                    Env(o, 0.001f, 0.2f); Norm(o, 0.55f); return o;
                }
                case Instr.Bass:
                {
                    int n = (int)(SR * 1.0f); var o = new float[n];
                    for (int i = 0; i < n; i++) { float t = i / (float)SR; float e = Mathf.Exp(-t * 3.2f); o[i] = e * (Mathf.Sin(2 * Mathf.PI * f * t) + 0.45f * Mathf.Sin(4 * Mathf.PI * f * t) + 0.2f * Mathf.Sin(6 * Mathf.PI * f * t)); }
                    Env(o, 0.006f, 0.1f); Norm(o, 0.6f); return o;
                }
                case Instr.Bell:
                {
                    int n = (int)(SR * 2.6f); var o = new float[n];
                    for (int i = 0; i < n; i++) { float t = i / (float)SR; float idx = 5f * Mathf.Exp(-t * 2.2f); float e = Mathf.Exp(-t * 1.5f); o[i] = e * Mathf.Sin(2 * Mathf.PI * f * t + idx * Mathf.Sin(2 * Mathf.PI * f * 3.5f * t)); }
                    Env(o, 0.002f, 0.3f); Norm(o, 0.45f); return o;
                }
                default: // Pad
                {
                    int n = (int)(SR * 3.6f); var o = new float[n]; float[] dt = { 0.994f, 1f, 1.006f }; float lp = 0;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)SR; float x = 0;
                        for (int k = 0; k < 3; k++) { float ph = (f * dt[k] * t) % 1f; x += (ph * 2f - 1f) * 0.33f; }
                        lp += 0.06f * (x - lp); o[i] = lp;
                    }
                    Env(o, 0.6f, 0.9f); Norm(o, 0.35f); return o;
                }
            }
        }
        static void Env(float[] o, float att, float rel)
        {
            int a = Mathf.Max(1, (int)(att * SR)), r = Mathf.Max(1, (int)(rel * SR)); int n = o.Length;
            for (int i = 0; i < Mathf.Min(a, n); i++) o[i] *= i / (float)a;
            for (int i = 0; i < Mathf.Min(r, n); i++) o[n - 1 - i] *= i / (float)r;
        }
        static void Norm(float[] o, float peak) { float m = 1e-5f; for (int i = 0; i < o.Length; i++) m = Mathf.Max(m, Mathf.Abs(o[i])); float g = peak / m; for (int i = 0; i < o.Length; i++) o[i] *= g; }

        // ----- scales / progression -----
        public static readonly int[] Major = { 0, 2, 4, 7, 9 }, Minor = { 0, 3, 5, 7, 10 };
        public static int[] ScaleNotes(int root, bool minor, int count)
        {
            var sc = minor ? Minor : Major; var r = new int[count];
            for (int i = 0; i < count; i++) r[i] = root + sc[i % 5] + 12 * (i / 5);
            return r;
        }
        /// <summary>Two-bar backing loop (8 beats): pad chords + bass, in the word's key. Times are in beats.</summary>
        public static void BedEvents(int root, bool minor, List<NoteEvent> o, int loops, float beatLen)
        {
            int[][] chords = minor
                ? new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -2, 2, 5 }, new[] { -5, -1, 2 } }
                : new[] { new[] { -3, 0, 4 }, new[] { -7, -3, 0 }, new[] { 0, 4, 7 }, new[] { -5, -1, 2 } };
            int[] bassRoot = minor ? new[] { 0, -4, -2, -5 } : new[] { -3, -7, 0, -5 };
            for (int l = 0; l < loops; l++)
                for (int c = 0; c < 4; c++)
                {
                    float t0 = (l * 8 + c * 2) * beatLen;
                    for (int k = 0; k < 3; k++) o.Add(new NoteEvent { t = t0, ins = Instr.Pad, midi = root + 12 + chords[c][k], vel = 0.55f });
                    o.Add(new NoteEvent { t = t0, ins = Instr.Bass, midi = root - 12 + bassRoot[c], vel = 0.9f });
                    o.Add(new NoteEvent { t = t0 + beatLen, ins = Instr.Bass, midi = root - 12 + bassRoot[c], vel = 0.6f });
                    o.Add(new NoteEvent { t = t0 + beatLen * 0.5f, ins = Instr.Bell, midi = root + 24 + chords[c][2], vel = 0.25f });
                }
        }

        // ----- offline studio bounce -----
        public static float[] Bounce(List<NoteEvent> ev, float length, float reverb, float wet)
        {
            int n = (int)(length * SR); var mix = new float[n];
            foreach (var e in ev)
            {
                int s0 = (int)(e.t * SR); if (s0 >= n) continue; var smp = Samples(e.ins, e.midi); int m = Mathf.Min(smp.Length, n - s0); float g = e.vel;
                for (int i = 0; i < m; i++) mix[s0 + i] += smp[i] * g;
            }
            if (wet > 0.001f) Reverb(mix, reverb, wet);
            float pk = 1e-5f; for (int i = 0; i < n; i++) pk = Mathf.Max(pk, Mathf.Abs(mix[i]));
            float gain = 0.89f / pk; int fade = SR; for (int i = 0; i < n; i++) { float g = gain; if (i > n - fade) g *= (n - i) / (float)fade; mix[i] *= g; }
            return mix;
        }
        public static void Reverb(float[] x, float size, float wet)
        {
            int[] cd = { 1557, 1617, 1491, 1422 }; int[] ad = { 225, 556 }; float fb = Mathf.Lerp(0.72f, 0.9f, size); int n = x.Length;
            var dry = (float[])x.Clone(); var acc = new float[n];
            for (int c = 0; c < cd.Length; c++)
            {
                int d = (int)(cd[c] * Mathf.Lerp(0.8f, 1.7f, size)); var buf = new float[d]; int p = 0; float lp = 0;
                for (int i = 0; i < n; i++) { float y = buf[p]; lp += 0.35f * (y - lp); buf[p] = dry[i] + lp * fb; p++; if (p >= d) p = 0; acc[i] += y * 0.25f; }
            }
            for (int a = 0; a < ad.Length; a++)
            {
                int d = ad[a]; var buf = new float[d]; int p = 0;
                for (int i = 0; i < n; i++) { float bo = buf[p]; float v = acc[i] + bo * 0.5f; buf[p] = v; p++; if (p >= d) p = 0; acc[i] = bo - acc[i] * 0.5f; }
            }
            for (int i = 0; i < n; i++) x[i] = dry[i] * (1f - 0.35f * wet) + acc[i] * wet * 2.2f;
        }

        public static void WriteWav(string path, float[] mono, int rate)
        {
            using (var fs = new FileStream(path, FileMode.Create)) using (var w = new BinaryWriter(fs))
            {
                int n = mono.Length; w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2); w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16); w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
                for (int i = 0; i < n; i++) w.Write((short)(Mathf.Clamp(mono[i], -1f, 1f) * 32000f));
            }
        }
    }

    /// <summary>Pooled spatial one-shot player so poking 8 orbs in a second never allocates.</summary>
    public class LFNotePlayer : MonoBehaviour
    {
        AudioSource[] _src; int _n;
        void Awake() { _src = new AudioSource[16]; for (int i = 0; i < _src.Length; i++) { var go = new GameObject("note" + i); go.transform.SetParent(transform, false); var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0.75f; s.rolloffMode = AudioRolloffMode.Linear; s.maxDistance = 12f; _src[i] = s; } }
        public void Play(Instr ins, int midi, float vol, Vector3 pos)
        {
            var s = _src[_n++ % _src.Length]; s.transform.position = pos; s.clip = LFSynth.Clip(ins, midi); s.volume = vol; s.Play();
        }
    }
}
