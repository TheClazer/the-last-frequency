import os
S = r'E:\Winter\metavr\LastFrequency'
def sub(p, pairs):
    p = os.path.join(S, p); s = open(p, encoding='utf8').read()
    for a, b in pairs:
        if a not in s: print("MISSING in", os.path.basename(p), ":", a[:90])
        s = s.replace(a, b)
    open(p, 'w', encoding='utf8').write(s)
sub('Assets/Scripts/LFApp.cs', [(' LFSpirits.Ensure(); }', ' }')])
sub('Assets/Scripts/LFAuto.cs', [(' LFSpirits.Ensure(); UI.Hint("Listen. Two spirits meet. Then push your hand outward.");', ' UI.Hint("Listen. Then push your hand outward.");'),
 ('{ 25f, 31f, 37f, 43f, 49f,', '{ 16f, 24f, 32f, 40f, 49f,')])
sub('Assets/Scripts/LFWorld.cs', [('public void SpawnBuds(int n)\n        {', 'public void SpawnBuds(int n)\n        {\n            if (_jungle) return;')])
