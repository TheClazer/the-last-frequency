// Additive magic dust: soft halo + hot core + four-point star glint. Vertex colour carries tint; alpha carries brightness (and twinkle).
Shader "LF/Mote"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.col = a.col; o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float2 d = i.uv - 0.5; float r = length(d) * 2.0;
                float soft = pow(saturate(1.0 - r), 2.4);
                float core = smoothstep(0.22, 0.0, r);
                float ax = abs(d.x), ay = abs(d.y);
                float cross = exp(-ax * 52.0) * exp(-ay * 6.0) + exp(-ay * 52.0) * exp(-ax * 6.0);
                half3 c = (i.col.rgb * _Tint.rgb) * (soft * 0.55 + core * 1.1 + cross * 0.9 * saturate(1.0 - r)) * i.col.a * 1.7;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
