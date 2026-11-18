"""THE JUNGLE (hero geometry): polar terrain with cliffs, waterfalls, river, terraces; a colossal weathered stone sentinel; stepped temple; broken arched bridge.
Per-vertex baked lighting (AO + sun shadow via BVH ray casts) is written with the meshes. Run:  blender -b --factory-startup -P build_jungle.py
Outputs: Assets/Resources/Jungle/*.bytes, anchors.json, preview PNGs in Art/jungle_preview."""
import bpy, bmesh, math, json, os, sys, time
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from jungle_lib import *
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = r'E:\Winter\metavr\LastFrequency\Assets\Resources\Jungle'; PREV = r'E:\Winter\metavr\LastFrequency\Art\jungle_preview'
os.makedirs(OUT, exist_ok=True); os.makedirs(PREV, exist_ok=True)
rng = np.random.default_rng(11); T0 = time.time()
def log(*a): print("[jungle %5.1fs]" % (time.time() - T0), *a, flush=True)

SUN = np.array([-0.42, 0.62, 0.66]); SUN = SUN / np.linalg.norm(SUN)      # toward the sun: front-left, low, golden hour (statue is rim-lit)
SUN_COL = np.array([1.0, 0.72, 0.42]); AMB_TOP = np.array([0.30, 0.48, 0.70]); AMB_BOT = np.array([0.20, 0.26, 0.12])
STATUE_C = np.array([0.0, 62.0, 5.0]); TEMPLE_C = np.array([-74.0, -52.0, -3.0]); SC = 11.5

# ------------------------------------------------------------------ terrain
RIVER_A = [(-40, 84), (-26, 68), (-10, 50), (-4, 32), (-18, 12), (-44, -4), (-76, -26), (-110, -66), (-150, -130), (-190, -210)]
RIVER_B = [(40, 84), (26, 68), (12, 50), (-4, 32)]
FALL_PHI = [-0.5, 0.52]
def height(X, Y):
    r = np.hypot(X, Y); phi = np.arctan2(X, Y)
    n1 = fbm(X * 0.011, Y * 0.011, 5, 1); n2 = fbm(X * 0.05, Y * 0.05, 5, 2); n3 = ridged(X * 0.02, Y * 0.02, 4, 3); n4 = fbm(X * 0.2, Y * 0.2, 4, 9)
    floor = -18 + (n2 - 0.5) * 3.4
    wall = smooth(40, 150, r) * (0.5 + 0.5 * n1) * 36
    cliff = smooth(92, 114, r) * (36 + 24 * n3) * (1 + 0.35 * np.exp(-(phi / 0.9) ** 2))
    rim = smooth(114, 170, r) * (10 + 28 * n1)
    h = floor + wall + cliff + rim + (n2 - 0.5) * 6 * smooth(30, 120, r) + (n4 - 0.5) * 1.6
    # layered sandstone ledges on the big walls (strata you can read from far away)
    tt = (h + 18) / 5.5 + (n2 - 0.5) * 0.9; ledge = (np.floor(tt) + smooth(0.55, 0.95, tt - np.floor(tt))) * 5.5 - 18
    h = lerp(h, ledge, smooth(50, 100, r) * 0.7 * smooth(-12, 8, h))
    h = h + (ridged(X * 0.09, Y * 0.09, 4, 31) - 0.4) * 7 * smooth(60, 120, r)
    # terraces on the right-hand slopes (Machu Picchu rhythm)
    tmask = smooth(25, 45, r) * smooth(118, 90, r) * smooth(0.55, 0.95, phi) * smooth(2.5, 2.0, phi)
    t = (h + 18) / 3.2; hq = (np.floor(t) + smooth(0.72, 1.0, t - np.floor(t))) * 3.2 - 18; h = lerp(h, hq, tmask * 0.95)
    # statue shelf and temple shelf
    h = lerp(h, 5.0 + (n4 - 0.5) * 0.5, smooth(34, 23, np.hypot(X - STATUE_C[0], Y - STATUE_C[1])))
    h = lerp(h, -3.0 + (n4 - 0.5) * 0.4, smooth(36, 26, np.hypot(X - TEMPLE_C[0], Y - TEMPLE_C[1])))
    # the courtyard pillar the player sits on
    pr = smooth(6.6, 19, r); h = lerp(0.0, h, pr) + (n2 - 0.5) * 2.2 * pr
    # river + plunge pools
    dr = np.minimum(seg_dist(X, Y, RIVER_A), seg_dist(X, Y, RIVER_B)); h = h - 2.6 * np.exp(-(dr / 5.5) ** 2) * smooth(70, 40, r + 0) * smooth(-30, -14, h + 3)
    for p in FALL_PHI:
        fx, fy = 98 * math.sin(p), 98 * math.cos(p); dg = np.hypot(X - fx, Y - fy) * 0 + seg_dist(X, Y, [(fx, fy), (88 * math.sin(p), 88 * math.cos(p))])
        h = h - 7 * np.exp(-(dg / 2.8) ** 2) * smooth(80, 92, r) * smooth(120, 106, r)
        pool = np.hypot(X - 84 * math.sin(p), Y - 84 * math.cos(p)); h = h - 3.5 * np.exp(-(pool / 6.0) ** 2)
    return h

