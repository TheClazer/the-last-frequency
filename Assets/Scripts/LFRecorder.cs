using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LF
{
    /// <summary>Event log for the sound mix of the simulation video (frame, story seconds, kind, id, value).</summary>
    public static class LFLog
    {
        public static readonly List<string> Rows = new List<string>(); public static int Frame; public static float Story;
        public static void Ev(string kind, string id, float val = 0f) { Rows.Add(Frame + "\t" + Story.ToString("F3") + "\t" + kind + "\t" + id + "\t" + val.ToString("F3")); }
        public static void Flush(string path)
        {
            // the autopilot passes a PC path (E:\...); on the headset write next to the app's own data instead
            try { if (!Application.isEditor) path = System.IO.Path.Combine(Application.persistentDataPath, "events.tsv"); var dir = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir); File.WriteAllLines(path, Rows.ToArray(), Encoding.UTF8); }
            catch (System.Exception e) { Debug.LogWarning("[LF] event log not written: " + e.Message); }
        }
    }

    /// <summary>Frame-exact capture for the simulation video: -lf-video renders the head view every frame at 30 fps story time into logs\video\frames as JPG.</summary>
    public class LFRecorder : MonoBehaviour
    {
        public const string Dir = @"E:\Winter\metavr\LastFrequency\logs\video\frames";
        Camera _src, _cam; RenderTexture _rt; Texture2D _tex; int _n; const int W = 1280, H = 720; public static int Count; int _stop = -1;

        public void Init(Camera src)
        {
            _src = src; Directory.CreateDirectory(Dir); foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
            Time.captureFramerate = 30; AudioListener.volume = 0f;
            var go = new GameObject("VideoCam"); _cam = go.AddComponent<Camera>(); _cam.CopyFrom(src); _cam.fieldOfView = 78f; _cam.nearClipPlane = 0.05f; _cam.farClipPlane = 400f;
            var ad = _cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ad.renderPostProcessing = false;
            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; _tex = new Texture2D(W, H, TextureFormat.RGB24, false); _cam.targetTexture = _rt; _cam.enabled = false; _cam.tag = "Untagged";
        }

        // Picture-in-picture, top right: a third-person camera that stays in front of the singer and follows their facing and movement, so the singer is seen as a shadow with the world behind them.
        Camera _pip; static readonly Rect PipRect = new Rect(0.70f, 0.655f, 0.285f, 0.32f);
        void RenderPip()
        {
            var app = LFApp.I; if (app == null || app.Cam == null) return;
            if (_pip == null)
            {
                var g = new GameObject("PipCam"); _pip = g.AddComponent<Camera>(); _pip.CopyFrom(_cam); _pip.targetTexture = _rt; _pip.rect = PipRect; _pip.clearFlags = CameraClearFlags.Depth; _pip.fieldOfView = 58f; _pip.cullingMask = -1; _pip.enabled = false; _pip.tag = "Untagged";
                g.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;
            }
            var head = app.Cam.transform; float yaw = head.eulerAngles.y, t = Time.time; Vector3 hp = head.position;
            Vector3 dir = Quaternion.Euler(0f, yaw + 22f * Mathf.Sin(t * 0.25f), 0f) * Vector3.forward;
            _pip.transform.SetPositionAndRotation(hp + dir * 2.9f + Vector3.up * 0.2f, Quaternion.LookRotation((hp + Vector3.down * 0.35f - (hp + dir * 2.9f + Vector3.up * 0.2f)).normalized));
            _pip.Render();
        }
        void PipBorder()
        {
            int x0 = Mathf.RoundToInt(PipRect.x * W), y0 = Mathf.RoundToInt(PipRect.y * H), x1 = Mathf.RoundToInt((PipRect.x + PipRect.width) * W) - 1, y1 = Mathf.RoundToInt((PipRect.y + PipRect.height) * H) - 1; var c = new Color32(255, 106, 31, 255);
            for (int b = 0; b < 4; b++) { for (int x = x0; x <= x1; x++) { _tex.SetPixel(x, y0 + b, c); _tex.SetPixel(x, y1 - b, c); } for (int y = y0; y <= y1; y++) { _tex.SetPixel(x0 + b, y, c); _tex.SetPixel(x1 - b, y, c); } }
        }

        void LateUpdate()
        {
            if (_src == null) return;
            if (LFApp.I != null && LFApp.I.Film && LFApp.I.Dir != null) { _cam.transform.SetPositionAndRotation(_src.transform.position, _src.transform.rotation); _cam.fieldOfView = 78f; _cam.cullingMask = ~(1 << LFAvatar.ShadowLayer); _cam.Render(); RenderPip(); var pv = RenderTexture.active; RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); PipBorder(); _tex.Apply(); RenderTexture.active = pv; File.WriteAllBytes(Dir + @"\f" + (_n++).ToString("D5") + ".jpg", _tex.EncodeToJPG(90)); Count = _n; LFLog.Frame = _n; if (_n % 300 == 0) Debug.Log("[LFVideo] film " + _n); return; }
            float tt = Time.time; Vector3 sway = new Vector3(Mathf.Sin(tt * 0.31f) * 0.02f, Mathf.Sin(tt * 0.9f) * 0.012f, Mathf.Sin(tt * 0.23f) * 0.015f);
            Quaternion jit = Quaternion.Euler(Mathf.Sin(tt * 0.37f) * 1.1f, Mathf.Sin(tt * 0.21f) * 1.6f, Mathf.Sin(tt * 0.29f) * 0.6f);
            _cam.transform.SetPositionAndRotation(_src.transform.position + sway, _src.transform.rotation * jit); _cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); _tex.Apply(); RenderTexture.active = prev;
            File.WriteAllBytes(Dir + @"\f" + (_n++).ToString("D5") + ".jpg", _tex.EncodeToJPG(90)); Count = _n; LFLog.Frame = _n;
            if (_stop < 0) { _stop = 0; var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-lf-stopat") int.TryParse(a[i + 1], out _stop); }
            if (_stop > 0 && _n >= _stop) { LFLog.Flush(@"E:\Winter\metavr\LastFrequency\logs\video\events.tsv"); Debug.Log("[LF] STOPAT reached " + _n);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(0);
#endif
            }
            if (_n % 300 == 0) Debug.Log("[LFVideo] " + _n + " frames, story " + LFLog.Story.ToString("F0") + "s");
        }
    }
}
