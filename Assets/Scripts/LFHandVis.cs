using UnityEngine;

namespace LF
{
    /// <summary>The white glossy first-person hands (same models as THE ROOM IS LYING v2) with a glowing orb at the pinch point.
    /// Shown in simulation / video and whenever no tracked hands exist; on a headset with tracking, the real hands take over.</summary>
    public partial class LFApp
    {
        class HV { public Transform root; public GameObject open, pinch; public Transform orb; public Vector3 pos, vel; public Quaternion rot = Quaternion.identity; public bool init; }
        readonly HV[] _hv = new HV[3]; bool _handsBuilt; float _trackedCheck; bool _anyTracked;
        const float HSc = 1.3f; static readonly Vector3 PinchLocal = new Vector3(-0.028f, -0.079f, 0.131f);

        void BuildHandVis()
        {
            var po = Resources.Load<GameObject>("Hands/hand_open_v2"); var pp = Resources.Load<GameObject>("Hands/hand_pinch_v2"); if (po == null || pp == null) return;
            var mat = new Material(Shader.Find("LF/Hand"));
            for (int i = 0; i < 3; i++)
            {
                float side = i == 0 ? -1f : 1f; var h = new HV(); h.root = new GameObject("HandVis" + i).transform; h.root.SetParent(transform, false); h.root.localScale = new Vector3(side * HSc, HSc, HSc);
                h.open = MakeHand(po, h.root, mat); h.pinch = MakeHand(pp, h.root, mat); h.pinch.SetActive(false);
                var orb = new GameObject("orb"); orb.transform.SetParent(h.root, false); orb.transform.localPosition = PinchLocal; orb.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("orb"); var mr = orb.AddComponent<MeshRenderer>(); mr.sharedMaterial = LFMats.Toon(LFMats.Gold); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; orb.transform.localScale = Vector3.one * 0.07f; h.orb = orb.transform;
                h.root.gameObject.SetActive(false); _hv[i] = h;
            }
            _handsBuilt = true;
        }
        static GameObject MakeHand(GameObject prefab, Transform parent, Material mat)
        {
            var g = Instantiate(prefab, parent); g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true)) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            return g;
        }

        void LateUpdate()
        {
            while (_q.Count > 0) _q.Dequeue().Invoke();
            if (!_handsBuilt || _head == null) return;
            _trackedCheck -= Time.deltaTime;
            if (_trackedCheck <= 0f) { _trackedCheck = 0.5f; _anyTracked = false; if (!Auto) foreach (var oh in FindObjectsByType<OVRHand>(FindObjectsSortMode.None)) if (oh.IsTracked) { _anyTracked = true; break; } }
            Vector3 fwd = _head.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 1e-4f ? Vector3.forward : fwd.normalized; Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            for (int i = 0; i < 3; i++)
            {
                var h = _hv[i]; var p = Pointers[i]; bool show = p.active && !_anyTracked && !Film; if (h.root.gameObject.activeSelf != show) { h.root.gameObject.SetActive(show); h.init = false; }
                if (!show) continue; float side = i == 0 ? -1f : 1f;
                Vector3 shoulder = _head.position + right * 0.2f * side + Vector3.down * 0.34f;
                Quaternion tr = Quaternion.LookRotation((p.pos - shoulder).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -14f * side);
                Vector3 off = tr * (Vector3.Scale(PinchLocal, new Vector3(side, 1f, 1f)) * HSc); Vector3 tp = p.pos - off;
                if (!h.init) { h.pos = tp; h.rot = tr; h.init = true; }
                h.pos = Vector3.SmoothDamp(h.pos, tp, ref h.vel, 0.04f); h.rot = Quaternion.Slerp(h.rot, tr, 1f - Mathf.Exp(-16f * Time.deltaTime)); h.root.SetPositionAndRotation(h.pos, h.rot);
                h.open.SetActive(!p.pinch); h.pinch.SetActive(p.pinch); h.orb.localScale = Vector3.one * (p.pinch ? 0.09f : 0.05f) * (1f + 0.15f * Mathf.Sin(Time.time * 4f + i));
            }
        }
    }
}
