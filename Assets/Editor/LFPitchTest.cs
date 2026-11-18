using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LF.Editor
{
    /// <summary>Batch test of the microphone pitch tracker (simulator / editor only; no headset involved).
    ///   ./chain.sh pt1 LF.Editor.LFPitchTest.Run
    /// Feeds sung notes through the exact runtime function LFVoice.PitchOf (and, for comparison, the pre-fix detector kept here as Legacy):
    ///  1) synthetic sung tones, MIDI 40 (E2) .. 84 (C6): harmonics with vowel formants, 5.5 Hz vibrato, breath noise, for a low voice and a high voice
    ///     timbre and an easy and a hard condition;
    ///  2) if present, real sung vocals prepared by Tools/pitch/prep_vocal.py (logs/pitch/*_vocal16k.wav + *_ref.csv, pYIN reference).
    /// A frame is "correct" within 50 cents of the truth, "octave" when off by a whole number of octaves, "unvoiced" when no pitch is returned.
    /// Writes logs/pitch/pitch_test.txt and prints "[LF][PITCH]" lines.</summary>
    public static class LFPitchTest
    {
        const int Rate = LFVoice.Rate, Win = LFVoice.Win, Hop = 512;
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "logs", "pitch"));

        class Tally { public int ok, oct, miss, wrong, tot; public void Add(float est, float truth) { tot++; if (est <= 0f) { miss++; return; } float e = est - truth; if (Mathf.Abs(e) < 0.5f) ok++; else if (Mathf.Abs(e - 12f * Mathf.Round(e / 12f)) < 0.5f) oct++; else wrong++; }
            public string Row() { return tot == 0 ? "n/a" : string.Format("correct {0,5:F1}%  octave {1,5:F1}%  other {2,5:F1}%  unvoiced {3,5:F1}%  ({4} frames)", 100f * ok / tot, 100f * oct / tot, 100f * wrong / tot, 100f * miss / tot, tot); } }

        static readonly int[] RangeLo = { 40, 48, 60, 72 }, RangeHi = { 47, 59, 71, 84 }; static readonly string[] RangeName = { "E2-B2 (82-123 Hz)", "C3-B3 (131-247 Hz)", "C4-B4 (262-494 Hz)", "C5-C6 (523-1047 Hz)" };
        static int RangeOf(float midi) { for (int r = 0; r < 4; r++) if (midi >= RangeLo[r] - 0.5f && midi < RangeHi[r] + 0.5f) return r; return -1; }

        public static void Run()
        {
            var sb = new StringBuilder(); int code = 0;
            try
            {
                Directory.CreateDirectory(Dir);
                sb.AppendLine("Last Frequency pitch test, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "  (measured in simulator/editor, not on a headset)");
                sb.AppendLine("Detector NEW = LFVoice.PitchOf (NSDF, lags " + LFVoice.MinLag + ".." + LFVoice.MaxLag + " at " + Rate + " Hz).  OLD = pre-fix autocorrelation, lags 40..200.");
                sb.AppendLine("Frames: the last " + Win + " samples, every " + Hop + " samples (as the headset reads the microphone). Correct = within 50 cents.");
                sb.AppendLine();
                var buf = new float[Win]; var ns = new float[LFVoice.MaxLag + 2];
                // ---------------------------------------------------------------- synthetic
                string[] voice = { "low voice (bass/baritone timbre)", "high voice (alto/soprano timbre)" }; int[] vLo = { 40, 55 }, vHi = { 67, 84 };
                string[] cond = { "easy: 35 cents vibrato, 20 dB SNR", "hard: 60 cents vibrato, 5 dB SNR breath noise" }; float[] vib = { 35f, 60f }, snr = { 20f, 5f };
                var newAll = new Tally[4]; var oldAll = new Tally[4]; for (int r = 0; r < 4; r++) { newAll[r] = new Tally(); oldAll[r] = new Tally(); }
                for (int c = 0; c < 2; c++) for (int v = 0; v < 2; v++)
                {
                    var tn = new Tally[4]; var to = new Tally[4]; for (int r = 0; r < 4; r++) { tn[r] = new Tally(); to[r] = new Tally(); }
                    for (int m = vLo[v]; m <= vHi[v]; m++)
                    {
                        float[] f0; var y = Sung(m, 1.0f, vib[c], snr[c], v == 1, 1000 * c + 100 * v + m, out f0);
                        for (int end = Win; end <= y.Length; end += Hop)
                        {
                            Array.Copy(y, end - Win, buf, 0, Win); double fm = 0; for (int i = end - Win; i < end; i++) fm += f0[i]; float truth = 69f + 12f * Mathf.Log((float)(fm / Win) / 440f, 2f);
                            float en = LFVoice.PitchOf(buf, ns), eo = Legacy(buf); int r = RangeOf(m);
                            tn[r].Add(en, truth); to[r].Add(eo, truth); newAll[r].Add(en, truth); oldAll[r].Add(eo, truth);
                        }
                    }
                    sb.AppendLine("SYNTHETIC  " + voice[v] + ", MIDI " + vLo[v] + ".." + vHi[v] + ", " + cond[c]);
                    for (int r = 0; r < 4; r++) if (tn[r].tot > 0) { sb.AppendLine("  " + RangeName[r].PadRight(20) + " NEW " + tn[r].Row()); sb.AppendLine("  " + "".PadRight(20) + " OLD " + to[r].Row()); }
                }
                sb.AppendLine(); sb.AppendLine("SYNTHETIC, all voices and conditions, per range");
                for (int r = 0; r < 4; r++) { sb.AppendLine("  " + RangeName[r].PadRight(20) + " NEW " + newAll[r].Row()); sb.AppendLine("  " + "".PadRight(20) + " OLD " + oldAll[r].Row());
                    Debug.Log("[LF][PITCH] synthetic " + RangeName[r] + " NEW " + newAll[r].Row() + " | OLD " + oldAll[r].Row()); }
                // silence / noise must stay unvoiced
                { var rng = new System.Random(5); int fpN = 0, fpO = 0; for (int k = 0; k < 300; k++) { for (int i = 0; i < Win; i++) buf[i] = 0.05f * Gauss(rng); if (LFVoice.PitchOf(buf, ns) > 0f) fpN++; if (Legacy(buf) > 0f) fpO++; }
                  sb.AppendLine("  white noise, 300 frames: voiced NEW " + fpN + ", OLD " + fpO); Debug.Log("[LF][PITCH] noise frames voiced NEW " + fpN + " OLD " + fpO + " of 300"); }
                // ---------------------------------------------------------------- real sung vocals (AI-generated songs, vocal separated from the mix)
                sb.AppendLine();
                int files = 0;
                foreach (var wav in Directory.GetFiles(Dir, "*_vocal16k.wav"))
                {
                    string refp = wav.Replace("_vocal16k.wav", "_ref.csv"); if (!File.Exists(refp)) continue; files++;
                    int sr; var y = ReadWav(wav, out sr); if (sr != Rate) { sb.AppendLine("  skip " + Path.GetFileName(wav) + ": rate " + sr); continue; }
                    var tn = new Tally[4]; var to = new Tally[4]; var tnA = new Tally(); var toA = new Tally(); for (int r = 0; r < 4; r++) { tn[r] = new Tally(); to[r] = new Tally(); }
                    foreach (var line in File.ReadAllLines(refp))
                    {
                        var p = line.Split(','); int end; float rm; if (p.Length < 2 || !int.TryParse(p[0], out end) || !float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rm)) continue;
                        if (end > y.Length || end < Win) continue; Array.Copy(y, end - Win, buf, 0, Win);
                        double e = 0; for (int i = 0; i < Win; i++) e += buf[i] * buf[i]; float lvl = Mathf.Clamp01(Mathf.Sqrt((float)(e / Win)) * 6f); if (lvl <= 0.06f) continue;   // the app only tracks pitch above this level
                        float en = LFVoice.PitchOf(buf, ns), eo = Legacy(buf); tnA.Add(en, rm); toA.Add(eo, rm); int r = RangeOf(rm); if (r >= 0) { tn[r].Add(en, rm); to[r].Add(eo, rm); }
                    }
                    string nm = Path.GetFileName(wav);
                    sb.AppendLine("RECORDED   " + nm + " (AI-sung song vocal separated from its mix; pYIN reference; residual music present)");
                    sb.AppendLine("  all voiced frames      NEW " + tnA.Row()); sb.AppendLine("                         OLD " + toA.Row());
                    for (int r = 0; r < 4; r++) if (tn[r].tot > 0) { sb.AppendLine("  " + RangeName[r].PadRight(20) + " NEW " + tn[r].Row()); sb.AppendLine("  " + "".PadRight(20) + " OLD " + to[r].Row()); }
                    Debug.Log("[LF][PITCH] recorded " + nm + " NEW " + tnA.Row() + " | OLD " + toA.Row());
                }
                if (files == 0) sb.AppendLine("RECORDED   none found in " + Dir + " (run python -I Tools/pitch/prep_vocal.py)");
                // ---------------------------------------------------------------- cost (editor CPU; the headset is slower)
                { var rng = new System.Random(9); for (int i = 0; i < Win; i++) buf[i] = Mathf.Sin(i * 0.1f) + 0.1f * Gauss(rng); var sw = System.Diagnostics.Stopwatch.StartNew(); for (int k = 0; k < 400; k++) LFVoice.PitchOf(buf, ns); sw.Stop();
                  float us = (float)(sw.Elapsed.TotalMilliseconds * 1000.0 / 400); sw.Restart(); for (int k = 0; k < 400; k++) Legacy(buf); sw.Stop(); float uo = (float)(sw.Elapsed.TotalMilliseconds * 1000.0 / 400);
                  sb.AppendLine(); sb.AppendLine(string.Format("Cost per call in the editor (Mono, desktop CPU): NEW {0:F0} us, OLD {1:F0} us. Not measured on the headset.", us, uo)); Debug.Log("[LF][PITCH] cost us NEW " + us.ToString("F0") + " OLD " + uo.ToString("F0")); }
                File.WriteAllText(Path.Combine(Dir, "pitch_test.txt"), sb.ToString());
                Debug.Log("[LF][PITCH] DONE report " + Path.Combine(Dir, "pitch_test.txt"));
            }
            catch (Exception e) { Debug.LogError("[LF][PITCH] failed: " + e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        /// <summary>The detector as it was before 2026-11-17 (normalised autocorrelation, lags 40..200, highest peak wins).</summary>
        static float Legacy(float[] b)
        {
            const int MinLag = 40, MaxLag = 200; int n = Win - MaxLag; float best = 0f; int bl = 0; double e0 = 0; for (int i = 0; i < n; i++) e0 += b[i] * b[i];
            if (e0 < 1e-6) return 0f;
            for (int lag = MinLag; lag <= MaxLag; lag++) { double s = 0, e1 = 0; for (int i = 0; i < n; i++) { float a = b[i], c = b[i + lag]; s += a * c; e1 += c * c; } float cc = (float)(s / Math.Sqrt(e0 * e1 + 1e-9)); if (cc > best) { best = cc; bl = lag; } }
            if (best < 0.55f || bl == 0) return 0f;
            return 69f + 12f * Mathf.Log((float)Rate / bl / 440f, 2f);
        }

        static float Gauss(System.Random r) { double u1 = 1.0 - r.NextDouble(), u2 = r.NextDouble(); return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)); }

        /// <summary>A sung note: harmonics shaped by vowel formants (lower formants for the low voice), vibrato, small pitch jitter, breath noise at the given SNR.</summary>
        static float[] Sung(int midi, float secs, float vibCents, float snrDb, bool high, int seed, out float[] f0)
        {
            var rng = new System.Random(seed); int N = (int)(Rate * secs); var y = new double[N]; f0 = new float[N];
            double baseHz = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0), vph = rng.NextDouble() * 6.28, ph = 0, jit = 0;
            double[] fm = high ? new[] { 850.0, 1300.0, 2900.0 } : new[] { 600.0, 1050.0, 2500.0 }; double[] fa = { 1.5, 1.0, 0.6 }, fw = { 300.0, 400.0, 500.0 };
            int H = 1; while (H < 40 && baseHz * (H + 1) * 1.04 < 7600.0) H++;
            var hp = new double[H + 1]; var ha = new double[H + 1];
            for (int h = 1; h <= H; h++) { hp[h] = rng.NextDouble() * 6.28; double fh = baseHz * h, g = 1.0; for (int q = 0; q < 3; q++) g += fa[q] * Math.Exp(-Math.Pow((fh - fm[q]) / fw[q], 2)); ha[h] = g / Math.Pow(h, high ? 1.6 : 1.2); }
            for (int i = 0; i < N; i++)
            {
                if (i % 160 == 0) jit = 0.85 * jit + 0.15 * (rng.NextDouble() - 0.5) * 0.004;
                double f = baseHz * Math.Pow(2.0, vibCents / 1200.0 * Math.Sin(2 * Math.PI * 5.5 * i / Rate + vph)) * (1.0 + jit); f0[i] = (float)f; ph += 2 * Math.PI * f / Rate;
                double s = 0; for (int h = 1; h <= H; h++) s += ha[h] * Math.Sin(h * ph + hp[h]); y[i] = s;
            }
            double ey = 0; for (int i = 0; i < N; i++) ey += y[i] * y[i]; double sy = Math.Sqrt(ey / N) + 1e-12; double nk = Math.Pow(10.0, -snrDb / 20.0);
            var o = new float[N]; double eo = 0; for (int i = 0; i < N; i++) { double v = y[i] / sy + nk * Gauss(rng); o[i] = (float)v; eo += v * v; }
            float g2 = (float)(0.1 / Math.Sqrt(eo / N)); for (int i = 0; i < N; i++) o[i] *= g2; return o;
        }

        static float[] ReadWav(string path, out int rate)
        {
            var b = File.ReadAllBytes(path); rate = 0; int ch = 1, bits = 16, pos = 12; float[] outp = new float[0];
            while (pos + 8 <= b.Length)
            {
                string id = Encoding.ASCII.GetString(b, pos, 4); int len = BitConverter.ToInt32(b, pos + 4); int d = pos + 8;
                if (id == "fmt ") { ch = BitConverter.ToInt16(b, d + 2); rate = BitConverter.ToInt32(b, d + 4); bits = BitConverter.ToInt16(b, d + 14); }
                else if (id == "data" && bits == 16) { int n = Math.Min(len, b.Length - d) / 2 / ch; outp = new float[n]; for (int i = 0; i < n; i++) { float s = 0; for (int c = 0; c < ch; c++) s += BitConverter.ToInt16(b, d + (i * ch + c) * 2) / 32768f; outp[i] = s / ch; } }
                pos = d + len + (len & 1);
            }
            return outp;
        }
    }
}
