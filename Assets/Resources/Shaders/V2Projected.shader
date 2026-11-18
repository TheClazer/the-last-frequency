// "Camera projection" painting: the picture captured from the first head pose is projected onto the real room's geometry.
// From that pose it looks like a flat painting; when the room moves, the paint smears over the surfaces and black void
// shows where nothing was painted. _Dissolve chips the paint away in blocks to reveal the real, lit room underneath.
Shader "RoomIsLying/V2/Projected"
{
    Properties
    {
        _Paint ("Paint", 2D) = "black" {}
        _Void ("Void", Color) = (0.015, 0.014, 0.02, 1)
        _Edge ("Burn edge", Color) = (1, 0.55, 0.18, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On ZTest LEqual Cull Off
            Offset -2, -2
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Paint); SAMPLER(sampler_Paint);
            float4x4 _V2ProjVP; float4 _V2ProjPos;
            CBUFFER_START(UnityPerMaterial)
                half4 _Void; half4 _Edge; float _Dissolve;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.wp = TransformObjectToWorld(i.positionOS.xyz);
                o.wn = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(o.wp);
                return o;
            }

            float h31(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                // dissolve: blocky chips, nearer-first, with a burning edge
                float cell = h31(floor(i.wp * 24.0));
                float d = cell * 0.55 + saturate((i.wp.z + 3.0) * 0.07) * 0.45;
                float t = _Dissolve * 1.25 - 0.1;
                clip(d - t);
                float burn = 1.0 - saturate((d - t) / 0.035);

                float4 p = mul(_V2ProjVP, float4(i.wp, 1.0));
                float3 n = normalize(i.wn) * (front ? 1 : -1);
                float facing = dot(n, normalize(_V2ProjPos.xyz - i.wp));
                float2 uv = p.xy / max(p.w, 1e-4) * 0.5 + 0.5;
                bool inside = p.w > 0.01 && uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1 && facing > -0.15;
                half3 paint = SAMPLE_TEXTURE2D_LOD(_Paint, sampler_Paint, uv, 0).rgb;
                half3 col = inside ? paint : _Void.rgb;
                col = lerp(col, _Edge.rgb * 1.6, burn);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
