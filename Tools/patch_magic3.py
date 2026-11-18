import os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:80])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# ---- irregular ice-burst of thin shards instead of clean prisms
sub('Scripts/LFMeshes.cs', [
('case "crystals": Crystals(b); return b.Build(name);', 'case "crystals": Crystals(b); return b.Build(name);\n                case "fan": Fan(b); return b.Build(name);'),
('static void Crystals(MB b)', '''static void Fan(MB b)
        {
            var rnd = new System.Random(5);
            for (int k = 0; k < 22; k++)
            {
                float ang = (float)rnd.NextDouble() * 6.2832f; float rad = (float)rnd.NextDouble() * 0.14f; var bp = new Vector3(Mathf.Sin(ang) * rad, 0, Mathf.Cos(ang) * rad);
                float tall = k < 4 ? 1f : (k < 10 ? 0.45f + 0.35f * (float)rnd.NextDouble() : 0.12f + 0.3f * (float)rnd.NextDouble());
                float lean = (k < 4 ? 0.06f : 0.15f) + (k < 10 ? 0.55f : 1.3f) * (float)rnd.NextDouble(); float la = (float)rnd.NextDouble() * 6.2832f;
                var dir = (Vector3.up + new Vector3(Mathf.Sin(la), 0, Mathf.Cos(la)) * lean).normalized; Crystal(b, bp, dir, tall, 0.018f + 0.03f * tall * (float)rnd.NextDouble() + 0.012f);
            }
        }
        static void Crystals(MB b)'''),
])
# ---- world: ribbons, column glitter, finer dust, use fan
sub('Scripts/LFWorld.cs', [
('"crystals"', '"fan"'),
('_dust = MakeFlow("HandDust", 900, 0.05f, 1.4f);', '_dust = MakeFlow("HandDust", 2600, 0.045f, 1.5f);'),
('MakeFlow("MotesFeet", 520, 0.1f, 3.6f); _above = MakeFlow("MotesAbove", 460, 0.11f, 5f); _swirl = MakeFlow("MotesSwirl", 620, 0.09f, 6f);', 'MakeFlow("MotesFeet", 700, 0.075f, 3.6f); _above = MakeFlow("MotesAbove", 600, 0.085f, 5f); _swirl = MakeFlow("MotesSwirl", 800, 0.07f, 6f);'),
('SetRate(_feet, 90f * k); SetRate(_above, 70f * k); SetRate(_swirl, 110f * k);', 'SetRate(_feet, 150f * k); SetRate(_above, 110f * k); SetRate(_swirl, 170f * k);'),
('ParticleSystem _dust, _flakes, _haze;', 'ParticleSystem _dust, _flakes, _haze; class Ribbon { public Transform tf; public TrailRenderer tr; public float ph, sp, r, hy; } Ribbon[] _rib = new Ribbon[8];'),
('            SetFlow(0.0f);\n        }', '''            for (int i = 0; i < _rib.Length; i++)
            {
                var g = new GameObject("Ribbon" + i); g.transform.SetParent(_root, false); var tr = g.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 2.8f; tr.minVertexDistance = 0.03f; tr.numCapVertices = 3;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.11f), new Keyframe(0.15f, 0.07f), new Keyframe(1, 0f)); var gr = new Gradient();
                gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#8fd6ff"), 0.35f), new GradientColorKey(LFUtil.Hex("#8a78ff"), 1f) }, new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = gr; tr.emitting = false;
                _rib[i] = new Ribbon { tf = g.transform, tr = tr, ph = i * 0.785f, sp = 0.32f + 0.1f * (i % 3), r = 1.2f + 0.34f * i, hy = 0.2f + 0.25f * (i % 4) };
            }
            SetFlow(0.0f);
        }'''),
('            _flakeT -= dt;', '''            _flakeT -= dt;
            { float sp0 = 0.55f + _intensity * 1.5f; for (int i = 0; i < _rib.Length; i++)
              {
                var rb = _rib[i]; if (!_floorOn) { rb.tr.emitting = false; continue; } rb.tr.emitting = true;
                float ang = tm * rb.sp * sp0 + rb.ph; float rad = rb.r + 0.5f * Mathf.Sin(tm * 0.4f + rb.ph * 2f); float amp = 0.5f + 1.1f * _intensity;
                float y = 0.12f + rb.hy * 0.5f + (0.5f + 0.5f * Mathf.Sin(tm * 0.55f + rb.ph * 1.7f)) * amp * (1f + rb.hy);
                var rp = new Vector3(Mathf.Sin(ang) * rad, y, Mathf.Cos(ang) * rad); rb.tf.position = rp;
                _ep.position = rp; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.12f; _ep.startColor = Color.Lerp(LFUtil.Hex("#a8e4ff"), Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.06f); _ep.startLifetime = UnityEngine.Random.Range(1f, 1.8f); _dust.Emit(_ep, 1);
              } }'''),
('                if (u >= 1f && !p.hazed)', '''                if (u >= 1f && p.scale.y > 1.8f && ((Time.frameCount + i) & 1) == 0)
                {
                    float hh = UnityEngine.Random.value * p.scale.y * 0.95f; float ang2 = UnityEngine.Random.value * 6.2832f; float rr = UnityEngine.Random.Range(0.05f, 0.34f) * p.scale.x * (1f - hh / (p.scale.y * 1.2f));
                    _ep.position = p.tf.position + new Vector3(Mathf.Sin(ang2) * rr, hh, Mathf.Cos(ang2) * rr); _ep.applyShapeToPosition = true; _ep.velocity = new Vector3(Mathf.Cos(ang2), 0.45f, -Mathf.Sin(ang2)) * 0.18f;
                    _ep.startColor = Color.Lerp(LFUtil.Hex("#9fdcff"), Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.06f); _ep.startLifetime = UnityEngine.Random.Range(1.2f, 2.2f); _dust.Emit(_ep, 1);
                }
                if (u >= 1f && !p.hazed)'''),
])
# ---- quick test path: skip the tour, go straight into a 30 s performance excerpt
sub('Scripts/LFAuto.cs', [
('yield return StartCoroutine(BootIntro()); BuildMenu(); State = LFState.Menu; yield return new WaitForSeconds(1.5f);',
 'yield return StartCoroutine(BootIntro()); BuildMenu(); State = LFState.Menu; yield return new WaitForSeconds(1.5f);\n            { bool quick = false; foreach (var qa in System.Environment.GetCommandLineArgs()) if (qa == "-lf-quick") quick = true; if (quick) { UI.Remove(_menu); _menu = null; StartPerformanceAt(46f, -1f); while (true) yield return null; } }'),
])
print('patched3')