NA, NR = 330, 150
rs = 0.45 * (460.0 / 0.45) ** (np.arange(NR) / (NR - 1)); th = np.arange(NA) / NA * 2 * math.pi
R_, T_ = np.meshgrid(rs, th, indexing='ij'); X_ = R_ * np.sin(T_); Y_ = R_ * np.cos(T_); Z_ = height(X_, Y_)
Pt = np.stack([X_, Y_, Z_], -1).reshape(-1, 3); Ft = []
for i in range(NR - 1):
    a = i * NA + np.arange(NA); b = i * NA + (np.arange(NA) + 1) % NA; c = (i + 1) * NA + np.arange(NA); d = (i + 1) * NA + (np.arange(NA) + 1) % NA
    Ft.append(np.stack([a, b, c], 1)); Ft.append(np.stack([b, d, c], 1))
Ft = np.vstack(Ft); terrain = Mesh(Pt, Ft)
if terrain.normals()[:, 2].mean() < 0: terrain.F = terrain.F[:, [0, 2, 1]]
log("terrain", len(terrain.P), "verts", terrain.tris(), "tris")
def terrain_h(x, y): return height(np.asarray(x, float), np.asarray(y, float))

# ------------------------------------------------------------------ colour helpers
def tint(rock, col, w): return rock * (1 - w[:, None]) + np.asarray(col) * w[:, None]
def weather(P, N, base, moss_amt=1.0, seed=3, terracotta=0.0):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]; n_a = fbm(x * 0.35 + z * 0.1, y * 0.35, 4, seed); n_b = fbm(x * 1.7, y * 1.7 + z * 1.3, 3, seed + 5); up = np.clip(N[:, 2], 0, 1)
    col = np.tile(np.asarray(base, float), (len(P), 1)) * (0.78 + 0.5 * n_b[:, None])
    streak = fbm(x * 2.2, z * 0.25 + y * 0.1, 3, seed + 9); col *= (1.0 - 0.28 * np.clip(streak - 0.55, 0, 1)[:, None] * 3)
    if terracotta > 0: col = tint(col, (0.58, 0.31, 0.15), np.clip((n_a - 0.5) * 4, 0, 1) * terracotta)
    moss = np.clip((up * 1.25 - 0.28 + (n_a - 0.5) * 1.1) * 2.2, 0, 1) * moss_amt
    return tint(col, (0.10, 0.25, 0.06), moss * 0.92), moss

def colour_terrain(m):
    P = m.P; N = m.normals(); x, y, z = P[:, 0], P[:, 1], P[:, 2]; r = np.hypot(x, y); slope = 1 - np.clip(N[:, 2], 0, 1)
    n1 = fbm(x * 0.06, y * 0.06, 5, 21); n2 = fbm(x * 0.6, y * 0.6, 3, 22); strata = 0.5 + 0.5 * np.sin(z * 2.4 + n1 * 6)
    rock = np.array([0.40, 0.34, 0.27]) * (0.62 + 0.5 * strata[:, None]) * (0.8 + 0.4 * n2[:, None])
    green = np.array([0.13, 0.30, 0.07]) * (0.7 + 0.6 * n2[:, None]); lush = np.array([0.07, 0.17, 0.05]) * (0.7 + 0.8 * n1[:, None]); grass = np.array([0.24, 0.42, 0.08])
    mossw = np.clip((0.55 - slope * 1.15 + (n1 - 0.5) * 0.7) * 3.0, 0, 1)
    col = tint(rock, green, mossw); col = tint(col, lush, np.clip((0.5 - slope * 1.4) * 2, 0, 1) * smooth(-12, -17, z) * 0.9)
    col = tint(col, grass, np.clip(n2 - 0.55, 0, 1) * 3 * np.clip(0.4 - slope, 0, 1) * 2 * smooth(20, 5, z + 10) * 0.8)
    dr = np.minimum(seg_dist(x, y, RIVER_A), seg_dist(x, y, RIVER_B)); col = tint(col, (0.10, 0.09, 0.06), np.exp(-(dr / 6.5) ** 2) * 0.9)
    court = np.clip(1 - (r - 6.0) / 0.6, 0, 1); tile = 0.9 + 0.2 * np.clip(np.abs(np.sin(x * 1.4)) * np.abs(np.sin(y * 1.4)) * 4, 0, 1)
    col = tint(col, np.array([0.55, 0.50, 0.42]) * tile[:, None] * (0.8 + 0.3 * n2[:, None]), court)
    wetm = np.exp(-(dr / 7) ** 2)
    return col, wetm

