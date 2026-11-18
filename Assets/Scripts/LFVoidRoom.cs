using UnityEngine;

namespace LF
{
    /// <summary>The opening void: a dark concrete room, almost black, lit only by the player's fingertips. It dissolves away as the world is built.</summary>
    public class LFVoidRoom : MonoBehaviour
    {
        public static float Fade;            // 0 = full dark room, 1 = gone
        public float Target;                 // set by LFApp
        public bool Hidden;                  // mixed reality: the real room replaces the dark one
        Transform _head; Material _mat; GameObject _root;
        static readonly int H0 = Shader.PropertyToID("_LFHand0"), H1 = Shader.PropertyToID("_LFHand1"), H2 = Shader.PropertyToID("_LFHand2"), FadeId = Shader.PropertyToID("_LFVoidFade");

        public void Init(Transform head, float scale = 1f)
        {
            _head = head; Fade = 0f; Shader.SetGlobalFloat(FadeId, 0f); _mat = new Material(Shader.Find("LF/VoidRoom")) { name = "LFVoidRoom" };
            _root = new GameObject("VoidRoom"); _root.transform.SetParent(transform, false);
            var fl = new GameObject("Floor"); fl.transform.SetParent(_root.transform, false); fl.AddComponent<MeshFilter>().sharedMesh = Floor(5.2f, 36); fl.AddComponent<MeshRenderer>().sharedMaterial = _mat;
            var wl = new GameObject("Wall"); wl.transform.SetParent(_root.transform, false); wl.AddComponent<MeshFilter>().sharedMesh = Wall(5.2f, 5.5f, 48); wl.AddComponent<MeshRenderer>().sharedMaterial = _mat;
            foreach (var mr in _root.GetComponentsInChildren<MeshRenderer>()) { mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; }
            _root.transform.position = new Vector3(0, -0.015f, 0); _root.transform.localScale = Vector3.one * scale;
        }

        void Update()
        {
            if (_root == null) return;
            Fade = Mathf.MoveTowards(Fade, Target, Time.deltaTime / 9f); Shader.SetGlobalFloat(FadeId, Fade);
            _root.SetActive(Fade < 0.995f && !Hidden);
            var app = LFApp.I; if (app == null) return;
            Shader.SetGlobalVector(H0, Light(app.Pointers[0])); Shader.SetGlobalVector(H1, Light(app.Pointers[1])); Shader.SetGlobalVector(H2, Light(app.Pointers[2]));
            if (_head != null && Application.isMobilePlatform == false) { }
            var rp = _root.transform.position; var hp = _head != null ? _head.position : Vector3.zero; _root.transform.position = new Vector3(hp.x, -0.015f, hp.z);
        }
        static Vector4 Light(Pointer p) { return p != null && p.active ? new Vector4(p.pos.x, p.pos.y, p.pos.z, 1f) : Vector4.zero; }

        static Mesh Floor(float r, int seg)
        {
            var v = new Vector3[seg + 1]; var n = new Vector3[seg + 1]; var t = new int[seg * 3]; v[seg] = Vector3.zero; n[seg] = Vector3.up;
            for (int i = 0; i < seg; i++) { float a = i / (float)seg * 6.2832f; v[i] = new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r); n[i] = Vector3.up; t[i * 3] = seg; t[i * 3 + 1] = (i + 1) % seg; t[i * 3 + 2] = i; }
            var m = new Mesh { name = "VoidFloor" }; m.vertices = v; m.normals = n; m.triangles = t; m.RecalculateBounds(); return m;
        }
        static Mesh Wall(float r, float h, int seg)
        {
            var v = new Vector3[seg * 2]; var n = new Vector3[seg * 2]; var t = new int[seg * 6];
            for (int i = 0; i < seg; i++) { float a = i / (float)seg * 6.2832f; var o = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); v[i * 2] = o * r; v[i * 2 + 1] = o * r + Vector3.up * h; n[i * 2] = n[i * 2 + 1] = -o;
                int j = (i + 1) % seg; t[i * 6] = i * 2; t[i * 6 + 1] = i * 2 + 1; t[i * 6 + 2] = j * 2; t[i * 6 + 3] = i * 2 + 1; t[i * 6 + 4] = j * 2 + 1; t[i * 6 + 5] = j * 2; }
            var m = new Mesh { name = "VoidWall" }; m.vertices = v; m.normals = n; m.triangles = t; m.RecalculateBounds(); return m;
        }
    }
}
