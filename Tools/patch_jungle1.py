import os
p = r'E:\Winter\metavr\LastFrequency\Art\scripts\build_jungle.py'
s = open(p, encoding='utf8').read()
def rep(a, b):
    global s
    if a not in s: print("MISSING:", a[:80])
    s = s.replace(a, b)
rep("SC = 9.5", "SC = 11.5")
rep("    h = floor + wall + cliff + rim + (n2 - 0.5) * 6 * smooth(30, 120, r) + (n4 - 0.5) * 1.6\n",
    "    h = floor + wall + cliff + rim + (n2 - 0.5) * 6 * smooth(30, 120, r) + (n4 - 0.5) * 1.6\n"
    "    # layered sandstone ledges on the big walls (strata you can read from far away)\n"
    "    tt = (h + 18) / 5.5 + (n2 - 0.5) * 0.9; ledge = (np.floor(tt) + smooth(0.55, 0.95, tt - np.floor(tt))) * 5.5 - 18\n"
    "    h = lerp(h, ledge, smooth(50, 100, r) * 0.7 * smooth(-12, 8, h))\n"
    "    h = h + (ridged(X * 0.09, Y * 0.09, 4, 31) - 0.4) * 7 * smooth(60, 120, r)\n")
rep("fog = 1 - np.exp(-d * 0.0042)", "fog = 1 - np.exp(-d * 0.0016)")
# smoother broken bridge: one sloped deck, arches beneath
a0 = s.index("def make_bridge():"); a1 = s.index("# ------------------------------------------------------------------ baking")
new_bridge = '''def make_bridge():
    b = Mesh(); y0, y1 = 6.4, 40.0; w = 4.8; L = y1 - y0; spans = 4; sp = L / spans; rise = 5.0
    def zdeck(y): return rise * ((y - y0) / L) ** 2
    ang = math.atan2(zdeck(y1) - zdeck(y0), L)
    for i in range(spans + 1):
        y = y0 + i * sp; zt = zdeck(y) - 0.6; zb = -22.0
        b.add(box_sub((0, y, (zt + zb) * 0.5), (4.4, 3.2, zt - zb), 10))
    for i in range(spans):
        ya = y0 + i * sp; yb = ya + sp; ym = (ya + yb) * 0.5; n = 14; pts = []
        for k in range(n + 1):
            a = k / n * math.pi; pts.append((ym - math.cos(a) * sp * 0.47, zdeck(ym) - 0.9 - 0.0 + math.sin(a) * 4.4 - 4.4 + 0.0))
        for k in range(n):
            (ya_, za_), (yb_, zb_) = pts[k], pts[k + 1]
            b.add(box_sub((0, (ya_ + yb_) * 0.5, (za_ + zb_) * 0.5 - 0.5), (w - 0.5, math.hypot(yb_ - ya_, zb_ - za_) + 0.12, 1.0), 2, rotx(-math.atan2(zb_ - za_, yb_ - ya_))))
    # deck: slabs along the slope, with a few missing so it reads as ruined
    nsl = 20
    for k in range(nsl):
        ya = y0 + k * L / nsl; yb = ya + L / nsl; ym = (ya + yb) * 0.5
        if k in (11, 12) and False: continue
        b.add(box_sub((0, ym, zdeck(ym) - 0.35), (w, L / nsl + 0.04, 0.9), 4, rotx(-ang * 0.0)))
    for sx in (-1, 1):
        for k in range(46):
            if rng.random() < 0.2: continue
            ym = y0 + (k + 0.5) * L / 46; b.add(box_sub((sx * (w / 2 - 0.2), ym, zdeck(ym) + 0.55 - (0.3 if rng.random() < 0.3 else 0)), (0.5, L / 46 * 0.9, 0.9 + rng.random() * 0.4), 2))
    return b

'''
s = s[:a0] + new_bridge + s[a1:]
rep('shot("front", (0, 0, 1.4), (0, 60, 14), 20);', 'shot("front", (0, 0, 1.4), (0, 60, 14), 20); shot("statue_close", (0, 28, 6.5), (0, 62, 24), 24); shot("temple", (-30, -20, 4), (-74, -52, 8), 22);')
open(p, 'w', encoding='utf8').write(s); print("patched jungle1")
