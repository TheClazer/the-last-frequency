"""Explainer cards for the demo video. Editorial style: warm off-white, charcoal type, one orange accent, thin rules. 1280x720."""
import os
from PIL import Image, ImageDraw, ImageFont

R = r'E:\Winter\metavr\LastFrequency'
SZ = (1280, 720)
BG = (247, 243, 236); INK = (30, 28, 26); MUTE = (112, 106, 98); ORANGE = (255, 106, 31); RULE = (219, 212, 201); TINT = (255, 234, 218)
HEAD = 'C:/Windows/Fonts/bahnschrift.ttf'; BODY = 'C:/Windows/Fonts/segoeui.ttf'; BOLD = 'C:/Windows/Fonts/segoeuib.ttf'

def fh(sz, weight=None):
    f = ImageFont.truetype(HEAD, sz)
    try:
        if weight: f.set_variation_by_axes([weight, 100])
    except Exception: pass
    return f
def fb(sz, bold=False): return ImageFont.truetype(BOLD if bold else BODY, sz)

def frame(idx, total, headline, kicker):
    im = Image.new('RGB', SZ, BG); d = ImageDraw.Draw(im)
    d.text((56, 38), 'THE LAST FREQUENCY', font=fh(18, 600), fill=ORANGE)
    d.text((SZ[0] - 56, 38), '%02d / %02d' % (idx, total), font=fh(18, 500), fill=MUTE, anchor='ra')
    d.line([(56, 74), (SZ[0] - 56, 74)], fill=RULE, width=2)
    d.text((56, 104), kicker.upper(), font=fh(20, 500), fill=MUTE)
    d.text((56, 134), headline, font=fh(60, 600), fill=INK)
    return im, d

def wrap(d, text, font, width):
    words = text.split(); lines = []; cur = ''
    for w in words:
        t = (cur + ' ' + w).strip()
        if d.textlength(t, font=font) <= width: cur = t
        else: lines.append(cur); cur = w
    if cur: lines.append(cur)
    return lines

def title(path):
    im = Image.new('RGB', SZ, BG); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 18, SZ[1]], fill=ORANGE)
    d.text((90, 230), 'THE LAST', font=fh(150, 700), fill=INK); d.text((90, 370), 'FREQUENCY', font=fh(150, 700), fill=ORANGE)
    d.text((94, 560), 'Sing, gesture, and a world builds around you.', font=fb(34), fill=INK)
    d.text((94, 612), 'A hands-only, seated Quest 3 experience. Original songs. Everything runs on the headset.', font=fb(22), fill=MUTE)
    d.text((SZ[0] - 56, 44), 'Meta VR Start Developer Competition  /  Entertainment', font=fh(18, 500), fill=MUTE, anchor='ra')
    im.save(path)

