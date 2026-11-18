// Enchanted night sky: deep navy-violet gradient, luminous aurora ribbons, horizon mist glow, twinkling stars. Swells with the song. Draw on an inverted sphere.
Shader "LF/Sky"
{
    Properties { _AurA("Aurora A", Color) = (0.2, 0.9, 1, 1) _AurB("Aurora B", Color) = (0.6, 0.35, 1, 1) _Void("Void", Color) = (0.012, 0.02, 0.1, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        Pass
        {
            Cull Front ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _AurA, _AurB, _Void; CBUFFER_END
            float _LFPulse, _LFIntensity, _LFPitch, _LFKind;
            struct A { float4 pos : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 d : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.d = a.pos.xyz; return o; }
            float h21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d); float t = _Time.y;
                float el = saturate(d.y * 0.95 + 0.05);
                half3 grad = lerp(_LFKind > 0.5 ? _Void.rgb * 2.5 + _AurB.rgb * 0.03 : half3(0.025, 0.06, 0.2), _Void.rgb, saturate(pow(el, 0.5)));
                float hor = pow(1.0 - saturate(abs(d.y) * 2.2), 3.0);
                grad += (_LFKind > 0.5 ? _AurA.rgb * 0.22 + _AurB.rgb * 0.06 : half3(0.05, 0.16, 0.38)) * hor * (0.4 + _LFIntensity * 0.9);
                float ph = d.x * 4.0 + sin(d.z * 2.5 + t * 0.07) * 1.6;
                float w1 = sin(ph + t * 0.12) * 0.5 + 0.5; float w2 = sin(d.z * 6.0 - t * 0.1 + sin(d.x * 3.0) * 2.0) * 0.5 + 0.5;
                float ribbon = pow(w1, 3.0) * pow(w2, 1.4);
                float band = ribbon * smoothstep(0.05, 0.35, el) * (1.0 - smoothstep(0.75, 1.0, el));
                half3 aur = lerp(_AurA.rgb, _AurB.rgb, saturate(el * 1.3 + w2 * 0.4)); aur = lerp(aur * half3(0.55, 0.35, 1.2), aur * half3(0.9, 1.25, 1.1), _LFPitch);
                half3 c = grad + aur * band * (0.5 + _LFIntensity * 2.4 + _LFPulse * 0.6);
                float3 q = d * 110.0; float3 fc = floor(q); float s = h21(fc.xz + fc.y * 7.13);
                float3 f3 = frac(q) - 0.5; float dotm = smoothstep(0.3, 0.05, length(f3));
                float star = step(0.985, s) * dotm * (0.55 + 0.45 * sin(t * 2.5 + s * 50.0)) * smoothstep(0.0, 0.25, el + 0.1);
                c += star * (0.8 + _LFIntensity);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
