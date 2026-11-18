using UnityEngine;

namespace LF
{
    /// <summary>Comfort and access options. Calm mode: softer pulses, slower drift, a standing vignette. Always: a vignette that rises while the view is travelling (stairs, drift). No-mic mode: a gentle simulated voice so the world still builds without singing.</summary>
    public class LFComfort : MonoBehaviour
    {
        public static bool Calm, NoMic;
        Material _m; Transform _q; Vector3 _last; float _amt; bool _have;

        void Start()
        {
            var app = LFApp.I; if (app == null || app.Cam == null) return;
            _q = new GameObject("LFVignette").transform; _q.SetParent(app.Cam.transform, false); _q.localPosition = new Vector3(0, 0, 0.32f); _q.localScale = new Vector3(0.9f, 0.9f, 1f);
            _q.gameObject.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("quad"); var r = _q.gameObject.AddComponent<MeshRenderer>(); _m = new Material(Shader.Find("LF/Vignette")); r.sharedMaterial = _m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void LateUpdate()
        {
            var app = LFApp.I; if (app == null || app.Cam == null || _m == null) return; float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 p = app.Cam.transform.position; float v = _have ? (p - _last).magnitude / dt : 0f; _last = p; _have = true; if (v > 6f) v = 0f;   // ignore teleports
            float target = Mathf.Clamp01((v - 0.25f) / 1.2f) * 0.75f + (Calm ? 0.2f : 0f);
            _amt = Mathf.Lerp(_amt, target, 1f - Mathf.Exp(-(target > _amt ? 4f : 1.5f) * dt)); _m.SetFloat("_Amt", _amt);
        }
    }
}
