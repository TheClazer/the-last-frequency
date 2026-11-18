using System;
using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    // ---------- song pack (see Tools/analyze_song.py) ----------
    [Serializable] public class SongSection { public float start, end, energy; public string kind; }
    [Serializable] public class SongLine { public string text, part; public float start, end; }
    [Serializable] public class SongCue { public float t; public string type; public int i; }
    [Serializable]
    public class SongData
    {
        public string id, title, audio, guide; public float bpm, duration; public int melodyHz = 10;
        public SongSection[] sections; public float[] beats; public float[] intensity; public SongLine[] lines; public SongCue[] cues; public float[] melody;
        public AudioClip inst, guideClip;
        public float Intensity(float t) { if (intensity == null || intensity.Length == 0) return 0.5f; int i = Mathf.Clamp((int)(t * 2f), 0, intensity.Length - 1); return intensity[i]; }
        public float MelodyAt(float t) { if (melody == null || melody.Length == 0) return 0f; int i = Mathf.Clamp((int)(t * melodyHz), 0, melody.Length - 1); return melody[i]; }
        public int SectionAt(float t) { if (sections == null) return 0; for (int i = sections.Length - 1; i >= 0; i--) if (t >= sections[i].start) return i; return 0; }
        public int LineAt(float t) { if (lines == null) return -1; int r = -1; for (int i = 0; i < lines.Length; i++) { if (lines[i].start - 0.6f <= t) r = i; else break; } return r; }
    }

    /// <summary>Catalogue seam. LocalPackProvider reads packs shipped in Resources/Songs. A LicensedApiProvider (label / lyrics service) plugs in here,
    /// returning the same SongData: licensed audio + licensed timed lyrics + analyzer-generated cues. Nothing licensed is bundled.</summary>
    public interface ISongProvider { string[] Ids(); SongData Load(string id); }
    public class LocalPackProvider : ISongProvider
    {
        public string[] Ids() { return new[] { "last_frequency", "wake_the_green" }; }
        public SongData Load(string id)
        {
            var ta = Resources.Load<TextAsset>("Songs/" + id + "/song.json"); if (ta == null) return null;
            var d = JsonUtility.FromJson<SongData>(ta.text);
            d.inst = Resources.Load<AudioClip>("Songs/" + id + "/inst"); d.guideClip = Resources.Load<AudioClip>("Songs/" + id + "/guide");
            return d;
        }
    }

    // ---------- pointers (fingertips) ----------
    public class Pointer
    {
        public bool active; public Vector3 pos, prev; public bool pinch; public int id;
        public Vector3 vel;
    }

    // ---------- global shader params ----------
    public static class LFShaderIds
    {
        public static readonly int Pitch = Shader.PropertyToID("_LFPitch"), Pulse = Shader.PropertyToID("_LFPulse"), Intensity = Shader.PropertyToID("_LFIntensity"), Stair = Shader.PropertyToID("_LFStair"), Voice = Shader.PropertyToID("_LFVoice");
    }

    // ---------- tiny helpers ----------
    public static class LFUtil
    {
        public static float Damp(float cur, float tgt, float rate, float dt) { return Mathf.Lerp(cur, tgt, 1f - Mathf.Exp(-rate * dt)); }
        public static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x) - 1f; return 1f + c3 * x * x * x + c1 * x * x; }
        public static int Hash(string s) { unchecked { int h = 23; for (int i = 0; i < s.Length; i++) h = h * 31 + s[i]; return h; } }
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
