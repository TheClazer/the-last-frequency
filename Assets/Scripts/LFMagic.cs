using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>Gesture magic. Two languages, both learned from how film magic moves:
    ///  1) CONJURE: a sweeping open hand drags a glittering stream; when the hand settles, a snowflake crystallises in the palm and blooms.
    ///  2) DRAW: pinch and draw a shape in the air; on release it is recognised (rectangle, circle, triangle, other) and transforms into something
    ///     magical (door of light, snowflake bloom, crystal spire, comet ribbon).
    /// Voice sets the character of everything (World.SetTimbre): high soprano = fine, bright, fast; low = thick, slow, heavy.</summary>
    public class LFMagic : MonoBehaviour
    {
        LFWorld _w; Pointer[] _p; Transform _head; LFNotePlayer _notes;
        readonly List<Vector3>[] _path = { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() }; readonly bool[] _drawing = new bool[3];
        readonly float[] _sp = new float[3], _peak = new float[3], _sweep = new float[3]; readonly Vector3[][] _hist = new Vector3[3][]; readonly int[] _hi = new int[3];
        float _cool; const int H = 24;
        class Door { public Transform tf; public Material mat; public float t; public bool live; }
        readonly Door[] _doors = new Door[3]; int _dc;
        public static event System.Action<string> OnShape; public static bool JungleMode; public static float LastPushT = -99f, LastPushAz; float _pushCool, _liftCool, _climbCool; float[] _outT = { -9f, -9f, -9f }; float _pendT = -1f; Vector3 _pendDir; float _lastStroke;

        public void Init(LFWorld w, Pointer[] p, Transform head, LFNotePlayer notes)
        {
            _w = w; _p = p; _head = head; _notes = notes;
            for (int i = 0; i < 3; i++) _hist[i] = new Vector3[H];
            for (int i = 0; i < _doors.Length; i++)
            {
                var g = new GameObject("Door" + i); g.transform.SetParent(transform, false); g.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad"); var mr = g.AddComponent<MeshRenderer>();
                var m = new Material(Shader.Find("LF/Portal")); mr.sharedMaterial = m; g.SetActive(false); _doors[i] = new Door { tf = g.transform, mat = m };
            }
        }

        void Update()
        {
            float dt = Time.deltaTime; _cool -= dt; _pushCool -= dt; _liftCool -= dt; _climbCool -= dt;
            // hand magic belongs to the song (and Listen mode); in menus, the studio and results it would fire while the player pokes buttons
            if (LFApp.I != null && LFApp.I.State != LFState.Perform && LFApp.I.State != LFState.Listen) { for (int q = 0; q < 3; q++) { _drawing[q] = false; _sp[q] = 0f; _peak[q] = 0f; _sweep[q] = 0f; } return; }
            for (int k = 0; k < 3; k++)
            {
                var pt = _p[k]; if (!pt.active) { _drawing[k] = false; _sp[k] = 0; continue; }
                _hist[k][_hi[k] = (_hi[k] + 1) % H] = pt.pos; _sp[k] = LFUtil.Damp(_sp[k], pt.vel.magnitude, 14f, dt);
                // CONJURE: sweep then settle
                if (_sp[k] > 0.55f) { _peak[k] = Mathf.Max(_peak[k], _sp[k]); _sweep[k] += dt; }
                else if (_sp[k] < 0.22f)
                {
                    if (_peak[k] > 0.7f && _sweep[k] > 0.2f && _cool <= 0f && !_drawing[k]) { Conjure(k); _cool = 0.9f; }
                    _peak[k] = 0f; _sweep[k] = 0f;
                }
                // PUSH (jungle): a hand thrown outward in any direction draws the world there
                if (JungleMode && !_drawing[k]) JungleGestures(k, pt);
                // DRAW: pinch held
                if (pt.pinch)
                {
                    if (!_drawing[k]) { _drawing[k] = true; _path[k].Clear(); }
                    var pl = _path[k]; if (pl.Count == 0 || (pl[pl.Count - 1] - pt.pos).sqrMagnitude > 0.012f * 0.012f) { pl.Add(pt.pos); _w.EmitDust(pt.pos, Vector3.zero, LFUtil.Hex("#bfeaff"), 0.07f, 1.8f); }
                }
                else if (_drawing[k]) { _drawing[k] = false; if (_path[k].Count > 8) Recognize(_path[k]); }
            }
            for (int i = 0; i < _doors.Length; i++)
            {
                var d = _doors[i]; if (!d.live) continue; d.t += dt; d.mat.SetFloat("_T", Mathf.Clamp01(d.t / 1.6f));
                if (d.t > 9f) { float f = 1f - (d.t - 9f) / 1.2f; d.mat.SetFloat("_T", Mathf.Clamp01(f)); if (f <= 0f) { d.live = false; d.tf.gameObject.SetActive(false); } }
            }
        }

        void JungleGestures(int k, Pointer pt)
        {
            Vector3 rel = pt.pos - _head.position; Vector3 relH = rel; relH.y = 0f; float rl = relH.magnitude; var J = LFJungle.I; if (J == null) return;
            bool outward = rl > 0.2f && pt.vel.magnitude > 0.9f && Vector3.Dot(pt.vel, relH / Mathf.Max(rl, 1e-3f)) > 0.85f && Mathf.Abs(pt.vel.y) < 0.8f;
            if (outward && Time.time - _outT[k] > 1.0f) { _outT[k] = Time.time; }
            if (outward && _pushCool <= 0f && Time.time - _outT[0] < 0.35f && Time.time - _outT[1] < 0.35f && _p[0].active && _p[1].active) { _pushCool = 1.4f; _pendT = -1f; StartCoroutine(BothRoutine()); return; }
            if (outward && _pushCool <= 0f && _pendT < 0f) { _pendT = Time.time + 0.3f; _pendDir = relH / rl; _pendFrom = pt.pos; }
            if (_pendT > 0f && Time.time >= _pendT) { _pendT = -1f; if (_pushCool <= 0f) { _pushCool = 1.1f; StartCoroutine(PushRoutine(_pendFrom, _pendDir)); } }
            if (_liftCool <= 0f && rel.y > 0.1f && pt.vel.y > 0.8f && !outward) { _liftCool = 1.6f; Vector3 d = rl > 0.12f ? relH / rl : new Vector3(_head.forward.x, 0, _head.forward.z).normalized; StartCoroutine(LiftRoutine(pt.pos, d)); }
            if (J.StairProgress > 0.4f && _climbCool <= 0f && rel.y < 0.15f && Mathf.Abs(pt.vel.y) > 0.9f) { float sg = Mathf.Sign(pt.vel.y); if (sg != _lastStroke) { _lastStroke = sg; _climbCool = 0.3f; J.Climb(1.1f); _w.EmitDust(pt.pos, Vector3.up * 0.5f, Color.white, 0.05f, 1f); LFLog.Ev("climb", "stroke", J.Climbed); } }
        }
        Vector3 _pendFrom;
        System.Collections.IEnumerator BothRoutine()
        {
            Vector3 f = new Vector3(_head.forward.x, 0, _head.forward.z).normalized; var go = new GameObject("StairComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.4f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0f)); tr.minVertexDistance = 0.03f;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#ffd27a"), 0.5f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr;
            Vector3 a = (_p[0].pos + _p[1].pos) * 0.5f; Vector3 b = new Vector3(0f, 0.4f, 8f); float t = 0f; if (_notes != null) { _notes.Play(Instr.Bass, 40, 1f, a); _notes.Play(Instr.Pad, 52, 0.8f, a); }
            while (t < 1.2f) { t += Time.deltaTime; float e = t / 1.2f; e = e * e * (3f - 2f * e); go.transform.position = Vector3.Lerp(a, b + Vector3.up * 3f * (1f - e), e); _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.4f, Color.white, 0.07f, 1.4f); yield return null; }
            LFJungle.I.PushBoth(); LFLog.Ev("stairs", "push", LFJungle.I.StairProgress); _w.Burst(b, LFUtil.Hex("#ffd27a"), 90, 7f); _w.RingAt(new Vector3(0, 0.1f, 8f), LFUtil.Hex("#ffd27a"), 10f, true); Destroy(go, 1.6f);
        }
        System.Collections.IEnumerator LiftRoutine(Vector3 from, Vector3 dir)
        {
            Vector3 top = _head.position + dir * 60f + Vector3.up * 40f; var go = new GameObject("LiftComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.0f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.12f), new Keyframe(1, 0f)); tr.colorGradient = new Gradient();
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#9fd8ff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; float t = 0f;
            if (_notes != null) _notes.Play(Instr.Bell, 84, 0.8f, from);
            while (t < 1.0f) { t += Time.deltaTime; go.transform.position = Vector3.Lerp(from, top, t * t); _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.3f, Color.white, 0.05f, 1f); yield return null; }
            LFJungle.I.Lift(Mathf.Atan2(dir.x, dir.z)); LFLog.Ev("lift", "water", 1f); Destroy(go, 1.2f);
        }
        System.Collections.IEnumerator PushRoutine(Vector3 from, Vector3 dir)
        {
            var go = new GameObject("PushComet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.2f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.16f), new Keyframe(1, 0f)); tr.minVertexDistance = 0.02f;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#ffd27a"), 0.4f), new GradientColorKey(LFUtil.Hex("#ff9a58"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr;
            Vector3 target = _head.position + dir * 42f + Vector3.down * 4f; float t = 0f; float dur = 0.9f; Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (_notes != null) { _notes.Play(Instr.Pad, 55, 0.7f, from); _notes.Play(Instr.Bell, 79, 0.6f, from); }
            while (t < dur) { t += Time.deltaTime; float u = t / dur; float e = u * u * (3f - 2f * u); go.transform.position = Vector3.Lerp(from, target, e) + side * Mathf.Sin(u * 12f) * 0.25f * (1f - u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.5f; _w.EmitDust(go.transform.position, Random.insideUnitSphere * 0.3f, Color.white, 0.06f, 1.2f); yield return null; }
            float az = Mathf.Atan2(dir.x, dir.z); LastPushT = Time.time; LastPushAz = az; int layer = LFJungle.I != null ? LFJungle.I.PushOne(az) : 0; LFLog.Ev("push", layer.ToString(), az);
            _w.Burst(target, LFUtil.Hex("#ffd27a"), 70, 6f); _w.RingAt(target, LFUtil.Hex("#ffd27a"), 9f, false); Destroy(go, 1.5f);
        }

        // ---------------------------------------------------------------- conjure
        void Conjure(int k)
        {
            Vector3 pos = _p[k].pos; var col = LFUtil.Hex("#9fe0ff");
            for (int i = 0; i < H; i++)   // the stream the hand just dragged
            {
                Vector3 a = _hist[k][(_hi[k] - i + H * 4) % H]; for (int s = 0; s < (JungleMode ? 1 : 4); s++) _w.EmitDust(a + Random.insideUnitSphere * 0.03f, Random.insideUnitSphere * 0.25f + Vector3.up * 0.1f, Color.Lerp(col, Color.white, Random.value), Random.Range(0.03f, 0.08f), Random.Range(1f, 2f));
            }
            _w.EmitFlake(pos + Vector3.up * 0.03f, Vector3.up * 0.05f, Color.white, 0.34f, 4f);
            for (int i = 0; i < 6; i++) _w.EmitFlake(pos + Random.insideUnitSphere * 0.2f, Random.insideUnitSphere * 0.3f, Color.Lerp(col, Color.white, 0.6f), Random.Range(0.1f, 0.2f), Random.Range(2.5f, 3.5f));
            _w.RingAt(pos, col, 0.7f, false); _w.Beat(0.7f); LFLog.Ev("conjure", "palm", 1f);
            if (_notes != null) _notes.Play(Instr.Bell, 86 - (int)(Random.value * 6), 0.6f, pos);
        }

        // ---------------------------------------------------------------- shape recognition
        void Recognize(List<Vector3> raw)
        {
            Vector3 fwd = _head.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, fwd), up = Vector3.up;
            var pts = new List<Vector2>(raw.Count); Vector3 c3 = Vector3.zero; foreach (var v in raw) c3 += v; c3 /= raw.Count;
            foreach (var v in raw) { var d = v - c3; pts.Add(new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, up))); }
            float minx = 9, maxx = -9, miny = 9, maxy = -9, len = 0; for (int i = 0; i < pts.Count; i++) { minx = Mathf.Min(minx, pts[i].x); maxx = Mathf.Max(maxx, pts[i].x); miny = Mathf.Min(miny, pts[i].y); maxy = Mathf.Max(maxy, pts[i].y); if (i > 0) len += (pts[i] - pts[i - 1]).magnitude; }
            float w = maxx - minx, h = maxy - miny, size = Mathf.Max(w, h); string kind;
            if (size < 0.1f) { _w.Burst(c3, Color.white, 20, 0.6f); return; }
            bool closed = (pts[0] - pts[pts.Count - 1]).magnitude < 0.28f * len;
            if (!closed) kind = "line";
            else
            {
                var simp = new List<Vector2>(); Rdp(pts, 0, pts.Count - 1, size * 0.14f, simp); int corners = Mathf.Max(0, simp.Count - 1);
                float mr = 0; var cen = new Vector2((minx + maxx) * 0.5f, (miny + maxy) * 0.5f); foreach (var p in pts) mr += (p - cen).magnitude; mr /= pts.Count; float var2 = 0; foreach (var p in pts) var2 += Mathf.Pow((p - cen).magnitude - mr, 2); float rough = Mathf.Sqrt(var2 / pts.Count) / Mathf.Max(mr, 1e-4f);
                kind = rough < 0.13f ? "circle" : (corners == 3 ? "triangle" : (corners >= 4 && corners <= 5 ? "rect" : "circle"));
            }
            Vector3 center = c3; Trace(raw); LFLog.Ev("shape", kind, size); OnShape?.Invoke(kind); StartCoroutine(Transform(kind, center, right, up, w, h, size));
        }
        static void Rdp(List<Vector2> p, int a, int b, float eps, List<Vector2> o)
        {
            float dm = 0; int idx = -1; Vector2 A = p[a], B = p[b]; Vector2 ab = B - A; float l = ab.magnitude;
            for (int i = a + 1; i < b; i++) { float d = l < 1e-5f ? (p[i] - A).magnitude : Mathf.Abs(ab.x * (A.y - p[i].y) - (A.x - p[i].x) * ab.y) / l; if (d > dm) { dm = d; idx = i; } }
            if (dm > eps && idx >= 0) { Rdp(p, a, idx, eps, o); o.RemoveAt(o.Count - 1); Rdp(p, idx, b, eps, o); } else { o.Add(A); o.Add(B); }
        }
        void Trace(List<Vector3> raw)   // the drawn line flares into light
        {
            for (int i = 0; i < raw.Count; i++) { for (int s = 0; s < 3; s++) _w.EmitDust(raw[i], Random.insideUnitSphere * 0.4f, Color.Lerp(LFUtil.Hex("#9fe0ff"), Color.white, Random.value), Random.Range(0.03f, 0.08f), Random.Range(1f, 2.2f)); }
        }

        System.Collections.IEnumerator Transform(string kind, Vector3 c, Vector3 right, Vector3 up, float w, float h, float size)
        {
            yield return new WaitForSeconds(0.55f);
            Vector3 face = -(_head.position - c).normalized; face.y = 0; if (face.sqrMagnitude < 0.01f) face = Vector3.forward; face.Normalize(); Vector3 floor = new Vector3(c.x, 0f, c.z) * 2.6f;
            if (floor.sqrMagnitude < 4.5f) floor = new Vector3(face.x, 0, face.z) * 2.2f;
            switch (kind)
            {
                case "rect":   // door of light: the rectangle becomes a doorway that opens onto another place
                {
                    var d = _doors[(_dc++) % _doors.Length]; d.live = true; d.t = 0; d.tf.gameObject.SetActive(true); float s = Mathf.Clamp(size * 5.2f, 1.6f, 3.2f);
                    Vector3 dp = new Vector3(face.x, 0, face.z) * 3.1f + Vector3.up * (s * 0.58f); d.tf.position = dp; d.tf.rotation = Quaternion.LookRotation(dp - new Vector3(0, dp.y, 0) * 0f - new Vector3(0, _head.position.y, 0)) * Quaternion.identity;
                    d.tf.rotation = Quaternion.LookRotation(new Vector3(face.x, 0, face.z)); d.tf.localScale = new Vector3(s * Mathf.Clamp(w / Mathf.Max(h, 0.01f), 0.55f, 1.3f), s, 1f); d.mat.SetFloat("_T", 0f);
                    _w.RingAt(new Vector3(dp.x, 0.03f, dp.z), LFUtil.Hex("#8fd8ff"), 3.4f, true); _w.GrowFan(new Vector3(dp.x - face.z * 1.4f, 0, dp.z + face.x * 1.4f), 2.4f, 1.1f, 0); _w.GrowFan(new Vector3(dp.x + face.z * 1.4f, 0, dp.z - face.x * 1.4f), 2.9f, 1.2f, 3);
                    if (_notes != null) { _notes.Play(Instr.Pad, 62, 0.8f, dp); _notes.Play(Instr.Bell, 81, 0.7f, dp); } break;
                }
                case "circle": // bloom: a halo of snowflakes opens outward from the drawn circle, out in the world in front of you
                {
                    c = _head.position + (c - _head.position).normalized * 2.3f; right *= 2.2f; up *= 2.2f;
                    for (int ring = 0; ring < 3; ring++) for (int i = 0; i < 12 + ring * 6; i++) { float a = i / (float)(12 + ring * 6) * 6.2832f; Vector3 dir = right * Mathf.Cos(a) + up * Mathf.Sin(a); _w.EmitFlake(c + dir * 0.1f, dir * (0.45f + ring * 0.4f), Color.Lerp(LFElements.A3, Color.white, ring * 0.4f), 0.3f + ring * 0.12f, 3.4f + ring * 0.5f); }
                    _w.RingAt(c, LFElements.A1, 3.4f, false); _w.RingAt(c, LFElements.A2, 5.2f, false); _w.Beat(1f); _w.FlakeBloom(c, LFElements.A3, 18); _w.Frost(c, 3.2f);
                    if (_notes != null) { _notes.Play(Instr.Bell, 88, 0.8f, c); _notes.Play(Instr.Bell, 93, 0.6f, c); } break;
                }
                case "triangle": // spire: crystals erupt from the floor and climb
                {
                    _w.GrowFan(floor, Mathf.Clamp(size * 22f, 3f, 7f), 1.6f, 4); _w.GrowFan(floor + right * 0.9f, 2.6f, 1.2f, 0); _w.GrowFan(floor - right * 0.9f, 3.2f, 1.3f, 5); _w.RingAt(new Vector3(floor.x, 0.04f, floor.z), LFUtil.Hex("#8fd8ff"), 4f, true);
                    for (int i = 0; i < 70; i++) _w.EmitDust(floor + Random.insideUnitSphere * 0.5f, Vector3.up * Random.Range(0.8f, 2.4f), Color.white, Random.Range(0.03f, 0.08f), Random.Range(1.5f, 3f));
                    if (_notes != null) { _notes.Play(Instr.Bass, 38, 0.9f, floor); _notes.Play(Instr.Piano, 74, 0.8f, floor); } break;
                }
                default:       // comet ribbon: the line lifts off as a streak of light and scatters into snowflakes
                {
                    var go = new GameObject("Comet"); var tr = go.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 1.6f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.12f), new Keyframe(1, 0f)); tr.minVertexDistance = 0.015f;
                    var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#86d4ff"), 0.4f), new GradientColorKey(LFUtil.Hex("#8a78ff"), 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; StartCoroutine(Comet(go.transform, c, right, up)); break;
                }
            }
        }
        System.Collections.IEnumerator Comet(Transform t, Vector3 c, Vector3 right, Vector3 up)
        {
            float tt = 0; Vector3 p0 = c; while (tt < 2.2f) { tt += Time.deltaTime; float u = tt / 2.2f; float a = u * 9f; t.position = p0 + (right * Mathf.Sin(a) * (0.3f + u * 2.4f)) + up * (u * u * 6f) + (new Vector3(_head.forward.x, 0, _head.forward.z).normalized * u * 3f); _w.EmitDust(t.position, Random.insideUnitSphere * 0.2f, Color.white, 0.05f, 1.2f); yield return null; }
            _w.FlakeBloom(t.position, LFElements.A1, 14); _w.Frost(t.position, 2.6f); Destroy(t.gameObject, 2f);
        }
    }
}
