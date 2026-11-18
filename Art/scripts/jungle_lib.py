"""Numpy mesh toolkit + noise for the Jungle theme. Blender (Z-up, +Y forward) coordinates throughout; the writer converts to Unity (x, z, y)."""
import numpy as np, struct, math

# ------------------------------------------------------------------ noise
def hash2(ix, iy, seed):
    n = (ix.astype(np.int64) * 374761393 + iy.astype(np.int64) * 668265263 + int(seed) * 1442695041) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    n = n ^ (n >> 16)
    return (n & 0xFFFF) / 65535.0
def vnoise(x, y, seed=0):
    x = np.asarray(x, dtype=np.float64); y = np.asarray(y, dtype=np.float64)
    ix = np.floor(x); iy = np.floor(y); fx = x - ix; fy = y - iy; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    a = hash2(ix, iy, seed); b = hash2(ix + 1, iy, seed); c = hash2(ix, iy + 1, seed); d = hash2(ix + 1, iy + 1, seed)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy
def fbm(x, y, octv=5, seed=0, lac=2.0, gain=0.5):
    s = 0.0; a = 0.5; tot = 0.0
    for o in range(octv):
        s = s + a * vnoise(x, y, seed + o * 17); tot += a; x = x * lac + 31.7; y = y * lac + 17.3; a *= gain
    return s / tot
def ridged(x, y, octv=4, seed=0):
    s = 0.0; a = 0.5; tot = 0.0
    for o in range(octv):
        n = 1.0 - np.abs(vnoise(x, y, seed + o * 13) * 2 - 1); s = s + a * n * n; tot += a; x = x * 2.1 + 11.1; y = y * 2.1 + 5.3; a *= 0.5
    return s / tot
def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)
def lerp(a, b, t): return a + (b - a) * t
def seg_dist(X, Y, poly):
    d = np.full(X.shape, 1e9)
    for (x0, y0), (x1, y1) in zip(poly[:-1], poly[1:]):
        vx, vy = x1 - x0, y1 - y0; L2 = vx * vx + vy * vy + 1e-9
        t = np.clip(((X - x0) * vx + (Y - y0) * vy) / L2, 0, 1); px = x0 + t * vx; py = y0 + t * vy
        d = np.minimum(d, np.hypot(X - px, Y - py))
    return d

# ------------------------------------------------------------------ mesh container
class Mesh:
    def __init__(s, P=None, F=None, C=None, U=None):
        s.P = np.zeros((0, 3)) if P is None else np.asarray(P, float); s.F = np.zeros((0, 3), int) if F is None else np.asarray(F, int)
        s.C = np.zeros((len(s.P), 4)) if C is None else np.asarray(C, float); s.U = np.zeros((len(s.P), 2)) if U is None else np.asarray(U, float)
    def add(s, o):
        off = len(s.P); s.P = np.vstack([s.P, o.P]); s.F = np.vstack([s.F, o.F + off]); s.C = np.vstack([s.C, o.C]); s.U = np.vstack([s.U, o.U]); return s
    def xf(s, M=None, t=(0, 0, 0), sc=1.0):
        P = s.P * np.asarray(sc)
        if M is not None: P = P @ np.asarray(M).T
        return Mesh(P + np.asarray(t), s.F.copy(), s.C.copy(), s.U.copy())
    def normals(s):
        P, F = s.P, s.F
        fn = np.cross(P[F[:, 1]] - P[F[:, 0]], P[F[:, 2]] - P[F[:, 0]]); N = np.zeros_like(P)
        for k in range(3): np.add.at(N, F[:, k], fn)
        L = np.linalg.norm(N, axis=1, keepdims=True); L[L < 1e-12] = 1; return N / L
    def tris(s): return len(s.F)

def rotz(a): c, s_ = math.cos(a), math.sin(a); return np.array([[c, -s_, 0], [s_, c, 0], [0, 0, 1]])
def rotx(a): c, s_ = math.cos(a), math.sin(a); return np.array([[1, 0, 0], [0, c, -s_], [0, s_, c]])
def roty(a): c, s_ = math.cos(a), math.sin(a); return np.array([[c, 0, s_], [0, 1, 0], [-s_, 0, c]])
def basis_from_dir(d):
    d = np.asarray(d, float); d = d / (np.linalg.norm(d) + 1e-12); a = np.array([0, 0, 1.0]) if abs(d[2]) < 0.95 else np.array([1.0, 0, 0])
    u = np.cross(a, d); u /= np.linalg.norm(u); v = np.cross(d, u); return u, v, d

def grid(nu, nv, fn, close_u=False):
    """fn(u,v)->(x,y,z) arrays over a (nv,nu) grid; returns Mesh"""
    us = np.linspace(0, 1, nu); vs = np.linspace(0, 1, nv); U, V = np.meshgrid(us, vs); P = np.stack(fn(U, V), -1).reshape(-1, 3)
    F = []
    for j in range(nv - 1):
        for i in range(nu - 1):
            a = j * nu + i; b = a + 1; c = a + nu; d = c + 1; F += [(a, b, c), (b, d, c)]
    return Mesh(P, np.array(F), U=np.stack([U.ravel(), V.ravel()], 1))

