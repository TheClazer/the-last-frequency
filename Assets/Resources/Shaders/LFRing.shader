// Expanding shockwave ring on a flat quad (additive). _T runs 0..1.
Shader "LF/Ring"
{
    Properties { _Color("Color", Color) = (1,1,1,1) _T("T", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Color; half _T; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float d = length(i.uv - 0.5) * 2.0; float r = _T; float th = 0.12 * (1.0 - _T) + 0.015;
                float ring = smoothstep(th, 0.0, abs(d - r)); float fade = (1.0 - _T);
                float2 cell = frac(i.uv * 48.0) - 0.5; float dots = step(length(cell), 0.38 + ring * 0.1);
                return half4(_Color.rgb * ring * fade * (1.0 + 0.0 * dots) * 2.0, 1);
            }
            ENDHLSL
        }
    }
}
