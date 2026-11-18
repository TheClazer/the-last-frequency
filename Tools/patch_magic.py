import re, os
os.chdir(r'E:\Winter\metavr\LastFrequency\Assets')
def sub(p, pairs):
    s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", p, ":", a[:70])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# ---------- LFMeshes
sub('Scripts/LFMeshes.cs', [
('case "spire3": return Spire(b, 4, 0.16f, 1f, 0f);', 'case "spire3": return Spire(b, 4, 0.16f, 1f, 0f);\n                case "crystals": Crystals(b); return b.Build(name);'),
('static void Bipyramid(MB b, float r, float h, int sides)', '''static void Crystals(MB b)
        {
            var rnd = new System.Random(11); float[] hs = { 1f, 0.74f, 0.64f, 0.52f, 0.44f, 0.36f, 0.3f };
            for (int k = 0; k < hs.Length; k++)
            {
                float ang = k * 2.4f; float rad = k == 0 ? 0f : 0.15f + 0.035f * k; var bp = new Vector3(Mathf.Sin(ang) * rad, 0, Mathf.Cos(ang) * rad);
                float lean = k == 0 ? 0.03f : 0.22f + 0.08f * (float)rnd.NextDouble(); var dir = (Vector3.up + new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang)) * lean).normalized;
                Crystal(b, bp, dir, hs[k], 0.085f + 0.03f * (1f - k / 7f));
            }
        }
        static void Crystal(MB b, Vector3 bp, Vector3 dir, float h, float r)
        {
            b.orient = true; b.ctr = bp + dir * h * 0.4f; Vector3 u = Vector3.Cross(dir, Vector3.right).normalized; if (u.sqrMagnitude < 0.1f) u = Vector3.Cross(dir, Vector3.forward).normalized; Vector3 v = Vector3.Cross(dir, u);
            const int S = 6; var r0 = new Vector3[S]; var r1 = new Vector3[S];
            for (int i = 0; i < S; i++) { float a = i / (float)S * Mathf.PI * 2f; Vector3 off = u * Mathf.Cos(a) + v * Mathf.Sin(a); r0[i] = bp + off * r * 0.8f; r1[i] = bp + dir * h * 0.72f + off * r; }
            Vector3 tip = bp + dir * h;
            for (int i = 0; i < S; i++) { int j = (i + 1) % S; b.Quad(r0[i], r1[i], r1[j], r0[j]); b.Tri(r1[i], tip, r1[j]); }
        }
        static void Bipyramid(MB b, float r, float h, int sides)'''),
('new[] { "#12d6ff", "#ff3bd4" }, new[] { "#ff2e9a", "#ffd23b" }, new[] { "#ffc83b", "#ff4a3b" },\n            new[] { "#6a4bff", "#18e6ff" }, new[] { "#2bffb0", "#2b6bff" }, new[] { "#f2fbff", "#33d6ff" } };',
 'new[] { "#2fa8ff", "#d9f6ff" }, new[] { "#a064ff", "#ffc9f2" }, new[] { "#ffcf7a", "#fff4d0" },\n            new[] { "#4458ff", "#bfc9ff" }, new[] { "#2fe0cc", "#cffff5" }, new[] { "#8fd0ff", "#ffffff" } };'),
('public static Material Floor() { if (_floor == null) { _floor = new Material(Toon(Violet)) { name = "LFFloor" }; _floor.SetFloat("_Glow", 1.2f); _floor.SetFloat("_OutW", 0.012f); } return _floor; }',
 'public static Material Floor() { if (_floor == null) _floor = new Material(Shader.Find("LF/Floor")) { name = "LFFloor" }; return _floor; }\n        static Material _flake, _trail;\n        public static Material Flake() { if (_flake == null) _flake = new Material(Shader.Find("LF/Flake")); return _flake; }\n        public static Material Trail() { if (_trail == null) _trail = new Material(Shader.Find("LF/Trail")); return _trail; }'),
])
sub('Resources/Shaders/LFRing.shader', [('(0.6 + 0.6 * dots)', '(1.0 + 0.0 * dots)')])

