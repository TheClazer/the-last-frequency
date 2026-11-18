# DreamSpace benchmark and 8-day plan for THE LAST FREQUENCY

Written 2026-11-09. Deadline: 2026-11-18 12:00 PT (= 01:30 IST on 19 Nov; PST applies, DST ended 1 Nov). Treat 17 Nov evening as the real deadline.

## 0. Method and limits (read first)

Read directly in the browser: the DreamSpace Devpost page and its 3 update posts, the 2025 contest overview, rules and winners gallery, the winner's Meta Store page (including reviews), the YouTube video's title, length, date and chapter list, and the Devpost pages of two other winners (Inkphony, (m)ORPH). Read from our repo: a grep of `Assets/Scripts/*.cs` (2,146 lines across 14 files), `Assets/Editor/LFBuilder.cs`, `Packages/manifest.json`, the memory file and `Submission/RULES_TO_VERIFY.md`.

Not done: I did not watch the video or read a transcript, so everything about its footage comes from the chapter list. No judge scores or feedback are public, so "why it won" below is inference, not fact. I did not open the 2026 Devpost pages. I used the criteria you supplied, and the 2025 rules page lists the same four criteria at 25% each (verified).

Context difference that matters: in 2025 hands were an optional side award ("Best Implementation of Hand Interactions"). In 2026, per the Meta blog quote in RULES_TO_VERIFY.md, hands-only is mandatory. DreamSpace's own store page lists "Hands, Controllers", so it would not be the hands-first benchmark this year. Our edge has to come from concept, feel and polish, not from "we support hands".

---

## (a) What DreamSpace did

**Result:** Winner, Best Entertainment Experience ($100k in 2025). Submitted by a two-person team (VR Tales).

**What it is:** a Quest mixed-reality app that turns your real room into an ambient place. You cut virtual windows into your real walls and look out at a storm, a New York skyline at sunrise and sunset, or a world above the clouds. A "proximity passthrough" mode shows the real room only for nearby objects (your bed, your desk) and keeps everything farther away virtual. Sold as an everyday backdrop for watching movies, browsing, or relaxing. The 2025 Entertainment track was about making content consumption more immersive, which it matches closely.

**It was not a new app.** It has been on the Meta Store since 16 Apr 2025 (v0.6.9, $9.99, 1.17 GB, 68 ratings, 4.2 stars, 59% five-star; comfort "Comfortable"; roomscale, standing and sitting; hands and controllers). The entry qualified as an UPDATE under the "significant updates" rule. Added for the contest:
1. Proximity-based passthrough blending (MRUK-based room data).
2. A window-creation tool: place windows on real walls.
3. Two new environments, Above the Clouds and Home in New York.
4. User-controlled heavy rain and fog in Midnight Storm.
5. Real-time day/night cycles with user-adjustable time.

**Tech stack shown:** Unity, C#, Meta MRUK, passthrough, custom shaders for sky, fog, rain and lighting, "optimized standalone rendering". The Devpost write-up is short (about 350 words) and has no frame-rate numbers, no SDK version, and no hands or ISDK claims. Its stated challenges are blending windows with real walls, rain and fog cost on standalone, and comfortable passthrough transitions. Its "what's next" is deeper interactivity, more window customisation and shared windows for several users.

**Submission materials:** 6 gallery images (several GIF-style); a build summary posted as an update with a changelog layout; five separate per-feature demo videos linked in a second update (Above the Clouds, Home in New York, rain, proximity passthrough, window creation); a public YouTube video. The one I found (`ogtDlO9M6jA`) is titled "Best Entertainment Experience", was published 12 Dec 2025, and runs 3:50, so it is a post-win promo, not the under-3-minute submission cut. Its chapters: Above the Clouds 0:00, Home in New York 0:40, time-of-day control 1:09, entry to immersive worlds 1:35, window creation 2:16, passthrough 2:41, rain and fog 3:15. Reviews show the developer replying to users, including fixes for room-scan problems.

