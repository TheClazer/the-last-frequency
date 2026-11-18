"""Mix the simulation-video soundtrack from logs/video/events.tsv (frame, story_s, kind, id, value), then mux with the JPG frames.
Placed: song instrumental + guide vocal (the simulated singer) at their start offsets, pop bells, compose notes (piano/sax/guitar/bass/bell/pad),
the studio-bounced composition WAV, boot hum and UI blips. Usage: python mix_video.py"""
import numpy as np, subprocess, os, glob, sys
SR = 48000; FPS = 30.0
ROOT = r'E:\Winter\metavr\LastFrequency'; EV = ROOT + r'\logs\video\events.tsv'; OUT = ROOT + r'\logs\video\mix.wav'
FF = r'C:\ProgramData\chocolatey\bin\ffmpeg.exe'

def load(path, af=None):
    cmd = [FF, '-v', 'error', '-i', path] + (['-af', af] if af else []) + ['-f', 'f32le', '-ac', '2', '-ar', str(SR), '-']
    return np.frombuffer(subprocess.run(cmd, capture_output=True).stdout, np.float32).reshape(-1, 2).copy()

def hz(m): return 440.0 * 2 ** ((m - 69) / 12.0)
def env(n, a, r):
    e = np.ones(n, np.float32); a = min(n, int(a * SR)); r = min(n, int(r * SR))
    if a: e[:a] = np.linspace(0, 1, a)
    if r: e[-r:] *= np.linspace(1, 0, r)
    return e
def note(ins, m, dur=None):
    f = hz(m)
    if ins == 0:   # piano
        n = int(SR * 1.8); t = np.arange(n) / SR; o = sum((1 / h ** 1.25) * np.exp(-t / (0.9 / (1 + .35 * (h - 1)))) * np.sin(2 * np.pi * f * h * t) for h in range(1, 8) if f * h < SR / 2.2)
        return o * env(n, .004, .2) * .5
    if ins == 1:   # sax
        n = int(SR * 1.4); t = np.arange(n) / SR; ph = 2 * np.pi * f * t + .02 * np.sin(2 * np.pi * 5.2 * t)
        o = sum(np.sin(ph * h) / h * (1 if h % 2 else .55) for h in range(1, 12) if f * h < SR / 2.3); o += np.random.randn(n) * .03
        k = np.exp(-np.arange(0, 400) / 40.0); o = np.convolve(o, k / k.sum(), 'same'); return o * env(n, .05, .25) * .35
    if ins == 2:   # plucked guitar (Karplus-Strong)
        n = int(SR * 1.6); L = max(2, int(SR / f)); d = np.random.uniform(-1, 1, L).astype(np.float32); o = np.zeros(n, np.float32); p = 0; prev = 0
        for i in range(n):
            cur = d[p]; d[p] = .5 * (cur + prev) * .9965; prev = cur; p = (p + 1) % L; o[i] = cur
        return o * env(n, .001, .2) * .5
    if ins == 3:   # bass
        n = int(SR * .9); t = np.arange(n) / SR; return np.exp(-t * 3.2) * (np.sin(2 * np.pi * f * t) + .45 * np.sin(4 * np.pi * f * t)) * env(n, .006, .1) * .55
    if ins == 4:   # bell
        n = int(SR * 2.4); t = np.arange(n) / SR; return np.exp(-t * 1.5) * np.sin(2 * np.pi * f * t + 5 * np.exp(-t * 2.2) * np.sin(2 * np.pi * f * 3.5 * t)) * env(n, .002, .3) * .4
    n = int(SR * 3.0); t = np.arange(n) / SR; o = sum(2 * ((f * d * t) % 1) - 1 for d in (.994, 1, 1.006)) / 3  # pad
    k = np.exp(-np.arange(0, 300) / 60.0); o = np.convolve(o, k / k.sum(), 'same'); return o * env(n, .6, .9) * .3

rows = []
for ln in open(EV, encoding='utf-8-sig'):
    p = ln.rstrip('\n').split('\t')
    if len(p) >= 5: rows.append((int(p[0]) / FPS, p[2], p[3], float(p[4])))
