using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace LF
{
    /// <summary>Share to phone with no cloud: the headset serves a small web page from the app's recordings folder; the phone scans the QR code (same Wi-Fi), then plays or downloads the song
    /// (WAV) and the film (MP4) if one exists. Nothing leaves the local network, nothing is uploaded.</summary>
    public class LFShare : MonoBehaviour
    {
        public const int Port = 8080;
        TcpListener _l; Thread _t; volatile bool _run; string _dir;
        public string Url { get; private set; }

        public static string LocalIP()
        {
            try { using (var s = new UdpClient()) { s.Connect("8.8.8.8", 65530); return ((IPEndPoint)s.Client.LocalEndPoint).Address.ToString(); } }
            catch { return "127.0.0.1"; }
        }

        public string Begin(string dir)
        {
            _dir = dir; Directory.CreateDirectory(dir); if (_run) return Url;
            try { _l = new TcpListener(IPAddress.Any, Port); _l.Start(); } catch (Exception e) { Debug.LogWarning("[LF] share server: " + e.Message); return null; }
            _run = true; _t = new Thread(Loop) { IsBackground = true }; _t.Start(); Url = "http://" + LocalIP() + ":" + Port + "/"; return Url;
        }
        /// <summary>Stop serving. The privacy text promises the server is open only while the share screen is showing.</summary>
        public void End() { _run = false; try { _l?.Stop(); } catch { } _l = null; }
        void OnDestroy() { End(); }

        void Loop()
        {
            while (_run)
            {
                try { var l = _l; if (l == null) break; var c = l.AcceptTcpClient(); ThreadPool.QueueUserWorkItem(_ => Handle(c)); } catch { if (!_run) break; }
            }
        }

        void Handle(TcpClient c)
        {
            try
            {
                using (c) { c.ReceiveTimeout = 4000; var ns = c.GetStream(); var buf = new byte[4096]; int n = ns.Read(buf, 0, buf.Length); string req = Encoding.ASCII.GetString(buf, 0, n); var lines = req.Split('\n'); var first = lines[0].Split(' '); string path = first.Length > 1 ? Uri.UnescapeDataString(first[1]) : "/";
                    long rs = -1, re = -1; foreach (var ln in lines) if (ln.StartsWith("Range:", StringComparison.OrdinalIgnoreCase)) { var v = ln.Substring(ln.IndexOf('=') + 1).Trim().Split('-'); long.TryParse(v[0], out rs); if (v.Length > 1 && v[1].Length > 0) long.TryParse(v[1], out re); }
                    if (path == "/" || path.StartsWith("/?")) { Send(ns, 200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(Page())); return; }
                    if (path.StartsWith("/f/")) { string name = Path.GetFileName(path.Substring(3)); string fp = Path.Combine(_dir, name); if (!File.Exists(fp)) { Send(ns, 404, "text/plain", Encoding.ASCII.GetBytes("not found")); return; } SendFile(ns, fp, rs, re); return; }
                    Send(ns, 404, "text/plain", Encoding.ASCII.GetBytes("not found")); }
            }
            catch { }
        }
        static void Send(NetworkStream ns, int code, string type, byte[] body)
        {
            var h = Encoding.ASCII.GetBytes("HTTP/1.1 " + code + (code == 200 ? " OK" : " Not Found") + "\r\nContent-Type: " + type + "\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n"); ns.Write(h, 0, h.Length); ns.Write(body, 0, body.Length);
        }
        static void SendFile(NetworkStream ns, string fp, long rs, long re)
        {
            var fi = new FileInfo(fp); long len = fi.Length; string type = fp.EndsWith(".mp4") ? "video/mp4" : (fp.EndsWith(".wav") ? "audio/wav" : "application/octet-stream"); bool range = rs >= 0; if (!range) { rs = 0; re = len - 1; } else if (re < 0 || re >= len) re = len - 1;
            long count = re - rs + 1; string hdr = "HTTP/1.1 " + (range ? "206 Partial Content" : "200 OK") + "\r\nContent-Type: " + type + "\r\nAccept-Ranges: bytes\r\nContent-Length: " + count + "\r\n" + (range ? "Content-Range: bytes " + rs + "-" + re + "/" + len + "\r\n" : "") + "Content-Disposition: inline; filename=\"" + fi.Name + "\"\r\nConnection: close\r\n\r\n";
            var hb = Encoding.ASCII.GetBytes(hdr); ns.Write(hb, 0, hb.Length);
            using (var fs = new FileStream(fp, FileMode.Open, FileAccess.Read, FileShare.Read)) { fs.Seek(rs, SeekOrigin.Begin); var b = new byte[64 * 1024]; long left = count; while (left > 0) { int r = fs.Read(b, 0, (int)Math.Min(b.Length, left)); if (r <= 0) break; ns.Write(b, 0, r); left -= r; } }
        }
        string Page()
        {
            var files = new DirectoryInfo(_dir).GetFiles(); Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime)); var sb = new StringBuilder();
            sb.Append("<!doctype html><meta name=viewport content='width=device-width,initial-scale=1'><title>The Last Frequency</title><style>body{margin:0;background:radial-gradient(120% 90% at 50% 0%,#1b2a6b,#070b1f 60%);color:#eaf3ff;font:16px system-ui,sans-serif;padding:24px}h1{font-weight:600;letter-spacing:.04em}.c{background:#ffffff14;border:1px solid #ffffff30;border-radius:18px;padding:16px;margin:14px 0}a{color:#ffd27a}audio,video{width:100%;margin-top:8px}</style><h1>THE LAST FREQUENCY</h1><p>Your performances, saved on your headset.</p>");
            foreach (var f in files) { string u = "/f/" + Uri.EscapeDataString(f.Name); bool v = f.Extension == ".mp4"; if (!(v || f.Extension == ".wav")) continue; sb.Append("<div class=c><b>" + f.Name + "</b><br><small>" + f.LastWriteTime.ToString("g") + "</small>" + (v ? "<video controls playsinline src='" + u + "'></video>" : "<audio controls src='" + u + "'></audio>") + "<br><a href='" + u + "' download>Download</a></div>"); }
            if (files.Length == 0) sb.Append("<div class=c>Nothing saved yet. Press SAVE MY SONG in the headset.</div>"); return sb.ToString();
        }

        // ------------------------------------------------------------------------ QR panel in the world
        public static Texture2D QrTexture(string text, int scale = 8)
        {
            var m = LFQR.Encode(text); int n = m.GetLength(0), q = 4, S = (n + q * 2) * scale; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp }; var px = new Color32[S * S]; var W = new Color32(255, 255, 255, 255); var K = new Color32(8, 10, 30, 255);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { int mx = x / scale - q, my = y / scale - q; bool dark = mx >= 0 && my >= 0 && mx < n && my < n && m[n - 1 - my, mx]; px[y * S + x] = dark ? K : W; }
            tex.SetPixels32(px); tex.Apply(); return tex;
        }
    }
}