# ------------------------------------------------------------------ the colossal sentinel (metaballs -> mesh, then carved, weathered)
def build_statue():
    import bpy
    mb = bpy.data.metaballs.new('S'); mb.resolution = 0.045; mb.render_resolution = 0.045; mb.threshold = 0.5
    ob = bpy.data.objects.new('S', mb); bpy.context.scene.collection.objects.link(ob)
    def E(co, size=(1, 1, 1), rad=0.3, neg=False, typ='ELLIPSOID', stiff=2.0):
        e = mb.elements.new(type=typ); e.co = co; e.radius = rad; e.size_x, e.size_y, e.size_z = size; e.stiffness = stiff; e.use_negative = neg
    def chain(pts, r0, r1, n=6):
        for i in range(len(pts) - 1):
            for k in range(n):
                t = k / n; p = np.array(pts[i]) * (1 - t) + np.array(pts[i + 1]) * t; rr = r0 + (r1 - r0) * ((i + t) / (len(pts) - 1)); E(tuple(p), rad=rr, stiff=2.2)
    # seat and crossed legs
    E((0, 0.05, 0.38), (1.5, 1.05, 0.38), 0.62); E((-0.95, -0.55, 0.42), rad=0.40); E((0.95, -0.55, 0.42), rad=0.40); E((0, -0.6, 0.30), (1.4, 0.5, 0.3), 0.42)
    chain([(-0.3, -0.2, 0.55), (-0.85, -0.7, 0.45)], 0.34, 0.30); chain([(0.3, -0.2, 0.55), (0.85, -0.7, 0.45)], 0.34, 0.30)
    # torso, belly, chest, shoulders, neck
    E((0, 0.0, 0.95), (0.78, 0.56, 0.62), 0.72); E((0, -0.18, 0.78), (0.72, 0.55, 0.45), 0.52); E((0, 0.02, 1.38), (0.88, 0.5, 0.52), 0.60)
    E((-0.78, 0.0, 1.55), rad=0.32); E((0.78, 0.0, 1.55), rad=0.32); E((0, 0.02, 1.86), (0.8, 0.8, 1.2), 0.22)
    # head
    E((0, -0.04, 2.20), (0.88, 0.86, 1.12), 0.40); E((0, -0.28, 2.04), (1.0, 0.8, 0.9), 0.26); E((0, -0.10, 2.50), (0.9, 0.9, 0.5), 0.22)
    E((-0.19, -0.33, 2.28), (1.2, 0.5, 0.5), 0.09, False, stiff=3); E((0.19, -0.33, 2.28), (1.2, 0.5, 0.5), 0.09, False, stiff=3)    # brow
    E((0, -0.42, 2.14), (0.6, 0.8, 1.6), 0.07, stiff=3)                                                       # nose
    E((0, -0.38, 1.98), (2.2, 0.55, 0.5), 0.045, stiff=3)                                                      # lips
    E((-0.43, -0.02, 2.14), (0.35, 0.5, 1.5), 0.10, stiff=3); E((0.43, -0.02, 2.14), (0.35, 0.5, 1.5), 0.10, stiff=3)   # ears
    for sx in (-1, 1):
        E((sx * 0.46, -0.0, 1.86), rad=0.06, stiff=3)                                                           # ear lobes
        E((sx * 0.17, -0.41, 2.22), (1.5, 0.4, 0.3), 0.04, True, stiff=3)                                      # carved eye slits
    E((0, -0.45, 1.93), (3, 0.3, 0.25), 0.028, True, stiff=3)                                                  # mouth line
    E((-0.045, -0.47, 2.06), rad=0.03, neg=True, stiff=3); E((0.045, -0.47, 2.06), rad=0.03, neg=True, stiff=3)   # nostrils
    # four arms
    for sx in (-1, 1):
        chain([(sx * 0.82, 0, 1.55), (sx * 1.18, -0.2, 1.22), (sx * 1.02, -0.78, 1.38)], 0.24, 0.17); E((sx * 1.0, -0.88, 1.40), (1, 1.2, 1), 0.2)          # lower arms forward, palms up
        chain([(sx * 0.80, 0.1, 1.72), (sx * 1.35, 0.05, 1.95), (sx * 1.48, -0.45, 2.45)], 0.2, 0.14); E((sx * 1.48, -0.5, 2.55), rad=0.17)                 # upper arms raised, fists
    mb.update_tag(); bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH'); ob = bpy.context.view_layer.objects.active
    dg = bpy.context.evaluated_depsgraph_get(); me = ob.evaluated_get(dg).to_mesh()
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm, faces=bm.faces[:])
    P = np.array([v.co[:] for v in bm.verts]); F = np.array([[v.index for v in f.verts] for f in bm.faces]); bm.free()
    bpy.data.objects.remove(ob)
    return Mesh(P, F)

