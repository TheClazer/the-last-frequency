// THE USER DRAWS THE WORLD. Reveal progress lives in 12 azimuth sectors around the player, one value per layer (0 forms, 1 growth, 2 water + light).
// A fragment is visible only once the "drawing front" has passed it: bottom-to-top (dir +1) or top-to-bottom (dir -1), near-to-far, with a scribbled glowing edge.
#ifndef LF_REVEAL_INCLUDED
#define LF_REVEAL_INCLUDED
float _LFRev0[12]; float _LFRev1[12]; float _LFRev2[12]; float _LFRev3[12]; float4 _LFRevCfg; float4 _LFRevOrigin; float4 _LFHand0; float4 _LFHand1;   // cfg.x = reveal system on, cfg.y = mean layer-2 progress
float LFRevProg(float3 wp, int layer)
{
    float a = atan2(wp.x - _LFRevOrigin.x, wp.z - _LFRevOrigin.z); float f = (a / 6.2831853 + 0.5) * 12.0; float fl = floor(f); int i0 = ((int)fl % 12 + 12) % 12; int i1 = (i0 + 1) % 12; float w = f - fl; w = w * w * (3.0 - 2.0 * w);
    float p0, p1;
    if (layer == 0) { p0 = _LFRev0[i0]; p1 = _LFRev0[i1]; } else if (layer == 1) { p0 = _LFRev1[i0]; p1 = _LFRev1[i1]; } else if (layer == 2) { p0 = _LFRev2[i0]; p1 = _LFRev2[i1]; } else { p0 = _LFRev3[i0]; p1 = _LFRev3[i1]; }
    return lerp(p0, p1, w);
}
// x = visible (0/1), y = glowing edge band
float2 LFReveal(float3 wp, int layer, float dir)
{
    if (_LFRevCfg.x < 0.5) return float2(1.0, 0.0);
    float prog = LFRevProg(wp, layer); float dist = length(wp.xz - _LFRevOrigin.xz); float h = saturate((wp.y + 22.0) / 120.0); float far = saturate(dist / 230.0);
    float key = (dir > 0.0 ? h : 1.0 - h) * 0.72 + far * 0.28;
    if (layer == 3) key = saturate((dist - 6.0) / 62.0);   // stairs and statue grow outward, one step after another
    float n = frac(sin(dot(floor(wp * 2.5), float3(12.9898, 78.233, 37.719))) * 43758.5453) * 0.05;
    float t = prog * 1.12 - 0.04 - n; float vis = step(key, t); float edge = vis * (1.0 - step(key + 0.007, t));
    if (_LFRevCfg.z > 0.5) { float dh = min(distance(wp, _LFHand0.xyz), distance(wp, _LFHand1.xyz)); if (dh < _LFHand0.w) { vis = 0.0; edge = 0.0; } }   // proximity passthrough: real hands and what is near them show through
    return float2(vis, edge);
}
#endif
