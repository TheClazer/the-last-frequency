"""Builds Submission/package/LastFrequency_demo.mp4 (<3:00): footage clips + editorial explainer cards, loudness-normalised.
usage: python -I Tools/make_demo.py   (needs ffmpeg on PATH, Pillow). Clip sources are listed in CLIPS below."""
import os, subprocess, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import demo_cards as C

R = r'E:\Winter\metavr\LastFrequency'; V = R + r'\logs\video'; W = R + r'\Work\demo'
OUT = R + r'\Submission\package\LastFrequency_demo_v3.mp4'   # v3 (2026-11-17): honest editor-capture labels, no PC-only film segment; the Nov 12 LastFrequency_demo.mp4 is kept
os.makedirs(W, exist_ok=True); os.makedirs(os.path.dirname(OUT), exist_ok=True)

JUN = V + r'\jungle_final.mp4'; ICE = V + r'\ice_clip.mp4'; ELEM = V + r'\element_clip.mp4'; DUEL = V + r'\duel_clip.mp4'; FILM = V + r'\film_clip.mp4'; VOID = V + r'\void_clip.mp4'
VF = 'scale=1280:720,fps=30,format=yuv420p'; AF = 'aresample=48000,aformat=channel_layouts=stereo'
ENC = ['-c:v', 'libx264', '-preset', 'fast', '-crf', '19', '-c:a', 'aac', '-b:a', '192k', '-ar', '48000', '-ac', '2']

def run(a):
    r = subprocess.run(['ffmpeg', '-v', 'error', '-y'] + a, capture_output=True, text=True)
    if r.returncode: print(r.stderr[-1500:]); sys.exit(1)

def clip(name, src, ss, dur, num, text, left=False):
    cp = f'{W}/{name}_cap.png'; C.caption(cp, num, text, left=left)
    f = (f'[0:v]{VF}[v];[v][1:v]overlay[o];[o]fade=t=in:st=0:d=0.4,fade=t=out:st={dur - 0.4}:d=0.4[ov];'
         f'[0:a]{AF},afade=t=in:st=0:d=0.4,afade=t=out:st={dur - 0.5}:d=0.5[oa]')
    run(['-ss', str(ss), '-t', str(dur), '-i', src, '-loop', '1', '-t', str(dur), '-i', cp, '-filter_complex', f, '-map', '[ov]', '-map', '[oa]'] + ENC + [f'{W}/{name}.mp4'])
    return f'{W}/{name}.mp4'

def card(name, png, dur, music_src=JUN, music_ss=100, vol=0.8):
    f = (f'[0:v]{VF},fade=t=in:st=0:d=0.4,fade=t=out:st={dur - 0.4}:d=0.4[v];'
         f'[1:a]{AF},volume={vol},afade=t=in:st=0:d=0.4,afade=t=out:st={dur - 0.5}:d=0.5[a]')
    run(['-loop', '1', '-t', str(dur), '-i', png, '-ss', str(music_ss), '-t', str(dur), '-i', music_src, '-filter_complex', f, '-map', '[v]', '-map', '[a]'] + ENC + [f'{W}/{name}.mp4'])
    return f'{W}/{name}.mp4'

T = 8
cards = {'t': C.title, 'modes': lambda p: C.modes(p, 2, T), 'arch': lambda p: C.arch(p, 3, T), 'listen': lambda p: C.listen(p, 4, T), 'duel': lambda p: C.duel(p, 5, T),
         'share': lambda p: C.share(p, 6, T), 'content': lambda p: C.content(p, 7, T), 'end': C.end}
for n, fn in cards.items(): fn(f'{W}/{n}.png')

segs = [card('s00', f'{W}/t.png', 4, JUN, 100), card('s01', f'{W}/modes.png', 10, JUN, 106)]
if os.path.exists(VOID): segs.append(clip('s02', VOID, 0, 9, 1, 'The void: you feel the room, you barely see it'))
segs.append(clip('s03', JUN, 20, 18, 2, 'Sing: your voice and hands raise the world'))
segs.append(clip('s04', JUN, 100, 14, 3, 'Both hands: stairs grow to the guardian'))
segs.append(clip('s05', JUN, 143, 6, 3, 'The world is yours'))
segs.append(card('s06', f'{W}/arch.png', 11, JUN, 118))
segs.append(card('s07', f'{W}/listen.png', 9, JUN, 122))
segs.append(clip('s08', ICE, 9, 18, 4, 'Listen and Build: frost spreads, ice rises with the music'))
segs.append(clip('s09a', ELEM, 15, 7, 5, 'Elements: water'))
segs.append(clip('s09b', ELEM, 29, 6, 5, 'Elements: earth'))
segs.append(clip('s09c', ELEM, 42, 8, 5, 'Elements: fire'))
if os.path.exists(DUEL):
    segs.append(card('s10', f'{W}/duel.png', 5, JUN, 130)); segs.append(clip('s11', DUEL, 12, 10, 6, 'Rival duel: hold notes to win pieces back'))
# the silhouette film is rendered offline on a PC, not by the headset app, so it is not shown (2026-11-17)
segs.append(card('s13', f'{W}/share.png', 9, JUN, 134)); segs.append(card('s14', f'{W}/content.png', 7, JUN, 138)); segs.append(card('s15', f'{W}/end.png', 4, JUN, 142))

lst = f'{W}/list.txt'; open(lst, 'w').write(''.join("file '%s'\n" % s.replace('\\', '/') for s in segs))
tmp = f'{W}/_concat.mp4'; run(['-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', tmp])
run(['-i', tmp, '-c:v', 'copy', '-af', 'loudnorm=I=-14:TP=-1.5:LRA=11', '-c:a', 'aac', '-b:a', '192k', OUT])   # streaming-loudness master so the music is clearly audible
dur = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', OUT], capture_output=True, text=True).stdout.strip()
print('DEMO', OUT, dur, 's')
