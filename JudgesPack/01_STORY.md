# You Sang It Into Being

*The story behind The Last Frequency, a seated VR musical where your voice and your hands build the world around you.*

---

## Karaoke shows you words

Think about the last time you sang along to something. Not performed, just sang: in a car, in a kitchen, under your breath on a walk. Something happens in that moment that has nothing to do with how good your voice is. The song stops being something you listen to and becomes something you are part of.

Karaoke has spent fifty years trying to bottle that feeling, and it mostly gives you a screen of lyrics turning yellow. The Last Frequency started with one question: what if the song didn't just show you the words, but answered you? What if every note you sang left something behind?

## It begins in the dark

You put on the headset and there is nothing. A dark room, bare walls. The only light comes from your own fingertips, each one trailing a faint glow as you move it. The title fades in: THE LAST FREQUENCY.

That darkness is deliberate. The idea behind the whole experience is a world that has gone silent, and you are its last frequency: the one signal still able to wake it. Nothing out there responds until you do.

You reach out and poke SING. The music begins.

## The world answers

The default world is Aurora Ice. As the song moves through its verses, the dark room around you dissolves and something takes its place, built in waves timed to the music itself.

During the quiet opening, small wonder-buds drift within arm's reach, and when you touch one it blooms and rings a note. As the song builds, spires of ice crystal rise around you. At its peaks, arches and towers climb into an aurora sky and frost spreads across the frozen lake under your seat. Everything happens in a full circle around you, so turning your head is always rewarded.

Your voice shapes it as it grows. The pitch you sing changes the character of the magic: sing high and it turns thin, bright and quick; sing low and it becomes thick, heavy and slow. Hold a note and a spiral staircase climbs a little higher into the sky. Raise both hands above your head and you conduct a burst of light.

Your hands draw, too. Pinch and trace a shape in the air: a rectangle opens a door of light, a circle opens into a halo of snowflakes, a triangle raises a ring of crystal spires.

When the last note fades, the words appear: THE WORLD IS YOURS. And then, quieter: You sang it into being.

## A second world: the jungle temple

Press the WORLD button and the ice gives way to Jungle Temple, with its own original song, "Wake the Green".

This one is slower and stranger. It starts in the dark again, and this time your hands do the building. Push a hand outward and the land rises in that direction, trees and all. Lift a hand and a waterfall pours. Push with both hands together and a staircase grows toward a giant stone guardian. Then you climb it, hand over hand. Buttons beside you change the hour, the rain and the fog, so the same temple can be a misty dawn or a storm at dusk.

## Your song is yours

At the end of a performance, SAVE MY SONG mixes your microphone take with the music into a WAV file on the headset. SHARE TO PHONE puts a QR code in front of you. Scan it with your phone on the same Wi-Fi and your song plays right there, ready to download.

Nothing is uploaded anywhere. The headset itself serves the page over your home network, only while the share screen is open, and stops the moment you close it.

## More ways in

Not everyone wants to sing, and not everyone can. So the experience has other doors.

**COMPOSE** turns a word you type into a song. The word becomes a key, a tempo and six instruments you play by touch, then mix into a track of your own.

**LISTEN AND BUILD** reacts to music playing in your room: its loudness, its bass, its brightness and its tempo. Set it to AUTO and it picks the element the world is built from, ice, water, earth or fire, based on how the music feels.

Under **OPTIONS** there is a calm mode that softens the pulses and slows the drift, a no-microphone mode where the song itself leads the world, and mixed reality, which opens the world into your real room.

## Building a voice-driven world: the hard parts

### High notes vanished

The heart of the experience is pitch: the app has to hear what note you are singing, live, on a standalone headset. The first pitch tracker worked well for low voices and fell apart higher up. Sung notes in the fifth octave came back as nothing, or an octave or more too low. For anyone with a high voice, the magic simply did not answer.

I replaced it with a different detection method that reaches from about 73 Hz, a low male voice, up to about 1.3 kHz, well into a high soprano. Then I built a test that feeds it synthetic sung notes with vibrato, harmonics and noise at known pitches, plus vocal clips pulled from the two original songs, and checks every answer.

| Test input | Old tracker | New tracker |
|---|---|---|
| Synthetic sung notes, C5 to C6 (high voice) | 0% correct | 100% correct |
| Synthetic sung notes, C4 to B4 | 71% | 100% |
| Synthetic sung notes, E2 to C4 (low voice) | 96 to 100% | 100% |
| Vocal clip from "Light the Silence" | 24% | 88% |
| Vocal clip from "Wake the Green" | 70% | 82% |

The vocal clips were also used to tune one threshold, so treat those two numbers as optimistic. The synthetic results are clean.

### The microphone that never woke up

The first time anyone launches the app, the headset asks for permission to use the microphone. The original code started listening at the same moment it asked, before you had even answered, and on Android that produces a silent microphone for the rest of the session. A first-time player would sing their heart out to a world that couldn't hear them.

Now the app waits for your answer before it starts listening. And if you say no, it doesn't just break: the song's own melody leads the performance, so the world still builds around you and the results screen tells you honestly that the microphone was off.

### A menu you had to reach under a desk for

The original menu stacked its buttons so far down that the lowest ones floated about 0.8 metres below eye level, inside the desk of anyone sitting at one. It became two pages: the main page leads with one obvious action, a wide glowing SING button and a single line, "Start here: poke SING, then sing or hum along." Everything else lives one poke away under OPTIONS.

### Honest scores only

An early results screen showed an "on-key" percentage even when nothing had been measured. It now shows a score only when there is a real measurement behind it.

## Built for a chair

The Last Frequency is built to be enjoyed sitting down. The world moves, but you don't have to: everything is placed within easy reach of a seated person, a soft vignette rises whenever the view travels, and calm mode is always one option away.

It is made in Unity 6 with the Meta XR SDK on OpenXR, using hand tracking and passthrough, with custom shaders that have a lighter tier for the headset. Pitch is tracked on the device from the microphone, the hand-played instruments come from a procedural synthesiser, and a headless autopilot drives the real app end to end for testing and for the demo footage. Both songs, "Light the Silence" and "Wake the Green", were generated with ElevenLabs Music. The jungle's trees, statue, rocks, cliffs and arches were generated with Tripo and then cleaned, lit and assembled in Blender. AI coding assistants helped with the engineering. No third-party film, game or song material is included.

## What is real today

| What | Status |
|---|---|
| Aurora Ice and Jungle Temple performances, results, saving a song | Play end to end in headless runs of the real app, with no errors |
| Pitch accuracy table | Measured in an editor test on synthetic and AI-sung clips, not on a live voice in a headset |
| Microphone permission flow, no-microphone fallback | Fixed in code; to be confirmed on a Quest |
| The demo video | Captured from the app running in the Unity editor with a scripted autopilot; labelled "Unity editor capture" |
| Frame rate, mixed reality, share-to-phone on the headset | To be measured on the headset; nothing here claims more than has been measured |

## The moment

Somewhere in the second minute there is a moment the whole thing was built for. You are singing, a little self-conscious at first, and as the chorus lifts, towers of light climb into the sky all around you, timed to the music and grown by your voice. And you realise it is waiting for you.

That is the last frequency. It's your voice.
