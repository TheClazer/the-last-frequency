// Turns a captured frame of the lit room into a hand-made drawing: graphite lines that draw themselves in, hatched tone,
// then a painterly (Kuwahara) colour wash. Used once at the start: pass 0 = painterly smoothing, pass 1 = per-frame compose.
Shader "Hidden/RoomIsLying/V2Painter"
{
    Properties { _MainTex ("Tex", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex; float4 _MainTex_TexelSize;
    float _Draw, _Tone, _Color, _Sat, _Warm, _Spread;
    float4 _Paper;

    float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
    float vnoise(float2 p)
    {
        float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
        return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
    }
    float luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }
    float L(float2 u) { return luma(pow(saturate(tex2D(_MainTex, u).rgb * 2.6), 0.78)); }

    // ---- pass 0: Kuwahara, four quadrants, pick the calmest
    float4 fragKuw(v2f_img i) : SV_Target
    {
        float2 px = _MainTex_TexelSize.xy * 1.6;
        float3 m[4]; float3 s[4];
        for (int q = 0; q < 4; q++) { m[q] = 0; s[q] = 0; }
        for (int y = 0; y <= 4; y++)
            for (int x = 0; x <= 4; x++)
            {
                float2 o = float2(x, y);
                float3 c0 = tex2D(_MainTex, i.uv + float2(-o.x, -o.y) * px).rgb; m[0] += c0; s[0] += c0 * c0;
                float3 c1 = tex2D(_MainTex, i.uv + float2( o.x, -o.y) * px).rgb; m[1] += c1; s[1] += c1 * c1;
                float3 c2 = tex2D(_MainTex, i.uv + float2(-o.x,  o.y) * px).rgb; m[2] += c2; s[2] += c2 * c2;
                float3 c3 = tex2D(_MainTex, i.uv + float2( o.x,  o.y) * px).rgb; m[3] += c3; s[3] += c3 * c3;
            }
        float best = 1e9; float3 res = 0;
        for (int k = 0; k < 4; k++)
        {
            float3 mean = m[k] / 25.0; float3 var3 = abs(s[k] / 25.0 - mean * mean);
            float v = var3.r + var3.g + var3.b;
            if (v < best) { best = v; res = mean; }
        }
        return float4(res, 1);
    }

    // ---- pass 1: compose the drawing from the smoothed frame
    float4 fragCompose(v2f_img i) : SV_Target
    {
        float2 uv = i.uv; float2 px = _MainTex_TexelSize.xy * 1.4;
        float3 sm = tex2D(_MainTex, uv).rgb; sm = pow(saturate(sm * 2.6), 0.78);
        float l00 = L(uv + float2(-px.x, -px.y)), l10 = L(uv + float2(0, -px.y)), l20 = L(uv + float2(px.x, -px.y));
        float l01 = L(uv + float2(-px.x, 0)), l21 = L(uv + float2(px.x, 0));
        float l02 = L(uv + float2(-px.x, px.y)), l12 = L(uv + float2(0, px.y)), l22 = L(uv + float2(px.x, px.y));
        float gx = (l20 + 2 * l21 + l22) - (l00 + 2 * l01 + l02);
        float gy = (l02 + 2 * l12 + l22) - (l00 + 2 * l10 + l20);
        float edge = saturate(sqrt(gx * gx + gy * gy) * 7.5 - 0.07);
        edge = max(edge, saturate(vnoise(uv * 700) - 0.93) * edge * 6);   // pencil tooth

        float grain = vnoise(uv * float2(1100, 620)) * 0.07 + vnoise(uv * 70) * 0.035;
        float3 paper = _Paper.rgb - grain;

        // wavefronts: lines sweep in from the left like a hand drawing; tone and colour bloom out from the centre
        float wf = uv.x * 0.85 + uv.y * 0.15 + (vnoise(uv * 9) - 0.5) * 0.30;
        float lineVis = smoothstep(wf - 0.05, wf, _Draw * 1.3);
        float rad = distance(uv, float2(0.5, 0.55)) * 1.1 + (vnoise(uv * 14) - 0.5) * 0.25;
        float toneVis = smoothstep(rad - 0.12, rad, _Tone * 1.35);
        float colVis = smoothstep(rad - 0.18, rad, _Color * 1.45);

        float dark = saturate(1.0 - luma(sm) * 1.35);
        float h1 = abs(frac((uv.x + uv.y) * 170 + vnoise(uv * 30) * 0.6) - 0.5) * 2;
        float h2 = abs(frac((uv.x - uv.y) * 170 + vnoise(uv * 25) * 0.6) - 0.5) * 2;
        float hatch = saturate(step(h1, dark * 1.15) * 0.6 + step(h2, dark - 0.4) * 0.5);

        float3 col = paper;
        col = lerp(col, float3(0.20, 0.19, 0.21), hatch * toneVis * 0.55);
        // colour: boost saturation, warm the whole thing, keep a hint of the hatching so it still reads as a drawing
        float3 c = sm; float g = luma(c);
        c = lerp(float3(g, g, g), c, _Sat);
        c *= lerp(float3(1, 1, 1), float3(1.12, 1.0, 0.82), _Warm);
        c = saturate(c * 1.25 + 0.04);
        c *= 1.0 - 0.25 * hatch;
        c = lerp(c, c * c * 1.6, 0.25);
        col = lerp(col, c, colVis * _Color);
        float ink = edge * lineVis;
        col = lerp(col, float3(0.06, 0.055, 0.07), ink * lerp(0.9, 0.55, colVis * _Color));
        return float4(col, 1);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragKuw
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragCompose
            ENDCG }
    }
}
