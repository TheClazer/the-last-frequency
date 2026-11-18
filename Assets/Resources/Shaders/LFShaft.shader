// Crepuscular light shafts: additive tall card, bright at the source, fading down, streaked and shimmering. uv.y: 0 = bottom, 1 = top (sun side).
Shader "LF/Shaft"
{
    Properties { _Amount("Amount", Float) = 1 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "LFReveal.hlsl"
            CBUFFER_START(UnityPerMaterial) float _Amount; CBUFFER_END
            float4 _LFSunCol; float _LFIntensity, _LFPulse;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wp : TEXCOORD0; float2 uv : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 wp = TransformObjectToWorld(a.pos.xyz); o.wp = wp; o.cs = TransformWorldToHClip(wp); o.uv = a.uv; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vn(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x), lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y); }
            half4 frag(V i) : SV_Target
            {
                float u = i.uv.x, v = i.uv.y; float side = pow(saturate(1.0 - abs(u - 0.5) * 2.0), 1.6); float len = smoothstep(0.0, 0.25, v) * pow(saturate(1.0 - (1.0 - v) * 0.0 - 0.0), 1.0) * (0.35 + 0.65 * v);
                float st = 0.55 + 0.45 * vn(float2(u * 12.0 + _Time.y * 0.05, v * 1.5 - _Time.y * 0.03)); float sh = 0.8 + 0.2 * sin(_Time.y * 0.7 + i.wp.x * 0.05);
                float rp = _LFRevCfg.x > 0.5 ? _LFRevCfg.y : 1.0; float a = side * len * st * sh * _Amount * (0.12 + _LFIntensity * 0.55 + _LFPulse * 0.1) * smoothstep(0.1, 0.9, rp);
                return half4(_LFSunCol.rgb * a * 0.7, 1);
            }
            ENDHLSL
        }
    }
}
