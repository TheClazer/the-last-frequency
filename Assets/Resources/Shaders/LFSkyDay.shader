// Golden-hour sky: deep blue zenith, warm horizon toward the sun, sun disc with halo, layered clouds lit from below. Inverted sphere.
Shader "LF/SkyDay"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        Pass
        {
            Cull Front ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "LFReveal.hlsl"
            float4 _LFSunDir, _LFSunCol, _LFFogCol; float _LFIntensity, _LFPulse, _LFQuality;   // _LFQuality: 1 = full (PC, video), 0 = Quest tier
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 d : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.d = a.pos.xyz; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vn(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x), lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y); }
            float fbm(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 5; k++) { s += a * vn(p); p = p * 2.03 + 7.1; a *= 0.5; } return s; }
            float fbm3(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 3; k++) { s += a * vn(p); p = p * 2.03 + 7.1; a *= 0.5; } return s * 1.107; }   // Quest tier: 3 octaves
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d); float3 L = normalize(_LFSunDir.xyz); float t = _Time.y; float sd = saturate(dot(d, L));
                float el = saturate(d.y);
                half3 zen = half3(0.07, 0.24, 0.55), mid = half3(0.32, 0.58, 0.86), hor = half3(1.0, 0.78, 0.52);
                half3 c = lerp(hor, mid, smoothstep(0.0, 0.32, el)); c = lerp(c, zen, smoothstep(0.25, 0.85, el));
                c = lerp(c, half3(1.0, 0.62, 0.32), pow(sd, 3.0) * (1.0 - smoothstep(0.0, 0.5, el)) * 0.75);
                c += half3(1.0, 0.82, 0.55) * pow(sd, 14.0) * 0.55 + half3(1.0, 0.9, 0.7) * pow(sd, 900.0) * 6.0 + half3(1.0, 0.75, 0.45) * pow(sd, 90.0) * 1.2;
                float2 uv = d.xz / (d.y + 0.28) * 0.9 + float2(t * 0.006, t * 0.002); float cl, lit;
                [branch] if (_LFQuality > 0.5) { cl = fbm(uv * 1.6); float cl2 = fbm(uv * 1.6 + float2(0.0, 0.07)); lit = saturate((cl - cl2) * 9.0 + 0.5); }   // full: two 5-octave fbm (lit from below)
                else { cl = fbm3(uv * 1.6); lit = saturate(0.35 + (0.75 - cl) * 1.6); }   // Quest tier: one 3-octave fbm (12 hashes instead of 40), thin edges lit
                float cd = smoothstep(0.52, 0.78, cl) * smoothstep(0.02, 0.28, d.y) * (1.0 - smoothstep(0.85, 1.0, d.y));
                half3 cc = lerp(half3(0.55, 0.50, 0.62), half3(1.0, 0.86, 0.66), lit) * (0.8 + pow(sd, 2.0) * 0.9);
                c = lerp(c, cc, cd * 0.9);
                c = lerp(c, _LFFogCol.rgb, smoothstep(0.02, -0.14, d.y));
                c *= 1.0 + _LFIntensity * 0.25;
                float night = _LFFogCol.a; { float2 sc2 = floor(d.xz / (abs(d.y) + 0.3) * 150.0); float st2 = step(0.985, h21(sc2)) * (0.5 + 0.5 * sin(_Time.y * 2.0 + h21(sc2 + 3.0) * 30.0)) * smoothstep(-0.05, 0.25, d.y); half3 nc = lerp(half3(0.02, 0.03, 0.09), half3(0.06, 0.08, 0.2), saturate(d.y)) + st2 * half3(0.9, 0.95, 1.0) + half3(0.8, 0.85, 1.0) * pow(sd, 400.0) * 0.0; c = lerp(c, nc, night); }
                if (_LFRevCfg.x > 0.5)
                {   // the sky is drawn top-to-bottom as the user builds; void with stars until then
                    float3 wp = _LFRevOrigin.xyz + d * 400.0; float prog = LFRevProg(wp, 2); float key = 1.0 - saturate(d.y * 0.85 + 0.15); float v = smoothstep(key, key + 0.12, prog * 1.15);
                    float2 sc = floor(d.xz / (abs(d.y) + 0.3) * 160.0); float st = step(0.992, h21(sc)) * (0.5 + 0.5 * sin(_Time.y * 2.0 + h21(sc + 3.0) * 30.0)) * smoothstep(-0.05, 0.2, d.y);
                    half3 voidc = half3(0.004, 0.006, 0.02) + st * half3(0.9, 0.95, 1.0); c = lerp(voidc, c, v); if (_LFRevCfg.z > 0.5) clip(v - 0.5);
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
