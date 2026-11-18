// Butterfly: a flat two-wing card that flaps in the vertex shader; wing shape, veins, spots and glow are procedural. Cutout, lit warmly, glows with the song.
Shader "LF/Butterfly"
{
    Properties { _ColA("Wing A", Color) = (1.0, 0.55, 0.1, 1) _ColB("Wing B", Color) = (0.12, 0.05, 0.03, 1) _Phase("Phase", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _ColA, _ColB; float _Phase; CBUFFER_END
            float4 _LFSunCol, _LFFogCol, _LFFog; float _LFIntensity, _LFPulse;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; float3 wp : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a)
            {
                V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = a.pos.xyz; float flap = sin(_Time.y * 17.0 + _Phase) * 0.85; p.y += abs(p.x) * flap * 0.9; p.x *= cos(flap * 0.6);
                float3 wp = TransformObjectToWorld(p); o.wp = wp; o.cs = TransformWorldToHClip(wp); o.uv = a.uv; return o;
            }
            half4 frag(V i) : SV_Target
            {
                float2 p = float2(abs(i.uv.x - 0.5) * 2.0, i.uv.y * 2.0 - 1.0);   // x: 0 body .. 1 wing tip, y: -1 tail .. 1 head
                float up = smoothstep(1.0, 0.1, length((p - float2(0.55, 0.38)) * float2(0.95, 1.1))); float lo = smoothstep(1.0, 0.15, length((p - float2(0.45, -0.42)) * float2(1.15, 1.35)));
                float shape = max(up, lo * 0.9); clip(shape - 0.32);
                float edge = smoothstep(0.32, 0.5, shape); float vein = smoothstep(0.06, 0.0, abs(frac(atan2(p.y - 0.1, p.x) * 4.0) - 0.5) - 0.42) * 0.7;
                float spots = smoothstep(0.1, 0.0, length(frac(p * float2(5, 6)) - 0.5) - 0.12) * smoothstep(0.62, 0.5, shape) * step(0.55, shape);
                half3 c = lerp(_ColB.rgb, _ColA.rgb, saturate(edge * 1.1 - vein * 0.5)); c = lerp(c, half3(1, 1, 0.9), spots * 0.8); c *= (0.8 + _LFSunCol.rgb * 0.6) * (1.0 + _LFIntensity * 0.5);
                c += _ColA.rgb * 0.25 * (_LFIntensity + 0.3);
                float dist = length(_WorldSpaceCameraPos - i.wp); c = lerp(c, _LFFogCol.rgb, saturate(1.0 - exp(-dist * _LFFog.x))); return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
