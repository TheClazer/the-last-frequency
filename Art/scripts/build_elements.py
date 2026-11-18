# Blender 4.5:  blender -b --factory-startup -P build_elements.py
# Smooth, organic meshes for the water, earth and fire elements (unit height, base at 0), written as LFM1 to Assets/Resources/Ice/*.bytes
import bpy, bmesh, math, random, struct, os
import numpy as np
from mathutils import Vector, noise

OUT = r'E:\Winter\metavr\LastFrequency\Assets\Resources\Ice'
os.makedirs(OUT, exist_ok=True)

def clear():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
    for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)

def export(bm, name, smooth=True, height=1.0, bevel=0.0):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    for p in me.polygons: p.use_smooth = smooth
    if bevel > 0: bv = ob.modifiers.new('bev', 'BEVEL'); bv.width = bevel; bv.segments = 2; bv.limit_method = 'ANGLE'; bv.angle_limit = math.radians(30)
    ob.modifiers.new('tri', 'TRIANGULATE')
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg); m2 = ev.to_mesh(); m2.calc_loop_triangles()
    co = np.array([v.co[:] for v in m2.vertices]); zmin, zmax = co[:, 2].min(), co[:, 2].max(); s = (height / (zmax - zmin)) if height else 1.0
    cx, cy = (co[:, 0].min() + co[:, 0].max()) / 2, (co[:, 1].min() + co[:, 1].max()) / 2
    nrm = np.array([c.vector[:] for c in m2.corner_normals])
    P = []; N = []; F = []
    for t in m2.loop_triangles:
        b = len(P)
        for li in t.loops:
            v = m2.vertices[m2.loops[li].vertex_index].co; P.append(((v.x - cx) * s, (v.y - cy) * s, (v.z - zmin) * s)); N.append(tuple(nrm[li]))
        F.append((b, b + 1, b + 2))
    ev.to_mesh_clear()
    P = np.array(P, 'f4'); N = np.array(N, 'f4'); F = np.array(F, 'u4')
    Pu = P[:, [0, 2, 1]].astype('<f4'); Nu = N[:, [0, 2, 1]].astype('<f4'); Fu = F[:, [0, 2, 1]].astype('<u4'); C = np.full((len(P), 4), 255, 'u1')
    U0 = np.stack([Pu[:, 0] + Pu[:, 2], Pu[:, 1]], 1).astype('<f4'); U1 = np.zeros((len(P), 2), '<f4')
    with open(os.path.join(OUT, name + '.bytes'), 'wb') as f:
        f.write(struct.pack('<4sII', b'LFM1', len(P), len(Fu))); f.write(Pu.tobytes()); f.write(Nu.tobytes()); f.write(C.tobytes()); f.write(U0.tobytes()); f.write(U1.tobytes()); f.write(Fu.tobytes())
    print('[ELEM]', name, len(P), 'verts', len(Fu), 'tris')

def lathe(profile, sides=24, seed=0, wob=0.0, wscale=2.0, twist=0.0, cap=True):
    """surface of revolution from (z, r) rings, optionally displaced by noise so nothing is perfectly round."""
    bm = bmesh.new(); rings = []
    for k, (z, r) in enumerate(profile):
        ring = []
        for i in range(sides):
            a = i / sides * 6.2832 + twist * z
            rr = r * (1.0 + wob * noise.noise(Vector((math.cos(a) * wscale, math.sin(a) * wscale, z * wscale + seed * 7.3))))
            ring.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        for i in range(sides):
            j = (i + 1) % sides; bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
    if cap:
        bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.normal_update(); return bm

def prof(fn, n=18, z0=0.0, z1=1.0):
    return [(z0 + (z1 - z0) * i / (n - 1), max(0.004, fn(i / (n - 1)))) for i in range(n)]

