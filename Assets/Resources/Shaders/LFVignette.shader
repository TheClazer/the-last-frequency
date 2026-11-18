// Comfort vignette: a soft dark ring at the edge of view that fades in while the rig is moving (stair climb, camera drift). Unlit, drawn over everything.
Shader "LF/Vignette"
{
    Properties { _Amt("Amount", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+10" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" } Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float _Amt; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target { float r = length(i.uv - 0.5) * 2.0; float a = smoothstep(0.42, 1.05, r) * _Amt; return half4(0, 0, 0.02, a); }
            ENDHLSL
        }
    }
}
