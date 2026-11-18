# Blender 4.5:  blender -b --factory-startup -P build_ice.py
# Builds realistic ice meshes (crystal clusters, fractured chunks, stair steps) with bevelled edges and correct split normals,
# and writes them in the LFM1 runtime format (Unity axes) to Assets/Resources/Ice/*.bytes
import bpy, bmesh, math, random, struct, os
import numpy as np
from mathutils import Vector, Matrix, noise

OUT = r'E:\Winter\metavr\LastFrequency\Assets\Resources\Ice'
os.makedirs(OUT, exist_ok=True)

def clear():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete()
    for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)

def finish(bm, name, height=None, bevel=0.012, segs=2, disp=0.0, dscale=3.0):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    if disp > 0:   # irregular, hand-of-nature surface: subdivide a little then push vertices with noise
        sm = ob.modifiers.new('sub', 'SUBSURF'); sm.levels = 1; sm.subdivision_type = 'SIMPLE'
        bpy.ops.object.modifier_apply(modifier=sm.name)
        for v in me.vertices:
            p = v.co * dscale; d = noise.noise(p) * disp
            v.co += v.normal * d
    bv = ob.modifiers.new('bev', 'BEVEL'); bv.width = bevel; bv.segments = segs; bv.limit_method = 'ANGLE'; bv.angle_limit = math.radians(25)
    tr = ob.modifiers.new('tri', 'TRIANGULATE')
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg); m2 = ev.to_mesh()
    m2.calc_loop_triangles()
    # scale to requested height (Z up), base at z=0, centred in XY
    co = np.array([v.co[:] for v in m2.vertices]); zmin, zmax = co[:, 2].min(), co[:, 2].max()
    s = (height / (zmax - zmin)) if height else 1.0
    cx, cy = (co[:, 0].min() + co[:, 0].max()) / 2, (co[:, 1].min() + co[:, 1].max()) / 2
    nrm = np.array([c.vector[:] for c in m2.corner_normals]) if hasattr(m2, 'corner_normals') else None
    P = []; N = []; F = []
    for t in m2.loop_triangles:
        base = len(P)
        for li in t.loops:
            vi = m2.loops[li].vertex_index; v = m2.vertices[vi].co
            P.append(((v.x - cx) * s, (v.y - cy) * s, (v.z - zmin) * s)); N.append(tuple(nrm[li]) if nrm is not None else tuple(t.split_normals[0]))
        F.append((base, base + 1, base + 2))
    ev.to_mesh_clear()
    P = np.array(P, 'f4'); N = np.array(N, 'f4'); F = np.array(F, 'u4')
    return write(name, P, N, F)

def write(name, P, N, F):
    Pu = P[:, [0, 2, 1]].astype('<f4'); Nu = N[:, [0, 2, 1]].astype('<f4'); Fu = F[:, [0, 2, 1]].astype('<u4')
    C = np.full((len(P), 4), 255, 'u1')
    # uv0: simple triplanar-ish (x+z, y) so the shader can read a stable coordinate; uv1 = 0
    U0 = np.stack([Pu[:, 0] + Pu[:, 2], Pu[:, 1]], 1).astype('<f4'); U1 = np.zeros((len(P), 2), '<f4')
    with open(os.path.join(OUT, name + '.bytes'), 'wb') as f:
        f.write(struct.pack('<4sII', b'LFM1', len(P), len(Fu))); f.write(Pu.tobytes()); f.write(Nu.tobytes()); f.write(C.tobytes()); f.write(U0.tobytes()); f.write(U1.tobytes()); f.write(Fu.tobytes())
    print('[ICE]', name, len(P), 'verts', len(Fu), 'tris')
    return len(Fu)