**Other winners in that gallery (24 listed on page 1 of 28):** Inkphony (Best Lifestyle; draw music on real surfaces, Meta Passthrough Camera Access plus Gemini plus ElevenLabs generating instruments from real objects), Saber Punks, Tiny Golf (all-pinch mixed-reality mini golf), Photon, The Tarot Experience, Falcon, Little Critters, (m)ORPH (mixed-reality music remixing; hand grab, throw, clap, thumb microgestures, spatial audio, colocated multiplayer; a 2021 app ported to Unity 6 for the contest), IMMERROCK, Pack Attack, Le Dino Labo, Pocket Lands, Awesome Hand, Hand Survivor, Flows.xr, Language IRL, Chores.gg, Mise, Compile Crops, The Nanauts, Dun Jun, Magic Wonders, GeoPortal. The gallery does not show which prize each won except where the project page says so. By their descriptions, about 15 of the 24 are explicitly mixed-reality or passthrough-based; only about 9 are pure VR. Two winners are music tools, and both pair music with the real room.

### Why it most likely won (inference)

1. **MRUK is the product, not a bolt-on.** Windows sit on your actual walls and passthrough reveals by proximity. This is the "unique Quest capability" that the Innovation criterion asks for, and it cannot exist on a flat screen or a phone.
2. **One-sentence legibility.** "Your room becomes a place." A judge understands the value in 5 seconds and in the first video frame.
3. **It is a real shipped product with users.** A store listing, ratings, reviews, a developer replying to reviews, and a visible changelog make "Technical Implementation: reliable, minimal bugs" credible without any test lab.
4. **Itemised significant-update story.** Five named features, each with its own demo video. Judges can score each in seconds. This matches the "significant updates" rule directly.
5. **Environmental polish.** Weather, day/night, user-controlled time and handcrafted mood lighting. High visual finish per minute of attention, and the ambient pace is comfortable.
6. **Comfort and repeat use.** The listing says "Comfortable" and the Innovation criterion mentions "drive repeat usage". An ambient room you leave on is the opposite of a one-time demo.
7. **Weakness the judges forgave:** very little interaction depth, no real gameplay, no hand-first design, no performance numbers in the text. The bar for the Devpost text was low. The bar for MR use that visibly works was high.

---

## (b) Gap table (DreamSpace's strength versus us)

Our facts come from the repo grep, not from a headset. Severity = how likely it costs points on the 25% criteria.

| # | DreamSpace strength | THE LAST FREQUENCY today | Severity | Criterion hit |
|---|---|---|---|---|
| 1 | MRUK and passthrough are central; real room changes the experience | None. `LFBuilder.cs` sets `isInsightPassthroughEnabled = false` and destroys the passthrough layer. No MRUK reference in `Assets/Scripts`. The player sits in a fully virtual void | **High** (about 15 of 24 winners are MR; the criteria text names Passthrough/MRUK) | Innovation, Technical |
| 2 | Verified on device by real users; 4.2 stars across 68 ratings | **Zero headset testing.** Mic path, hand tracking path, 60 fps and comfort are all unverified. | **Critical** | Technical (60 fps), Experience, Polish |
| 3 | Instantly legible one-line concept | "Sing to build worlds" is strong but there are four ideas competing (sing, draw shapes, compose, rival duel) | Medium | Experience Design |
| 4 | Comfort: ambient, calm, all play modes | Slow camera rig motion in the plan, halftone and ink-outline look with flashing comic pops, no comfort options, no vignette, no height recenter | **High** | Experience Design, Polish |
| 5 | Polish of environments (weather, day/night, lighting) | Jungle with baked lighting, Tripo assets, deer and butterflies is competitive in look, but unproven on device and the stylised toon shader may clash with the realistic jungle (user has complained about low-poly and static before) | Medium | Polish |
| 6 | Itemised update story with per-feature clips | A new project, so no update story, but we have 4 to 5 distinct demonstrable moments. No trailer, no Devpost text finalised, `Docs/` was empty | **High** (pure time risk) | Polish & Presentation |
| 7 | Live store listing, privacy policy, ratings | No listing, no icon or cover art, no privacy policy URL, IARC rating unconfirmed (RULES_TO_VERIFY items 8, 14) | Medium | Polish |
| 8 | Hands: only "supports" (hands + controllers) | Hands-only by design, custom OVRHand index-tip poke UI. Not Meta Interaction SDK (ISDK) | Medium. The criterion names "Hand Interactions". Custom pokes are fine if reliable, but a judge looking for ISDK sees none. Also no tracking-loss handling beyond a tracked check in `LFHandVis` | Technical |
| 9 | Spatial data, hand tracking permissions declared | Mic needed for the headline feature; `microphoneUsageDescription` is empty in ProjectSettings | Medium (permission prompt flow untested) | Technical |
| 10 | Repeat-use hooks (time-of-day, weather) | One 2:30 song, one jungle plus Aurora Ice theme. Compose-your-own and SAVE MY SONG (WAV) help replay value. "SHARE TO PHONE" is only a hint string ("Tap SAVE first...") in `ShareInfo()`, not a feature | Medium. Do not market it as sharing | Innovation |
| 11 | Dev responsiveness / support | Not applicable before judging, but a tester brief and a known-issues list help | Low | Polish |

