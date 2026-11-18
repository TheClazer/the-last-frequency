import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:80])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)
sub('Scripts/LFMeshes.cs', [
('float R = 2.7f, rise = 0.24f, dA = 13f * Mathf.Deg2Rad;', 'float R = 3.7f, rise = 0.22f, dA = 11f * Mathf.Deg2Rad;'),
('new Vector3(0.95f, 0.14f, 0.55f)', 'new Vector3(1.15f, 0.045f, 0.62f)'),
])
sub('Scripts/LFWorld.cs', [
('new Vector3(Mathf.Sin(a) * 2.7f, _stairShown * 0.24f + 0.18f, Mathf.Cos(a) * 2.7f)', 'new Vector3(Mathf.Sin(a) * 3.7f, _stairShown * 0.22f + 0.1f, Mathf.Cos(a) * 3.7f)'),
('float a = _stairShown * 13f * Mathf.Deg2Rad;', 'float a = _stairShown * 11f * Mathf.Deg2Rad;'),
('float wd = R(1.5f, 2.6f);', 'float wd = R(0.9f, 1.7f);'),
('new Vector3(2.8f, h, 2.8f)', 'new Vector3(1.7f, h, 1.7f)'),
('new Vector3(3.0f, h, 3.0f)', 'new Vector3(1.9f, h, 1.9f)'),
('tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.11f), new Keyframe(0.15f, 0.07f), new Keyframe(1, 0f));', 'tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.17f), new Keyframe(0.2f, 0.1f), new Keyframe(1, 0f));'),
('tr.minVertexDistance = 0.03f;', 'tr.minVertexDistance = 0.012f;'),
('new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; tr.emitting = false;', 'new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.35f, 0.4f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; tr.emitting = false;'),
])
sub('Resources/Shaders/LFToon.shader', [
('col += _ColorB.rgb * fr * (1.2 + _LFPulse * 1.5);', 'col += _ColorB.rgb * fr * (0.55 + _LFPulse * 0.9);'),
('col *= (0.55 + _Glow * (0.5 + _LFIntensity * 0.9 + _LFPulse * 0.7)) * (1.0 + _LFVoice * 0.3);', 'col *= (0.38 + _Glow * (0.3 + _LFIntensity * 0.6 + _LFPulse * 0.5)) * (1.0 + _LFVoice * 0.25);'),
('float alpha = saturate(0.30 + fr * 0.7 + up * 0.25);', 'float alpha = saturate(0.22 + fr * 0.55 + up * 0.2);'),
])
print('patched4')