def modes(path, idx, total):
    im, d = frame(idx, total, 'Six ways to play', 'Modes')
    items = [('01', 'SING', 'Karaoke that builds worlds. Your voice and hands raise a 360 degree scene to an original song.'),
             ('02', 'COMPOSE', 'Play instruments in the air. The app bounces your song to a WAV you can keep.'),
             ('03', 'SHOW ME THE FLOW', 'A ghost hand presses the buttons and previews a performance.'),
             ('04', 'LISTEN AND BUILD', 'Plays from any music near the headset. The mic reads tempo, bass and brightness.'),
             ('05', 'ELEMENTS', 'Ice, water, earth or fire, or let the music choose. The element is the material the world is built from.'),
             ('06', 'RIVAL DUEL', 'A scripted rival claims pieces of the world. Hold notes to take them back.')]
    x0, y0, cw, rh = 56, 250, 580, 140
    for k, (n, t, s) in enumerate(items):
        x = x0 + (k % 2) * (cw + 48); y = y0 + (k // 2) * rh
        d.line([(x, y), (x + cw, y)], fill=RULE, width=2)
        d.text((x, y + 16), n, font=fh(40, 700), fill=ORANGE); d.text((x + 74, y + 18), t, font=fh(28, 600), fill=INK)
        yy = y + 62
        for ln in wrap(d, s, fb(20), cw - 74): d.text((x + 74, yy), ln, font=fb(20), fill=MUTE); yy += 28
    d.text((56, 676), 'Two worlds: Jungle Temple and Aurora Ice.  Mixed reality opens your real room into either one.', font=fb(20, True), fill=INK)
    im.save(path)

def arch(path, idx, total):
    im, d = frame(idx, total, 'How it works', 'Architecture')
    cols = [('INPUT', ['*Microphone', '*Hand tracking', '*Song pack', '*Room audio']),
            ('ANALYSIS', ['*Pitch', 'NSDF, E2 to C6', '*Gestures', 'push, lift, draw shapes', '*Song', 'sections, beats, melody', '*Room', 'tempo, bass, brightness']),
            ('ENGINE', ['*Director', 'cues from the music', '*Reveal layers', 'ground, trees, water,', 'stairs', '*Elements', 'ice, water, earth, fire']),
            ('RENDER', ['*Baked-light meshes', '*Element shaders', 'ice, water, rock, flame', '*Fingertip-lit void', '*Frost that grows', '*GPU particles']),
            ('OUTPUT', ['*Quest 3 / 3S app', '*Saved song (WAV)', '*Share to phone', 'QR over local Wi-Fi', '*Performance file', 'head, hands, voice'])]
    x0, y0, cw, gap = 56, 250, 210, 28
    for k, (h, lines) in enumerate(cols):
        x = x0 + k * (cw + gap)
        d.rectangle([x, y0, x + cw, y0 + 46], fill=ORANGE if k == 2 else INK)
        d.text((x + 16, y0 + 9), h, font=fh(24, 600), fill=(255, 255, 255))
        yy = y0 + 66
        for ln in lines:
            b = ln.startswith('*'); d.text((x + 4, yy), ln.lstrip('*'), font=fb(20 if b else 18, b), fill=INK if b else MUTE); yy += 30 if b else 26
        if k < 4: d.polygon([(x + cw + 6, y0 + 23 - 9), (x + cw + 22, y0 + 23), (x + cw + 6, y0 + 23 + 9)], fill=ORANGE)
    d.line([(56, 590), (SZ[0] - 56, 590)], fill=RULE, width=2)
    d.text((56, 606), 'Unity 6  /  OpenXR  /  Meta XR SDK  /  Quest 3 and 3S  /  hands only  /  seated', font=fb(22, True), fill=INK)
    d.text((56, 646), 'Fixed object pools, no per-frame allocation. Analysis and rendering both run on the headset; nothing is uploaded.', font=fb(20), fill=MUTE)
    im.save(path)

def listen(path, idx, total):
    im, d = frame(idx, total, 'Listen and Build', 'Mode 04')
    d.text((56, 224), 'The headset hears the room. The room becomes the instrument.', font=fb(26), fill=INK)
    steps = [('1', 'HEAR', 'The mic picks up music playing nearby: a video, a speaker, a band. No song identification, no upload.'),
             ('2', 'FEEL', 'Loudness, bass, brightness and tempo are measured on the device. Calm, build and peak are detected.'),
             ('3', 'CHOOSE', 'Fire is loud, fast and bright. Ice is bright and airy. Water is slow and soft. Earth is bass-heavy.'),
             ('4', 'BUILD', 'Peaks raise towers and arches, beats ripple the floor, and the stairs grow. Or pick the element yourself.')]
    y = 290
    for n, t, s in steps:
        d.line([(56, y), (SZ[0] - 56, y)], fill=RULE, width=2)
        d.text((56, y + 12), n, font=fh(46, 700), fill=ORANGE); d.text((130, y + 16), t, font=fh(28, 600), fill=INK)
        for j, ln in enumerate(wrap(d, s, fb(21), 800)): d.text((380, y + 14 + j * 30), ln, font=fb(21), fill=MUTE)
        y += 92
    d.text((56, 668), 'Editor clips in this video use a synthetic music signal, because the editor run has no microphone.', font=fb(19), fill=ORANGE)
    im.save(path)

def content(path, idx, total):
    im, d = frame(idx, total, 'How the content is made', 'Pipeline')
    rows = [('AUDIO', ['Original song and vocal', 'generated with ElevenLabs', 'Music'], ['Analysis with librosa', 'sections, beats, melody,', 'timed lyrics'], ['Song pack', 'loaded at runtime', 'by the director']),
            ('3D', ['Mesh from Tripo,', 'cleaned in Blender', '(jungle assets)'], ['Ice crystals and chunks', 'modelled procedurally', 'in Blender'], ['Baked ambient occlusion', 'and sun visibility,', 'custom mesh format'])]
    y = 260
    for name, a, b, c in rows:
        d.text((56, y + 8), name, font=fh(44, 700), fill=ORANGE)
        for k, blk in enumerate((a, b, c)):
            x = 250 + k * 330
            for j, ln in enumerate(blk): d.text((x, y + j * 32), ln, font=fb(22, j == 0), fill=INK if j == 0 else MUTE)
            if k < 2: d.polygon([(x + 280, y + 16 - 9), (x + 296, y + 16), (x + 280, y + 16 + 9)], fill=ORANGE)
        y += 170
        d.line([(56, y - 40), (SZ[0] - 56, y - 40)], fill=RULE, width=2)
    d.text((56, 626), 'AI tools used for songs, meshes and code are disclosed in the description.', font=fb(21, True), fill=INK)
    d.text((56, 664), 'No third-party film, game or song material is included.', font=fb(21), fill=MUTE)
    im.save(path)

def share(path, idx, total):
    im, d = frame(idx, total, 'Save and share', 'Output')
    steps = [('1', 'SAVE', 'SAVE MY SONG mixes your microphone take with the song and keeps it on the headset as a WAV.'),
             ('2', 'SHARE', 'The headset serves a small page on your own Wi-Fi. Scan the QR code on your phone. Nothing is uploaded.')]
    y = 270
    for n, t, s in steps:
        d.line([(56, y), (800, y)], fill=RULE, width=2)
        d.text((56, y + 12), n, font=fh(46, 700), fill=ORANGE); d.text((130, y + 16), t, font=fh(28, 600), fill=INK)
        for j, ln in enumerate(wrap(d, s, fb(21), 600)): d.text((130, y + 62 + j * 28), ln, font=fb(21), fill=MUTE)
        y += 130
    qr = R + r'\logs\qr_test.png'
    if os.path.exists(qr):
        q = Image.open(qr).convert('RGB').resize((260, 260), Image.NEAREST); d.rectangle([880, 250, 1160, 530], fill=(255, 255, 255), outline=RULE, width=2); im.paste(q, (890, 260))
        d.text((1020, 552), 'QR generated by the app', font=fb(18), fill=MUTE, anchor='ma')
    im.save(path)

def duel(path, idx, total):
    im, d = frame(idx, total, 'Rival duel', 'Mode 06')
    d.text((56, 270), 'A scripted rival claims pieces of the world.', font=fb(34), fill=INK)
    d.text((56, 326), 'Every resonant note you sing claims one back.', font=fb(34), fill=INK)
    d.line([(56, 420), (SZ[0] - 56, 420)], fill=RULE, width=2)
    d.text((56, 440), 'TODAY', font=fh(24, 600), fill=ORANGE); d.text((200, 438), 'A scripted rival that claims pieces on the beat.', font=fb(26), fill=INK)
    d.text((56, 500), 'NEXT', font=fh(24, 600), fill=ORANGE); d.text((200, 498), 'Live co-op and duel multiplayer on the same engine.', font=fb(26), fill=MUTE)
    im.save(path)

def end(path):
    im = Image.new('RGB', SZ, BG); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 18, SZ[1]], fill=ORANGE)
    d.text((90, 250), 'Sing it into being.', font=fh(110, 700), fill=INK)
    d.text((94, 420), 'The Last Frequency', font=fh(44, 600), fill=ORANGE)
    d.text((94, 500), 'Hands only  /  Seated  /  Original songs  /  Quest 3 and 3S', font=fb(26), fill=INK)
    d.text((94, 546), 'Footage: the app running in the Unity editor on a PC (scripted autopilot), not headset capture.', font=fb(20), fill=MUTE)
    im.save(path)

def caption(path, num, text, sim=True, left=False):
    """lower-third on footage: white bar, orange numeral, charcoal text; small orange tag top-right."""
    im = Image.new('RGBA', SZ, (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    f = fb(30, True); w = int(d.textlength(text, font=f)) + 120
    d.rectangle([0, 622, w, 684], fill=(247, 243, 236, 235)); d.rectangle([0, 622, 10, 684], fill=ORANGE + (255,))
    d.text((28, 628), '%02d' % num, font=fh(40, 700), fill=ORANGE + (255,)); d.text((100, 636), text, font=f, fill=INK + (255,))
    if sim:
        x0 = 22 if left else SZ[0] - 318
        d.rectangle([x0, 22, x0 + 296, 58], fill=(247, 243, 236, 225)); d.text((x0 + 148, 40), 'UNITY EDITOR CAPTURE', font=fh(18, 600), fill=INK + (255,), anchor='mm')
    im.save(path)
