// The singer as a shadow: an almost-black silhouette with a faint cold rim so the shape reads against any scene. Used by the film's avatar.
Shader "LF/Shadow"
{
    Properties { _Alpha("Alpha", Range(0, 1)) = 1 _Rim("Rim", Color) = (0.85, 0.92, 1, 1) _RimPow("Rim power", Float) = 5.0 _Flutter("Cloth flutter", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+2" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half _Alpha; half4 _Rim; half _RimPow; half _Flutter; CBUFFER_END
            float _LFVoice;
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; float4 col : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 p = a.pos.xyz; float fl = _Flutter * a.col.g; float t = _Time.y;
                p.x += sin(t * 2.3 + p.y * 7.0) * 0.045 * fl * (1.0 + _LFVoice * 2.0); p.z += sin(t * 1.7 + p.y * 5.0 + 1.3) * 0.06 * fl * (1.0 + _LFVoice * 2.0);   // the hem moves; the shoulders do not
                float3 wp = TransformObjectToWorld(p); o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.cs = TransformWorldToHClip(wp); return o; }
            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp); float ndv = abs(dot(n, v));
                float rim = pow(1.0 - ndv, _RimPow);
                half3 col = lerp(half3(0.004, 0.004, 0.006), _Rim.rgb, rim * 0.3);   // black body, a hairline of light on the edge
                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}
