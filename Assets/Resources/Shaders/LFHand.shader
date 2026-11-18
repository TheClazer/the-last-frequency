// Luminous white-glass hands: soft wrapped shading, cool rim light, faint inner glow, glints. Unlit (no scene lights needed).
Shader "LF/Hand"
{
    Properties { _Tint("Tint", Color) = (0.96, 0.97, 1, 1) _Rim("Rim", Color) = (0.55, 0.85, 1, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+5" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint, _Rim; CBUFFER_END
            float _LFPulse, _LFIntensity;
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wn : TEXCOORD0; float3 wp : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 wp = TransformObjectToWorld(a.pos.xyz); o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.cs = TransformWorldToHClip(wp); return o; }
            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp);
                float3 L = normalize(float3(-0.35, 0.8, -0.4)); float l = saturate(dot(n, L) * 0.5 + 0.5);
                float fr = pow(1.0 - saturate(dot(n, v)), 2.4); float spec = pow(saturate(dot(reflect(-L, n), v)), 28.0);
                half3 c = _Tint.rgb * (0.38 + 0.62 * l) + _Rim.rgb * fr * (0.9 + _LFPulse + _LFIntensity * 0.6) + spec * 0.8 + _Rim.rgb * 0.05;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
