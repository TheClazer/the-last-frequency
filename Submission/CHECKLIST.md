# Submission checklist (deadline 2026-11-18 12:00 PT, aim to submit by evening of 17 Nov)

## Needs the headset (cannot be done in the simulator)
- [ ] Install APK, confirm hands-only works, no controller prompts
- [ ] Frame rate >= 60 fps in Jungle and Ice (OVR Metrics), note worst case
- [ ] Mic pitch tracking works (hum, sing high/low)
- [ ] Passthrough: MR toggle, proximity passthrough, hand holes, windows on real walls
- [ ] Comfort check: seated, 10 minutes, no discomfort during stair climb and camera drift
- [ ] Share to phone over Wi-Fi (QR scan, page loads, WAV plays)
- [ ] Record real headset footage for the demo video
- [ ] Hands: logcat shows `[LF] hands: skeleton XRHandLeft/XRHandRight, N bones, pointer bone XRHand_IndexTip found`, and poking a button with the index finger (not the ring finger) presses it
- [ ] Pitch: sing a high note (C5 or above) and a low note in the studio; the floor and ring answer both (simulator test passes, headset not measured)
- [ ] Mixed reality in Aurora Ice: OPTIONS > MIXED REALITY ON shows your real room behind the world (sky dome and dark room now step aside)
- [ ] Meta button mid-song and back: the song continues where it was (logcat `[LF] resumed the song at ...`)

## Demo video (under 3:00, real footage only)
- 0:00-0:20 the void and first sound; 0:20-1:30 jungle build; 1:30-2:10 mixed reality, weather, time; 2:10-2:40 share to phone; 2:40-3:00 closing line
- Label editor renders "Unity editor capture" (they are the app running in the Unity editor on a PC, not the XR Simulator and not a headset). No AI-generated video.

## Dashboard
- [ ] Title, short and long description, 3-5 screenshots from real footage, icon and cover art at the sizes the Dashboard asks for
- [ ] Privacy policy URL (see PRIVACY_POLICY.md, host as one static page)
- [ ] Age rating / IARC questionnaire; supported mode: sitting; input: hands
- [ ] Upload APK to the "Competition" channel; open the video logged out to confirm it plays

## Rights (an open item beats any score gain)
- [ ] Re-read the contest rules for the AI-asset policy and the existing-work rule
- [ ] ElevenLabs plan terms: commercial use of generated songs
- [ ] Tripo plan terms for the generated assets used (trees, statue, rocks, cliffs, arches)
- [ ] Disclose AI-assisted assets in the Devpost text
- [ ] No film, game or song material from third parties in the build; the licensed-song slot ships empty

## Build status (2026-11-11)
- [x] Quest APK builds headless: `./chain.sh apkN LF.Editor.LFBuilder.BuildNamed -apkOut E:/Winter/metavr/LastFrequency/Builds/LastFrequency.apk` (Unity Succeeded, 0 errors, ~87 MB APK)
- [x] Music now fades out over 3.5 s instead of cutting; AudioListener guaranteed; finale extended ("You sang it into being.")
- [x] Full jungle simulator run completes with no exceptions; 157 s mix in logs/video/LastFrequency_simulation.mp4 (label as XR Simulator footage)
- [ ] Not built yet: Taj Mahal world, avatar MP4 film, live multiplayer (async ghost rival only)
- [ ] Everything under "Needs the headset" is still unverified

## Demo video (2026-11-11)
- [x] Submission/package/LastFrequency_demo.mp4 (167 s): jungle, ice, compose, async rival duel, avatar film, 3 engine cards; rebuild with `python -I Tools/make_demo.py`
- [ ] All footage is XR Simulator (labelled). Replace with real Quest capture where possible; frame-rate claim intentionally not made until measured.
- [ ] Rival duel is an async ghost, not live multiplayer.

## Elements + Listen and Build (2026-11-11)
- [x] Element picker (ICE / WATER / EARTH / FIRE / AUTO) and LISTEN AND BUILD mode in the menu; `LFElements.cs`, `LFFeel.cs`
- [x] Simulator-verified: auto selection water -> earth -> ice -> earth -> fire from a synthetic music signal
- [ ] NOT verified: mic tempo/brightness/bass detection with real music on a headset; element visuals on device; earth "stone" material look
- [ ] Rights: the app only reacts to audio the mic hears; it ships no third-party music. Re-read the contest rules for any issue with reacting to user-played music.

