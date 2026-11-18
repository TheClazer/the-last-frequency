import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets\Resources\Shaders')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)
EDGE = "half3(1.0, 0.82, 0.45) * 5.0"
sub('LFLit.shader', [
('            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            TEXTURE2D(_Albedo);', '            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            #include "LFReveal.hlsl"\n            TEXTURE2D(_Albedo);'),
('float _UseTex, _NStr, _Sway, _Gloss, _Sat; CBUFFER_END', 'float _UseTex, _NStr, _Sway, _Gloss, _Sat, _Layer, _Dir; CBUFFER_END'),
('        _Sat("Saturation", Float) = 1.15\n', '        _Sat("Saturation", Float) = 1.15\n        _Layer("Reveal layer", Float) = 0\n        _Dir("Reveal direction", Float) = 1\n'),
('                float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 n = normalize(i.wn);', '                float2 rv = LFReveal(i.wp, (int)_Layer, _Dir); clip(rv.x - 0.5);\n                float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 n = normalize(i.wn);'),
('                col = col * _LFFog.w; col = col / (1.0 + col * 0.18);\n                return half4(col, 1);', '                col = col * _LFFog.w; col = col / (1.0 + col * 0.18); col += ' + EDGE + ' * rv.y;\n                return half4(col, 1);'),
])
sub('LFWater.shader', [
('            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            CBUFFER_START', '            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            #include "LFReveal.hlsl"\n            CBUFFER_START'),
('                float t = _Time.y; float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 L = normalize(_LFSunDir.xyz); float u = i.uv.x', '                float2 rv = LFReveal(i.wp, 2, -1.0); clip(rv.x - 0.5);\n                float t = _Time.y; float3 V3 = normalize(_WorldSpaceCameraPos - i.wp); float3 L = normalize(_LFSunDir.xyz); float u = i.uv.x'),
('                return half4(col, alpha);', '                col += ' + EDGE + ' * rv.y * 0.5; return half4(col, alpha);'),
])
sub('LFSkyDay.shader', [
('            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            float4 _LFSunDir', '            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            #include "LFReveal.hlsl"\n            float4 _LFSunDir'),
('                c *= 1.0 + _LFIntensity * 0.25;\n                return half4(c, 1);', '''                c *= 1.0 + _LFIntensity * 0.25;
                if (_LFRevCfg.x > 0.5)
                {   // the sky is drawn top-to-bottom as the user builds; void with stars until then
                    float3 wp = _LFRevOrigin.xyz + d * 400.0; float prog = LFRevProg(wp, 2); float key = 1.0 - saturate(d.y * 0.85 + 0.15); float v = smoothstep(key, key + 0.12, prog * 1.15);
                    float2 sc = floor(d.xz / (abs(d.y) + 0.3) * 160.0); float st = step(0.992, h21(sc)) * (0.5 + 0.5 * sin(_Time.y * 2.0 + h21(sc + 3.0) * 30.0)) * smoothstep(-0.05, 0.2, d.y);
                    half3 voidc = half3(0.004, 0.006, 0.02) + st * half3(0.9, 0.95, 1.0); c = lerp(voidc, c, v);
                }
                return half4(c, 1);'''),
])
sub('LFShaft.shader', [
('            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            CBUFFER_START', '            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"\n            #include "LFReveal.hlsl"\n            CBUFFER_START'),
('float a = side * len * st * sh * _Amount * (0.12 + _LFIntensity * 0.55 + _LFPulse * 0.1);', 'float rp = _LFRevCfg.x > 0.5 ? _LFRevCfg.y : 1.0; float a = side * len * st * sh * _Amount * (0.12 + _LFIntensity * 0.55 + _LFPulse * 0.1) * smoothstep(0.1, 0.9, rp);'),
])
print('reveal shaders patched')
