"""Prepare real sung-vocal test material for the LFVoice pitch test (simulator only).
There is no isolated vocal recording in the project. The two vocal songs in Audio/raw (ElevenLabs, AI-generated singing) are
separated into a vocal foreground with librosa's REPET-SIM filter, resampled to 16 kHz mono (the headset microphone rate) and
labelled with a pYIN reference pitch. Residual accompaniment remains in the signal, so this is a harder test than a clean take.
Output: logs/pitch/<id>_vocal16k.wav and logs/pitch/<id>_ref.csv (end_sample, ref_midi) for frames pYIN marks voiced with p >= 0.85.
usage: python -I Tools/pitch/prep_vocal.py"""
import os, sys
import numpy as np, librosa, soundfile as sf

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "logs", "pitch"); os.makedirs(OUT, exist_ok=True)
SR, HOP, WIN = 16000, 512, 1024

for sid, src in (("last_frequency", "Audio/raw/lf_song_vocal.mp3"), ("wake_the_green", "Audio/raw/wtg_song_vocal.mp3")):
    y, _ = librosa.load(os.path.join(ROOT, src), sr=22050, mono=True)
    S_full, phase = librosa.magphase(librosa.stft(y, n_fft=2048, hop_length=512))
    S_filter = librosa.decompose.nn_filter(S_full, aggregate=np.median, metric="cosine", width=int(librosa.time_to_frames(2, sr=22050)))
    S_filter = np.minimum(S_full, S_filter)
    mask_v = librosa.util.softmask(S_full - S_filter, 10 * S_filter, power=2)
    v = librosa.istft(mask_v * S_full * phase, hop_length=512, length=len(y))
    v16 = librosa.resample(v, orig_sr=22050, target_sr=SR)
    v16 = (0.1 * v16 / (np.sqrt(np.mean(v16 ** 2)) + 1e-9)).astype(np.float32)
    sf.write(os.path.join(OUT, sid + "_vocal16k.wav"), v16, SR, subtype="PCM_16")
    # reference: pYIN over the same 1024-sample window the headset uses, one value per 512-sample hop, window = the LAST 1024 samples
    f0, vf, vp = librosa.pyin(v16, fmin=librosa.note_to_hz("C2"), fmax=librosa.note_to_hz("C6"), sr=SR, frame_length=WIN, hop_length=HOP, center=False)
    rows = []
    for i in range(len(f0)):
        if vf[i] and vp[i] >= 0.85 and np.isfinite(f0[i]):
            rows.append((i * HOP + WIN, float(librosa.hz_to_midi(f0[i]))))
    with open(os.path.join(OUT, sid + "_ref.csv"), "w") as fh:
        fh.write("end_sample,ref_midi\n")
        for e, m in rows: fh.write(f"{e},{m:.3f}\n")
    mids = np.array([m for _, m in rows])
    print(sid, "seconds", round(len(v16) / SR, 1), "voiced ref frames", len(rows), "midi range", round(float(mids.min()), 1), round(float(mids.max()), 1), "median", round(float(np.median(mids)), 1))