## Visual upgrade (2026-11-12)
- [x] Void: dark concrete room lit by fingertips, dissolves as the world builds (`LFVoidRoom.cs`, `LFVoidRoom.shader`)
- [x] Elements rebuilt with baked Blender meshes and one shader with four materials: refractive ice (+ frost snowflakes that grow on the floor), flowing water, lit rock, animated flame (`LFToon.shader`, `Art/scripts/build_ice.py`, `build_elements.py`)
- [x] Film: black shadow avatar + third-person corner view (`LFShadow.shader`, `LFRecorder.cs`)
- [x] Demo video rebuilt (169 s): light editorial cards (modes, architecture, listen and build, pipeline, share), loudness-normalised to -14 LUFS
- [x] Performance prep: noise from a texture, Quest quality tier, foveated rendering + dynamic resolution (`LFNoise.cs`)
- [x] APK rebuilt with all of the above: Unity Succeeded, 0 errors, 88 MB
- [ ] NOT measured: frame rate on a Quest. If under 60 fps, lower the quality tier further (ice floor first).
- [ ] Tripo balance is 0, so no new generated assets; ice/water/earth/fire meshes are procedural Blender models.
- [ ] Songs unchanged (cannot be judged by ear here); only levels were fixed.

## 2026-11-16 / 2026-11-17 (lf-agent audit; nothing here is measured on a headset)
Done on Nov 16 (verified by reading the code on Nov 17):
- [x] Microphone starts only after the permission grant (`LFVoice.OpenMic`); two-page menu with OPTIONS; song-end stall guard; CLOSE stops the share server (`LFShare.End`); the song leads when there is no microphone
- [ ] "Version code 2" was NOT true: `LFSetup.Apply` reset it to 1 on every build; the Nov 16 APK is versionCode 1 (aapt). Fixed: `LFSetup.VersionCode = 3`

Fixed on Nov 17 (simulator/headless only):
- [x] Hands (S0): the project uses the OpenXR hand skeleton but read the legacy index-tip id (= ring tip in OpenXR) with legacy hand prefabs. Now picks the bone by the skeleton type the runtime reports, logs it once, and the scene uses OpenXRCustomHandPrefab_L/R (`LFBuilder.EnsureOpenXRHands`)
- [x] Pitch: notes above ~400 Hz were reported an octave or more low; new NSDF detector 73 Hz to 1.3 kHz. Editor test `./chain.sh pt LF.Editor.LFPitchTest.Run` writes logs/pitch/pitch_test.txt
- [x] Main menu: SING is the top, widest, glowing button with a title and "Start here" line; LISTEN AND BUILD moved to SING's old slot; VISUAL and MODE unchanged
- [x] Results: no invented ON-KEY score in simulated / no-mic runs; studio says when the microphone is off
- [x] Share panel no longer overlaps live results buttons; share closes when leaving to studio or compose
- [x] Meta button / headset off mid-song no longer ends the song (focus guard + resume)
- [x] Mixed reality in Aurora Ice now hides the opaque sky dome and dark room so the passthrough underlay is visible
- [x] Layout: studio, compose row and sky panel raised to 32 degrees or less below the eye and above desk height
- [x] Quest shader tier made real ([branch]; the old ?: tier computed both sides): ice/water/rock floor, jungle sky, jungle terrain
- [x] Manifest: USE_ANCHOR_API and legacy BLUETOOTH removed; RECORD_AUDIO, INTERNET, HAND_TRACKING kept
- [x] Demo video re-cut as `package/LastFrequency_demo_v3.mp4` (155 s): "Unity editor capture" labels, PC-only film segment removed, claims corrected; the Nov 12 video is kept
- [x] DEVPOST_TEXT.md rewritten to match the build (Aurora Ice is the default world; jungle via WORLD), 491 words; PRIVACY_POLICY.md and package/privacy.html corrected (performance file is written after every song)

Still unverified (needs the headset): everything in "Needs the headset" above, plus frame rate with the Quest shader tier, the look of the Quest tier (never rendered: the editor uses the full tier), microphone pitch on a real voice, and the OpenXR hand prefab visuals.