def crystal(bm, base, axis, length, radius, sides=6, taper=0.82, tip=0.32, rnd=None):
    """one hexagonal crystal: prism that tapers slightly, then a faceted pyramidal tip with an off-axis apex (real crystals are never symmetric)."""
    axis = axis.normalized(); up = Vector((0, 0, 1)); x = axis.cross(up) if abs(axis.dot(up)) < 0.99 else Vector((1, 0, 0)); x.normalize(); y = axis.cross(x)
    rings = []
    ang0 = rnd.uniform(0, 6.28)
    for k, (t, r) in enumerate([(0.0, radius), (1.0 - tip, radius * taper), (1.0 - tip * 0.35, radius * taper * 0.78)]):
        ring = []
        for i in range(sides):
            a = ang0 + i / sides * 6.2832 + rnd.uniform(-0.05, 0.05)
            rr = r * (1 + rnd.uniform(-0.07, 0.07))
            ring.append(bm.verts.new(base + axis * (length * t) + x * math.cos(a) * rr + y * math.sin(a) * rr))
        rings.append(ring)
    apex = bm.verts.new(base + axis * length + x * rnd.uniform(-0.18, 0.18) * radius + y * rnd.uniform(-0.18, 0.18) * radius)
    cap = bm.verts.new(base - axis * radius * 0.2)
    for k in range(2):
        for i in range(sides):
            j = (i + 1) % sides; bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
    for i in range(sides):
        j = (i + 1) % sides; bm.faces.new((rings[2][i], rings[2][j], apex)); bm.faces.new((rings[0][j], rings[0][i], cap))

def cluster(name, seed, n, height, spread=0.45, bevel=0.01):
    rnd = random.Random(seed); bm = bmesh.new()
    for i in range(n):
        a = rnd.uniform(0, 6.28); tilt = rnd.uniform(0.0, spread) * (1.0 if i else 0.0)
        axis = Vector((math.sin(tilt) * math.cos(a), math.sin(tilt) * math.sin(a), math.cos(tilt)))
        d = rnd.uniform(0.0, 0.22 * height) * (1 if i else 0); base = Vector((math.cos(a + 1.1) * d, math.sin(a + 1.1) * d, 0))
        L = height * (1.0 if i == 0 else rnd.uniform(0.22, 0.78)); R = L * rnd.uniform(0.07, 0.12)
        crystal(bm, base, axis, L, R, sides=rnd.choice([5, 6, 6, 7]), rnd=rnd)
    bm.normal_update(); return finish(bm, name, height=height, bevel=bevel * height, segs=2, disp=0.0)

def chunk(name, seed, size, flat=0.7, cuts=9, bevel=0.03):
    """fractured ice boulder: noisy sphere sliced by random planes (conchoidal fracture), then bevelled."""
    rnd = random.Random(seed); bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=3, radius=size)
    for v in bm.verts:
        d = noise.noise(v.co * 1.6 / size + Vector((seed, 0, 0))) * 0.22 * size; v.co *= (1 + d / size)
        v.co.z *= flat
    for i in range(cuts):
        n = Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(-0.2, 1))).normalized(); off = size * rnd.uniform(0.45, 0.8)
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=n * off, plane_no=n, clear_outer=True)
        bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary]) if any(e.is_boundary for e in bm.edges) else None
    bm.normal_update(); return finish(bm, name, height=None, bevel=bevel * size, segs=2, disp=0.0)

def steps(name, n=22):
    """chunky, slightly irregular ice steps (matches the runtime stair path: radius grows along the climb)."""
    rnd = random.Random(3); bm = bmesh.new()
    for i in range(n):
        w = 1.9 + 0.0 * i; h = 0.28; d = 0.62
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation((0, i * d, i * h)) @ Matrix.Diagonal((w * rnd.uniform(0.96, 1.04), d * 1.02, h * 3.2, 1.0)))
    bm.normal_update(); return finish(bm, name, height=None, bevel=0.03, segs=2)

clear(); cluster('IceCrystal_A', 11, 13, 1.0); clear(); cluster('IceCrystal_B', 23, 9, 1.0, spread=0.3); clear(); cluster('IceCrystal_C', 37, 17, 1.0, spread=0.6)
clear(); chunk('IceChunk_A', 5, 0.5, 0.75); clear(); chunk('IceChunk_B', 9, 0.5, 0.55, cuts=11); clear(); chunk('IceChunk_C', 14, 0.5, 0.9, cuts=7)
print('[ICE] done')