def decimate_mesh(m, target):
    import bpy
    me = bpy.data.meshes.new('d'); me.from_pydata(m.P.tolist(), [], m.F.tolist()); me.update(); ob = bpy.data.objects.new('d', me); bpy.context.scene.collection.objects.link(ob)
    mod = ob.modifiers.new('dec', 'DECIMATE'); mod.ratio = min(1.0, target / max(len(m.F), 1)); mod.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg); e = ev.to_mesh(); e.calc_loop_triangles()
    P = np.array([v.co[:] for v in e.vertices]); F = np.array([t.vertices[:] for t in e.loop_triangles]); bpy.data.objects.remove(ob)
    return Mesh(P, F)

def weather_displace(m, amp, freq, seed=1):
    N = m.normals(); P = m.P; d = (fbm(P[:, 0] * freq + P[:, 2] * 0.7 * freq, P[:, 1] * freq + P[:, 2] * 0.3 * freq, 5, seed) - 0.5) * amp
    d += (fbm(P[:, 0] * freq * 4, P[:, 1] * freq * 4 + P[:, 2] * freq * 3, 3, seed + 3) - 0.5) * amp * 0.35
    return Mesh(P + N * d[:, None], m.F.copy(), m.C.copy(), m.U.copy())

def statue_extras():
    """crown, necklace, armbands, staffs, pedestal: crisp man-made detail in statue-local units"""
    ex = Mesh()
    # tiered crown
    prof = [(0.36, 2.58), (0.34, 2.66), (0.30, 2.70), (0.33, 2.76), (0.27, 2.84), (0.28, 2.90), (0.20, 3.02), (0.22, 3.08), (0.12, 3.26), (0.05, 3.42), (0.0, 3.50)]
    ex.add(lathe(prof, 20, (0, -0.04, 0.0)))
    for k in range(10):
        a = k / 10 * 2 * math.pi; ex.add(lathe([(0.05, 0), (0.03, 0.1), (0.0, 0.22)], 6, (math.sin(a) * 0.31, -0.04 + math.cos(a) * 0.31, 2.70)))
    ex.add(torus((0, 0.0, 1.78), 0.34, 0.035, 28, 6))
    for k in range(14):
        a = (k / 14 - 0.5) * 2.1; ex.add(lathe([(0.0, 0), (0.07, 0.04), (0.05, 0.11), (0.0, 0.15)], 8, (math.sin(a) * 0.43, -math.cos(a) * 0.43 + 0.02, 1.62 - 0.17 * math.cos(a * 1.4))))
    for sx in (-1, 1):
        ex.add(torus((sx * 1.18, -0.2, 1.22), 0.2, 0.04, 14, 6, rotx(1.57))); ex.add(torus((sx * 1.40, 0.0, 1.98), 0.16, 0.035, 14, 6, rotx(1.2)))
        ex.add(tube([(sx * 1.55, -0.52, 0.2), (sx * 1.54, -0.51, 1.8), (sx * 1.50, -0.5, 3.3)], [0.045, 0.04, 0.04], 8)); ex.add(lathe([(0.05, 0), (0.12, 0.08), (0.06, 0.2), (0.0, 0.34)], 8, (sx * 1.50, -0.5, 3.3)))   # staffs
        ex.add(lathe([(0.04, 0), (0.1, 0.05), (0.04, 0.12)], 8, (sx * 0.8, 0.0, 1.88)))
    # pedestal (stepped, wide)
    for i, (w, d, h) in enumerate([(3.0, 2.4, 0.22), (2.6, 2.05, 0.2), (2.25, 1.75, 0.2)]):
        ex.add(box_sub((0, 0.0, -0.1 - i * 0.2 + 0.1), (w * 1.0, d, h), 6))
    return ex

