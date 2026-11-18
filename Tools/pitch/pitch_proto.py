"""Prototype of LFVoice pitch detection (old autocorrelation vs new NSDF/McLeod), run on synthetic sung tones.
Used to choose the C# fix; the authoritative numbers come from the Unity editor test (LF.Editor.LFPitchTest).
usage: python -I Tools/pitch/pitch_proto.py            (simulator only, no headset involved)"""
import numpy as np

RATE, WIN = 16000, 1024


def pitch_old(buf):
    MinLag, MaxLag = 40, 200
    n = WIN - MaxLag
    a = buf[:n]
    e0 = float(np.dot(a, a))
    if e0 < 1e-6:
        return 0.0
    best, bl = 0.0, 0
    for lag in range(MinLag, MaxLag + 1):
        b = buf[lag:lag + n]
        c = float(np.dot(a, b)) / np.sqrt(e0 * float(np.dot(b, b)) + 1e-9)
        if c > best:
            best, bl = c, lag
    if best < 0.55 or bl == 0:
        return 0.0
    return 69 + 12 * np.log2(RATE / bl / 440.0)


def pitch_new(buf, MinLag=12, MaxLag=220, k=0.95, thr=0.6):
    n = WIN - MaxLag
    a = buf[:n]
    e0 = float(np.dot(a, a))
    if e0 < 1e-6:
        return 0.0
    ns = np.zeros(MaxLag + 2)
    e1 = float(np.dot(buf[1:1 + n], buf[1:1 + n]))
    for lag in range(1, MaxLag + 1):
        b = buf[lag:lag + n]
        if lag > 1:
            e1 += buf[lag + n - 1] ** 2 - buf[lag - 1] ** 2
        ns[lag] = 2 * float(np.dot(a, b)) / (e0 + e1 + 1e-9)
    # key maxima: one per positive lobe, after the first negative-going zero crossing
    keys = []
    lag = 1
    while lag <= MaxLag and ns[lag] > 0:
        lag += 1
    while lag <= MaxLag:
        while lag <= MaxLag and ns[lag] <= 0:
            lag += 1
        if lag > MaxLag:
            break
        bi, bv = lag, ns[lag]
        while lag <= MaxLag and ns[lag] > 0:
            if ns[lag] > bv:
                bi, bv = lag, ns[lag]
            lag += 1
        if bi >= MinLag and bi < MaxLag:
            keys.append((bi, bv))
    if not keys:
        return 0.0
    gmax = max(v for _, v in keys)
    if gmax < thr:
        return 0.0
    for bi, bv in keys:
        if bv >= k * gmax:
            break
    y0, y1, y2 = ns[bi - 1], ns[bi], ns[bi + 1]
    d = y0 - 2 * y1 + y2
    off = 0.5 * (y0 - y2) / d if abs(d) > 1e-9 else 0.0
    off = max(-0.5, min(0.5, off))
    return 69 + 12 * np.log2(RATE / (bi + off) / 440.0)


def sung(midi, secs=1.0, vib_cents=35, snr_db=20, seed=0):
    rng = np.random.default_rng(seed + int(midi * 10))
    t = np.arange(int(RATE * secs)) / RATE
    f0 = 440 * 2 ** ((midi - 69) / 12) * 2 ** (vib_cents / 1200 * np.sin(2 * np.pi * 5.5 * t + rng.uniform(0, 6.28)))
    ph = 2 * np.pi * np.cumsum(f0) / RATE
    y = np.zeros_like(t)
    for h in range(1, 40):
        if f0.max() * h > 7600:
            break
        fh = f0.mean() * h
        formant = 1 + 1.5 * np.exp(-((fh - 700) / 300) ** 2) + 1.0 * np.exp(-((fh - 1200) / 400) ** 2) + 0.6 * np.exp(-((fh - 2700) / 500) ** 2)
        y += formant / h ** 1.3 * np.sin(h * ph + rng.uniform(0, 6.28))
    y /= np.sqrt((y ** 2).mean())
    noise = rng.normal(size=len(t))
    noise /= np.sqrt((noise ** 2).mean())
    y = y + noise * 10 ** (-snr_db / 20)
    return 0.1 * y / np.sqrt((y ** 2).mean()), f0


def run(fn, notes, **kw):
    rows = {}
    for m in notes:
        y, f0 = sung(m, **kw)
        ok = octv = miss = tot = 0
        for end in range(WIN, len(y) + 1, 512):
            buf = y[end - WIN:end].astype(np.float32)
            truth = 69 + 12 * np.log2(f0[end - WIN:end].mean() / 440)
            est = fn(buf)
            tot += 1
            if est <= 0:
                miss += 1
                continue
            e = est - truth
            if abs(e) < 0.5:
                ok += 1
            elif abs(e - 12 * round(e / 12)) < 0.5:
                octv += 1
        rows[m] = (ok, octv, miss, tot)
    return rows


if __name__ == "__main__":
    notes = list(range(40, 85))
    for name, fn in (("old", pitch_old), ("new", pitch_new)):
        r = run(fn, notes)
        for lo, hi, lab in ((40, 47, "E2-B2"), (48, 59, "C3-B3"), (60, 71, "C4-B4"), (72, 84, "C5-C6")):
            ok = sum(r[m][0] for m in range(lo, hi + 1)); oc = sum(r[m][1] for m in range(lo, hi + 1)); mi = sum(r[m][2] for m in range(lo, hi + 1)); tt = sum(r[m][3] for m in range(lo, hi + 1))
            print(f"{name} {lab}: correct {100*ok/tt:5.1f}%  octave {100*oc/tt:5.1f}%  unvoiced {100*mi/tt:5.1f}%  frames {tt}")
