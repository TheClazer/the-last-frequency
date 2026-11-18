// River / waterfall ribbon. _Fall = 0: calm flowing river (sky reflection, glints, foam edges). _Fall = 1: falling water (fast vertical streaks, whitening foam toward the base).
// uv.x across (0..1), uv.y along the flow (metres / 8).
Shader "LF/Water"
{
    Properties { _Fall("Fall", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-2" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "LFReveal.hlsl"
            CBUFFER_START(UnityPerMaterial) float _Fall; CBUFFER_END
            float4 _LFSunDir, _LFSunCol, _LFFogCol, _LFFog; float _LFIntensity;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wp : TEXCOORD0; float2 uv : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 wp = TransformObjectToWorld(a.pos.xyz); o.wp = wp; o.cs = TransformWorldToHClip(wp); o.uv = a.uv; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vn(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x), lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y); }
            half4 frag(V i) : SV_Target
            {
                float2 rv = LFReveal(i.wp, 2, -1.0); clip(rv.x - 0.5);
                float t = _Time.y; float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 L = normalize(_LFSunDir.xyz); float u = i.uv.x, v = i.uv.y; float e = abs(u - 0.5) * 2.0;
                half3 col; float alpha;
                if (_Fall > 0.5)
                {
                    float n = vn(float2(u * 16.0, v * 3.0 - t * 2.6)) * 0.55 + vn(float2(u * 34.0, v * 7.0 - t * 4.2)) * 0.45; float streak = pow(n, 1.6);
                    float foam = smoothstep(0.72, 1.0, v * 0.125 + 0.0) ; float edge = 1.0 - smoothstep(0.55, 1.0, e);
                    col = lerp(half3(0.55, 0.78, 0.86), half3(1.0, 1.0, 1.0), streak * 0.9 + foam * 0.5) * (0.65 + _LFSunCol.rgb * 0.5);
                    alpha = edge * (0.45 + streak * 0.5);
                }
                else
                {
                    float2 p = float2(u * 5.0, v * 1.6 - t * 0.35); float n = vn(p * 2.0) * 0.5 + vn(p * 5.0 + 3.0) * 0.5;
                    float3 nrm = normalize(float3((n - 0.5) * 0.5, 1.0, (vn(p * 3.0 + 7.0) - 0.5) * 0.5));
                    float fr = pow(1.0 - saturate(nrm.y * V3.y + sqrt(max(0, 1 - nrm.y * nrm.y)) * 0.0), 4.0) * 0.0 + pow(1.0 - saturate(dot(nrm, V3)), 3.0);
                    half3 deep = half3(0.02, 0.15, 0.14), shallow = half3(0.12, 0.38, 0.32), sky = half3(0.55, 0.72, 0.9);
                    col = lerp(deep, shallow, n) + sky * fr * 0.85;
                    float3 r = reflect(-V3, nrm); col += _LFSunCol.rgb * pow(saturate(dot(r, L)), 70.0) * 2.2;
                    float foam = smoothstep(0.62, 0.95, e) * (0.5 + 0.5 * vn(p * 8.0)); col = lerp(col, half3(0.9, 0.95, 0.92), foam * 0.7);
                    alpha = (1.0 - smoothstep(0.82, 1.0, e)) * 0.92;
                }
                float dist = length(_WorldSpaceCameraPos - i.wp); float fogT = 1.0 - exp(-dist * _LFFog.x); col = lerp(col, _LFFogCol.rgb, saturate(fogT)); col *= _LFFog.w;
                col += half3(1.0, 0.72, 0.36) * 1.2 * rv.y; return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