Where we are ahead, to lean on:
- **Original song with live sing-to-build:** a real interaction loop, versus DreamSpace's near-passive viewing.
- **Genuine hands-only gesture magic** (sweep, drawn shapes, push to open a 360-degree void). Nothing in the winners reads like this.
- **Lean-back entertainment fit:** a 2:30 performance you can sit through is closer to "content you experience" than most winners.
- **Music is proven:** (m)ORPH and Inkphony both won with music and the real room.

Honest verdict: on concept and interaction we are ahead. On everything a judge can verify quickly (runs well, comfortable, uses Quest-unique features, looks finished in the video) we are behind until the work below is done.

---

## (c) Prioritised 8-day plan (10 Nov to 17 Nov; 18 Nov is buffer only)

Principle: cut scope, do not add. Every item below is tied to a criterion. Items marked **MUST** are the ones that, if they slip, make the entry weaker than DreamSpace on a verifiable point.

### Day 1 (10 Nov): get onto a headset and prove the pipeline. MUST
- Get a Quest 3 for the week: borrow one, or have 2 external testers (Start Discord/forum, friends) on a "Competition" channel invite. Without this, "no headset testing" is the single largest risk and no other item can be trusted.
- Build the APK and upload it to the Dashboard release channel named "Competition" today, even if rough, and create the invite URL. Discover upload, signing, permission and manifest problems on day 1, not day 8.
- Install OVR Metrics Tool on the test device. Record frame rate in the jungle at the busiest moment (peak chorus, all waterfalls, butterflies, deer). The target is a flat 72 fps minimum, and anything under 60 is a fail against the rule.
- Fill the 22 blanks in `RULES_TO_VERIFY.md` that matter: AI-asset policy (#9), existing-work rule (#11), mic/privacy (#13), age/IARC (#14), video hosting and captions (#7), exact deadline (#17).

### Day 2 (11 Nov): performance and comfort baseline. MUST
- Fix whatever the device shows. Likely levers: baked lighting only, no real-time shadows, GPU instancing and batching on Tripo meshes, particle budget (waterfall mist, butterflies), MSAA 4x not higher, fixed foveated rendering on, texture compression ASTC, a lower render scale.
- Add a runtime governor: if the average frame time stays above about 13.8 ms for 3 seconds, step the render scale and particle counts down automatically; log it. Small to build, and it covers the 60 fps criterion on devices we never tested.
- Comfort baseline: a soft vignette whenever the rig is moving, a "calm mode" toggle (no screen-flash comic pops, no rapid halftone flicker, slower camera drift), seated-height recenter via a poke button, and a rule that nothing within about 0.4 m of the head is ever animated fast.

### Day 3 (12 Nov): the passthrough moment. MUST (guarded)
The idea that genuinely changes the experience, and is directly our signature gesture:
- **"The room opens."** The app starts in passthrough: your real room, slightly dimmed, with a small glowing point at head height. When the player pushes a hand outward to draw the 360-degree void, the real room dissolves outward from the hand in a sphere (a dissolve shader over the passthrough underlay), and the lit jungle grows in where your walls were. During the song the jungle is fully VR. At the end, the world recedes and the real room returns, so the performance reads as a transformation of the player's own space instead of a teleport.
- **MRUK part (purposeful use):** load the scene model. Use wall and floor anchors to (1) start the reveal on the nearest real wall, so the "window" the first sound opens is on a real surface, (2) align the jungle's ground to the real floor height so a seated player is not floating, and (3) have a few butterflies land on real table or window-sill anchors in the outro. Keep it to those three uses; each one is visible in footage.
- **Safe fallback (required):** if there is no room scan, MRUK is unavailable, or passthrough fails, skip the real-room phase and use the current VR void start. Make this the default code path until a tester has verified passthrough. A broken passthrough start would lose more than it gains.
- Known technical risk to budget for: URP needs an alpha-capable camera clear and the jungle sky must be alpha-masked to see the underlay. Test that this does not break the baked lighting or tonemapping, and do not enable it unverified.

### Day 4 (13 Nov): onboarding in the first 60 seconds. MUST
Judges score "grasp where to go and what to do" from the moment they enter. Script the first run (the existing "SHOW ME THE FLOW" ghost tour is good, but make the first minute automatic and short):

| Time | What happens |
|---|---|
| 0 to 5 s | Fade in, title card, soft chime. Hand-tracking check: "Show me your hands" until both are tracked (hands-lost message must be gentle and not block). |
| 5 to 20 s | One glowing orb in front of the player. Poke or pinch it. Immediate sound, immediate visual reward. A hint says what the hand just did. |
| 20 to 40 s | "Hum a note." The floor answers with light that follows pitch. If the mic is denied or silent for 4 s, offer "tap the rhythm instead" and continue (do not dead-end). |
| 40 to 60 s | The push gesture: "Push your hand outward to open the world." The room-opens moment (or the VR void) triggers, and the song starts. |

- Show the permission rationale before the mic prompt (fill `microphoneUsageDescription`). Keep text large, short, and in the world, not on a flat HUD.
- Provide a visible, reachable "skip intro" for repeat users.

### Day 5 (14 Nov): accessibility and reliability
- Left- and right-hand parity: any single hand can do every action (the poke UI already reads both fingertips; verify a one-handed pass through the whole experience).
- Mic-less path: confirmed to complete the whole song using hand pitch (height) or taps, so judges with no mic permission or a noisy room still get the full experience. Note the simulated path already exists in `LFVoice`; make it a user-facing option rather than a fallback.
- Subtitles and lyrics: karaoke lyrics exist; add a size and contrast toggle. Add volume and a "no vocal guide" toggle.
- Reliability: handle tracking loss (pause the song cleanly, resume when hands return), headset removed/put on, app focus loss, and low battery interruption. Test a 10-minute soak for memory growth (waves of spawned objects, the TrailRenderers).
- No-flash guarantee: nothing flashes more than 3 times per second at full screen, even in the comic style. State it in the listing.

### Day 6 (15 Nov): capture footage. MUST
Capture real device footage (Meta Quest recording or tester recordings) wherever possible. Where only the XR Simulator is available, label it "XR Simulator footage" on screen and in the Devpost text; the rules allow simulator capture but it is weaker than device footage. Do not use AI-generated video for any shot.

**Trailer shot list, target 2:35, hard cap 2:55 (judges stop at 3:00):**

| Time | Shot | Purpose |
|---|---|---|
| 0:00 to 0:08 | Real-room passthrough, one hand lifts, a single note, the silence. Title text. | Hook in 8 s |
| 0:08 to 0:25 | Hand push: the room dissolves, the void opens, the jungle blooms around the seated player. First-person footage only. | Innovation + MR |
| 0:25 to 0:45 | Onboarding beats: poke the orb, hum, floor lights up. On-screen caption "Hands only. No controllers." | Experience Design |
| 0:45 to 1:20 | Verse and chorus: lyrics, sing-to-build, waterfalls, statue, arches, butterflies. Wide shots of the 360 world. | Polish |
| 1:20 to 1:45 | Gesture magic: sweep to glitter, draw a shape and watch it transform. Close-up of hand with the world reacting. | Technical (hands) |
| 1:45 to 2:05 | Theme change to Aurora Ice, then Compose-your-own: tap orbs, record a loop. | Replay value |
| 2:05 to 2:25 | End of song: the world recedes, real room returns, SAVE MY SONG. | Closure |
| 2:25 to 2:35 | Title card with tagline, "Quest 3, hands only, seated". Frame-rate note only if measured on device. | Close |

Rules for the cut: first-person footage only, no mockups, no stock; no claims we have not built (no multiplayer, no avatar, no phone sharing); keep the frame-rate overlay out unless it is a real device capture. Add captions for the whole video.

### Day 7 (16 Nov): submission materials. MUST
- **Devpost text (500 words or less; sections in this order):**
  1. Tagline (140 characters max), for example: "Sing, and your voice builds a lit jungle around you. A hands-only musical for Quest 3, starting from your own room." (verify the count).
  2. Inspiration (3 sentences): a song that needs you to finish it.
  3. What it does: the first-minute flow, the push-to-open moment, singing builds the world, gesture magic, compose-your-own.
  4. How we built it: Unity 6 URP, Meta XR SDK (state the real version), hand tracking, MRUK (only if it ships), pitch detection and a procedural synth, baked lighting, Tripo-generated props.
  5. Performance and comfort: only numbers measured on device; the comfort options.
  6. AI disclosure: Tripo models and the ElevenLabs-generated song, whatever the Day 1 check of the AI-asset rule says. State it plainly. Confirm that our ElevenLabs plan covers commercial use of the generated song before submitting; the free tier does not.
  7. Challenges and what's next.
  8. Track: Entertainment (or the track you confirm on Day 1), "New project". Verify the creation dates honestly against the "existing work" rule.
- **Dashboard listing:** title, short description, 3 to 5 screenshots from real footage, icon and cover art at the sizes the Dashboard asks for (check live; I did not verify current pixel sizes), comfort rating "Comfortable" only if the Day 2 comfort work is done, privacy policy URL (one static page; we use the mic and do not upload audio), age/IARC questionnaire, supported modes: sitting; input: hands only.
- Upload the video to YouTube as public. Open it logged out to confirm.

### Day 8 (17 Nov): freeze and dry run. MUST
- Final APK uploaded to "Competition", invite URL tested by a clean account (not yours) on a real headset from install to the end of the song.
- Re-run the 10-minute soak and the OVR Metrics capture on the final build. If the frame rate fails, reduce effects, do not add features.
- Submit on the form by evening 17 Nov. Use 18 Nov only for fixes. Rules say changes after the entry period are not allowed, so confirm the Dashboard build is the one you want judged.

### Stretch only if Days 1 to 5 land on time
- A second visible interaction proving "hands first": a two-handed conducting gesture that already exists (`_conductCool` in `LFApp.cs`) clearly shown once in the trailer.
- Time-of-day or "dusk to dawn" progression across the song, since this was one of DreamSpace's strongest repeat-use hooks and our baked lighting limits it; only if the lighting setup supports a second set.

---

## (d) Risks and what NOT to build

### Top risks
1. **No headset testing.** If the first device run shows less than 60 fps, a broken mic path or a hand-tracking failure, those are the entire Technical Implementation criterion. Mitigation: Day 1 device access and testers; runtime quality governor; mic-less path.
2. **Passthrough regression.** URP plus alpha clear plus baked lighting is a known fragile combination, and we have never tested it on device. Mitigation: ship the VR-only start as the default and enable the room-opens start only after a tester confirms it.
3. **Rules we have not read.** AI-asset policy, existing-work rule, whether a hands-only app may show any controller prompt, and exact video host rules are all open in `RULES_TO_VERIFY.md`. A disqualification beats any score gain.
4. **Rights.** The ElevenLabs song and Tripo assets are generated. Confirm the licence terms of both plans allow commercial and competition use, and keep the proof (RIGHTS_AUDIT.md).
5. **Style clash.** The halftone and ink-outline look versus the realistic jungle you asked for. Pick one look for the trailer and apply it consistently; do not ship a half-converted mix. If time is short, keep the toon outline minimal on the jungle and let the baked lighting carry realism.
6. **Overclaiming.** "SHARE TO PHONE" is a hint, the rival is a single-player ghost, and nothing is device-verified. Judges who find a claim false penalise Polish and Technical at once.
7. **Video length.** DreamSpace's public video is 3:50; ours must be under 3:00 and judges are not required to watch past it.

### Do NOT build (before the deadline)
- A 3D avatar of the player or an MP4 offline export (listed as unbuilt; high cost, not a scored criterion).
- True multiplayer or networked rival duel. Keep the ghost rival only as a small mode, and describe it as "ghost rival" everywhere. If it gets in the way of the trailer, cut it from the video.
- More themes or "Seven Wonders" style locations. Two polished worlds (jungle, Aurora Ice) beat six rough ones. DreamSpace won with a few well-made environments.
- Licensed or catalogue songs. The rules require original content; the `ISongProvider` seam stays unused.
- A website or phone sharing flow. Do not claim it; remove or reword the hint text.
- New shader looks, new gestures, or new instruments in the last four days. Fixes and measurement only after Day 5.
- Anything that needs a controller, even in a menu or error dialog (hands-only is mandatory).
- Passthrough Camera Access with AI (Inkphony's approach). It is a separate award and a large integration with cloud calls; it does not fit our scope or the rules on network use.
