// Element materials (name kept as LF/Toon for the C# code). One shader, four physically-inspired looks chosen by the global _LFKind:
//   0 ICE    refractive, fresnel reflection of a procedural sky, absorption (thick = deep cyan), frost, fractures, glints
//   1 WATER  flowing normals, refraction, foam at crests, animated vertices
//   2 EARTH  lit rock: strata, cracks, grain, moss on top faces, gold light in the cracks on the beat
//   3 FIRE   animated flame tongues (additive), hot core to red edge
// With _Floor = 1 the same looks become the ground: frozen lake (with frost snowflakes that grow from seeds), water surface, rock, cooling lava.
Shader "LF/Toon"
{
    Properties
    {
        _ColorA("Base", Color) = (0.2, 0.7, 1, 1)
        _ColorB("Top", Color) = (0.85, 0.97, 1, 1)
        _Ink("Unused", Color) = (0,0,0,1)
        _Glow("Glow", Range(0, 3)) = 0.4
        _Dots("Unused", Range(0, 1)) = 0
        _OutW("Unused", Float) = 0
        _Height("Height Range", Float) = 3
        _UseReveal("Stair Reveal", Float) = 0
        _DstBlend("Dst Blend", Float) = 1
        _ZW("ZWrite", Float) = 0
        _Solid("Unused", Range(0, 1)) = 0
        _Floor("Floor", Float) = 0
        _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" }
        Pass
        {
            Name "Main" Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha [_DstBlend] ZWrite [_ZW] Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vm
            #pragma fragment fm
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA, _ColorB, _Ink; half _Glow, _Dots, _OutW, _Height, _UseReveal, _Solid, _Floor;
            CBUFFER_END
            float _LFPulse, _LFIntensity, _LFStair, _LFVoice, _LFKind, _LFQuality;   // _LFQuality: 1 = full (PC, video), 0 = Quest tier
            float4 _LFFrost0, _LFFrost1, _LFFrost2, _LFFrost3, _LFFrostRad;   // xyz = seed position, w = age in seconds (< 0 = off); radii
            struct A { float4 pos : POSITION; float3 nrm : NORMAL; float4 col : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs : SV_POSITION; float3 wn : TEXCOORD0; float3 wp : TEXCOORD1; float h : TEXCOORD2; float oy : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };
            static const float3 SUN = float3(0.4264, 0.8528, 0.3198);

            float h31(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            TEXTURE2D(_LFNoiseTex); SAMPLER(sampler_LFNoiseTex);
            // 3D value noise from ONE bilinear fetch of a 256x256 texture whose G channel is R shifted by (37,17): about ten times cheaper than hashing on a mobile GPU
            float vn(float3 x) { float3 p = floor(x); float3 f = frac(x); f = f * f * (3.0 - 2.0 * f); float2 uv = (p.xy + float2(37.0, 17.0) * p.z) + f.xy;
                float2 rg = SAMPLE_TEXTURE2D_LOD(_LFNoiseTex, sampler_LFNoiseTex, (uv + 0.5) / 256.0, 0).yx; return lerp(rg.x, rg.y, f.z); }
            float3 bump(float3 p, float e) { float b0 = vn(p); return float3(vn(p + float3(e, 0, 0)), vn(p + float3(0, e, 0)), vn(p + float3(0, 0, e))) - b0; }

            // a procedural sky to reflect and refract: zenith/horizon gradient, an aurora band, a sun and two soft lights
            float3 env(float3 r, float3 zen, float3 hor, float3 a1, float3 a2)
            {
                float y = r.y;
                float3 sky = lerp(hor, zen, saturate(y * 1.1));
                float3 grd = zen * 0.35 + hor * 0.22 * saturate(1.0 + y * 3.0);
                float3 e = y > 0.0 ? sky : lerp(sky, grd, saturate(-y * 4.0));
                float a = pow(saturate(1.0 - abs(y - 0.38) * 3.2), 2.0) * (0.55 + 0.45 * sin(r.x * 5.0 + _Time.y * 0.15));
                e += a1 * a * 0.55 + a2 * a * 0.35 * (0.5 + 0.5 * sin(r.z * 4.0 - _Time.y * 0.1));
                e += float3(1.0, 0.97, 0.92) * pow(saturate(dot(r, SUN)), 90.0) * 7.0;
                e += hor * pow(saturate(dot(r, normalize(float3(-0.6, 0.45, -0.5)))), 26.0) * 2.0;
                e += float3(0.9, 0.95, 1.0) * pow(saturate(dot(r, normalize(float3(0.1, 0.2, -0.95)))), 40.0) * 1.4;
                return e;
            }

            // six-fold dendritic snowflake (unit radius): main arms plus five tiers of side branches
            float flakeGlyph(float2 p, float w)
            {
                float r = length(p); float seg = 1.0471976; float th = atan2(p.y, p.x);
                float a = abs(fmod(abs(th), seg) - seg * 0.5); float b = seg * 0.5 - a;
                float2 q = r * float2(cos(b), sin(b));
                float g = smoothstep(w, 0.0, abs(q.y)) * step(q.x, 0.97);
                g += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - 0.18) * 1.73) * 0.5) * step(0.18, q.x) * step(q.x, 0.34);
                g += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - 0.34) * 1.73) * 0.5) * step(0.34, q.x) * step(q.x, 0.54);
                g += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - 0.50) * 1.73) * 0.5) * step(0.50, q.x) * step(q.x, 0.68);
                g += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - 0.66) * 1.73) * 0.5) * step(0.66, q.x) * step(q.x, 0.79);
                g += smoothstep(w, 0.0, abs(abs(q.y) - (q.x - 0.82) * 1.73) * 0.5) * step(0.82, q.x) * step(q.x, 0.90);
                return saturate(g);
            }
            // frost that grows outward from a seed as a snowflake; returns the frost amount, and the bright growth front
            float frostSeed(float4 s, float rad, float2 xz, inout float front)
            {
                if (s.w < 0.0 || rad < 0.01) return 0.0;
                float2 p = (xz - s.xz) / rad; float r = length(p); if (r > 1.0) return 0.0;
                float g = saturate(s.w / 3.8); g = 1.0 - (1.0 - g) * (1.0 - g);
                float reveal = smoothstep(g, g - 0.1, r) * step(0.001, g);
                float w = 0.0032 + 0.010 / rad;
                float lines = flakeGlyph(p, w) * 0.85; [branch] if (_LFQuality > 0.5) lines += flakeGlyph(p, w * 3.5) * 0.22;
                front += smoothstep(0.08, 0.0, abs(r - g)) * (1.0 - g);
                return saturate(lines) * reveal;
            }

            // ------------------------------------------------------------------ ICE
            half4 iceFrag(V i, float fl)
            {
                float3 n0 = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp);
                bool cheapFloor = fl > 0.5 && _LFQuality < 0.5;   // Quest tier on the frozen floor (the largest surface on screen): fewer noise fetches
                float3 n = n0; [branch] if (!cheapFloor) n = normalize(n0 + bump(i.wp * 6.0, 0.07) * (fl > 0.5 ? 0.9 : 1.7));   // [branch]: HLSL ?: evaluates both sides, so tiers need real branches   // micro-relief so reflections break up like real ice
                float ndv = saturate(dot(n, v)); float F = saturate((0.03 + 0.97 * pow(1.0 - ndv, 5.0)) * 1.6);
                float up = saturate(i.oy * 0.75 + i.h * 0.5);
                float3 zen = float3(0.06, 0.16, 0.46), hor = float3(0.62, 0.82, 1.0), a1 = float3(0.15, 0.95, 0.85), a2 = float3(0.55, 0.30, 1.0);
                float3 refl = env(reflect(-v, n), zen, hor, a1, a2);
                float3 rd = refract(-v, n, 0.763); if (dot(rd, rd) < 0.01) rd = reflect(-v, n);
                float thick = (0.35 + (1.0 - ndv) * 1.6) * (0.55 + 0.9 * vn(i.wp * 2.2));
                float3 absorb = exp(-float3(1.25, 0.36, 0.14) * thick * 1.6);
                float3 trans = lerp(zen, hor, saturate(rd.y * 0.5 + 0.5)); [branch] if (_LFQuality > 0.5) trans = env(rd, zen, hor, a1, a2); trans *= absorb;
                float3 deep = _ColorA.rgb * 0.55; float3 shallow = _ColorB.rgb;
                float3 body = lerp(deep, shallow, saturate(up * 0.8 + (1.0 - thick) * 0.25));
                float3 col = trans * 0.55 + body * 0.38 * (0.55 + 0.45 * ndv);
                float vein = pow(1.0 - abs(vn(i.wp * 3.0 + 7.1) * 2.0 - 1.0), 7.0);
                col += float3(0.8, 0.95, 1.0) * vein * 0.28 * (0.5 + 0.5 * ndv);     // internal fracture sheets
                float cell = h31(floor(i.wp * 38.0)); col += float3(0.9, 1.0, 1.0) * step(0.985, cell) * 0.35 * (1.0 - F);   // trapped air bubbles
                float sss = pow(1.0 - ndv, 1.6) + up * 0.35;
                col += _ColorB.rgb * sss * (0.16 + _LFIntensity * 0.35 + _LFPulse * 0.45) * (1.0 + _LFVoice * 0.4);   // light from inside, driven by the music
                col += refl * F * 1.15;
                float3 frostCol = float3(0.90, 0.96, 1.0) * (0.62 + 0.38 * saturate(dot(n0, SUN) * 0.5 + 0.5));
                float alpha;
                if (fl > 0.5)
                {   // frozen lake: dark clear ice, thin white cracks, a dusting of snow, and snowflake frost growing from each seed
                    col *= 0.62;
                    float crack = pow(1.0 - abs(vn(i.wp * 1.1 + 3.0) * 2.0 - 1.0), 14.0); float dustN = vn(i.wp * 0.45);
                    [branch] if (!cheapFloor) { crack += 0.5 * pow(1.0 - abs(vn(i.wp * 2.7) * 2.0 - 1.0), 18.0); dustN = dustN * 0.6 + vn(i.wp * 2.1) * 0.4; }
                    float dust = smoothstep(0.55, 0.9, dustN) * 0.2;
                    float front = 0.0;
                    float s = frostSeed(_LFFrost0, _LFFrostRad.x, i.wp.xz, front); s = max(s, frostSeed(_LFFrost1, _LFFrostRad.y, i.wp.xz, front));
                    s = max(s, frostSeed(_LFFrost2, _LFFrostRad.z, i.wp.xz, front)); s = max(s, frostSeed(_LFFrost3, _LFFrostRad.w, i.wp.xz, front));
                    col = lerp(col, frostCol, saturate(dust + crack * 0.5 + s * 0.72));
                    col += float3(0.55, 0.92, 1.0) * saturate(front) * 0.55 + float3(0.7, 0.93, 1.0) * s * (0.05 + _LFPulse * 0.3);
                    alpha = saturate(0.74 + F * 0.4 + s);
                }
                else
                {
                    float fr = smoothstep(0.58, 0.78, vn(i.wp * 3.4 + 2.0)) * saturate(1.0 - up * 1.5);
                    col = lerp(col, frostCol, fr * 0.6);
                    alpha = saturate(0.5 + F * 0.9 + (1.0 - ndv) * 0.3 + fr * 0.35);
                }
                float3 gp = floor(i.wp * 170.0); float sp = step(0.9988, h31(gp + floor(_Time.y * 4.0))); col += sp * (2.0 + F * 4.0);
                col *= 1.0 + _LFPulse * 0.12;
                return half4(col, alpha);
            }

            // ------------------------------------------------------------------ WATER
            half4 waterFrag(V i, float fl)
            {
                float3 n0 = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp); float t = _Time.y;
                float3 p1 = fl > 0.5 ? i.wp * float3(1.6, 1.0, 1.6) + float3(t * 0.25, t * 0.6, t * 0.2) : i.wp * float3(5.0, 2.2, 5.0) - float3(0, t * 1.6, 0);
                float3 p2 = fl > 0.5 ? i.wp * float3(4.5, 1.0, 4.5) - float3(t * 0.4, t * 0.9, -t * 0.3) : i.wp * float3(11.0, 5.0, 11.0) - float3(0, t * 2.6, 0);
                float3 n = n0 + bump(p1, 0.08) * 1.3; [branch] if (!(fl > 0.5 && _LFQuality < 0.5)) n += bump(p2, 0.08) * 0.55;   // Quest tier floor: one ripple octave
                if (fl > 0.5) { float rr = length(i.wp.xz); n.xz += normalize(i.wp.xz + 1e-4) * sin(rr * 5.0 - t * 4.0) * (0.04 + _LFPulse * 0.22) * saturate(1.2 - rr * 0.07); }
                n = normalize(n);
                float ndv = saturate(dot(n, v)); float F = saturate(0.02 + 0.98 * pow(1.0 - ndv, 5.0));
                float3 zen = float3(0.01, 0.10, 0.30), hor = float3(0.35, 0.85, 0.90), a1 = float3(0.1, 0.9, 0.8), a2 = float3(0.1, 0.4, 1.0);
                float3 refl = env(reflect(-v, n), zen, hor, a1, a2);
                float3 rd = refract(-v, n, 0.75); if (dot(rd, rd) < 0.01) rd = reflect(-v, n);
                float thick = 0.4 + (1.0 - ndv) * 1.5;
                float3 trans = lerp(zen, hor, saturate(rd.y * 0.5 + 0.5)); [branch] if (_LFQuality > 0.5) trans = env(rd, zen, hor, a1, a2); trans *= exp(-float3(0.95, 0.24, 0.12) * thick * 1.3);
                float up = i.oy;
                float3 body = lerp(_ColorA.rgb * 0.45, _ColorB.rgb, saturate(up * 0.6 + F));
                float3 col = trans * 0.62 + body * 0.26 + refl * F * 1.35;
                float foamN = vn(p1 * 1.7 + 5.0);
                float foam = fl > 0.5 ? smoothstep(0.78, 0.95, foamN) * 0.35 : smoothstep(0.62, 0.9, foamN) * (smoothstep(0.8, 1.0, up) + smoothstep(0.14, 0.0, up) * 0.8);
                col = lerp(col, float3(0.92, 0.98, 1.0), saturate(foam) * 0.75);
                col += _ColorB.rgb * pow(1.0 - ndv, 2.0) * (0.10 + _LFIntensity * 0.3 + _LFPulse * 0.4);
                float3 gp = floor(i.wp * 120.0 + float3(0, -t * 30.0, 0)); col += step(0.9992, h31(gp)) * 1.6;
                float alpha = fl > 0.5 ? saturate(0.78 + F * 0.3) : saturate(0.42 + F * 0.9 + foam * 0.4);
                return half4(col, alpha);
            }

            // ------------------------------------------------------------------ EARTH (and cooling lava when lava = 1)
            half4 rockFrag(V i, float lava, float fl)
            {
                float3 n0 = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp); if (dot(n0, v) < 0.0) n0 = -n0;
                float warp = vn(i.wp * 0.9) * 2.0;
                float strata = fl > 0.5 ? vn(i.wp * 0.6 + 4.0) : smoothstep(0.2, 0.8, sin(i.wp.y * 5.5 + warp * 2.2) * 0.5 + 0.5);
                bool cheapFloor = fl > 0.5 && _LFQuality < 0.5;   // Quest tier on the rock / lava floor: 6 fewer noise fetches
                float grain = vn(i.wp * 9.0); [branch] if (!cheapFloor) grain = grain * 0.6 + vn(i.wp * 23.0) * 0.4;
                float crack = pow(1.0 - abs(vn(i.wp * (fl > 0.5 ? 1.5 : 2.6) + 5.0) * 2.0 - 1.0), 22.0) + (fl > 0.5 ? 0.6 * pow(1.0 - abs(vn(i.wp * 3.7 + 1.0) * 2.0 - 1.0), 26.0) : 0.0);
                float3 nb = bump(i.wp * 2.5, 0.1) * 1.3; [branch] if (!cheapFloor) nb += bump(i.wp * 9.0, 0.05) * 2.4; float3 n = normalize(n0 + nb);
                float3 alb = lerp(_ColorA.rgb * 0.55, _ColorB.rgb * 0.8, saturate(strata * 0.6 + grain * 0.4)); alb *= 1.0 - saturate(crack) * 0.55;
                float moss = (1.0 - lava) * smoothstep(0.62, 0.92, n0.y) * smoothstep(0.5, 0.75, vn(i.wp * 1.7 + 9.0)) * (fl > 0.5 ? 0.6 : 1.0);
                alb = lerp(alb, float3(0.20, 0.36, 0.11) * (0.7 + grain * 0.6), moss * 0.8);
                if (lava > 0.5) alb = lerp(float3(0.045, 0.035, 0.035), float3(0.16, 0.10, 0.08), grain) * (1.0 - saturate(crack) * 0.4);
                float ndl = saturate(dot(n, SUN));
                float3 light = float3(1.0, 0.86, 0.66) * ndl * 1.25 + lerp(float3(0.16, 0.13, 0.10), float3(0.36, 0.42, 0.50), n.y * 0.5 + 0.5) * 0.7;
                if (lava > 0.5) light = float3(1.0, 0.55, 0.25) * (0.35 + ndl * 0.5);
                float ao = fl > 0.5 ? 0.9 : 0.55 + 0.45 * saturate(i.oy * 1.6 + 0.15);
                float3 col = alb * light * ao;
                col += float3(1.0, 0.9, 0.75) * pow(saturate(dot(reflect(-v, n), SUN)), 24.0) * 0.12 * (1.0 - moss);
                float3 glow = lava > 0.5 ? float3(1.0, 0.33, 0.04) * (0.5 + _LFPulse * 1.0 + _LFIntensity * 0.3) : float3(1.0, 0.7, 0.25) * (0.03 + _LFPulse * 0.7 + _LFIntensity * 0.12);
                col += glow * saturate(crack) * (1.0 - moss);
                return half4(col, 1.0);
            }

            // ------------------------------------------------------------------ FIRE
            half4 fireFrag(V i)
            {
                float3 n0 = normalize(i.wn); float3 v = normalize(GetCameraPositionWS() - i.wp); float ndv = abs(dot(n0, v)); float t = _Time.y;
                float3 p = i.wp * float3(2.6, 1.5, 2.6) - float3(0, t * 2.4, 0);
                float tur = vn(p) * 0.65 + vn(p * 2.3 + 4.0) * 0.35; float up = i.oy;
                float shape = saturate(ndv * 1.1 - 0.25 - up * 0.45 + (tur - 0.5) * 1.1);
                float heat = shape * (1.0 - up * 0.55);
                float3 c = lerp(float3(0.9, 0.08, 0.0), float3(1.0, 0.45, 0.05), smoothstep(0.1, 0.45, heat));
                c = lerp(c, float3(1.0, 0.85, 0.35), smoothstep(0.4, 0.75, heat)); c = lerp(c, float3(1.0, 1.0, 0.9), smoothstep(0.75, 1.0, heat));
                float a = smoothstep(0.05, 0.5, shape) * (1.0 - smoothstep(0.75, 1.0, up) * 0.7);
                return half4(c * (0.8 + _LFPulse * 0.5 + _LFIntensity * 0.3), a * 0.85);
            }

            V vm(A a)
            {
                V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = a.pos.xyz; if (_UseReveal > 0.5 && a.col.a > _LFStair) p = float3(0, -9999, 0);
                if (_Floor < 0.5 && _UseReveal < 0.5 && _LFKind > 0.5)
                {
                    float t = _Time.y; float seed = dot(float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23), float3(1.3, 0.7, 2.1)); float k = saturate(p.y);
                    if (_LFKind < 1.5) { p.xz += a.nrm.xz * sin(t * 2.4 + p.y * 9.0 + seed) * 0.05 * (0.3 + k); p.x += sin(t * 1.3 + p.y * 3.0 + seed * 2.0) * 0.04 * k; }   // water: the column breathes and sways
                    else if (_LFKind > 2.5) { float k2 = k * k; p.x += (vn(float3(p.y * 3.0 - t * 2.6, seed, t * 0.5)) - 0.5) * 0.5 * k2; p.z += (vn(float3(seed, p.y * 3.0 - t * 2.3, t * 0.4)) - 0.5) * 0.5 * k2; p.y *= 1.0 + 0.12 * sin(t * 5.0 + seed * 3.0); }   // fire: the tongue licks
                }
                float3 wp = TransformObjectToWorld(p); o.wp = wp; o.wn = TransformObjectToWorldNormal(a.nrm); o.cs = TransformWorldToHClip(wp); o.h = saturate(a.pos.y / _Height); o.oy = saturate(a.pos.y);
                return o;
            }
            half4 fm(V i) : SV_Target
            {
                if (_Floor > 0.5)
                {
                    if (_LFKind < 0.5) return iceFrag(i, 1.0);
                    if (_LFKind < 1.5) return waterFrag(i, 1.0);
                    return rockFrag(i, _LFKind > 2.5 ? 1.0 : 0.0, 1.0);
                }
                if (_LFKind < 0.5) return iceFrag(i, 0.0);
                if (_LFKind < 1.5) return waterFrag(i, 0.0);
                if (_LFKind < 2.5) return rockFrag(i, 0.0, 0.0);
                if (_UseReveal > 0.5) return rockFrag(i, 1.0, 0.0);
                return fireFrag(i);
            }
            ENDHLSL
        }
    }
}
