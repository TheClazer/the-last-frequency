"""A standing stag (metaball body -> mesh, decimated, antlers as tapered tubes) written in the Unity runtime format (deer.bytes).
Origin at the hooves, facing +Y (Blender) = +Z (Unity), ~1.6 m at the back, 2.3 m with antlers."""
import bpy, bmesh, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from jungle_lib import *
OUT = r'E:\Winter\metavr\LastFrequency\Assets\Resources\Jungle'

def build():
    mb = bpy.data.metaballs.new('D'); mb.resolution = 0.035; mb.render_resolution = 0.035; mb.threshold = 0.5
    ob = bpy.data.objects.new('D', mb); bpy.context.scene.collection.objects.link(ob)
    def E(co, size=(1, 1, 1), rad=0.2, neg=False, stiff=2.0):
        e = mb.elements.new(type='ELLIPSOID'); e.co = co; e.radius = rad; e.size_x, e.size_y, e.size_z = size; e.stiffness = stiff; e.use_negative = neg
    def chain(pts, r0, r1, n=5):
        for i in range(len(pts) - 1):
            for k in range(n):
                t = k / n; p = np.array(pts[i]) * (1 - t) + np.array(pts[i + 1]) * t; rr = r0 + (r1 - r0) * ((i + t) / (len(pts) - 1)); E(tuple(p), rad=rr, stiff=2.4)
    # torso (long barrel), chest, haunch
    E((0, 0.0, 1.05), (1.0, 1.75, 0.8), 0.34); E((0, 0.45, 1.12), (0.85, 1.0, 0.9), 0.32); E((0, -0.5, 1.08), (0.9, 1.0, 0.95), 0.32); E((0, 0.0, 0.92), (0.9, 1.5, 0.6), 0.28)
    # neck up and forward, head, muzzle, ears
    chain([(0, 0.62, 1.2), (0, 0.8, 1.5), (0, 0.9, 1.72)], 0.18, 0.12); E((0, 1.0, 1.82), (0.8, 1.5, 0.8), 0.13); E((0, 1.2, 1.76), (0.55, 1.2, 0.5), 0.07)
    for sx in (-1, 1): E((sx * 0.13, 0.92, 1.9), (0.35, 0.6, 1.5), 0.07, stiff=3)
    # legs: four, slightly bent
    for (sx, sy) in ((-1, 0.5), (1, 0.5), (-1, -0.55), (1, -0.55)):
        top = (sx * 0.16, sy, 0.95); knee = (sx * 0.15, sy + (0.04 if sy > 0 else -0.06), 0.52); hoof = (sx * 0.14, sy + (0.02 if sy > 0 else 0.0), 0.04)
        chain([top, knee, hoof], 0.13, 0.045, 5)
    E((0, -0.82, 1.1), (0.4, 0.8, 0.4), 0.09)    # tail
    mb.update_tag(); bpy.context.view_layer.update(); bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH'); ob = bpy.context.view_layer.objects.active; dg = bpy.context.evaluated_depsgraph_get(); me = ob.evaluated_get(dg).to_mesh()
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm, faces=bm.faces[:]); P = np.array([v.co[:] for v in bm.verts]); F = np.array([[v.index for v in f.verts] for f in bm.faces]); bm.free(); bpy.data.objects.remove(ob)
    return Mesh(P, F)
body = build(); print("deer raw tris", body.tris())
me = bpy.data.meshes.new('d'); me.from_pydata(body.P.tolist(), [], body.F.tolist()); me.update(); o = bpy.data.objects.new('d', me); bpy.context.scene.collection.objects.link(o)
mod = o.modifiers.new('dec', 'DECIMATE'); mod.ratio = min(1.0, 9000 / max(len(body.F), 1)); dg = bpy.context.evaluated_depsgraph_get(); e = o.evaluated_get(dg).to_mesh(); e.calc_loop_triangles()
P = np.array([v.co[:] for v in e.vertices]); F = np.array([t.vertices[:] for t in e.loop_triangles]); body = Mesh(P, F)
# antlers: branching tapered tubes
ant = Mesh(); rng = np.random.default_rng(4)
for sx in (-1, 1):
    base = np.array([sx * 0.07, 1.0, 1.95]); beam = [base, base + (sx * 0.15, -0.1, 0.25), base + (sx * 0.3, -0.18, 0.55), base + (sx * 0.33, -0.05, 0.85), base + (sx * 0.28, 0.12, 1.1)]
    ant.add(tube(beam, [0.028, 0.024, 0.02, 0.016, 0.008], 6))
    for k, tt in enumerate((0.3, 0.55, 0.8)):
        i = int(tt * 4); a = beam[i]; b = a + np.array([sx * 0.12, 0.1 + 0.05 * k, 0.22 + 0.04 * k]); ant.add(tube([a, (a + b) / 2 + (sx * 0.03, 0.03, 0), b], [0.014, 0.011, 0.006], 5))
full = Mesh().add(body).add(ant)
# colour: coat with lighter belly/rump and darker back, antlers pale bone
N = full.normals(); z = full.P[:, 2]; y = full.P[:, 1]; n = fbm(full.P[:, 0] * 6 + y * 3, z * 6, 3, 5)
coat = np.tile(np.array([0.46, 0.29, 0.15]), (len(full.P), 1)) * (0.8 + 0.4 * n[:, None]); belly = np.clip((0.95 - z) * 3, 0, 1) * np.clip(1 - np.abs(full.P[:, 0]) * 3, 0, 1); coat = coat * (1 - belly[:, None]) + np.array([0.78, 0.66, 0.5]) * belly[:, None]
rump = np.clip((-y - 0.55) * 4, 0, 1) * np.clip((z - 0.8) * 4, 0, 1); coat = coat * (1 - rump[:, None] * 0.6) + np.array([0.85, 0.78, 0.65]) * rump[:, None] * 0.6
isant = np.zeros(len(full.P), bool); isant[len(body.P):] = True; coat[isant] = np.array([0.72, 0.62, 0.45]); coat[(z < 0.1) & ~isant] = np.array([0.08, 0.06, 0.05])
sc = 1.0; full = full.xf(None, (0, 0, 0), sc); full.C = np.concatenate([coat, np.clip(0.55 + 0.45 * np.clip(z / 1.8, 0, 1), 0, 1)[:, None]], 1)
n_v, n_f = write_mesh(os.path.join(OUT, "deer.bytes"), full, sun=np.ones(len(full.P))); print("deer", n_v, n_f, full.P.min(0), full.P.max(0))
