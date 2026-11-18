// Procedural six-fold snowflake glyph with soft halo (additive billboard). Used for blossoms, drifting flakes and the pop bloom.
Shader "LF/Flake"
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
            float _LFKind;   // 0 snowflake, 1 water ripple, 2 earth facet, 3 fire ember
            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A a) { V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.cs = TransformObjectToHClip(a.pos.xyz); o.col = a.col; o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0; float r = length(p);
                float seg = 1.0471976; float th = atan2(p.y, p.x);
                float a = abs(fmod(abs(th), seg) - seg * 0.5); float b = seg * 0.5 - a;
                float2 q = r * float2(cos(b), sin(b));
                float w = 0.045;
                float arm = smoothstep(w, 0.0, abs(q.y)) * step(0.0, q.x) * step(q.x, 0.9);
                float br = 0.0;
                float t1 = 0.34, t2 = 0.58, t3 = 0.78;
                br += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - t1) * 1.73) * 0.5) * step(t1, q.x) * step(q.x, t1 + 0.2);
                br += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - t2) * 1.73) * 0.5) * step(t2, q.x) * step(q.x, t2 + 0.16);
                br += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - t3) * 1.73) * 0.5) * step(t3, q.x) * step(q.x, t3 + 0.1);
                float ring = smoothstep(0.035, 0.0, abs(r - 0.13));
                float glyph = saturate(arm + br + ring);
                float halo = pow(saturate(1.0 - r), 2.2) * 0.45;
                float core = smoothstep(0.1, 0.0, r);
                if (_LFKind > 0.5)
                {
                    if (_LFKind < 1.5)
                    {   // water: soft drop with two travelling ripple rings
                        glyph = smoothstep(0.05, 0.0, abs(r - 0.38)) * 0.9 + smoothstep(0.04, 0.0, abs(r - 0.72)) * 0.6 + smoothstep(0.5, 0.0, r) * 0.35;
                        halo = pow(saturate(1.0 - r), 1.6) * 0.3; core = smoothstep(0.16, 0.0, r) * 0.9;
                    }
                    else if (_LFKind < 2.5)
                    {   // earth: faceted hexagon with inner cross-lines
                        float2 ap = abs(p); float hx = max(ap.x * 0.866 + ap.y * 0.5, ap.y);
                        float l1 = smoothstep(0.04, 0.0, abs(p.y)) * step(r, 0.72); float l2 = smoothstep(0.04, 0.0, abs(p.y * 0.866 - p.x * 0.5)) * step(r, 0.72); float l3 = smoothstep(0.04, 0.0, abs(p.y * 0.866 + p.x * 0.5)) * step(r, 0.72);
                        glyph = saturate(smoothstep(0.06, 0.0, abs(hx - 0.72)) + (l1 + l2 + l3) * 0.55 + step(hx, 0.72) * 0.22);
                        halo = pow(saturate(1.0 - r), 3.0) * 0.25; core = smoothstep(0.12, 0.0, r) * 0.7;
                    }
                    else
                    {   // fire: four-ray ember spark with a hot core
                        float rays = smoothstep(0.06, 0.0, min(abs(p.x), abs(p.y))) * saturate(1.0 - r * 1.1);
                        float diag = smoothstep(0.05, 0.0, min(abs(p.x - p.y), abs(p.x + p.y)) * 0.7) * saturate(0.8 - r) * 0.6;
                        glyph = saturate(rays + diag); halo = pow(saturate(1.0 - r), 1.8) * 0.6; core = smoothstep(0.2, 0.0, r) * 1.2;
                    }
                }
                half3 c = (i.col.rgb * _Tint.rgb) * (glyph * 1.3 + halo + core * 0.8) * i.col.a * step(r, 1.0) * (_LFKind < 0.5 ? 0.5 : 1.0);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
