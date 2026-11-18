// Magic flower glyph (additive billboard): five soft petals with veins and a glowing heart. The jungle counterpart of the snowflake glyph.
Shader "LF/Flower"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" }
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
                float2 p = (i.uv - 0.5) * 2.0; float r = length(p); float a = atan2(p.y, p.x);
                float R = 0.30 + 0.62 * pow(abs(cos(2.5 * a)), 0.8); float petal = smoothstep(R, R - 0.1, r);
                float vein = smoothstep(0.05, 0.0, abs(sin(2.5 * a)) * r) * 0.5; float rim = smoothstep(0.06, 0.0, abs(r - R + 0.04)) * 0.8;
                float heart = smoothstep(0.2, 0.0, r); float halo = pow(saturate(1.0 - r), 2.4) * 0.4;
                half3 c = (i.col.rgb * _Tint.rgb) * (petal * (0.35 + vein) + rim + heart * 1.1 + halo) * i.col.a * step(r, 1.0);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
