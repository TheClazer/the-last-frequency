using UnityEngine;

namespace LF
{
    /// <summary>Performance setup shared by the shaders: a small noise texture (so the element and void shaders fetch noise instead of computing it),
    /// the quality tier, and the headset's foveated rendering and dynamic resolution.</summary>
    public static class LFNoise
    {
        static Texture2D _tex;
        public static void Init()
        {
            if (_tex == null)
            {
                const int N = 256; var rnd = new System.Random(7); var r = new byte[N * N]; rnd.NextBytes(r); var px = new Color32[N * N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { byte a = r[y * N + x]; byte b = r[((y - 17 + N) % N) * N + ((x - 37 + N) % N)]; px[y * N + x] = new Color32(a, b, 0, 255); }   // G(x,y) = R(x-37, y-17)
                _tex = new Texture2D(N, N, TextureFormat.RGBA32, false, true) { name = "LFNoise", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
                _tex.SetPixels32(px); _tex.Apply(false, true);
            }
            Shader.SetGlobalTexture("_LFNoiseTex", _tex);
            Shader.SetGlobalFloat("_LFQuality", Application.isMobilePlatform ? 0f : 1f);
        }
        /// <summary>Quest only: let the headset drop resolution at the edge of view and under load.</summary>
        public static void HeadsetPerf()
        {
            if (!Application.isMobilePlatform) return;
            try
            {
                OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High; OVRManager.useDynamicFoveatedRendering = true;
                var m = OVRManager.instance; if (m != null) m.enableDynamicResolution = true;
            }
            catch (System.Exception e) { Debug.LogWarning("[LF] headset perf settings: " + e.Message); }
        }
    }
}
