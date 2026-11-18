# How it works

## Stack
Unity 6 (6000.0.85f2), C#, Universal Render Pipeline, Meta XR SDK on OpenXR (hand tracking, passthrough), Android/Quest build (IL2CPP, ARM64). 25 runtime scripts, about 3,300 lines, plus 24 custom shaders, all written for this project.

## The pieces
| System | What it does | Where |
|---|---|---|
| **App flow** | Boot void, menu, studio, performance, results, compose, listen | `LFApp.cs` |
| **Voice** | Microphone at 16 kHz; level, pitch, "voiced" flag and sustained-note timer every frame, zero allocation after start; waits for the permission grant before opening the mic | `LFVoice.cs` |
| **Pitch tracking** | Normalised square difference over a 1024-sample window, coarse-to-fine with parabolic refinement; range about 73 Hz to 1.3 kHz | `LFVoice.Pitch()` |
| **World** | Waves of crystals, spires, arches and sky orbs placed in 360° around the seated player and timed to the song's analysed sections; staircase grown by sustained notes; pitch mapped to timbre | `LFWorld.cs`, `LFElements.cs` |
| **Hands** | Fingertip pointers from the tracked skeleton (bone chosen by the skeleton type the headset reports); poke buttons; pinch-draw shape recognition; push, lift and climb gestures in the jungle | `LFUI.cs`, `LFMagic.cs`, `LFJungle.cs` |
| **Songs** | Original songs; each comes with an analysed pack: sections, beats, intensity curve, timed lyrics, guide melody (librosa) | `Tools/analyze_song.py`, `Resources/Songs/*` |
| **Synth** | Procedural instruments for touch-played notes and COMPOSE | `LFSynth.cs`, `LFCompose.cs` |
| **Listen and Build** | Loudness, bass, brightness and tempo from the mic; chooses an element (ice, water, earth, fire) | `LFFeel.cs` |
| **Save and share** | Mixes the voice take with the instrumental on a worker thread into a WAV; a tiny web server on the headset serves it to a phone on the same Wi-Fi (QR code), stops when the share screen closes | `LFApp.SaveTake`, `LFShare.cs`, `LFQR.cs` |
| **Comfort** | Seated layout, vignette while the view travels, calm mode | `LFComfort.cs` |
| **Performance** | Noise from a texture instead of computed, a cheaper shader tier on the headset, foveated rendering and dynamic resolution | `LFNoise.cs` |
| **Autopilot** | Drives the real app end to end with a simulated voice and ghost hands, for testing and for the editor footage | `LFAuto.cs` |

## How we tested without guessing
- A headless autopilot plays the whole app (tour, both songs, compose, save) after every change; it must finish with no exceptions.
- A pitch test feeds synthesised sung notes (harmonics, vibrato, noise) from E2 to C6, plus vocal clips from the songs, through the real pitch code:

| Input | Old tracker | New tracker |
|---|---|---|
| Synthetic sung notes C5-C6 (high voice) | 0% | 100% |
| Synthetic sung notes C4-B4 | 71% | 100% |
| Synthetic sung notes E2-C4 (low voice) | 96-100% | 100% |
| Vocal clip, "Light the Silence" | 24% | 88% |
| Vocal clip, "Wake the Green" | 70% | 82% |

  (The vocal clips also tuned one threshold, so treat those two figures as optimistic.)
- On a Meta Quest 3S: full on-device run at a steady 72 fps; mixed reality passthrough confirmed; microphone and both hands detected.

## Made with
Unity, C#, Meta XR SDK, OpenXR, Blender, librosa, ffmpeg. Songs generated with ElevenLabs Music; jungle meshes (trees, statue, rocks, cliffs, arches) generated with Tripo and then cleaned, lit and assembled in Blender. AI coding assistants helped with the engineering. No third-party film, game or song material is included.
