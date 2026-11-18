// The void: a dark concrete room you can feel but barely see. Almost black; the only light is a faint cool wash from one side and the glow of your own fingertips,
// which reveals grungy plaster walls and a glossy floor (with a reflection of your hand). As the world is built the room dissolves away.
Shader "LF/VoidRoom"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-10" }
        Pass
        {
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _LFHand0, _LFHand1, _LFHand2; float _LFVoidFade;
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 wp = TransformObjectToWorld(a.pos.xyz); o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.cs = TransformWorldToHClip(wp); return o; }
            float h31(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            TEXTURE2D(_LFNoiseTex); SAMPLER(sampler_LFNoiseTex);
            // 3D value noise from ONE bilinear fetch of a 256x256 texture whose G channel is R shifted by (37,17): about ten times cheaper than hashing on a mobile GPU
            float vn(float3 x) { float3 p = floor(x); float3 f = frac(x); f = f * f * (3.0 - 2.0 * f); float2 uv = (p.xy + float2(37.0, 17.0) * p.z) + f.xy;
                float2 rg = SAMPLE_TEXTURE2D_LOD(_LFNoiseTex, sampler_LFNoiseTex, (uv + 0.5) / 256.0, 0).yx; return lerp(rg.x, rg.y, f.z); }
            float3 handLight(float4 h, float3 wp, float3 n, float3 v, float gloss, out float3 spec)
            {
                spec = 0; if (h.w < 0.01) return 0;
                float3 L = h.xyz - wp; float d = length(L); L /= max(d, 1e-3);
                float a = h.w * pow(saturate(1.0 - d / 5.6), 2.6);
                float diff = 0.3 + 0.7 * saturate(dot(n, L));
                float3 r = reflect(-v, n); spec = float3(0.65, 0.82, 1.0) * pow(saturate(dot(r, L)), 38.0) * a * gloss * 2.2;
                return float3(0.52, 0.72, 1.0) * a * diff * 2.0;
            }
            half4 frag(V i) : SV_Target
            {
                float3 wp = i.wp; float3 v = normalize(GetCameraPositionWS() - wp); float3 n = normalize(i.wn); if (dot(n, v) < 0.0) n = -n;
                float dn = vn(wp * 0.55 + 3.0) * 0.7 + vn(wp * 1.7) * 0.3; clip(dn - (_LFVoidFade * 1.2 - 0.1));   // dissolve as the world is built
                bool floorFace = n.y > 0.7;
                float m = vn(wp * 1.1) * 0.5 + vn(wp * 3.7) * 0.3 + vn(wp * 13.0) * 0.2;
                float stain = vn(wp * float3(0.6, 2.8, 0.6) + 11.0); stain = pow(stain, 2.2);
                float3 alb = lerp(float3(0.05, 0.07, 0.09), float3(0.34, 0.42, 0.46), saturate(m * m * 1.5)) * (1.0 - stain * 0.5);
                alb *= 0.8 + 0.4 * vn(wp * 38.0);
                float gloss = floorFace ? 1.0 : 0.25;
                float3 light = 0.016;
                float3 spill = normalize(float3(0.8, 0.0, -0.6)); float wash = pow(saturate(dot(normalize(float3(wp.x, 0.001, wp.z)), spill)), 4.0) * saturate(wp.y / 3.5 + 0.25) * 0.15; light += float3(0.55, 0.72, 0.85) * wash;
                float3 s0, s1, s2; light += handLight(_LFHand0, wp, n, v, gloss, s0) + handLight(_LFHand1, wp, n, v, gloss, s1) + handLight(_LFHand2, wp, n, v, gloss, s2);
                float3 col = alb * light + s0 + s1 + s2;
                float r = length((wp - GetCameraPositionWS()).xz); col *= saturate(1.25 - r / 9.0);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
