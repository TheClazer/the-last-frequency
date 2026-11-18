using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LF
{
    /// <summary>Life that arrives once the world is built: stags standing on the courtyard, bridge and shelf (they settle in from the ground up), and, toward the end of the song, butterflies lifting off.</summary>
    public class LFLife : MonoBehaviour
    {
        class Deer { public Transform tf; public float born = -1f, target, phase, yaw; public Vector3 pos; public int sector; }
        class Fly { public Transform tf; public Vector3 c; public Vector3 r; public float ph, sp, born; }
        readonly List<Deer> _deer = new List<Deer>(); readonly List<Fly> _fly = new List<Fly>(); Mesh _deerMesh; Material _deerMat; bool _flies;
        static readonly Vector3[] Spots = { new Vector3(3.8f, 0f, 2.6f), new Vector3(-3.6f, 0f, 3.2f), new Vector3(0.6f, 0f, -4.4f), new Vector3(-4.2f, 0f, -1.2f), new Vector3(1.1f, 0.9f, 21f), new Vector3(-1.2f, 2.6f, 31f), new Vector3(7f, 5f, 47f), new Vector3(-8f, 5f, 43f) };

        public void Init()
        {
            _deerMesh = LFMeshIO.Load("Jungle/deer"); _deerMat = new Material(Shader.Find("LF/Lit")); _deerMat.SetFloat("_Gloss", 0.03f); _deerMat.SetFloat("_Layer", 1f); _deerMat.SetFloat("_Sat", 1.1f); _deerMat.SetFloat("_NStr", 0f);
            _deerMat.SetFloat("_Dir", 1f); var rnd = new System.Random(3);
            for (int i = 0; i < Spots.Length; i++)
            {
                var g = new GameObject("Deer" + i); g.transform.SetParent(transform, false); g.AddComponent<MeshFilter>().sharedMesh = _deerMesh; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = _deerMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Vector3 p = Spots[i]; float yaw = i < 4 ? Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + (float)(rnd.NextDouble() * 70 - 35) : 180f + (float)(rnd.NextDouble() * 50 - 25);
                g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.zero; g.SetActive(false);
                int sec = Mathf.RoundToInt((Mathf.Atan2(p.x, p.z) / (2f * Mathf.PI) + 0.5f) * 12f) % 12; _deer.Add(new Deer { tf = g.transform, target = 0.62f, phase = i * 1.7f, yaw = yaw, pos = p, sector = sec });
            }
        }

        /// <summary>called by the world as sectors complete: the deer standing in that sector settle in</summary>
        public void Arrive(int sector, bool all)
        {
            foreach (var d in _deer) if ((all || Mathf.Abs(((d.sector - sector + 18) % 12) - 6) <= 2) && d.born < 0f) { d.born = Time.time + Random.Range(0f, 1.8f); d.tf.gameObject.SetActive(true); LFLog.Ev("life", "deer", 1f); }
        }

        public void Butterflies(Vector3 origin)
        {
            if (_flies) return; _flies = true; var q = new Mesh(); var V = new List<Vector3>(); var UV = new List<Vector2>(); var T = new List<int>();
            for (int sx = -1; sx <= 1; sx += 2) { int b = V.Count; V.Add(new Vector3(0, 0, -0.5f)); V.Add(new Vector3(sx * 1f, 0, -0.5f)); V.Add(new Vector3(0, 0, 0.5f)); V.Add(new Vector3(sx * 1f, 0, 0.5f)); UV.Add(new Vector2(0.5f, 0)); UV.Add(new Vector2(sx > 0 ? 1f : 0f, 0)); UV.Add(new Vector2(0.5f, 1)); UV.Add(new Vector2(sx > 0 ? 1f : 0f, 1)); if (sx > 0) { T.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); } else { T.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }); } }
            q.SetVertices(V); q.SetUVs(0, UV); q.SetTriangles(T, 0); q.RecalculateBounds();
            var cols = new[] { (new Color(1f, 0.55f, 0.1f), new Color(0.12f, 0.05f, 0.03f)), (new Color(0.35f, 0.7f, 1f), new Color(0.04f, 0.08f, 0.2f)), (new Color(1f, 0.9f, 0.4f), new Color(0.3f, 0.18f, 0.03f)), (new Color(1f, 0.5f, 0.8f), new Color(0.25f, 0.05f, 0.15f)) };
            for (int i = 0; i < 46; i++)
            {
                var m = new Material(Shader.Find("LF/Butterfly")); var cc = cols[i % cols.Length]; m.SetColor("_ColA", cc.Item1); m.SetColor("_ColB", cc.Item2); m.SetFloat("_Phase", Random.value * 6f);
                var g = new GameObject("Fly" + i); g.transform.SetParent(transform, false); g.AddComponent<MeshFilter>().sharedMesh = q; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; g.transform.localScale = Vector3.one * Random.Range(0.12f, 0.2f);
                Vector3 c = origin + new Vector3(Random.Range(-14f, 14f), Random.Range(1.5f, 6f), Random.Range(-8f, 30f)); _fly.Add(new Fly { tf = g.transform, c = c, r = new Vector3(Random.Range(2f, 7f), Random.Range(0.4f, 1.6f), Random.Range(2f, 7f)), ph = Random.value * 6.28f, sp = Random.Range(0.18f, 0.4f), born = Time.time + Random.Range(0f, 5f) });
                g.transform.position = origin + new Vector3(Random.Range(-2f, 2f), 0.1f, Random.Range(-2f, 2f)); g.SetActive(false);
            }
        }

        void Update()
        {
            float tm = Time.time;
            foreach (var d in _deer)
            {
                if (d.born < 0f || tm < d.born) continue; float u = Mathf.Clamp01((tm - d.born) / 3.2f); float s = d.target * LFUtil.EaseOutBack(u);
                float breathe = 1f + 0.012f * Mathf.Sin(tm * 1.6f + d.phase); d.tf.localScale = new Vector3(s, s * breathe, s); d.tf.rotation = Quaternion.Euler(0, d.yaw + Mathf.Sin(tm * 0.25f + d.phase) * 7f, Mathf.Sin(tm * 0.8f + d.phase) * 0.7f);
            }
            foreach (var f in _fly)
            {
                if (tm < f.born) continue; if (!f.tf.gameObject.activeSelf) f.tf.gameObject.SetActive(true); float t = tm * f.sp + f.ph;
                Vector3 p = f.c + new Vector3(Mathf.Sin(t) * f.r.x, Mathf.Sin(t * 1.7f) * f.r.y, Mathf.Cos(t * 0.8f) * f.r.z); Vector3 v = p - f.tf.position; f.tf.position = Vector3.Lerp(f.tf.position, p, 0.06f);
                if (v.sqrMagnitude > 1e-6f) f.tf.rotation = Quaternion.Slerp(f.tf.rotation, Quaternion.LookRotation(v.normalized, Vector3.up) * Quaternion.Euler(0, 0, Mathf.Sin(t * 3f) * 18f), 0.12f);
            }
        }
    }
}
