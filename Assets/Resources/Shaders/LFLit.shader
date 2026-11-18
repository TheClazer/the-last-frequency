// Baked-light stylised-realistic surface: albedo (texture or vertex colour) x [hemisphere ambient x baked AO + sun x baked sun visibility], optional normal map,
// wet specular, wind sway, aerial perspective with sun scatter. Unlit pass (no realtime lights/shadows): lighting was baked in Blender per vertex.
//   vertex colour: rgb = albedo (when _UseTex = 0), a = ambient occlusion;  uv1: x = sun visibility, y = wetness
Shader "LF/Lit"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _UseTex("Use texture", Float) = 0
        _NStr("Normal strength", Float) = 1
        _Tint("Tint", Color) = (1,1,1,1)
        _Sway("Wind sway", Float) = 0
        _Gloss("Gloss", Float) = 0.12
        _Sat("Saturation", Float) = 1.15
        _Layer("Reveal layer", Float) = 0
        _Dir("Reveal direction", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Main" Tags { "LightMode"="UniversalForward" } Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "LFReveal.hlsl"
            TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo); TEXTURE2D(_Normal); SAMPLER(sampler_Normal);
            CBUFFER_START(UnityPerMaterial) half4 _Tint; float _UseTex, _NStr, _Sway, _Gloss, _Sat, _Layer, _Dir; CBUFFER_END
            float4 _LFSunDir, _LFSunCol, _LFAmbTop, _LFAmbBot, _LFFogCol, _LFFog; float _LFIntensity, _LFPulse, _LFQuality;   // _LFQuality: 1 = full (PC, video), 0 = Quest tier
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; float4 tan : TANGENT; float4 col : COLOR; float2 uv : TEXCOORD0; float2 uv1 : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; float4 wt : TEXCOORD2; float4 col : COLOR; float2 uv : TEXCOORD3; float2 uv1 : TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 wp = TransformObjectToWorld(a.pos.xyz);
                float h = saturate(a.pos.y / 18.0); wp.x += sin(_Time.y * 0.9 + wp.x * 0.13 + wp.z * 0.1) * _Sway * h * h; wp.z += cos(_Time.y * 0.7 + wp.z * 0.11) * _Sway * 0.6 * h * h;
                o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.wt = float4(TransformObjectToWorldDir(a.tan.xyz), a.tan.w); o.cs = TransformWorldToHClip(wp); o.col = a.col; o.uv = a.uv; o.uv1 = a.uv1;
                return o;
            }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vn(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f); return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x), lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y); }
            half4 frag(V i) : SV_Target
            {
                float2 rv = LFReveal(i.wp, (int)_Layer, _Dir); clip(rv.x - 0.5);
                float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 n = normalize(i.wn); float3 L = normalize(_LFSunDir.xyz);
                half3 alb = i.col.rgb;
                if (_UseTex > 0.5)
                {
                    half3 t = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, i.uv).rgb; alb = t * _Tint.rgb * 1.1;
                    float3 tn = UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, i.uv), _NStr); float3 b = cross(n, i.wt.xyz) * i.wt.w; n = normalize(tn.x * i.wt.xyz + tn.y * b + tn.z * n);
                }
                else
                {   // fine rock/earth detail so large surfaces never look smooth
                    [branch] if (_LFQuality > 0.5)
                    {
                        float d = vn(i.wp.xz * 2.2 + i.wp.y * 0.7) * 0.6 + vn(i.wp.xz * 9.0 + i.wp.y * 3.0) * 0.4; alb *= 0.75 + 0.5 * d;
                        float3 g = float3(vn(i.wp.xz * 5.0 + 3.0) - 0.5, 0, vn(i.wp.zx * 5.0 + 9.0) - 0.5); n = normalize(n + g * 0.35 * (1.0 - n.y * 0.5));
                    }
                    else { float d = vn(i.wp.xz * 2.2 + i.wp.y * 0.7); alb *= 0.8 + 0.4 * d; }   // Quest tier: one noise (4 hashes instead of 16), no normal jitter
                }
                if (_UseTex < 0.5) alb = pow(max(alb, 0.0001), 2.2);
                float ao = lerp(1.0, saturate(i.col.a), 1.0); float sunV = i.uv1.x; float wet = i.uv1.y;
                float ndl = dot(n, L); float wrap = saturate((ndl + 0.3) / 1.3);
                half3 sun = _LFSunCol.rgb * (1.0 + _LFIntensity * 0.45 + _LFPulse * 0.2) * wrap * sunV * (0.45 + 0.55 * ao);
                half3 amb = lerp(_LFAmbBot.rgb, _LFAmbTop.rgb, n.y * 0.5 + 0.5) * ao * ao;
                half3 col = alb * (sun + amb);
                float3 hv = normalize(L + V3); float spec = pow(saturate(dot(n, hv)), 40.0) * (_Gloss + wet * 0.5) * sunV * step(0.0, ndl);
                col += _LFSunCol.rgb * spec;
                col += _LFSunCol.rgb * pow(1.0 - saturate(dot(n, V3)), 3.0) * 0.12 * sunV * saturate(-ndl * 0.5 + 0.5);   // rim / backlight
                float lum = dot(col, float3(0.3, 0.55, 0.15)); col = lerp(lum.xxx, col, _Sat + _LFIntensity * 0.25);
                float dist = length(_WorldSpaceCameraPos - i.wp); float fogT = 1.0 - exp(-dist * _LFFog.x * (1.0 + exp(-max(i.wp.y + 18.0, 0.0) * _LFFog.y) * 2.0));
                float sunAmt = pow(saturate(dot(-V3, L)), 6.0); half3 fogC = _LFFogCol.rgb + _LFSunCol.rgb * sunAmt * _LFFog.z;
                col = lerp(col, fogC, saturate(fogT));
                col = col * _LFFog.w; col = col / (1.0 + col * 0.18); col += half3(1.0, 0.72, 0.36) * 1.5 * rv.y;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