def tube(path, radii, sides=8, closed_cap=True):
    """path (n,3), radii (n,) -> tube mesh"""
    path = np.asarray(path, float); n = len(path); P = []; F = []; U = []
    for i in range(n):
        t = path[min(i + 1, n - 1)] - path[max(i - 1, 0)]; u, v, d = basis_from_dir(t)
        for k in range(sides):
            a = k / sides * 2 * math.pi; P.append(path[i] + (u * math.cos(a) + v * math.sin(a)) * radii[i]); U.append((k / sides, i / (n - 1)))
    for i in range(n - 1):
        for k in range(sides):
            a = i * sides + k; b = i * sides + (k + 1) % sides; c = a + sides; d = b + sides; F += [(a, c, b), (b, c, d)]
    if closed_cap:
        P.append(path[-1]); ci = len(P) - 1
        for k in range(sides): F.append(((n - 1) * sides + k, ci, (n - 1) * sides + (k + 1) % sides))
        U.append((0.5, 1))
    return Mesh(np.array(P), np.array(F), U=np.array(U))

def box_sub(c, size, sub=6, rot=None):
    """box with subdivided faces (so noise displacement has vertices to move)"""
    hx, hy, hz = np.asarray(size) * 0.5; meshes = Mesh()
    faces = [((-hx, -hy, hz), (2 * hx, 0, 0), (0, 2 * hy, 0)), ((-hx, -hy, -hz), (0, 2 * hy, 0), (2 * hx, 0, 0)), ((-hx, -hy, -hz), (2 * hx, 0, 0), (0, 0, 2 * hz)),
             ((-hx, hy, -hz), (0, 0, 2 * hz), (2 * hx, 0, 0)), ((-hx, -hy, -hz), (0, 0, 2 * hz), (0, 2 * hy, 0)), ((hx, -hy, -hz), (0, 2 * hy, 0), (0, 0, 2 * hz))]
    for o, a, b in faces:
        o = np.array(o); a = np.array(a); b = np.array(b)
        m = grid(sub + 1, sub + 1, lambda u, v: (o[0] + a[0] * u + b[0] * v, o[1] + a[1] * u + b[1] * v, o[2] + a[2] * u + b[2] * v)); meshes.add(m)
    out = meshes.xf(rot) if rot is not None else meshes
    return out.xf(None, c)

def lathe(profile, seg=24, c=(0, 0, 0), sc=1.0):
    prof = np.asarray(profile, float); n = len(prof); P = []; F = []; U = []
    for i in range(n):
        for k in range(seg):
            a = k / seg * 2 * math.pi; P.append((prof[i, 0] * math.cos(a), prof[i, 0] * math.sin(a), prof[i, 1])); U.append((k / seg, i / max(n - 1, 1)))
    for i in range(n - 1):
        for k in range(seg):
            a = i * seg + k; b = i * seg + (k + 1) % seg; cc = a + seg; d = b + seg; F += [(a, b, cc), (b, d, cc)]
    return Mesh(np.array(P), np.array(F), U=np.array(U)).xf(None, c, sc)

def torus(c, R, r, seg=24, tseg=8, rot=None):
    def fn(u, v):
        a = u * 2 * math.pi; b = v * 2 * math.pi; return ((R + r * np.cos(b)) * np.cos(a), (R + r * np.cos(b)) * np.sin(a), r * np.sin(b))
    m = grid(seg + 1, tseg + 1, fn)
    if rot is not None: m = m.xf(rot)
    return m.xf(None, c)

def card(c, w, h, rot=None, uv0=(0, 0), uv1=(1, 1), bend=0.0):
    """vertical quad (width along X, height along Z) centered at c; optional bend toward +Y for arching leaves"""
    ns = 4; us = np.linspace(-0.5, 0.5, 2); vs = np.linspace(0, 1, ns); P = []; U = []
    for v in vs:
        for u in us:
            P.append((u * w, bend * v * v * h, v * h)); U.append((uv0[0] + (u + 0.5) * (uv1[0] - uv0[0]), uv0[1] + v * (uv1[1] - uv0[1])))
    F = []
    for j in range(ns - 1):
        a = j * 2; F += [(a, a + 1, a + 2), (a + 1, a + 3, a + 2)]
    m = Mesh(np.array(P), np.array(F), U=np.array(U))
    if rot is not None: m = m.xf(rot)
    return m.xf(None, c)

# ------------------------------------------------------------------ writer (Unity runtime format, converted to Unity axes)
def write_mesh(path, mesh, sun=None, wet=None):
    P = mesh.P[:, [0, 2, 1]].astype('<f4'); N = mesh.normals()[:, [0, 2, 1]].astype('<f4'); F = mesh.F[:, [0, 2, 1]].astype('<u4')
    C = np.clip(mesh.C * 255 + 0.5, 0, 255).astype('u1'); U0 = mesh.U.astype('<f4')
    U1 = np.zeros((len(P), 2), '<f4')
    if sun is not None: U1[:, 0] = sun
    if wet is not None: U1[:, 1] = wet
    with open(path, 'wb') as f:
        f.write(struct.pack('<4sII', b'LFM1', len(P), len(F))); f.write(P.tobytes()); f.write(N.tobytes()); f.write(C.tobytes()); f.write(U0.tobytes()); f.write(U1.tobytes()); f.write(F.tobytes())
    return len(P), len(F)
