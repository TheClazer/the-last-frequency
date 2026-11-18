"""Original song 'Wake the Green' for the Jungle theme (ElevenLabs Music; key from ELEVENLABS_API_KEY only; prints names and HTTP codes only).
Style brief: uplifting Pacific-island-inspired cinematic adventure anthem. No artist, film or song is named or copied; lyrics are original.
Usage: python gen_song2.py [vocal|inst] [--force]"""
import os, sys, json, urllib.request, urllib.error
HERE = os.path.dirname(os.path.abspath(__file__)); RAW = os.path.join(HERE, "raw"); os.makedirs(RAW, exist_ok=True)
LEN_MS = 150_000
LYRICS = """[Verse 1]
Before the first leaf, before the river had a name
There was only the dark and a spark inside my hands
I lift them slow and the mountains start to climb
Green is waking up at the edges of my mind

[Pre-Chorus]
Every step I take draws the earth beneath me
Every note I sing makes the wild believe me

[Chorus]
Wake the green, wake the green, from the roots to the sky
Every wave and every wing is learning how to fly
Wake the green, wake the green, let the drumbeat ring
I am the one who's drawing this and the whole wide world will sing

[Verse 2]
Waterfalls come tumbling from the places that I point
Stone giants open their eyes at the lifting of my hand
The deer come down to listen and the butterflies take flight
Everything I'm singing is turning into light

[Chorus]
Wake the green, wake the green, from the roots to the sky
Every wave and every wing is learning how to fly
Wake the green, wake the green, let the drumbeat ring
I am the one who's drawing this and the whole wide world will sing

[Bridge]
And if the thunder calls my name I'll answer back in gold
This island's in my heartbeat and the story's mine to hold
Hold it till the morning, hold it till it's wide
Hold it till the whole horizon opens up inside

[Final Chorus]
Wake the green, wake the green, from the roots to the sky
Every wave and every wing is learning how to fly
Wake the green, wake the green, let the drumbeat ring
I am the one who's drawing this and the whole wide world will sing
Oh, the whole wide world will sing"""
STYLE = ("Uplifting cinematic adventure anthem inspired by Pacific island music, 100 bpm, major key. Begins in near-silence with a low breathy layered chant (open vowels, no words) and a soft pulsing log drum, "
 "then warm ukulele and acoustic guitar strums enter, bamboo flute lines, then driving deep pahu drums and rolling toms, lush swelling strings, and a huge triumphant chorus with a stacked chant choir answering the lead. "
 "A hushed bridge with just chant, flute and heartbeat drum, then a final chorus lifted a step with full orchestra, shakers and ocean-wave textures. Joyful, epic, wide and organic, polished film-musical production.")
VOCAL = (" Lead vocal: a real-sounding human female singer, warm and earthy chest voice rising to a powerful soaring belt, natural vibrato, slight imperfections and breaths, close and intimate in the verses, "
 "unprocessed, no autotune, no robotic or synthetic tone, clear diction, with a layered group chant answering her in the choruses. Lyrics:\n" + LYRICS)
INST = " Instrumental karaoke backing track with no lead vocal (keep the wordless chant choir only as texture), leave clear space where a lead vocal would sit."
JOBS = {"vocal": ("wtg_song_vocal", STYLE + VOCAL, False), "inst": ("wtg_song_inst", STYLE + INST, False)}
def run(key, force):
    name, prompt, instr = JOBS[key]; out = os.path.join(RAW, name + ".mp3")
    if os.path.exists(out) and not force: print(name, "skip (exists)"); return 0
    req = urllib.request.Request("https://api.elevenlabs.io/v1/music?output_format=mp3_44100_192", data=json.dumps({"prompt": prompt, "music_length_ms": LEN_MS, "force_instrumental": instr}).encode(),
        headers={"xi-api-key": os.environ["ELEVENLABS_API_KEY"], "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=900) as r: data = r.read(); code = r.status
    except urllib.error.HTTPError as e: print(name, "HTTP", e.code, e.read()[:300].decode("utf8", "replace")); return 1
    except Exception as e: print(name, "error", type(e).__name__); return 1
    open(out, "wb").write(data); print(name, code, len(data), "bytes"); return 0
if __name__ == "__main__":
    if "ELEVENLABS_API_KEY" not in os.environ: print("ERROR no key in environment"); sys.exit(2)
    which = [a for a in sys.argv[1:] if a in JOBS] or list(JOBS); force = "--force" in sys.argv; rc = 0
    for k in which: rc |= run(k, force)
    open(os.path.join(HERE, "lyrics2.txt"), "w", encoding="utf8").write(LYRICS); sys.exit(rc)