def make_statue():
    log("statue: metaballs...")
    m = build_statue(); log("  raw", m.tris()); m = decimate_mesh(m, 42000); log("  decimated", m.tris())
    ex = statue_extras(); ex = Mesh(ex.P, ex.F)
    ex_subd = ex
    full = Mesh(m.P, m.F); full = weather_displace(full, 0.05, 6.0, 4)
    # sit on its pedestal: lift to base z=0 and place
    allm = Mesh().add(full).add(ex_subd); lo = allm.P[:, 2].min(); allm = allm.xf(None, (0, 0, -lo - 0.55))
    R = rotz(0.0); allm = allm.xf(R, STATUE_C + np.array([0, 0, 0]), SC)
    allm.P[:, 2] += 0.0; return allm

# ------------------------------------------------------------------ temple pyramid
def make_temple():
    t = Mesh(); half = [17, 14.5, 12, 9.5, 7.2]; hh = 4.0
    for i, hw in enumerate(half):
        t.add(box_sub((0, 0, hh * (i + 0.5)), (hw * 2, hw * 2, hh), 16))
    # stairs on the +Y... (faces the player at -Y side of the pyramid is toward the origin; stairs on all four)
    for s in range(4):
        ang = s * math.pi / 2; M = rotz(ang)
        for k in range(20):
            w = 4.2; y = -(17 + 1.4) + k * (17 - 7.2 + 1.2) / 20 * 1.0; z = (k + 0.5) * 0.99 * (hh * 5 / 20)
            st = box_sub((0, y * 0 - (17 - k * 0.55) - 0.35, z), (w, 1.2, 0.4), 2); t.add(st.xf(M))
    # shrine on top
    t.add(box_sub((0, 0, hh * 5 + 2.2), (9.5, 9.5, 4.4), 12)); t.add(box_sub((0, 0, hh * 5 + 5.0), (11, 11, 1.2), 10))
    t.add(box_sub((0, 0, hh * 5 + 7.0), (6.5, 6.5, 3.2), 8)); t.add(lathe([(3.8, 0), (3.4, 1.0), (2.2, 2.0), (0.9, 3.2), (0, 4.0)], 4, (0, 0, hh * 5 + 8.6)))
    # corner serpent-like columns + steps ornaments
    for sx in (-1, 1):
        for sy in (-1, 1): t.add(lathe([(0.9, 0), (0.7, 1.4), (0.8, 2.6), (0.5, 3.2)], 8, (sx * 17.5, sy * 17.5, 0.0)))
    return t

# ------------------------------------------------------------------ broken arched bridge from the courtyard to the shelf
def make_bridge():
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

# ------------------------------------------------------------------ baking (AO + sun shadow through a BVH of everything static)
def build_bvh(meshes):
    P = np.vstack([m.P for m in meshes]); F = []; off = 0
    for m in meshes: F.append(m.F + off); off += len(m.P)
    F = np.vstack(F); return BVHTree.FromPolygons([tuple(p) for p in P], [tuple(f) for f in F], epsilon=0.0)
HEM = None
def hemi(k):
    global HEM
    if HEM is None:
        g = np.random.default_rng(5); u = g.random(k); v = g.random(k); r = np.sqrt(u); a = 2 * np.pi * v; HEM = np.stack([r * np.cos(a), r * np.sin(a), np.sqrt(1 - u)], 1)
    return HEM
def bake(m, bvh, ao_rays=9, ao_len=7.0, far=170.0, shadow_len=900.0):
    P = m.P; N = m.normals(); nv = len(P); ao = np.ones(nv); sv = np.ones(nv); dirs = hemi(ao_rays); sunV = Vector(SUN.tolist())
    for i in range(nv):
        n = N[i]; p = P[i]; org = Vector((p + n * 0.12).tolist())
        ndl = float(n @ SUN)
        if ndl <= 0: sv[i] = 0.0
        else:
            hit = bvh.ray_cast(org, sunV, shadow_len); sv[i] = 0.0 if hit[0] is not None else 1.0
        if abs(p[0]) + abs(p[1]) > far: continue
        u, v, d = basis_from_dir(n); hits = 0
        for k in range(ao_rays):
            dv = u * dirs[k, 0] + v * dirs[k, 1] + d * dirs[k, 2]
            if bvh.ray_cast(org, Vector(dv.tolist()), ao_len)[0] is not None: hits += 1
        ao[i] = 1.0 - hits / ao_rays
        if i % 20000 == 0: log("   baked", i, "/", nv)
    return ao, sv

