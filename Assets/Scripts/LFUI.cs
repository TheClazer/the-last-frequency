using System;
using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>World-space, hands-only UI: comic panels you poke with a fingertip, comic onomatopoeia, karaoke lyrics, captions.
    /// Poke = fingertip crosses the button plane (no controller, no pinch precision). Everything is pooled; text is TextMesh with a font material.</summary>
    public class LFButton
    {
        public Transform tf; public Material mat; public TextMesh label, shadow; public Vector2 size; public Action onPress; public float cool, pressT; public bool hot, toggled; public string text; public bool visible = true; public float age, delay;
    }

    public class LFUI : MonoBehaviour
    {
        public Pointer[] pointers; public Transform head;
        public readonly List<LFButton> buttons = new List<LFButton>();
        public event Action<LFButton> OnAnyPress;
        Font _font; Material _fontMat; Vector3[] _pp;
        TextMesh _lyricA, _lyricAs, _lyricB, _lyricBs, _hint, _hintS, _bigT, _bigTs; Transform _lyricRoot, _hintRoot, _bigRoot;
        struct Comic { public Transform tf; public TextMesh a, b; public float t, life; public bool live; }
        Comic[] _comics = new Comic[8]; int _cc; float _bigLife, _bigT0; int _lastChars = -1; int _lastLine = -2;

        public void Init(Transform headT)
        {
            head = headT; _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); _fontMat = _font.material;
            var lr = new GameObject("Lyrics"); lr.transform.SetParent(transform, false); _lyricRoot = lr.transform;
            _lyricA = T(_lyricRoot, "", new Vector3(0, 0.0f, 0), 0.024f, Color.white, out _lyricAs); _lyricB = T(_lyricRoot, "", new Vector3(0, -0.25f, 0), 0.013f, new Color(1, 1, 1, 0.55f), out _lyricBs);
            var hr = new GameObject("Hint"); hr.transform.SetParent(transform, false); _hintRoot = hr.transform; _hint = T(_hintRoot, "", Vector3.zero, 0.0105f, LFUtil.Hex("#fff3b0"), out _hintS);
            var br = new GameObject("Big"); br.transform.SetParent(transform, false); _bigRoot = br.transform; _bigT = T(_bigRoot, "", Vector3.zero, 0.06f, Color.white, out _bigTs);
            for (int i = 0; i < _comics.Length; i++) { var g = new GameObject("Comic" + i); g.transform.SetParent(transform, false); _comics[i].a = T(g.transform, "", Vector3.zero, 0.03f, LFUtil.Hex("#ffe14a"), out _comics[i].b); _comics[i].tf = g.transform; g.SetActive(false); }
            _lyricRoot.gameObject.SetActive(false); _hintRoot.gameObject.SetActive(false); _bigRoot.gameObject.SetActive(false);
        }

        TextMesh T(Transform parent, string s, Vector3 lp, float size, Color c, out TextMesh shadow)
        {
            shadow = Mk(parent, s, lp + new Vector3(0.006f, -0.006f, 0.002f), size, new Color(0.02f, 0.01f, 0.1f, c.a), TextAnchor.MiddleCenter);
            return Mk(parent, s, lp, size, c, TextAnchor.MiddleCenter);
        }
        TextMesh Mk(Transform parent, string s, Vector3 lp, float size, Color c, TextAnchor a)
        {
            var g = new GameObject("T"); g.transform.SetParent(parent, false); g.transform.localPosition = lp;
            var t = g.AddComponent<TextMesh>(); t.text = s; t.anchor = a; t.alignment = TextAlignment.Center; t.characterSize = size; t.fontSize = 64; t.color = c; t.font = _font;
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = _fontMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return t;
        }
        static void SetT(TextMesh t, string s) { if (t.text != s) t.text = s; }

        // ------------------------------------------------------------- panels and buttons
        public Transform Panel(string name, Vector3 pos, bool faceHead = true)
        {
            var g = new GameObject(name); g.transform.SetParent(transform, false); g.transform.position = pos;
            if (head != null) { var d = pos - head.position; d.y = 0; if (d.sqrMagnitude > 0.001f) g.transform.rotation = Quaternion.LookRotation(d.normalized); }
            return g.transform;
        }
        public LFButton Button(Transform panel, string text, Vector3 lp, Vector2 size, Action onPress, int palette)
        {
            var g = new GameObject("Btn_" + text); g.transform.SetParent(panel, false); g.transform.localPosition = lp; g.transform.localScale = new Vector3(size.x, size.y, 1f);
            g.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad"); var mr = g.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("LF/Panel")); mat.SetColor("_ColorA", LFMats.Tint(palette)); mat.SetColor("_ColorB", LFMats.Tint2(palette)); mat.SetFloat("_Aspect", size.x / size.y); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var tg = new GameObject("L"); tg.transform.SetParent(panel, false); tg.transform.localPosition = lp + new Vector3(0, 0, -0.004f);
            TextMesh sh; var tm = T(tg.transform, text, Vector3.zero, Mathf.Max(0.0032f, size.y * 0.034f), Color.white, out sh);
            var b = new LFButton { tf = g.transform, mat = mat, label = tm, shadow = sh, size = size, onPress = onPress, text = text, delay = 0.035f * (buttons.Count % 12) }; g.transform.localScale = Vector3.zero; buttons.Add(b); return b;
        }
        /// <summary>A plain text line on a panel (title, one-line instruction). Lives and dies with the panel.</summary>
        public TextMesh Label(Transform panel, string s, Vector3 lp, float size, Color c) { TextMesh sh; return T(panel, s, lp + new Vector3(0, 0, -0.004f), size, c, out sh); }
        public void SetText(LFButton b, string s) { b.text = s; SetT(b.label, s); SetT(b.shadow, s); }
        public void SetVisible(Transform panel, bool on) { panel.gameObject.SetActive(on); foreach (var b in buttons) if (b.tf != null && b.tf.IsChildOf(panel)) b.visible = on; }
        public void Remove(Transform panel) { for (int i = buttons.Count - 1; i >= 0; i--) if (buttons[i].tf != null && buttons[i].tf.IsChildOf(panel)) buttons.RemoveAt(i); Destroy(panel.gameObject); }
        public LFButton FindButton(string text) { foreach (var b in buttons) if (b.text == text && b.visible && b.tf != null && b.tf.gameObject.activeInHierarchy) return b; return null; }
        public Vector3 ButtonWorld(LFButton b) { return b.tf.position; }
        readonly bool[] _pinchWas = new bool[4];
        /// <summary>Panels registered here stay in comfortable reach: if one ends up more than 40 degrees off to the side, or nearer than 0.45 m / further than 1.1 m
        /// from the head (the app was started before the headset was on, the player turned, the view was recentred), it slides back in front after a second.</summary>
        public readonly List<Transform> follow = new List<Transform>(); readonly Dictionary<Transform, float> _off = new Dictionary<Transform, float>(); public System.Func<Vector3> followPos;
        void FollowPanels(float dt)
        {
            if (head == null || followPos == null) return;
            Vector3 hf = head.forward; hf.y = 0f; if (hf.sqrMagnitude < 1e-4f) return; hf.Normalize();
            for (int i = follow.Count - 1; i >= 0; i--)
            {
                var t = follow[i]; if (t == null) { follow.RemoveAt(i); continue; } if (!t.gameObject.activeInHierarchy) continue;
                Vector3 d = t.position - head.position; float dist = new Vector2(d.x, d.z).magnitude; Vector3 df = d; df.y = 0f;
                float ang = df.sqrMagnitude > 1e-4f ? Vector3.Angle(hf, df) : 180f; bool bad = ang > 40f || dist < 0.45f || dist > 1.1f || Mathf.Abs(d.y) > 0.6f;
                _off.TryGetValue(t, out float o); o = bad ? o + dt : 0f; _off[t] = o;
                if (o > 1.0f)
                {
                    Vector3 want = followPos(); t.position = Vector3.Lerp(t.position, want, 1f - Mathf.Exp(-6f * dt));
                    Vector3 fd = t.position - head.position; fd.y = 0f; if (fd.sqrMagnitude > 1e-4f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(fd.normalized), 1f - Mathf.Exp(-6f * dt));
                    if ((t.position - want).sqrMagnitude < 0.0004f) _off[t] = 0f;
                }
            }
        }
        public void Press(LFButton b) { if (b == null || b.cool > 0) return; b.cool = 0.5f; b.pressT = 0.25f; b.onPress?.Invoke(); OnAnyPress?.Invoke(b); }

        // ------------------------------------------------------------- comic text
        public void ComicText(string s, Vector3 pos, Color c, float size = 1f)
        {
            var cm = _comics[(_cc++) % _comics.Length]; cm.tf.gameObject.SetActive(true); cm.tf.position = pos; cm.t = 0; cm.life = 1.1f; cm.live = true; SetT(cm.a, s); SetT(cm.b, s); cm.a.color = c; _comics[(_cc - 1) % _comics.Length] = cm; cm.tf.localScale = Vector3.one * size;
            cm.tf.rotation = Quaternion.LookRotation(pos - head.position) * Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12f, 12f));
        }
        public void Big(string s, float life)
        {
            _bigRoot.gameObject.SetActive(true); SetT(_bigT, s); SetT(_bigTs, s); _bigLife = life; _bigT0 = 0; _bigRoot.position = head.position + Vector3.forward * 2.6f + Vector3.up * 0.55f; Place(_bigRoot, 2.6f, 0.55f);
        }
        /// <summary>Caption height at 2 m: about 17 degrees above eye level, so it clears the top of the menu panel (which sits at 0.64 m and reaches about 12 degrees up). Measured overlap on the headset at 0.28.</summary>
        const float HintDy = 0.6f;
        public void Hint(string s)
        {
            if (string.IsNullOrEmpty(s)) { _hintRoot.gameObject.SetActive(false); return; }
            bool snap = !_hintRoot.gameObject.activeSelf; _hintRoot.gameObject.SetActive(true); SetT(_hint, s); SetT(_hintS, s); if (snap) _hintRoot.position = head.position + Vector3.up * HintDy; Place(_hintRoot, 2.0f, HintDy); if (snap) { _hintRoot.position = head.position + Vector3.forward * 2.0f + Vector3.up * HintDy; Place(_hintRoot, 2.0f, HintDy); }
        }
        void Place(Transform t, float dist, float dy)
        {
            if (t == _lyricRoot && t.position == Vector3.zero) t.position = head.position + Vector3.forward * dist;
            if (head == null) return; var f = head.forward; f.y = 0; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
            var p = head.position + f * dist + Vector3.up * dy; t.position = Vector3.Lerp(t.position, p, 0.15f); t.rotation = Quaternion.LookRotation(t.position - head.position);
        }

        // ------------------------------------------------------------- lyrics
        public void ShowLyrics(bool on) { _lyricRoot.gameObject.SetActive(on); _lastLine = -2; _lastChars = -1; }
        public void Lyrics(SongData song, float t)
        {
            if (song == null || song.lines == null) return;
            int li = song.LineAt(t); if (li < 0) { SetT(_lyricA, ""); SetT(_lyricAs, ""); SetT(_lyricB, li == -1 && song.lines.Length > 0 ? song.lines[0].text : ""); SetT(_lyricBs, _lyricB.text); Place(_lyricRoot, 2.5f, -0.28f); return; }
            var ln = song.lines[li]; float u = Mathf.Clamp01((t - ln.start) / Mathf.Max(0.5f, ln.end - ln.start));
            string a = ln.text; if (li != _lastLine) { SetT(_lyricA, a); SetT(_lyricAs, a); string nx = li + 1 < song.lines.Length ? song.lines[li + 1].text : ""; SetT(_lyricB, nx); SetT(_lyricBs, nx); _lastLine = li; }
            float glow = 0.55f + 0.45f * u; _lyricA.color = Color.Lerp(new Color(0.8f, 0.9f, 1f), LFUtil.Hex("#ffe14a"), u); float cs = 0.0235f + 0.002f * Mathf.Sin(t * 3f) * (1f - u * 0.5f); _lyricA.characterSize = cs; _lyricAs.characterSize = cs;
            Place(_lyricRoot, 2.5f, -0.28f);
        }

        // ------------------------------------------------------------- update
        void Update()
        {
            float dt = Time.deltaTime;
            if (_bigLife > 0) { _bigLife -= dt; _bigT0 += dt; float s = LFUtil.EaseOutBack(Mathf.Clamp01(_bigT0 * 3f)); _bigRoot.localScale = Vector3.one * s; Place(_bigRoot, 2.6f, 0.55f); if (_bigLife <= 0) _bigRoot.gameObject.SetActive(false); }
            for (int i = 0; i < _comics.Length; i++)
            {
                if (!_comics[i].live) continue; _comics[i].t += dt; float u = _comics[i].t / _comics[i].life; if (u >= 1) { _comics[i].live = false; _comics[i].tf.gameObject.SetActive(false); continue; }
                float sc = LFUtil.EaseOutBack(Mathf.Clamp01(u * 5f)) * (1f - u * 0.3f); _comics[i].tf.localScale = Vector3.one * sc * 1.2f; _comics[i].tf.position += Vector3.up * dt * 0.25f;
                var c = _comics[i].a.color; c.a = 1f - Mathf.Clamp01((u - 0.6f) / 0.4f); _comics[i].a.color = c;
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i]; if (b.tf == null || !b.visible || !b.tf.gameObject.activeInHierarchy) continue;
                b.cool -= dt; b.pressT -= dt; bool hot = false;
                if (pointers != null) for (int k = 0; k < pointers.Length; k++)
                {
                    var p = pointers[k]; if (!p.active) continue;
                    if (_pp == null || _pp.Length != pointers.Length) { _pp = new Vector3[pointers.Length]; for (int q = 0; q < _pp.Length; q++) _pp[q] = pointers[q].pos; }
                    Vector3 lp = b.tf.parent.InverseTransformPoint(p.pos) - b.tf.localPosition; Vector3 pl = b.tf.parent.InverseTransformPoint(_pp[k]) - b.tf.localPosition;
                    if (Mathf.Abs(lp.x) < b.size.x * 0.5f && Mathf.Abs(lp.y) < b.size.y * 0.5f && lp.z < 0.09f && lp.z > -0.22f)
                    {
                        hot = true; if (pl.z < -0.03f && lp.z >= -0.03f && b.cool <= 0f) Press(b);
                        else if (p.pinch && !_pinchWas[k] && b.cool <= 0f) Press(b);   // a pinch while the fingertip highlights a button also presses it
                    }
                }
                b.hot = hot; float target = hot ? 1f : (b.toggled ? 0.5f : 0f); float cur = b.mat.GetFloat("_Hot"); b.mat.SetFloat("_Hot", Mathf.Lerp(cur, target, 0.25f));
                b.age += dt; float pop = LFUtil.EaseOutBack(Mathf.Clamp01((b.age - b.delay) * 3.5f));
                float s = (1f + (b.pressT > 0 ? -0.08f * (b.pressT / 0.25f) : 0f) + (hot ? 0.05f : 0f)) * pop; b.tf.localScale = new Vector3(b.size.x * s, b.size.y * s, 1f);
                if (b.label != null) b.label.transform.localScale = Vector3.one * Mathf.Max(pop, 0.001f);
            }
            if (pointers != null) { if (_pp == null || _pp.Length != pointers.Length) _pp = new Vector3[pointers.Length]; for (int k = 0; k < pointers.Length; k++) _pp[k] = pointers[k].pos; }
            if (pointers != null) for (int k = 0; k < pointers.Length && k < _pinchWas.Length; k++) _pinchWas[k] = pointers[k].active && pointers[k].pinch;
            FollowPanels(dt);
        }
    }
}
