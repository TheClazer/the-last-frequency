// Glass-ice stage: dark navy depth, glowing concentric rune rings with six-fold spokes, drifting glints, beat and voice pulses. Unlit.
Shader "LF/Floor"
{
    Properties { _ColorA("Deep", Color) = (0.02, 0.05, 0.2, 1) _ColorB("Glow", Color) = (0.45, 0.85, 1, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _ColorA, _ColorB; CBUFFER_END
            float _LFPulse, _LFIntensity, _LFVoice, _LFPitch;
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 xz : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.xz = a.pos.xz; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.xz); float th = atan2(i.xz.y, i.xz.x); float t = _Time.y;
                half3 c = lerp(_ColorA.rgb * 1.6, _ColorA.rgb * 0.5, saturate(r));
                float spokes = pow(abs(cos(th * 3.0 + t * 0.15)), 28.0) * smoothstep(1.0, 0.1, r) * 0.7;
                float rings = 0.0;
                rings += smoothstep(0.012, 0.0, abs(r - 0.96 - 0.01 * sin(t)));
                rings += smoothstep(0.02, 0.0, abs(r - 0.62)) * 0.7;
                rings += smoothstep(0.015, 0.0, abs(r - 0.34)) * 0.9;
                float rune = step(0.5, frac(th * 9.55 + 0.5)) * smoothstep(0.012, 0.0, abs(r - 0.8)) * 0.8;
                float wave = smoothstep(0.03, 0.0, abs(r - frac(t * 0.18) )) * 0.35;
                float2 g = floor(i.xz * 40.0 + floor(t * 2.0)); float spark = step(0.992, h21(g)) * 1.5;
                half3 glow = lerp(_ColorB.rgb * half3(0.6, 0.45, 1.2), _ColorB.rgb * half3(1.0, 1.2, 1.05), _LFPitch) * (spokes + rings + rune + wave) * (0.7 + _LFIntensity * 1.2 + _LFPulse * 1.5 + _LFVoice * 1.2);
                c += glow + spark * _ColorB.rgb * smoothstep(1.0, 0.2, r);
                c *= smoothstep(1.03, 0.9, r);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