def lit_preview(col, N, ao, sv, P):
    c = col ** 2.2; ndl = np.clip(N @ SUN, 0, 1); hemi_ = np.clip(N[:, 2] * 0.5 + 0.5, 0, 1)[:, None]
    amb = AMB_BOT * (1 - hemi_) + AMB_TOP * hemi_; L = c * (amb * (0.25 + 0.75 * ao[:, None]) * 1.1 + SUN_COL * (ndl * sv)[:, None] * 1.5 * (0.45 + 0.55 * ao[:, None]))
    d = np.linalg.norm(P - np.array([0, 0, 1.4]), axis=1); fog = 1 - np.exp(-d * 0.0016); fogc = np.array([0.62, 0.68, 0.66]); L = L * (1 - fog[:, None]) + fogc ** 2.2 * fog[:, None] * 0.9
    return np.clip(L / (1 + L * 0.35), 0, 1) ** (1 / 2.2)

# ------------------------------------------------------------------ Tripo assets (generated earlier, cleaned in Blender) placed into the baked scene
TRIPO = r'E:\Winter\metavr\TakeOne\Tools\tripo_out'; TEX = OUT
def import_fbx_mesh(name):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=os.path.join(TRIPO, name + ".fbx"))
    objs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']; P = []; F = []; U = []; off = 0
    for o in objs:
        me = o.data; me.calc_loop_triangles(); uv = me.uv_layers.active.data if me.uv_layers.active else None; Mw = np.array(o.matrix_world)
        co = np.array([v.co[:] for v in me.vertices]); co = (Mw[:3, :3] @ co.T).T + Mw[:3, 3]
        for lt in me.loop_triangles:
            for k in range(3):
                li = lt.loops[k]; P.append(co[me.loops[li].vertex_index]); U.append(uv[li].uv[:] if uv else (0, 0))
            F.append((off, off + 1, off + 2)); off += 3
    for o in objs: bpy.data.objects.remove(o)
    m = Mesh(np.array(P), np.array(F), U=np.array(U)); return m
_texcache = {}
def tex_sample(name, U):
    if name not in _texcache:
        img = bpy.data.images.load(os.path.join(TEX, name + "_a.png")); img.scale(256, 256); _texcache[name] = np.array(img.pixels[:], dtype=np.float64).reshape(256, 256, 4)[:, :, :3]
    im = _texcache[name]; x = np.clip((U[:, 0] % 1.0) * 255, 0, 255).astype(int); y = np.clip((U[:, 1] % 1.0) * 255, 0, 255).astype(int)
    return im[y, x]
def weld(m):    # merge identical positions+uvs so smoothing and file size are sane
    key = np.round(np.concatenate([m.P * 1000, m.U * 4000], 1)).astype(np.int64); _, idx, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    return Mesh(m.P[idx], inv[m.F], U=m.U[idx])
def fit(m, height, yaw=0.0, base=(0, 0, 0)):
    lo = m.P.min(0); hi = m.P.max(0); c = np.array([(lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2]]); k = height / max(hi[2] - lo[2], 1e-6)
    return Mesh((m.P - c) * k, m.F.copy(), m.C.copy(), m.U.copy()).xf(rotz(yaw), base)

A = {n: weld(import_fbx_mesh(n)) for n in ("GiantGuardian", "TempleArch", "TTree_A", "TTree_B", "TRock_A", "TCliff_A")}
for n, m in A.items(): log("tripo", n, m.tris(), "tris", np.round(m.P.max(0) - m.P.min(0), 1))
STATUE_YAW = float(os.environ.get("STATUE_YAW", "0"))
statue = fit(A["GiantGuardian"], 38.0, STATUE_YAW, STATUE_C)
arches = Mesh()
def zdeck(y): return 5.0 * ((y - 6.4) / 33.6) ** 2
for y in (24.0, 37.0): arches.add(fit(A["TempleArch"], 7.4, 0.0, (0, y, zdeck(y) - 0.2)))
temple = make_temple(); temple = weather_displace(temple, 0.5, 1.2, 8); temple = temple.xf(None, TEMPLE_C)
bridge = make_bridge(); bridge = weather_displace(bridge, 0.18, 1.6, 12); log("bridge tris", bridge.tris())

