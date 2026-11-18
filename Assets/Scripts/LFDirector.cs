using UnityEngine;

namespace LF
{
    /// <summary>The film's virtual cinematographer. Reads where the avatar is and what just happened (a push, the climb, the finale) and cuts between slow, deliberate shots:
    /// establishing orbit, over-the-shoulder, faceless hero close-up, the reveal whip toward what was just drawn, a follow up the stairs, and a closing crane toward the statue.</summary>
    [DefaultExecutionOrder(-200)]
    public class LFDirector : MonoBehaviour
    {
        public Vector3 Pos = new Vector3(0, 3, -6); public Quaternion Rot = Quaternion.identity; public float Fov = 62f;
        int _shot = -1, _seq; float _t0, _len = 8f; Vector3 _vel; bool _snap;
        static readonly int[] Seq = { 0, 2, 1, 2, 0, 1, 2 };

        void LateUpdate()
        {
            var app = LFApp.I; if (app == null || app.Cam == null) return; var head = app.Cam.transform; Vector3 hp = head.position; float yaw = head.eulerAngles.y; var yq = Quaternion.Euler(0, yaw, 0); Vector3 fwd = yq * Vector3.forward, rt = yq * Vector3.right;
            float t = Time.time; var J = LFJungle.I; float climb = J != null && app.Theme == 1 ? J.Climbed : 0f; float since = t - LFMagic.LastPushT; float dt = Time.deltaTime;
            if (_shot < 0 || t - _t0 > _len)
            {
                int want; float len;
                if (climb > 38f) { want = 5; len = 22f; } else if (climb > 3f) { want = 4; len = 6.5f; } else if (since < 1.3f && _shot != 3) { want = 3; len = 3.6f; } else { want = Seq[_seq++ % Seq.Length]; len = 6.5f; }
                if (want != _shot) { _shot = want; _t0 = t; _len = len; _snap = true; } else { _t0 = t; _len = len; }
            }
            else if (since < 0.6f && _shot != 3 && _shot != 5 && t - _t0 > 2.5f) { _shot = 3; _t0 = t; _len = 3.4f; _snap = true; }
            float k = Mathf.Clamp01((t - _t0) / Mathf.Max(_len, 0.1f)); Vector3 pos, look; float fov = 62f;
            switch (_shot)
            {
                case 0: { float ang = 0.5f + (t - _t0) * 0.1f + _seq * 0.9f; pos = hp + new Vector3(Mathf.Sin(ang) * 7.5f, 2.0f + 0.4f * Mathf.Sin(ang * 1.7f), Mathf.Cos(ang) * 7.5f); look = hp + Vector3.up * 0.3f; fov = 68f; break; }
                case 1: { pos = hp - fwd * 1.7f + rt * 0.5f + Vector3.up * 0.4f + rt * Mathf.Sin(t * 0.3f) * 0.15f; look = hp + fwd * 5f + Vector3.up * 0.9f; fov = 64f; break; }
                case 2: { float dist = Mathf.Lerp(2.7f, 1.8f, k); pos = hp + fwd * dist + rt * 0.35f + Vector3.up * -0.05f; look = hp + Vector3.up * 0.05f; fov = 50f; break; }
                case 3: { Vector3 d = new Vector3(Mathf.Sin(LFMagic.LastPushAz), 0, Mathf.Cos(LFMagic.LastPushAz)); Vector3 tgt = hp + d * 40f + Vector3.down * 3f; pos = hp - fwd * 2.1f + Vector3.up * 0.9f + rt * 0.45f - fwd * k * 0.6f; look = Vector3.Lerp(hp, tgt, 0.85f); fov = 66f; break; }
                case 4: { pos = hp - Vector3.forward * 2.3f + Vector3.up * 0.9f + Vector3.right * 0.45f; look = Vector3.Lerp(hp + Vector3.forward * 5f, new Vector3(0, hp.y + 8f, 62f), Mathf.Clamp01(climb / 44f)); fov = 64f; break; }
                default: { pos = hp + new Vector3(3.0f - k * 5f, 2.2f + k * 11f, -6f - k * 12f); look = Vector3.Lerp(hp + Vector3.up * 1.2f, new Vector3(0f, 17f, 62f), 0.5f + k * 0.4f); fov = Mathf.Lerp(60f, 66f, k); break; }
            }
            if (_snap) { Pos = pos; _vel = Vector3.zero; Rot = Quaternion.LookRotation((look - pos).normalized); _snap = false; }
            else { Pos = Vector3.SmoothDamp(Pos, pos, ref _vel, 0.45f); Rot = Quaternion.Slerp(Rot, Quaternion.LookRotation((look - Pos).normalized), 1f - Mathf.Exp(-4.5f * dt)); }
            Fov = Mathf.Lerp(Fov, fov, 1f - Mathf.Exp(-3f * dt));
        }
    }
}
