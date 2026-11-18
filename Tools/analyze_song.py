"""Analyze a karaoke backing track (+ optional guide vocal) into a Last Frequency song pack.
usage: python analyze_song.py <inst.mp3> <vocal.mp3|-> <lyrics.txt> <outdir> <id> <title>
Outputs outdir/song.json (bpm, beats, sections by energy, cues, lyric lines timed from guide-vocal phrases, melody contour)
Works for any licensed/owned track: lyrics may be plain text or LRC."""
import sys, json, os, re, numpy as np, librosa
inst, vocal, lyr, out, sid, title = sys.argv[1:7]
os.makedirs(out, exist_ok=True)
y, sr = librosa.load(inst, sr=22050, mono=True); dur = len(y)/sr
tempo, beats = librosa.beat.beat_track(y=y, sr=sr, units="time"); bpm = float(np.atleast_1d(tempo)[0])
hop = 512
rms = librosa.feature.rms(y=y, frame_length=2048, hop_length=hop)[0]
t = librosa.frames_to_time(np.arange(len(rms)), sr=sr, hop_length=hop)
# smooth energy per second
sec = int(dur)
energy = np.array([rms[(t>=i)&(t<i+1)].mean() if ((t>=i)&(t<i+1)).any() else 0 for i in range(sec)])
energy = energy/ (energy.max()+1e-9)
# intensity curve smoothed (0..1) sampled at 2 Hz for runtime
k = 4; sm = np.convolve(energy, np.ones(k)/k, mode="same")
intensity = [round(float(sm[min(len(sm)-1,int(i/2))]),3) for i in range(int(dur*2))]
# sections by structural segmentation (agglomerative on chroma+mfcc)
S = librosa.feature.chroma_cqt(y=y, sr=sr, hop_length=2048)
M = librosa.feature.mfcc(y=y, sr=sr, hop_length=2048, n_mfcc=12)
F = np.vstack([S/ (S.max()+1e-9), (M-M.mean(1,keepdims=True))/(M.std(1,keepdims=True)+1e-9)*0.3])
nseg = 8
bound = librosa.segment.agglomerative(F, nseg)
bt = librosa.frames_to_time(bound, sr=sr, hop_length=2048).tolist()
bt = sorted(set([0.0]+[float(b) for b in bt]+[dur]))
sections=[]
for a,b in zip(bt[:-1], bt[1:]):
    if b-a<4: continue
    m = float(energy[int(a):max(int(a)+1,int(b))].mean())
    sections.append({"start":round(a,2),"end":round(b,2),"energy":round(m,3)})
# label sections by relative energy
em = np.array([s["energy"] for s in sections]); lo, hi = np.percentile(em,30), np.percentile(em,70)
for s in sections: s["kind"] = "quiet" if s["energy"]<=lo else ("peak" if s["energy"]>=hi else "build")
# lyric lines
raw = open(lyr, encoding="utf8").read().splitlines()
lines=[]; tag=None
for l in raw:
    l=l.strip()
    if not l: continue
    m=re.match(r"^\[(.+)\]$", l)
    if m: tag=m.group(1); continue
    lines.append({"text":l,"part":tag})
phr=[]; melody=[]
if vocal!="-":
    v,_ = librosa.load(vocal, sr=22050, mono=True)
    f0, vf, vp = librosa.pyin(v, fmin=librosa.note_to_hz("C3"), fmax=librosa.note_to_hz("C6"), sr=sr, hop_length=hop)
    vt = librosa.frames_to_time(np.arange(len(f0)), sr=sr, hop_length=hop)
    voiced = vf & ~np.isnan(f0)
    # phrase segmentation: voiced stretches separated by gaps > 0.45 s
    idx = np.where(voiced)[0]
    if len(idx):
        s0=idx[0]; prev=idx[0]
        for i in idx[1:]:
            if vt[i]-vt[prev] > 0.45:
                phr.append([float(vt[s0]),float(vt[prev])]); s0=i
            prev=i
        phr.append([float(vt[s0]),float(vt[prev])])
    # melody contour @ 10 Hz as midi (0 = unvoiced)
    for i in range(0, len(f0), max(1,int(sr/hop/10))):
        melody.append(round(float(librosa.hz_to_midi(f0[i])),1) if voiced[i] else 0)
# map N lyric lines onto phrases: merge/split to N
n=len(lines)
while len(phr)>n:  # merge the closest pair
    gaps=[phr[i+1][0]-phr[i][1] for i in range(len(phr)-1)]; j=int(np.argmin(gaps)); phr[j]=[phr[j][0],phr[j+1][1]]; del phr[j+1]
while 0<len(phr)<n:  # split the longest
    L=[b-a for a,b in phr]; j=int(np.argmax(L)); a,b=phr[j]; m=(a+b)/2; phr[j:j+1]=[[a,m],[m,b]]
if not phr:
    step = dur*0.8/max(n,1); phr=[[8+i*step, 8+(i+1)*step-0.2] for i in range(n)]
for l,(a,b) in zip(lines,phr): l["start"]=round(a,2); l["end"]=round(b,2)
# cue timeline: section starts + sustained peaks + beat-synced pulse
cues=[]
for i,s in enumerate(sections):
    cues.append({"t":s["start"],"type":"SECTION_"+s["kind"].upper(),"i":i})
pk = np.argsort(-energy)[:0]
song = {"id":sid,"title":title,"bpm":round(bpm,1),"duration":round(dur,2),"audio":sid+"_inst.mp3","guide":sid+"_vocal.mp3" if vocal!="-" else "",
        "sections":sections,"beats":[round(float(b),3) for b in beats],"intensity":intensity,"lines":lines,"cues":cues,"melody":melody,"melodyHz":10}
json.dump(song, open(os.path.join(out,"song.json"),"w"), indent=1)
print("bpm",round(bpm,1),"dur",round(dur,1),"sections",[(s["start"],s["kind"]) for s in sections],"lines",n,"phrases",len(phr))
