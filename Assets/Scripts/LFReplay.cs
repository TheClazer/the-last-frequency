using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LF
{
    [Serializable] public class RFrame { public float lvl, midi; public Vector3 hp, p0, p1, p2; public Quaternion hr; public byte fl; }
    [Serializable] public class RFile { public string song; public int theme, outfit; public float dt = 1f / 30f; public List<RFrame> f = new List<RFrame>(); }

    /// <summary>Performance recorder / player. While you sing, head pose, both fingertips, pinch states and voice (level, pitch) are saved at 30 Hz next to your song.
    /// A film render replays those inputs through the real app (same gesture detection, same world), so the film shows exactly what you built, from cinematic cameras with your avatar in it.</summary>
    public class LFReplay : MonoBehaviour
    {
        public static LFReplay I; RFile _file; bool _rec; float _acc; public bool Recording { get { return _rec; } }
        RFile _pb; public RFile Playback { get { return _pb; } }

        void Awake() { I = this; }
        public void BeginRecord(string song, int theme, int outfit) { _file = new RFile { song = song, theme = theme, outfit = outfit }; _rec = true; _acc = 0f; }
        public string EndRecord(string dir)
        {
            if (!_rec) return null; _rec = false; Directory.CreateDirectory(dir); string path = Path.Combine(dir, "performance_" + _file.song + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".lfr");
            // a full song is ~5000 frames (a few MB of JSON): serialise and write on a worker thread so the finale does not hitch. The RFile is never touched again
            // (BeginRecord makes a new one), and JsonUtility may be used off the main thread.
            var file = _file; int count = file.f.Count;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    File.WriteAllText(path, JsonUtility.ToJson(file));
#if UNITY_EDITOR
                    try { Directory.CreateDirectory(@"E:\Winter\metavr\LastFrequency\logs\exports"); File.Copy(path, @"E:\Winter\metavr\LastFrequency\logs\exports\latest.lfr", true); } catch { }
#endif
                    Debug.Log("[LF] replay saved " + path + " frames " + count);
                }
                catch (Exception e) { Debug.LogWarning("[LF] replay save failed: " + e.Message); }
            });
            return path;
        }
        public void Sample(Camera cam, Pointer[] p, LFVoice v)
        {
            if (!_rec) return; _acc += Time.deltaTime; float dt = _file.dt;
            while (_acc >= dt)
            {
                _acc -= dt; var fr = new RFrame { lvl = v.Level, midi = v.Midi, hp = cam.transform.position, hr = cam.transform.rotation, p0 = p[0].pos, p1 = p[1].pos, p2 = p[2].pos };
                byte fl = 0; for (int i = 0; i < 3; i++) { if (p[i].active) fl |= (byte)(1 << i); if (p[i].pinch) fl |= (byte)(1 << (i + 3)); } fr.fl = fl; _file.f.Add(fr);
            }
        }
        public bool Load(string path) { try { _pb = JsonUtility.FromJson<RFile>(File.ReadAllText(path)); return _pb != null && _pb.f.Count > 0; } catch (Exception e) { Debug.LogError("[LF] replay load: " + e.Message); return false; } }
    }
}