# ---------- LFWorld
sub('Scripts/LFWorld.cs', [
('public int ClaimedPlayer, ClaimedRival;', 'public int ClaimedPlayer, ClaimedRival; ParticleSystem _dust, _flakes, _haze; float _flakeT; int _stairShown;'),
('public float rise;', 'public float rise; public bool hazed;'),
('fl.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("floor");', 'fl.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("disc");'),
('_burst = MakeFlow("Burst", 600, 0.07f, 1.4f);', '_burst = MakeFlow("Burst", 700, 0.09f, 1.6f); _dust = MakeFlow("HandDust", 900, 0.05f, 1.4f); _flakes = MakeFlow("Flakes", 300, 0.22f, 3f, true); _haze = MakeFlow("Haze", 60, 2.2f, 5f);'),
('ConfigureBurst(_burst);', 'ConfigureBurst(_burst); ConfigureBurst(_dust); ConfigureFlakes(_flakes); ConfigureBurst(_haze);'),
('ParticleSystem MakeFlow(string name, int max, float size, float life)', 'ParticleSystem MakeFlow(string name, int max, float size, float life, bool flake = false)'),
('m.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.5f);', 'm.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 2.0f);'),
('new GradientColorKey(LFMats.Tint(LFMats.Cyan), 0f), new GradientColorKey(LFMats.Tint(LFMats.Magenta), 0.5f), new GradientColorKey(LFMats.Tint(LFMats.Gold), 1f)', 'new GradientColorKey(LFUtil.Hex("#c8f2ff"), 0f), new GradientColorKey(LFUtil.Hex("#7fc4ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#cdbcff"), 1f)'),
('rend.sharedMaterial = LFMats.Mote();', 'rend.sharedMaterial = flake ? LFMats.Flake() : LFMats.Mote();'),
('void ConfigureBurst(ParticleSystem ps)', '''void ConfigureFlakes(ParticleSystem ps)
        {
            var em = ps.emission; em.enabled = false; var s = ps.shape; s.enabled = false; var m = ps.main; m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var r = ps.rotationOverLifetime; r.enabled = true; r.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.25f; lim.dampen = 0.06f;
        }
        void ConfigureBurst(ParticleSystem ps)'''),
('s.radius = 1.7f; s.radiusThickness = 0.35f;', 's.radius = 2.8f; s.radiusThickness = 0.5f;'),
('bool on = k > 0.01f; foreach (var ps in new[] { _feet, _above, _swirl }) { if (on && !ps.isPlaying) ps.Play(); }', 'bool on = k > 0.01f; if (on) { if (!_feet.isPlaying) _feet.Play(); if (!_above.isPlaying) _above.Play(); if (!_swirl.isPlaying) _swirl.Play(); }'),
('p.tf.GetComponent<MeshFilter>().sharedMesh = LFMeshes.Get(mesh);', 'p.hazed = instant; p.tf.GetComponent<MeshFilter>().sharedMesh = LFMeshes.Get(mesh);'),
('Place(p, i % 2 == 0 ? "spire" : "spire3", mat, to + Vector3.down * (h + 2f), to, new Vector3(R(0.8f, 1.5f), h, R(0.8f, 1.5f)),', 'float wd = R(1.5f, 2.6f); Place(p, "crystals", mat, to + Vector3.down * (h + 2f), to, new Vector3(wd, h, wd),'),
('Place(p, "spire", mat, to + Vector3.down * (h + 3f), to, new Vector3(1.4f, h, 1.4f),', 'Place(p, "crystals", mat, to + Vector3.down * (h + 3f), to, new Vector3(2.8f, h, 2.8f),'),
('Place(p, "spire3", mat, to + Vector3.down * (h + 4f), to, new Vector3(1.6f, h, 1.6f),', 'Place(p, "crystals", mat, to + Vector3.down * (h + 4f), to, new Vector3(3.0f, h, 3.0f),'),
('Place(sp, "spire", b.kind + 1, floorPt + Vector3.down * 2f, floorPt, new Vector3(0.7f, R(1.4f, 2.6f), 0.7f), 0f, 1.1f, 0.2f, false);', 'Place(sp, "crystals", b.kind + 1, floorPt + Vector3.down * 2f, floorPt, new Vector3(1.3f, R(1.6f, 3.0f), 1.3f), 0f, 1.2f, 0.2f, false); FlakeBloom(pos, c, 16);'),
('_floor.localScale = Vector3.one * LFUtil.EaseOutBack(_floorGrow);', '_floor.localScale = Vector3.one * 3.4f * LFUtil.EaseOutBack(_floorGrow);'),
('_stairTarget = 0; _stair = 0; Shader.SetGlobalFloat(LFShaderIds.Stair, 0f);', '_stairTarget = 0; _stair = 0; _stairShown = 0; Shader.SetGlobalFloat(LFShaderIds.Stair, 0f);'),
])
s = open('Scripts/LFWorld.cs', encoding='utf8').read()
s = s.replace("            if (head != null) { _sky.position = head.position; }", """            _flakeT -= dt;
            if (pointers != null) for (int k = 0; k < pointers.Length; k++)
            {
                var pt = pointers[k]; if (!pt.active) continue; float sp = pt.vel.magnitude; int n = 1 + Mathf.Min(7, (int)(sp * 10f));
                for (int q = 0; q < n; q++)
                {
                    _ep.position = pt.pos + UnityEngine.Random.insideUnitSphere * 0.012f; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.18f + pt.vel * 0.12f + Vector3.up * 0.05f;
                    _ep.startColor = Color.Lerp(LFUtil.Hex("#9fe0ff"), Color.white, UnityEngine.Random.value); _ep.startSize = UnityEngine.Random.Range(0.025f, 0.07f); _ep.startLifetime = UnityEngine.Random.Range(0.9f, 1.7f); _dust.Emit(_ep, 1);
                }
                if (_flakeT <= 0f && sp > 0.05f) { _flakeT = 0.22f; _ep.position = pt.pos; _ep.velocity = UnityEngine.Random.insideUnitSphere * 0.2f + Vector3.up * 0.08f; _ep.startColor = Color.Lerp(LFUtil.Hex("#a8e6ff"), Color.white, 0.5f); _ep.startSize = UnityEngine.Random.Range(0.1f, 0.2f); _ep.startLifetime = UnityEngine.Random.Range(2f, 3f); _flakes.Emit(_ep, 1); }
            }
            int shown = Mathf.FloorToInt(_stair * 24f + 0.001f);
            while (_stairShown < shown) { float a = _stairShown * 13f * Mathf.Deg2Rad; var sp2 = _stairs.TransformPoint(new Vector3(Mathf.Sin(a) * 2.7f, _stairShown * 0.24f + 0.18f, Mathf.Cos(a) * 2.7f)); Burst(sp2, LFUtil.Hex("#9fe0ff"), 12, 0.7f); _ep.position = sp2; _ep.velocity = Vector3.up * 0.12f; _ep.startColor = Color.white; _ep.startSize = 0.2f; _ep.startLifetime = 2.2f; _flakes.Emit(_ep, 1); _stairShown++; }
            if (shown < _stairShown) _stairShown = shown;
            if (head != null) { _sky.position = head.position; }""")
