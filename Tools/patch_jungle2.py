p = r'E:\Winter\metavr\LastFrequency\Art\scripts\build_jungle.py'
s = open(p, encoding='utf8').read()
cut = s.index("# ------------------------------------------------------------------ run")
head = s[:cut]
new = r'''# ------------------------------------------------------------------ Tripo assets (generated earlier, cleaned in Blender) placed into the baked scene
from PIL import Image
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
    if name not in _texcache: _texcache[name] = np.asarray(Image.open(os.path.join(TEX, name + "_a.png")).convert('RGB').resize((256, 256)), dtype=np.float64) / 255.0
    im = _texcache[name]; x = np.clip((U[:, 0] % 1.0) * 255, 0, 255).astype(int); y = np.clip((1 - (U[:, 1] % 1.0)) * 255, 0, 255).astype(int)
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
for y in (13.0, 25.0, 37.0): arches.add(fit(A["TempleArch"], 7.4, 0.0, (0, y, zdeck(y) - 0.2)))
temple = make_temple(); temple = weather_displace(temple, 0.5, 1.2, 8); temple = temple.xf(None, TEMPLE_C)
bridge = make_bridge(); bridge = weather_displace(bridge, 0.18, 1.6, 12); log("bridge tris", bridge.tris())

# scatter: trees on the valley floor and gentle slopes, rocks everywhere, cliff accents at the foot of the walls
def slope_at(x, y):
    e = 1.0; dx = (terrain_h(x + e, y) - terrain_h(x - e, y)) / (2 * e); dy = (terrain_h(x, y + e) - terrain_h(x, y - e)) / (2 * e); return np.hypot(dx, dy)
inst = {"TTree_A": [], "TTree_B": [], "TRock_A": [], "TCliff_A": []}
g = np.random.default_rng(23); tries = 0
def ok_tree(x, y, z):
    r = math.hypot(x, y); dr = min(seg_dist(np.array([x]), np.array([y]), RIVER_A)[0], seg_dist(np.array([x]), np.array([y]), RIVER_B)[0])
    if r < 14 or r > 190 or dr < 7: return False
    if abs(x) < 9 and 0 < y < 50: return False
    if math.hypot(x - STATUE_C[0], y - STATUE_C[1]) < 30 or math.hypot(x - TEMPLE_C[0], y - TEMPLE_C[1]) < 30: return False
    if z > 12 or slope_at(x, y) > 0.55: return False
    return True
while (len(inst["TTree_A"]) + len(inst["TTree_B"]) < 150) and tries < 20000:
    tries += 1; r = 14 + (g.random() ** 0.8) * 160; a = g.random() * 2 * math.pi; x, y = r * math.sin(a), r * math.cos(a); z = float(terrain_h(x, y))
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
    else: col, _ = weather(m.P, N, (0.46, 0.43, 0.36), 1.2, 9, 0.0)
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
    write_mesh(os.path.join(OUT, n + ".bytes"), m, sun=np.ones(len(m.P))); inst_out[n] = [[x, z, y, yw, sc_] for (x, y, z, yw, sc_) in inst[n]]; parts["i_" + n] = (m, tex_sample(n, m.U), ao, np.ones(len(m.P)), np.zeros(len(m.P)))
json.dump(inst_out, open(os.path.join(OUT, "instances.json"), "w"))

def U(p): return [float(p[0]), float(p[2]), float(p[1])]
falls = []
for pp in FALL_PHI:
    pts = []
    for k in range(48):
        r = 108 - k * (108 - 85) / 47; x, y = r * math.sin(pp), r * math.cos(pp); pts.append(U((x, y, float(terrain_h(x, y)) + 0.9)))
    falls.append(pts)
rivers = []
for poly in (RIVER_A, RIVER_B):
    dense = []
    for (x0, y0), (x1, y1) in zip(poly[:-1], poly[1:]):
        for t in np.linspace(0, 1, 8, endpoint=False): dense.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    dense.append(poly[-1]); rivers.append([U((x, y, float(terrain_h(x, y)) + 1.25)) for x, y in dense])
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
'''
open(p, 'w', encoding='utf8').write(head + new); print("patched jungle2")