# ---------------------------------------------------------------- WATER: rising columns with bulges, and a drop
clear(); export(lathe(prof(lambda u: 0.20 * (1 - 0.55 * u) * (1 + 0.10 * math.sin(u * 9.0)) * (1 - u ** 6) + 0.02, 22), 28, 1, 0.06, 2.2, 0.8), 'WaterColumn_A')
clear(); export(lathe(prof(lambda u: 0.24 * (1 - 0.7 * u ** 1.5) * (1 + 0.09 * math.sin(u * 6.5 + 1.0)) * (1 - u ** 8) + 0.015, 22), 28, 2, 0.08, 1.8, -0.6), 'WaterColumn_B')
clear(); export(lathe(prof(lambda u: 0.34 * math.sin(math.pi * min(1.0, u * 1.25)) ** 0.8 * (1 - 0.5 * u) if u < 0.8 else 0.34 * (1 - u) * 1.6, 18), 24, 3, 0.04, 2.0), 'WaterDrop')

# ---------------------------------------------------------------- FIRE: flame tongues
clear(); export(lathe(prof(lambda u: 0.24 * math.sin(math.pi * (u ** 0.62)) * (1 - 0.35 * u), 20), 22, 4, 0.10, 2.4, 1.6), 'Flame_A')
clear(); export(lathe(prof(lambda u: 0.30 * math.sin(math.pi * (u ** 0.5)) * (1 - 0.5 * u) * (1 + 0.15 * math.sin(u * 8)), 20), 22, 5, 0.14, 2.0, -2.2), 'Flame_B')

# ---------------------------------------------------------------- EARTH: rock spires, boulders, basalt columns
def rock(name, seed, hgt, rad, flat=1.0, rough=0.32):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=4, radius=1.0)
    for v in bm.verts:
        p = v.co.copy(); n = p.normalized()
        d = noise.noise(n * 1.3 + Vector((seed, 0, 0))) * rough + noise.noise(n * 3.1 + Vector((0, seed, 0))) * rough * 0.45 + noise.noise(n * 7.5) * rough * 0.16
        strata = 0.035 * math.sin(p.z * 14.0 + noise.noise(n * 2.0) * 3.0)      # layered ledges
        r = 1.0 + d + strata
        v.co = Vector((n.x * r * rad, n.y * r * rad, n.z * r * hgt * flat))
    lo = min(v.co.z for v in bm.verts)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    cut = lo * 0.55; bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector((0, 0, cut)), plane_no=Vector((0, 0, 1)))   # sit flat on the ground
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < cut - 1e-4], context='VERTS')
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.normal_update(); export(bm, name)

clear(); rock('RockSpire_A', 1.0, 1.0, 0.30)
clear(); rock('RockSpire_B', 2.7, 1.0, 0.24, rough=0.4)
clear(); rock('RockBoulder_A', 4.1, 0.55, 0.62, rough=0.28)
clear(); rock('RockBoulder_B', 6.3, 0.45, 0.55, rough=0.36)

def basalt(name, seed, n):
    rnd = random.Random(seed); bm = bmesh.new()
    for i in range(n):
        a = rnd.uniform(0, 6.28); d = rnd.uniform(0, 0.42) * (1 if i else 0); L = 1.0 if i == 0 else rnd.uniform(0.3, 0.85); R = rnd.uniform(0.09, 0.13)
        base = Vector((math.cos(a) * d, math.sin(a) * d, 0)); ang0 = rnd.uniform(0, 1.0); ring0 = []; ring1 = []
        tilt = Vector((rnd.uniform(-0.05, 0.05), rnd.uniform(-0.05, 0.05), 0))
        for k in range(6):
            aa = ang0 + k / 6 * 6.2832; o = Vector((math.cos(aa) * R, math.sin(aa) * R, 0))
            ring0.append(bm.verts.new(base + o)); ring1.append(bm.verts.new(base + o * rnd.uniform(0.9, 1.0) + Vector((0, 0, L + rnd.uniform(-0.02, 0.02))) + tilt * L))
        for k in range(6):
            j = (k + 1) % 6; bm.faces.new((ring0[k], ring0[j], ring1[j], ring1[k]))
        bm.faces.new(ring1); bm.faces.new(list(reversed(ring0)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.normal_update(); export(bm, name, smooth=False, bevel=0.012)
clear(); basalt('Basalt_A', 3, 14)
clear(); basalt('Basalt_B', 8, 9)

# ---------------------------------------------------------------- shared: a smooth sphere (buds, embers)
clear(); bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=3, radius=0.5); bm.normal_update(); export(bm, 'ElemSphere', height=None)
print('[ELEM] done')