s = s.replace("        void PopBud(Bud b)", """        public void FlakeBloom(Vector3 pos, Color c, int n)
        {
            for (int i = 0; i < n; i++)
            {
                _ep.position = pos; _ep.applyShapeToPosition = true; _ep.velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.5f, 1.3f); _ep.startColor = Color.Lerp(c, Color.white, 0.55f);
                _ep.startSize = UnityEngine.Random.Range(0.14f, 0.38f); _ep.startLifetime = UnityEngine.Random.Range(2.2f, 3.4f); _flakes.Emit(_ep, 1);
            }
            _ep.position = pos; _ep.velocity = Vector3.zero; _ep.startColor = new Color(c.r, c.g, c.b, 0.35f); _ep.startSize = 1.6f; _ep.startLifetime = 2.2f; _haze.Emit(_ep, 1);
        }

        void PopBud(Bud b)""")
s = s.replace("                if (p.to.y > 0.8f) p.tf.Rotate(0, p.spin * dt, 0, Space.World);", """                if (p.to.y > 0.8f) p.tf.Rotate(0, p.spin * dt, 0, Space.World);
                if (u >= 1f && !p.hazed) { p.hazed = true; _ep.position = p.tf.position + Vector3.up * Mathf.Min(p.scale.y * 0.55f, 4f); _ep.velocity = Vector3.zero; _ep.startColor = new Color(0.45f, 0.75f, 1f, 0.16f); _ep.startSize = Mathf.Clamp(p.scale.y * 0.5f, 1f, 3.2f); _ep.startLifetime = 5f; _haze.Emit(_ep, 1); Burst(p.tf.position + Vector3.up * 0.2f, LFUtil.Hex("#b6e8ff"), 16, 0.9f); }""")
open('Scripts/LFWorld.cs', 'w', encoding='utf8').write(s)

# ---------- LFApp / compose
sub('Scripts/LFApp.cs', [
('o.transform.localScale = Vector3.one * 0.035f; o.SetActive(false); _tipVis[i] = o.transform;', 'o.transform.localScale = Vector3.one * 0.016f; var tr = o.AddComponent<TrailRenderer>(); tr.sharedMaterial = LFMats.Trail(); tr.time = 0.95f; tr.minVertexDistance = 0.008f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.05f), new Keyframe(1, 0f)); tr.numCapVertices = 4; var tg = new Gradient(); tg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(LFUtil.Hex("#7fd0ff"), 0.5f), new GradientColorKey(LFUtil.Hex("#7a6cff"), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }); tr.colorGradient = tg; tr.emitting = false; o.SetActive(false); _tipVis[i] = o.transform;'),
('Cam.backgroundColor = Color.black;', 'Cam.backgroundColor = new Color(0.01f, 0.02f, 0.08f);'),
('"POW!"', '"Shimmer"'), ('"WHOOSH!"', '"Rise"'), ('"KA-BOOOM!"', '"Aurora"'), ('"SHIIING!"', '"Resonate"'), ('"HUM..."', '"Listen..."'), ('"RISE!"', '"Rise"'),
('new[] { "POP!", "ZAP!", "SPLASH!", "PING!", "BLOOM!", "SHIMMER!" }', 'new[] { "Bloom", "Shimmer", "Glow", "Echo", "Frost", "Spark" }'),
('_tipVis[i].gameObject.SetActive(p.active); if (p.active) _tipVis[i].position = p.pos;', '{ bool was = _tipVis[i].gameObject.activeSelf; _tipVis[i].gameObject.SetActive(p.active); if (p.active) { _tipVis[i].position = p.pos; var trr = _tipVis[i].GetComponent<TrailRenderer>(); if (!was) trr.Clear(); trr.emitting = true; } }'),
])
sub('Scripts/LFCompose.cs', [('new[] { "BOP!", "ZING!", "DOOT!", "TWANG!" }', 'new[] { "Chime", "Glow", "Hum", "Ring" }')])
print("patched")