# scatter: trees on the valley floor and gentle slopes, rocks everywhere, cliff accents at the foot of the walls
def slope_at(x, y):
    e = 1.0; dx = (terrain_h(x + e, y) - terrain_h(x - e, y)) / (2 * e); dy = (terrain_h(x, y + e) - terrain_h(x, y - e)) / (2 * e); return np.hypot(dx, dy)
inst = {"TTree_A": [], "TTree_B": [], "TRock_A": [], "TCliff_A": []}
g = np.random.default_rng(23); tries = 0
def ok_tree(x, y, z):
    r = math.hypot(x, y); dr = min(seg_dist(np.array([x]), np.array([y]), RIVER_A)[0], seg_dist(np.array([x]), np.array([y]), RIVER_B)[0])
    if r < 38 or r > 190 or dr < 7: return False
    if abs(x) < 9 and 0 < y < 50: return False
    if math.hypot(x - STATUE_C[0], y - STATUE_C[1]) < 30 or math.hypot(x - TEMPLE_C[0], y - TEMPLE_C[1]) < 30: return False
    if z > 12 or slope_at(x, y) > 0.55: return False
    return True
while (len(inst["TTree_A"]) + len(inst["TTree_B"]) < 150) and tries < 20000:
    tries += 1; r = 38 + (g.random() ** 0.8) * 150; a = g.random() * 2 * math.pi; x, y = r * math.sin(a), r * math.cos(a); z = float(terrain_h(x, y))
    if ok_tree(x, y, z): inst["TTree_A" if g.random() < 0.5 else "TTree_B"].append((x, y, z - 0.4, g.random() * 6.28, 0.75 + g.random() * 0.8))
tries = 0
while len(inst["TRock_A"]) < 90 and tries < 20000:
    tries += 1; r = 12 + g.random() * 150; a = g.random() * 2 * math.pi; x, y = r * math.sin(a), r * math.cos(a); z = float(terrain_h(x, y))
    if abs(x) < 6 and 0 < y < 50: continue
    if math.hypot(x - STATUE_C[0], y - STATUE_C[1]) < 24: continue
    inst["TRock_A"].append((x, y, z - 0.5, g.random() * 6.28, 0.25 + g.random() ** 2 * 1.2))
tries = 0
while len(inst["TCliff_A"]) < 16 and tries < 20000:
    tries += 1; r = 80 + g.random() * 40; a = g.random() * 2 * math.pi; x, y = r * math.sin(a), r * math.cos(a); z = float(terrain_h(x, y))
    if slope_at(x, y) > 0.3 and z < 20: inst["TCliff_A"].append((x, y, z - 1.0, g.random() * 6.28, 1.4 + g.random() * 1.6))
log("instances", {k: len(v) for k, v in inst.items()})

bvh = build_bvh([terrain, statue, arches, temple, bridge]); log("bvh built")
parts = {}
treepts = np.array([(x, y) for k in ("TTree_A", "TTree_B") for (x, y, z, yw, sc_) in inst[k]])
for name, m in (("statue", statue), ("arches", arches), ("temple", temple), ("bridge", bridge), ("terrain", terrain)):
    N = m.normals(); wet = np.zeros(len(m.P))
    if name == "terrain": col, wet = colour_terrain(m)
    elif name == "statue": col = tex_sample("GiantGuardian", m.U)
    elif name == "arches": col = tex_sample("TempleArch", m.U)
    elif name == "temple": col, _ = weather(m.P, N, (0.50, 0.46, 0.38), 1.1, 6, 0.1)
    else: col, _ = weather(m.P, N, (0.50, 0.46, 0.38), 0.3, 9, 0.12)
    log("baking", name, len(m.P)); ao, sv = bake(m, bvh, ao_rays=9 if name != "terrain" else 6, far=170.0 if name != "terrain" else 130.0)
    if name == "terrain":
        for (tx, ty) in treepts: d = np.hypot(m.P[:, 0] - tx, m.P[:, 1] - ty); ao *= 1 - 0.5 * np.exp(-(d / 7.5) ** 2)
    m.C = np.concatenate([col if name in ("terrain", "temple", "bridge") else np.ones((len(col), 3)), ao[:, None]], 1); parts[name] = (m, col, ao, sv, wet)
    n_v, n_f = write_mesh(os.path.join(OUT, name + ".bytes"), m, sun=sv, wet=wet); log("wrote", name, n_v, n_f)

