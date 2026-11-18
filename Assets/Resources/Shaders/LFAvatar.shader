// Spirit avatar cloth: two-colour gown that glows with the singer's voice, embroidered trim lines, soft rim light, scene sun and fog. Opaque, unlit pass (cheap on Quest).
Shader "LF/Avatar"
{
    Properties { _ColA("Bottom", Color) = (0.1, 0.3, 0.15, 1) _ColB("Top", Color) = (1, 0.8, 0.4, 1) _Trim("Trim", Color) = (1, 0.85, 0.4, 1) _Glow("Glow", Float) = 0.4 _Pattern("Pattern", Float) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+2" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" } Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _ColA, _ColB, _Trim; float _Glow, _Pattern; CBUFFER_END
            float4 _LFSunDir, _LFSunCol, _LFAmbTop, _LFFogCol, _LFFog; float _LFVoice, _LFPulse, _LFIntensity, _LFPitch;
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wn : TEXCOORD0; float3 wp : TEXCOORD1; float4 col : COLOR; float2 uv : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); float3 wp = TransformObjectToWorld(a.pos.xyz); o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.cs = TransformWorldToHClip(wp); o.col = a.col; o.uv = a.uv; return o; }
            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.wn) * (front ? 1 : -1); float3 v = normalize(_WorldSpaceCameraPos - i.wp); float3 L = normalize(_LFSunDir.xyz);
                float t = smoothstep(0.62, 1.0, i.col.r); half3 base = lerp(_ColA.rgb, _ColB.rgb, t); base = pow(max(base, 0.0001), 2.2);
                float lines = smoothstep(0.06, 0.0, abs(frac(i.uv.x * 14.0 + sin(i.uv.y * 9.0) * 0.15) - 0.5) - 0.44) * _Pattern; float hem = smoothstep(0.88, 1.0, i.col.g);
                half3 trim = pow(max(_Trim.rgb, 0.0001), 2.2); base = lerp(base, trim, saturate(lines * 0.6 + hem));
                float wrap = saturate((dot(n, L) + 0.35) / 1.35); half3 lit = base * (_LFSunCol.rgb * wrap * 0.9 + _LFAmbTop.rgb * 0.7 + 0.12);
                float fr = pow(1.0 - saturate(dot(n, v)), 2.6); half3 rim = lerp(half3(1.0, 0.86, 0.55), half3(0.6, 0.9, 1.0), saturate(_LFPitch));
                half3 col = lit + rim * fr * (0.5 + _Glow + _LFVoice * 1.4) + base * _LFVoice * 0.9 + trim * hem * (0.3 + _LFVoice);
                float dist = length(_WorldSpaceCameraPos - i.wp); col = lerp(col, _LFFogCol.rgb, saturate(1.0 - exp(-dist * _LFFog.x))); col = col * _LFFog.w; col = col / (1.0 + col * 0.18);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
