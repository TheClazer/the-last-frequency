// Additive ribbon for TrailRenderer: bright core, soft edges, vertex colour/alpha over the trail's life.
Shader "LF/Trail"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+2" }
        Pass
        {
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.col = a.col; o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float e = 1.0 - abs(i.uv.y - 0.5) * 2.0; float soft = e * e; float core = pow(e, 6.0);
                half3 c = i.col.rgb * _Tint.rgb * (soft * 0.8 + core * 1.4) * i.col.a * 1.6;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