# instanced assets: unit meshes (origin at the base), AO baked against themselves
UNIT = {"TTree_A": 19.0, "TTree_B": 18.0, "TRock_A": 7.0, "TCliff_A": 22.0}
inst_out = {}
for n, hgt in UNIT.items():
    m = fit(A[n], hgt); b1 = build_bvh([m]); ao, sv = bake(m, b1, ao_rays=8, ao_len=4.0, far=1e9); m.C = np.concatenate([np.ones((len(m.P), 3)), ao[:, None]], 1)
    write_mesh(os.path.join(OUT, n + ".bytes"), m, sun=np.ones(len(m.P))); inst_out[n] = [float(v) for (x, y, z, yw, sc_) in inst[n] for v in (x, z, y, yw, sc_)]; parts["i_" + n] = (m, tex_sample(n, m.U), ao, np.ones(len(m.P)), np.zeros(len(m.P)))
json.dump(inst_out, open(os.path.join(OUT, "instances.json"), "w"))

def U(p): return [float(p[0]), float(p[2]), float(p[1])]
falls = []
for pp in FALL_PHI:
    pts = []
    for k in range(48):
        r = 108 - k * (108 - 85) / 47; x, y = r * math.sin(pp), r * math.cos(pp); pts.append(U((x, y, float(terrain_h(x, y)) + 0.9)))
    falls.append({"p": [float(c) for q in pts for c in q]})
rivers = []
for poly in (RIVER_A, RIVER_B):
    dense = []
    for (x0, y0), (x1, y1) in zip(poly[:-1], poly[1:]):
        for t in np.linspace(0, 1, 8, endpoint=False): dense.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    dense.append(poly[-1]); rivers.append({"p": [float(c) for (x, y) in dense for c in U((x, y, float(terrain_h(x, y)) + 1.25))]})
anchors = {"sun": U(SUN), "statue": U(STATUE_C), "temple": U(TEMPLE_C), "falls": falls, "rivers": rivers, "courtyard": U((0, 0, 0)), "floor": -18.0}
json.dump(anchors, open(os.path.join(OUT, "anchors.json"), "w")); log("anchors written; rendering previews")

# ------------------------------------------------------------------ preview
import mathutils
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.render.resolution_x = 1280; sc.render.resolution_y = 720
sc.display.shading.light = 'FLAT'; sc.display.shading.color_type = 'VERTEX'; sc.display.shading.show_cavity = False; sc.view_settings.view_transform = 'Standard'
if sc.world is None: sc.world = bpy.data.worlds.new('w')
sc.world.color = (0.55, 0.66, 0.78)
def preview_obj(name, m, col, ao, sv):
    N = m.normals(); L = lit_preview(col, N, ao, sv, m.P)
    me = bpy.data.meshes.new(name); me.from_pydata(m.P.tolist(), [], m.F.tolist()); me.update(); ca = me.color_attributes.new('Col', 'FLOAT_COLOR', 'POINT'); ca.data.foreach_set('color', np.concatenate([L, np.ones((len(L), 1))], 1).ravel())
    ob = bpy.data.objects.new(name, me); sc.collection.objects.link(ob); return ob
for name, (m, col, ao, sv, wet) in parts.items():
    if name.startswith("i_"):
        k = name[2:]
        for (x, y, z, yw, sc_) in inst[k]: mm = m.xf(rotz(yw), (x, y, z), sc_); preview_obj(name, mm, col, ao, np.ones(len(ao)))
    else: preview_obj(name, m, col, ao, sv)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam; cam.data.clip_end = 2000
def shot(name, loc, target, lens=18):
    cam.location = loc; cam.data.lens = lens; d = mathutils.Vector(target) - mathutils.Vector(loc); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(PREV, name + ".png"); bpy.ops.render.render(write_still=True)
shot("front", (0, 0, 1.4), (0, 60, 18), 20); shot("statue_close", (0, 26, 7.0), (0, 62, 24), 24); shot("left", (0, 0, 1.4), (-60, 30, 8), 20); shot("right", (0, 0, 1.4), (70, 30, 5), 20); shot("back", (0, 0, 1.4), (-60, -50, 6), 20)
shot("aerial", (0, -170, 150), (0, 40, 0), 30)
log("done")
