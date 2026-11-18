import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)

# 1. far fewer particles in the jungle
sub('Assets/Scripts/LFWorld.cs', [
 ('k = Mathf.Clamp01(k); SetRate(_feet, 150f * k); SetRate(_above, 110f * k); SetRate(_swirl, 170f * k);',
  'k = Mathf.Clamp01(k); float jf = _jungle ? 0.12f : 1f; SetRate(_feet, 150f * k * jf); SetRate(_above, 110f * k * jf * 0.5f); SetRate(_swirl, 170f * k * jf);'),
 ('        public void Burst(Vector3 pos, Color c, int n, float speed)\n        {\n', '        public void Burst(Vector3 pos, Color c, int n, float speed)\n        {\n            if (_jungle) n = Mathf.Max(4, n / 4);\n'),
 ('for (int i = 0; i < n * 2; i++) EmitDust(pos, UnityEngine.Random.onUnitSphere', 'for (int i = 0; i < n / 3; i++) EmitDust(pos, UnityEngine.Random.onUnitSphere'),
])
sub('Assets/Scripts/LFMagic.cs', [
 ('for (int s = 0; s < 4; s++) _w.EmitDust(a + Random.insideUnitSphere * 0.03f', 'for (int s = 0; s < (JungleMode ? 1 : 4); s++) _w.EmitDust(a + Random.insideUnitSphere * 0.03f'),
])
# 2. no comic pops in the jungle, spirits intro, scenery waits for the meeting
sub('Assets/Scripts/LFApp.cs', [
 ('if (Theme == 1) { LFLog.Ev("cue", _song.sections[i].kind, i); if (_song.sections[i].kind == "peak") { World.Beat(1.2f); ComicPop("Rise", LFMats.Gold); } else ComicPop("Listen", LFMats.Gold); return; }',
  'if (Theme == 1) { LFLog.Ev("cue", _song.sections[i].kind, i); if (_song.sections[i].kind == "peak") World.Beat(0.5f); return; }'),
 ('if (Theme == 1) { EnsureJungle(); LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); }', 'if (Theme == 1) { EnsureJungle(); LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); LFSpirits.Ensure(); }'),
])
sub('Assets/Scripts/LFAuto.cs', [
 ('LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); UI.Hint("Push your hand outward. The world draws itself where you point."); yield return new WaitForSeconds(4.5f);',
  'LFMagic.JungleMode = true; Jungle.StartVoid(); World.SetFlow(0.5f); LFSpirits.Ensure(); UI.Hint("Listen. Two spirits meet. Then push your hand outward."); yield return new WaitForSeconds(4.5f);'),
 ('float[] dT0 = { 9f, 18f, 27f, 36f, 45f, 52f, 60f, 68f, 76f, 88f, 100f, 108f, 114f, 119f, 124f, 130f }; string[] dK = { "push", "push", "lift", "push", "push", "both", "both", "both", "climb", "climb", "climb", "sky:RAIN +", "sky:RAIN +", "sky:RAIN -", "sky:TIME +", "sky:TIME -" };',
  'float[] dT0 = { 25f, 31f, 37f, 43f, 49f, 55f, 62f, 70f, 78f, 90f, 97f, 104f, 111f, 121f, 127f, 133f, 139f }; string[] dK = { "push", "push", "push", "push", "lift", "push", "both", "both", "both", "climb", "climb", "climb", "climb", "sky:RAIN +", "sky:RAIN -", "sky:TIME +", "sky:TIME -" };'),
])
print("patched calm")