total = max(r[0] for r in rows) + 3.0; N = int(total * SR)
bus = {k: np.zeros((N, 2), np.float32) for k in ('music', 'fx')}
def place(b, sig, t, g=1.0, pan=0.0):
    i = int(t * SR)
    if i < 0 or i >= N: return
    if sig.ndim == 1: sig = np.stack([sig, sig], 1)
    n = min(len(sig), N - i); bus[b][i:i + n, 0] += sig[:n, 0] * g * (1 - max(0, pan)); bus[b][i:i + n, 1] += sig[:n, 1] * g * (1 + min(0, pan))

SONG = os.environ.get('SONG', 'last_frequency'); SD = os.path.join(ROOT, 'Assets', 'Resources', 'Songs', SONG); inst = load(os.path.join(SD, 'inst.ogg')); guide = load(os.environ.get('TAKE') or os.path.join(SD, 'guide.ogg'), 'aecho=0.8:0.6:60|120:0.2|0.1'); print('song', SONG, len(inst) / SR, 's')
comp = sorted(glob.glob(ROOT + r'\logs\exports\compose_*.wav'))
cache = {}
stop_t = None; song_start = None
for t, kind, i, v in rows:
    if kind == 'music' and i == 'inst': song_start = (t, v)
    if kind == 'music' and i == 'stop' and song_start:
        t0, off = song_start; a = int(off * SR); n = int((t - t0) * SR)
        seg = inst[a:a + n].copy(); seg *= env(len(seg), .05, 1.5)[:, None]; place('music', seg, t0, .9)
        g = guide[a:a + n].copy(); g *= env(len(g), .05, 1.5)[:, None]; place('music', g, t0, 1.0); song_start = None
    if kind == 'music' and i == 'comp' and comp:
        w = load(comp[-1]); place('music', w, t, .95)
    if kind == 'pop':
        m = int(i); cache.setdefault((4, m), note(4, m)); place('fx', cache[(4, m)], t, .22, pan=((m % 5) - 2) * .2)
    if kind == 'note':
        ins, m = [int(x) for x in i.split(':')]
        if (ins, m) not in cache: cache[(ins, m)] = note(ins, m)
        place('fx', cache[(ins, m)], t, .3 * v, pan=((m % 7) - 3) * .12)
    if kind == 'boot' or (kind == 'sfx' and i == 'floor'):
        d = np.arange(int(SR * 4)) / SR; s = (np.sin(2 * np.pi * 55 * d) * .5 + np.sin(2 * np.pi * 82.4 * d) * .3 + np.sin(2 * np.pi * 110 * d) * .15) * env(len(d), 1.5, 2.0) * (.5 if kind == 'boot' else .9); place('fx', s, t, .5)
    if kind == 'sfx' and i in ('tick', 'key', 'count', 'studio', 'conduct'):
        f = {'tick': 880, 'key': 1320, 'count': 660, 'studio': 440, 'conduct': 523}[i]; d = np.arange(int(SR * .28)) / SR
        place('fx', np.sin(2 * np.pi * f * d) * np.exp(-d * 14) * .4, t, .6)
if song_start:
    t0, off = song_start; a = int(off * SR); n = int((total - 2 - t0) * SR); seg = inst[a:a + n].copy(); place('music', seg, t0, .9); g = guide[a:a + n].copy(); place('music', g, t0, 1.0)
mix = bus['music'] + bus['fx']; pk = np.abs(mix).max() + 1e-6; mix *= .89 / pk
import wave
with wave.open(OUT, 'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes((np.clip(mix, -1, 1) * 32000).astype('<i2').tobytes())
print('mix', OUT, round(total, 1), 's')
fr = ROOT + r'\logs\video\frames'; final = ROOT + r'\logs\video\LastFrequency_simulation.mp4'
subprocess.run([FF, '-y', '-v', 'error', '-framerate', '30', '-i', fr + r'\f%05d.jpg', '-i', OUT, '-c:v', 'libx264', '-preset', 'medium', '-crf', '26', '-pix_fmt', 'yuv420p', '-af', 'loudnorm=I=-14:TP=-1.5:LRA=9', '-c:a', 'aac', '-b:a', '192k', '-shortest', final], check=True)
print('video', final)
