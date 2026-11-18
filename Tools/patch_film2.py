import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

sub('Assets/Resources/Shaders/LFAvatar.shader', [('float t = saturate(i.col.r);', 'float t = smoothstep(0.62, 1.0, i.col.r);')])
sub('Assets/Resources/Shaders/LFReveal.hlsl', [('float edge = vis * (1.0 - step(key + 0.028, t));', 'float edge = vis * (1.0 - step(key + 0.007, t));')])
sub('Assets/Scripts/LFShare.cs', [('public string Start(string dir)', 'public string Begin(string dir)')])
sub('Assets/Scripts/LFApp.cs', [('_share.Start(ExportDir)', '_share.Begin(ExportDir)')])
sub('Assets/Scripts/LFAvatar.cs', [
('        Mesh Gown()\n        {\n            float[,] prof = { { 0.07f, 0f }, { 0.13f, -0.07f }, { 0.20f, -0.2f }, { 0.17f, -0.38f }, { 0.20f, -0.55f }, { 0.28f, -0.75f }, { 0.4f, -0.95f }, { 0.55f, -1.1f } }; int rings',
 '        Mesh Gown() { return GownMesh(new float[,] { { 0.07f, 0f }, { 0.13f, -0.07f }, { 0.20f, -0.2f }, { 0.17f, -0.38f }, { 0.20f, -0.55f }, { 0.28f, -0.75f }, { 0.4f, -0.95f }, { 0.55f, -1.1f } }, true); }\n        Mesh GownMesh(float[,] prof, bool hemRuffle)\n        {\n            int rings'),
('(i == rings - 1 ? 0.12f * Mathf.Sin(a * 7f) : 0f)); V.Add(new Vector3(Mathf.Cos(a) * r, prof[i, 1] - (i == rings - 1 ? 0.06f * Mathf.Sin(a * 7f) : 0f)', '(hemRuffle && i == rings - 1 ? 0.12f * Mathf.Sin(a * 7f) : 0f)); V.Add(new Vector3(Mathf.Cos(a) * r, prof[i, 1] - (hemRuffle && i == rings - 1 ? 0.06f * Mathf.Sin(a * 7f) : 0f)'),
('Material _cloth;', 'Material _cloth, _skin; Transform _mantle;'),
('var h = new GameObject("Head"); h.transform.SetParent(transform, false); h.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("orb"); var hr = h.AddComponent<MeshRenderer>(); hr.sharedMaterial = LFMats.Toon(LFMats.Ice);',
 '_skin = new Material(Shader.Find("LF/Avatar")); _skin.SetColor("_ColA", new Color(0.96f, 0.88f, 0.76f)); _skin.SetColor("_ColB", new Color(1f, 0.95f, 0.85f)); _skin.SetFloat("_Pattern", 0f); _skin.SetFloat("_Glow", 0.9f);\n            var mt = new GameObject("Mantle"); mt.transform.SetParent(transform, false); mt.AddComponent<MeshFilter>().sharedMesh = GownMesh(new float[,] { { 0.1f, 0.0f }, { 0.28f, -0.04f }, { 0.4f, -0.16f }, { 0.38f, -0.3f }, { 0.3f, -0.36f } }, false); var mr2 = mt.AddComponent<MeshRenderer>(); mr2.sharedMaterial = _cloth; mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; _mantle = mt.transform;\n            var h = new GameObject("Head"); h.transform.SetParent(transform, false); h.AddComponent<MeshFilter>().sharedMesh = LFMeshes.Get("orb"); var hr = h.AddComponent<MeshRenderer>(); hr.sharedMaterial = _skin;'),
('            _head.GetComponent<MeshRenderer>().sharedMaterial = LFMats.Toon(o == 1 ? LFMats.Gold : (o == 0 ? LFMats.Ice : LFMats.Violet));', '            _skin.SetColor("_Trim", p[2]);'),
('            Vector3 neck = headPos + Vector3.down * 0.17f; _torso.position = neck;', '            Vector3 neck = headPos + Vector3.down * 0.17f; _torso.position = neck; _mantle.position = neck + Vector3.up * 0.0f; _mantle.rotation = yawQ; _mantle.localScale = new Vector3(1f + level * 0.08f, 1f, 1f + level * 0.08f);'),
('Place(_arm[idx * 2], sh, el, 0.075f); Place(_arm[idx * 2 + 1], el, hand, 0.095f);', 'Place(_arm[idx * 2], sh, el, 0.085f); Place(_arm[idx * 2 + 1], el, hand, 0.12f);'),
])
sub('Assets/Scripts/LFDirector.cs', [
('default: { pos = hp + new Vector3(2.6f - k * 3.6f, 1.4f + k * 5.5f, -3.4f - k * 1.8f); look = Vector3.Lerp(hp + Vector3.up * 0.5f, new Vector3(0f, 15f, 62f), 0.45f + k * 0.45f); fov = Mathf.Lerp(58f, 70f, k); break; }',
 'default: { pos = hp + new Vector3(3.0f - k * 5f, 2.2f + k * 11f, -6f - k * 12f); look = Vector3.Lerp(hp + Vector3.up * 1.2f, new Vector3(0f, 17f, 62f), 0.5f + k * 0.4f); fov = Mathf.Lerp(60f, 66f, k); break; }'),
('{ want = Seq[_seq++ % Seq.Length]; len = 8.5f; }', '{ want = Seq[_seq++ % Seq.Length]; len = 6.5f; }'),
('static readonly int[] Seq = { 0, 1, 2, 1, 0, 2 };', 'static readonly int[] Seq = { 0, 2, 1, 2, 0, 1, 2 };'),
])
print("patched film2")
