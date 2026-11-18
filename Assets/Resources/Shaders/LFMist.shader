// Soft atmospheric mist blob (alpha blended, vertex colour = tint and density).
Shader "LF/Mist"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-3" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.col = a.col; o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target { float r = length(i.uv - 0.5) * 2.0; float a = pow(saturate(1.0 - r), 1.6); return half4(i.col.rgb, i.col.a * a); }
            ENDHLSL
        }
    }
}
