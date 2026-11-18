"""Generate the original song 'Light the Silence' with ElevenLabs Music.
Key is read from ELEVENLABS_API_KEY only; prints names and HTTP codes only.
Two renders from one brief: a guide-vocal version and an instrumental karaoke backing.
Original lyrics and style brief (no artist or song named or copied).
Usage: python gen_song.py [vocal|inst] [--force]"""
import os, sys, json, urllib.request, urllib.error
HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, "raw"); os.makedirs(RAW, exist_ok=True)
LEN_MS = 150_000

LYRICS = """[Verse 1]
Silence wraps around me like a sheet of night
Not a single star to tell me which way is right
But there's a hum beneath my ribs, a spark I never named
One breath, one note, and nothing stays the same

[Pre-Chorus]
So I open up my lungs and I let the whole sky hear
Every shadow, every fear

[Chorus]
Light the silence, raise the floor beneath my feet
Every word I'm singing is a stair I climb to meet
I'm the frequency, I'm the flame, I'm the storm and the sea
Watch the whole world rise when I sing for me

[Verse 2]
Water that forgot it ever had the right to fly
Now it's spinning into spirals, writing wings across the sky
Every wall I whispered to is turning into light
I'm not afraid of the dark, I am the dark's first sunrise

[Chorus]
Light the silence, raise the floor beneath my feet
Every word I'm singing is a stair I climb to meet
I'm the frequency, I'm the flame, I'm the storm and the sea
Watch the whole world rise when I sing for me

[Bridge]
And if my voice should break, let it break into stars
Every crack a doorway, every scar a guitar
Hold the note a little longer, hold it till it's gold
Hold it till the heavens open and the whole sky is told

[Final Chorus]
Light the silence, raise the floor beneath my feet
Every word I'm singing is a stair I climb to meet
I'm the frequency, I'm the flame, I'm the storm and the sea
Watch the whole world rise when I sing for me
Oh, watch the whole world rise
When I sing for me"""

STYLE = ("Cinematic theatrical power ballad, key of D minor modulating up to E minor for the final chorus, 76 bpm. "
 "Starts with a lone felt piano and a soft sub pulse in near-silence, then strings enter, then a huge chorus with big drums, "
 "swelling orchestral strings, choir pads and a soaring melody, then a hushed bridge, then a final chorus lifted a step with full orchestra. "
 "Epic, emotional, uplifting, wide and spacious, polished modern film-musical production.")
VOCAL = (" Lead vocal: a real-sounding human female singer, warm and breathy chest voice rising to a powerful belt, natural vibrato, "
 "slight imperfections and breaths, close-mic intimate in the verses, unprocessed, no autotune, no robotic or synthetic tone, "
 "clear diction. Lyrics:\n" + LYRICS)
INST = " Instrumental karaoke backing track with no vocals at all, leave clear space in the mix where a lead vocal would sit."

JOBS = {
  "vocal": ("lf_song_vocal", STYLE + VOCAL, False),
  "inst":  ("lf_song_inst",  STYLE + INST,  True),
}

def run(key, force):
    name, prompt, instr = JOBS[key]
    out = os.path.join(RAW, name + ".mp3")
    if os.path.exists(out) and not force:
        print(name, "skip (exists)"); return 0
    req = urllib.request.Request("https://api.elevenlabs.io/v1/music?output_format=mp3_44100_192",
        data=json.dumps({"prompt": prompt, "music_length_ms": LEN_MS, "force_instrumental": instr}).encode(),
        headers={"xi-api-key": os.environ["ELEVENLABS_API_KEY"], "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=900) as r:
            data = r.read(); code = r.status
    except urllib.error.HTTPError as e:
        body = e.read()[:300].decode("utf8", "replace"); print(name, "HTTP", e.code, body); return 1
    except Exception as e:
        print(name, "error", type(e).__name__); return 1
    open(out, "wb").write(data); print(name, code, len(data), "bytes"); return 0

if __name__ == "__main__":
    if "ELEVENLABS_API_KEY" not in os.environ: print("ERROR no key in environment"); sys.exit(2)
    which = [a for a in sys.argv[1:] if a in JOBS] or list(JOBS)
    force = "--force" in sys.argv
    rc = 0
    for k in which: rc |= run(k, force)
    open(os.path.join(HERE, "lyrics.txt"), "w", encoding="utf8").write(LYRICS)
    sys.exit(rc)
