// Doorway of light: a glowing frame, and inside it swirling aurora that opens from the centre (_T 0..1). Additive, drawn on a quad whose uv is 0..1.
Shader "LF/Portal"
{
    Properties { _ColorA("A", Color) = (0.3, 0.8, 1, 1) _ColorB("B", Color) = (0.8, 0.5, 1, 1) _T("Open", Range(0,1)) = 0 _Aspect("Aspect", Float) = 0.6 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+3" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _ColorA, _ColorB; half _T, _Aspect; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0; float t = _Time.y;
                float2 q = abs(p); float edge = max(q.x, q.y);
                float frame = smoothstep(0.06, 0.0, abs(edge - 0.94)) * 1.6;
                float r = length(p); float a = atan2(p.y, p.x);
                float swirl = sin(a * 3.0 + r * 7.0 - t * 1.6) * 0.5 + 0.5; float swirl2 = sin(a * 5.0 - r * 5.0 + t * 1.1) * 0.5 + 0.5;
                float open = smoothstep(_T * 1.35, _T * 1.35 - 0.35, r);
                half3 c = lerp(_ColorA.rgb, _ColorB.rgb, swirl2) * (0.25 + swirl * 0.9) * open * smoothstep(1.0, 0.85, edge);
                c += _ColorA.rgb * frame * saturate(_T * 3.0 + 0.2);
                float spark = step(0.995, frac(sin(dot(floor(i.uv * 90.0 + floor(t * 4.0)), float2(12.9898, 78.233))) * 43758.5)) * open;
                return half4(c + spark, 1);
            }
            ENDHLSL
        }
    }
}
