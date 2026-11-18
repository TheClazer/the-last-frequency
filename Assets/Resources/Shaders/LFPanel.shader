// Frosted-glass UI panel: translucent ice, soft inner glow, thin luminous border, hot/press states.
Shader "LF/Panel"
{
    Properties
    {
        _ColorA("Color A", Color) = (0.2, 0.7, 1, 1)
        _ColorB("Color B", Color) = (0.85, 0.97, 1, 1)
        _Ink("Unused", Color) = (0,0,0,1)
        _Aspect("Aspect w/h", Float) = 2
        _Hot("Hot", Range(0,1)) = 0
        _Radius("Corner", Float) = 0.2
        _Alpha("Alpha", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-5" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _ColorA, _ColorB, _Ink; half _Aspect, _Hot, _Radius, _Alpha; CBUFFER_END
            float _LFPulse;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = a.uv; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half4 frag(V i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1.0); float2 hb = float2(_Aspect, 1.0) * 0.5 - _Radius;
                float2 q = abs(p) - hb; float sd = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Radius;
                if (sd > 0.0) discard;
                float edge = smoothstep(-0.05, 0.0, sd);
                half3 glass = lerp(half3(0.02, 0.04, 0.14), _ColorA.rgb * 0.38, saturate(0.2 + i.uv.y * 0.5));
                glass += _ColorB.rgb * pow(saturate(-sd * 5.0), 0.5) * 0.0;
                float2 g = floor(i.uv * float2(_Aspect, 1.0) * 60.0 + floor(_Time.y * 1.5)); float spark = step(0.985, h21(g)) * 0.9;
                half3 c = glass + _ColorB.rgb * edge * (0.9 + _Hot) + spark * _ColorB.rgb + _Hot * _ColorB.rgb * 0.35 + _LFPulse * 0.05;
                return half4(c, saturate(_Alpha * (0.93 + edge * 0.2) + _Hot * 0.05));
            }
            ENDHLSL
        }
    }
}
