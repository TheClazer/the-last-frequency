using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LF
{
    /// <summary>COMPOSE: type a word (poke letters), the word seeds a key, tempo and a set of sound-objects (piano, sax, guitar, bass, bell, pad).
    /// Poke an object to play it; REC overdubs into a looping backing; SEND TO STUDIO bounces the loop with reverb and mastering to a WAV.</summary>
    public partial class LFApp
    {
        class Orb { public Transform tf; public MeshRenderer mr; public Instr ins; public int midi; public float cool; public bool inside; public Vector3 baseP; public float phase; }
        string _word = ""; Transform _kb, _cpanel; List<Orb> _orbs = new List<Orb>(); readonly List<NoteEvent> _rec = new List<NoteEvent>(); readonly List<NoteEvent> _bed = new List<NoteEvent>();
        int _root; bool _minor, _recOn, _loopRun, _tourDone; float _beatLen, _loopT, _prevLoopT; LFButton _bRec; float[] _compMix; string _compWord = "";
        static readonly string[] InsName = { "PIANO", "SAX", "GUITAR", "BASS", "BELL", "PAD" };
        static readonly string[] InsMesh = { "cube", "drop", "shard", "orb", "orb", "drop" };
        static readonly int[] InsPal = { LFMats.Ice, LFMats.Gold, LFMats.Magenta, LFMats.Violet, LFMats.Cyan, LFMats.Mint };
        float LoopLen { get { return 8f * _beatLen; } }

        void OpenCompose()
        {
            CloseShare(); if (_menu != null) { UI.Remove(_menu); _menu = null; } State = LFState.Compose; _word = ""; UI.Hint("Type a word. It becomes your song's key, tempo and instruments.");
            _kb = UI.Panel("Keyboard", PanelPos(0.62f, -0.18f)); UI.follow.Add(_kb);
            for (int i = 0; i < 26; i++)
            {
                char ch = (char)('A' + i); int row = i / 9, col = i % 9;
                UI.Button(_kb, ch.ToString(), new Vector3((col - 4) * 0.086f, 0.1f - row * 0.088f, 0), new Vector2(0.075f, 0.075f), () => Q(() => TypeChar(ch)), i % 6);
            }
            UI.Button(_kb, "DEL", new Vector3((8 - 4) * 0.086f, 0.1f - 2 * 0.088f, 0), new Vector2(0.075f, 0.075f), () => Q(() => { if (_word.Length > 0) { _word = _word.Substring(0, _word.Length - 1); ShowWord(); } }), LFMats.Violet);
            UI.Button(_kb, "GO", new Vector3(0.1f, -0.2f, 0), new Vector2(0.3f, 0.1f), () => Q(() => { if (_word.Length >= 2) StartCoroutine(ComposeBuild()); }), LFMats.Magenta);
            UI.Button(_kb, "BACK", new Vector3(-0.2f, -0.2f, 0), new Vector2(0.22f, 0.1f), () => Q(() => { UI.Remove(_kb); _kb = null; BackToMenu(); }), LFMats.Violet);
            World.Intensity(0.3f); ShowWord();
        }
        void TypeChar(char c) { if (_word.Length < 8) { _word += c; ShowWord(); LFLog.Ev("sfx", "key"); } }
        void ShowWord() { UI.Hint("WORD:  " + (_word.Length == 0 ? "_" : _word)); }

        IEnumerator ComposeBuild()
        {
            _compWord = _word; int h = Mathf.Abs(LFUtil.Hash(_word)); _root = 50 + h % 12; _minor = ((h >> 3) & 1) == 1; _beatLen = 60f / (88 + h % 22);
            UI.Remove(_kb); _kb = null; UI.Hint("TUNING THE VOID TO \"" + _word + "\"..."); UI.Big(_word, 2.6f);
            int[] sc = LFSynth.ScaleNotes(_root + 12, _minor, 10);
            for (int ins = 0; ins < 6; ins++) { for (int k = 0; k < 10; k++) LFSynth.Samples((Instr)ins, sc[k]); yield return null; }
            _bed.Clear(); LFSynth.BedEvents(_root, _minor, _bed, 1, _beatLen);
            for (int i = 0; i < _bed.Count; i++) { var e = _bed[i]; LFSynth.Samples(e.ins, e.midi); if (i % 6 == 0) yield return null; }
            foreach (var o in _orbs) if (o.tf != null) Destroy(o.tf.gameObject); _orbs.Clear();
            var fwd = Quaternion.Euler(0, _head.eulerAngles.y, 0);
            for (int i = 0; i < 12; i++)
            {
                int row = i / 6, col = i % 6; float az = Mathf.Lerp(-62f, 62f, col / 5f) * Mathf.Deg2Rad; float rad = row == 0 ? 0.55f : 0.74f; float y = _head.position.y + (row == 0 ? -0.24f : 0.04f) + 0.05f * Mathf.Sin(col * 1.7f);
                var g = new GameObject("Orb" + i); g.transform.SetParent(transform, false); var mf = g.AddComponent<MeshFilter>(); var mr = g.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Instr ins = (Instr)(i % 6); mf.sharedMesh = LFMeshes.Get(InsMesh[(int)ins]); mr.sharedMaterial = LFMats.Toon(InsPal[(int)ins]);
                var orb = new Orb { tf = g.transform, mr = mr, ins = ins, midi = sc[(col * 2 + row * 3 + (h >> 5)) % 10], baseP = _head.position * 0f + fwd * new Vector3(Mathf.Sin(az) * rad, 0, Mathf.Cos(az) * rad) + new Vector3(_head.position.x, y, _head.position.z), phase = i * 0.9f };
                g.transform.position = orb.baseP; g.transform.localScale = Vector3.zero; _orbs.Add(orb);
                var tg = new GameObject("name"); tg.transform.SetParent(g.transform, false); tg.transform.localPosition = new Vector3(0, -0.85f, 0);
                var tm = tg.AddComponent<TextMesh>(); tm.text = InsName[(int)ins]; tm.characterSize = 0.06f; tm.fontSize = 64; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.white; tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tg.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
                tg.transform.rotation = Quaternion.LookRotation(orb.baseP - _head.position);
            }
            World.Intensity(0.45f); World.SetFlow(0.8f); World.FloorOn(true); World.Wave("shards", 6, 0, false); World.Wave("spires", 5, 2, false);
            // seated eye at the origin: the row sits ~32 deg down (was 35, at desk height) and BACK ~30 deg right (was 39), just below the lower ring of objects
            _cpanel = UI.Panel("ComposePanel", PanelPos(0.62f, -0.38f));
            _bRec = UI.Button(_cpanel, "REC: OFF", new Vector3(-0.3f, 0f, 0), new Vector2(0.2f, 0.09f), () => Q(() => { _recOn = !_recOn; UI.SetText(_bRec, _recOn ? "REC: ON" : "REC: OFF"); _bRec.toggled = _recOn; }), LFMats.Magenta);
            UI.Button(_cpanel, "CLEAR", new Vector3(-0.08f, 0f, 0), new Vector2(0.2f, 0.09f), () => Q(() => _rec.Clear()), LFMats.Violet);
            UI.Button(_cpanel, "SEND TO STUDIO", new Vector3(0.15f, 0f, 0), new Vector2(0.24f, 0.09f), () => Q(SendToStudio), LFMats.Gold);
            UI.Button(_cpanel, "BACK", new Vector3(0.36f, 0f, 0), new Vector2(0.14f, 0.09f), () => Q(LeaveCompose), LFMats.Mint);
            _loopT = 0; _prevLoopT = -0.001f; _loopRun = true; _recOn = false; _rec.Clear();
            UI.Hint("Poke the objects to play. REC overdubs onto the loop. Then SEND TO STUDIO.");
        }

        void LeaveCompose()
        {
            _loopRun = false; foreach (var o in _orbs) if (o.tf != null) Destroy(o.tf.gameObject); _orbs.Clear(); if (_cpanel != null) { UI.Remove(_cpanel); _cpanel = null; } if (_results != null) { UI.Remove(_results); _results = null; }
            _music.Stop(); BackToMenu();
        }

        void ComposeUpdate(float dt)
        {
            if (!_loopRun || _orbs.Count == 0) return; float tm = Time.time; float len = LoopLen;
            _prevLoopT = _loopT; _loopT += dt; bool wrap = false; if (_loopT >= len) { _loopT -= len; wrap = true; }
            for (int i = 0; i < _bed.Count; i++) { var e = _bed[i]; if (Crossed(e.t, _prevLoopT, _loopT, wrap)) { _notes.Play(e.ins, e.midi, e.vel * 0.5f, _head.position + Vector3.forward); LFLog.Ev("note", (int)e.ins + ":" + e.midi, e.vel * 0.5f); } }
            for (int i = 0; i < _rec.Count; i++) { var e = _rec[i]; if (Crossed(e.t, _prevLoopT, _loopT, wrap)) { _notes.Play(e.ins, e.midi, e.vel, _head.position + Vector3.forward * 0.6f); LFLog.Ev("note", (int)e.ins + ":" + e.midi, e.vel); } }
            if (Mathf.FloorToInt(_prevLoopT / _beatLen) != Mathf.FloorToInt(_loopT / _beatLen)) World.Beat(0.35f);
            for (int i = 0; i < _orbs.Count; i++)
            {
                var o = _orbs[i]; o.cool -= dt; float s = LFUtil.EaseOutBack(Mathf.Clamp01((tm - 0f) * 1f)) * (0.17f + 0.02f * Mathf.Sin(tm * 3f + o.phase));
                o.tf.localScale = Vector3.one * 0.17f * (1f + 0.0f); o.tf.position = o.baseP + new Vector3(0, Mathf.Sin(tm * 1.2f + o.phase) * 0.02f, 0);
                bool near = false;
                for (int k = 0; k < Pointers.Length; k++) { var p = Pointers[k]; if (!p.active) continue; if ((p.pos - o.tf.position).sqrMagnitude < 0.095f * 0.095f) { near = true; break; } }
                if (near && !o.inside && o.cool <= 0f) { o.cool = 0.16f; PlayOrb(o); }
                o.inside = near;
            }
        }
        static bool Crossed(float t, float a, float b, bool wrap) { return wrap ? (t > a || t <= b) : (t > a && t <= b); }

        void PlayOrb(Orb o)
        {
            _notes.Play(o.ins, o.midi, 1f, o.tf.position); LFLog.Ev("note", (int)o.ins + ":" + o.midi, 1f);
            var c = LFMats.Tint(InsPal[(int)o.ins]); World.Burst(o.tf.position, c, 26, 1.4f); World.RingAt(o.tf.position, c, 0.7f, false); World.Beat(0.5f);
            if (UnityEngine.Random.value < 0.5f) World.Wave(UnityEngine.Random.value < 0.5f ? "shards" : "skyorbs", 1, (int)o.ins, false);
            if (UnityEngine.Random.value < 0.35f) ComicPop(new[] { "Chime", "Glow", "Hum", "Ring" }[UnityEngine.Random.Range(0, 4)], InsPal[(int)o.ins]);
            if (_recOn) { float q = Mathf.Round(_loopT / (_beatLen * 0.5f)) * _beatLen * 0.5f; if (q >= LoopLen) q = 0; _rec.Add(new NoteEvent { t = q, ins = o.ins, midi = o.midi, vel = 1f }); }
        }

        void SendToStudio()
        {
            if (_bounceTask != null) return; _loopRun = false; UI.Hint("SENDING TO THE STUDIO: reverb, mastering, final mix..."); LFLog.Ev("sfx", "studio");
            var rec = new List<NoteEvent>(_rec); var bed1 = new List<NoteEvent>(_bed); float bl = _beatLen; int root = _root; bool minor = _minor; string word = _compWord; float size = PresetSize[Mathf.Max(Preset, 2)], wet = PresetWet[Mathf.Max(Preset, 2)];
            string dir = ExportDir; Directory.CreateDirectory(dir); string path = dir + "/compose_" + word + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
            _bounceTask = System.Threading.Tasks.Task.Run(() =>
            {
                var ev = new List<NoteEvent>(); int loops = 6; LFSynth.BedEvents(root, minor, ev, loops, bl);
                float L = 8f * bl; for (int l = 1; l < loops; l++) foreach (var e in rec) ev.Add(new NoteEvent { t = l * L + e.t, ins = e.ins, midi = e.midi, vel = e.vel });
                var mix = LFSynth.Bounce(ev, loops * L + 3.5f, size, wet); _compMix = mix; LFSynth.WriteWav(path, mix, LFSynth.SR);
                try { File.WriteAllText(Path.ChangeExtension(path, ".json"), "{\"word\":\"" + word + "\",\"root\":" + root + ",\"minor\":" + (minor ? "true" : "false") + ",\"beat\":" + bl.ToString("F4") + ",\"events\":" + (rec.Count) + "}"); } catch { }
#if UNITY_EDITOR
                try { Directory.CreateDirectory(@"E:\Winter\metavr\LastFrequency\logs\exports"); File.Copy(path, @"E:\Winter\metavr\LastFrequency\logs\exports\" + Path.GetFileName(path), true); } catch { }
#endif
                return path;
            });
        }
        void ComposeDone(string path)
        {
            if (_compMix != null) { var clip = AudioClip.Create("comp", _compMix.Length, 1, LFSynth.SR, false); clip.SetData(_compMix, 0); _music.clip = clip; _music.volume = 1f; _music.Play(); LFLog.Ev("music", "comp", 0f); }
            World.Wave("crown", 8, 1, false); World.Wave("skyorbs", 8, 2, false); World.Beat(1.2f); World.Burst(new Vector3(0, 1.8f, 1f), LFMats.Tint(LFMats.Gold), 120, 2.6f);
            UI.Big("YOUR SONG: " + _compWord, 3.5f); if (_cpanel != null) { UI.Remove(_cpanel); _cpanel = null; }
            foreach (var o in _orbs) if (o.tf != null) o.tf.gameObject.SetActive(false);
            if (_results != null) UI.Remove(_results); _results = UI.Panel("CResults", PanelPos(0.66f, -0.2f)); UI.follow.Add(_results);
            UI.Button(_results, "SHARE TO PHONE", new Vector3(-0.22f, 0.06f, 0), new Vector2(0.4f, 0.12f), () => Q(ShareInfo), LFMats.Gold);
            UI.Button(_results, "COMPOSE AGAIN", new Vector3(0.22f, 0.06f, 0), new Vector2(0.4f, 0.12f), () => Q(() => { _music.Stop(); UI.Remove(_results); _results = null; foreach (var o in _orbs) if (o.tf != null) Destroy(o.tf.gameObject); _orbs.Clear(); OpenCompose(); }), LFMats.Cyan);
            UI.Button(_results, "MENU", new Vector3(0f, -0.1f, 0), new Vector2(0.4f, 0.12f), () => Q(LeaveCompose), LFMats.Violet);
        }
    }
}
