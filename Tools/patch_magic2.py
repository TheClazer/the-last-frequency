import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:80])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# sky: deep indigo, luminous aurora only
sub('Resources/Shaders/LFSky.shader', [
('half3 grad = lerp(half3(0.07, 0.16, 0.42), _Void.rgb, saturate(pow(el, 0.55)));', 'half3 grad = lerp(half3(0.025, 0.06, 0.2), _Void.rgb, saturate(pow(el, 0.5)));'),
('grad += half3(0.12, 0.3, 0.6) * hor * (0.5 + _LFIntensity * 0.8);', 'grad += half3(0.05, 0.16, 0.38) * hor * (0.4 + _LFIntensity * 0.9);'),
('half3 c = grad + aur * band * (0.35 + _LFIntensity * 2.0 + _LFPulse * 0.5);', 'half3 c = grad + aur * band * (0.5 + _LFIntensity * 2.4 + _LFPulse * 0.6);'),
])
# crystals: deeper body, glassy tips, fine glints (no big squares)
sub('Resources/Shaders/LFToon.shader', [
('half3 body = lerp(_ColorA.rgb * 0.30, _ColorA.rgb, saturate(0.25 + ndv * 0.35 + up * 0.55));', 'half3 body = lerp(_ColorA.rgb * 0.10, _ColorA.rgb * 0.75, saturate(0.1 + ndv * 0.3 + up * 0.6));'),
('body *= 0.78 + 0.45 * facet;', 'body *= 0.65 + 0.7 * facet;'),
('body = lerp(body, _ColorB.rgb, up * up * 0.75);', 'body = lerp(body, _ColorB.rgb * 0.9, up * up * up * 0.8);'),
('float3 gp = floor(i.wp * 26.0);', 'float3 gp = floor(i.wp * 120.0);'),
('float spark = step(0.988, h31(gp + tw)) * (0.4 + fr * 2.0);', 'float spark = step(0.9975, h31(gp + tw)) * (0.5 + fr * 3.0);'),
('half3 col = body + _ColorB.rgb * fr * (0.9 + _LFPulse * 1.4)', 'half3 col = body + _ColorB.rgb * fr * (1.5 + _LFPulse * 1.6)'),
])
# panel: more opaque glass for legibility
sub('Resources/Shaders/LFPanel.shader', [
('half3 glass = lerp(half3(0.03, 0.07, 0.2), _ColorA.rgb * 0.55, saturate(0.35 + i.uv.y * 0.5));', 'half3 glass = lerp(half3(0.02, 0.04, 0.14), _ColorA.rgb * 0.38, saturate(0.2 + i.uv.y * 0.5));'),
('return half4(c, saturate(_Alpha * (0.78 + edge * 0.4) + _Hot * 0.1));', 'return half4(c, saturate(_Alpha * (0.93 + edge * 0.2) + _Hot * 0.05));'),
])
# hint/big: snap into place on first show
sub('Scripts/LFUI.cs', [
('_hintRoot.gameObject.SetActive(true); SetT(_hint, s); SetT(_hintS, s); Place(_hintRoot, 2.0f, 0.28f);', 'bool snap = !_hintRoot.gameObject.activeSelf; _hintRoot.gameObject.SetActive(true); SetT(_hint, s); SetT(_hintS, s); if (snap) _hintRoot.position = head.position + Vector3.up * 0.28f; Place(_hintRoot, 2.0f, 0.28f); if (snap) { _hintRoot.position = head.position + Vector3.forward * 2.0f + Vector3.up * 0.28f; Place(_hintRoot, 2.0f, 0.28f); }'),
('_bigRoot.gameObject.SetActive(true); SetT(_bigT, s); SetT(_bigTs, s); _bigLife = life; _bigT0 = 0; Place(_bigRoot, 2.6f, 0.55f);', '_bigRoot.gameObject.SetActive(true); SetT(_bigT, s); SetT(_bigTs, s); _bigLife = life; _bigT0 = 0; _bigRoot.position = head.position + Vector3.forward * 2.6f + Vector3.up * 0.55f; Place(_bigRoot, 2.6f, 0.55f);'),
('void Place(Transform t, float dist, float dy)\n        {', 'void Place(Transform t, float dist, float dy)\n        {\n            if (t == _lyricRoot && t.position == Vector3.zero) t.position = head.position + Vector3.forward * dist;'),
])
# keep structures out of the singer's line of sight
sub('Scripts/LFWorld.cs', [
('float rad = R(3.2f, 7.5f), h = R(1.6f, 4.2f) * k; var to', 'float rad = R(4.6f, 8f), h = R(1.6f, 4.2f) * k; az = AvoidFront(az); var to'),
('float rad = R(2.4f, 8f), y = R(1.2f, 6.5f); var to', 'float rad = R(3.4f, 8f), y = R(1.2f, 6.5f); if (rad < 6.5f) az = AvoidFront(az); var to'),
('float R() { return (float)_rnd.NextDouble(); }', 'float R() { return (float)_rnd.NextDouble(); }\n        float AvoidFront(float az) { float a = Mathf.Repeat(az + Mathf.PI, 6.2832f) - Mathf.PI; if (Mathf.Abs(a) < 0.55f) a = (a >= 0 ? 0.55f : -0.55f) + a * 0.2f; return a; }'),
])
print('patched2')
