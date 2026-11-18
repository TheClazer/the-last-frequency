# The Last Frequency: Devpost / store text (draft; fill the [brackets])

**Tagline:** Sing a world into being. Hands only, seated.

**Inspiration.** Karaoke shows you words. We wanted your voice and hands to build the room around you.

**What it does.** In a dark room lit only by your fingertips, poke SING and sing along to an original song. The world builds around you in 360 degrees as the song moves on: in Aurora Ice (the default world) waves of ice crystals and towers rise and a spiral staircase climbs, frost grows across a frozen lake, and wonder-buds float within reach and bloom when you touch them. Holding a note grows the staircase further. Your pitch changes the feel of the magic: high notes make it thin, bright and quick; low notes make it thick and slow. Pinch and draw a shape in the air: a rectangle opens a door of light, a circle blooms into snowflakes, a triangle raises crystal spires.

Switch the WORLD button to Jungle Temple for a second song, "Wake the Green": push a hand outward to raise land and trees in that direction, lift a hand to pour waterfalls, push with both hands to grow stairs toward a giant guardian statue, then climb them with your hands. Buttons change the time of day, rain and fog.

COMPOSE turns a typed word into a key, tempo and six instruments you play by touch and bounce to a WAV. LISTEN AND BUILD reacts to music playing in your room (loudness, bass, brightness, tempo); with AUTO chosen it picks the element: ice, water, earth or fire. OPTIONS holds mixed reality, calm mode and a no-microphone mode. After a song, SAVE MY SONG mixes your microphone take with the music into a WAV on the headset, and SHARE TO PHONE shows a QR code that opens it on a phone on the same Wi-Fi.

**How we built it.** Unity 6, Meta XR SDK on OpenXR (hand tracking, passthrough), custom shaders with a cheaper Quest tier, meshes with lighting baked in Blender, on-device pitch tracking from the microphone (normalised square difference, about 73 Hz to 1.3 kHz), a procedural synth, and a headless autopilot that drives the real app for tests and the demo footage.

**Challenges.** Seated comfort, a 360 degree world light enough for a standalone headset, and pitch tracking for high and low voices.

**AI disclosure.** The songs were generated with ElevenLabs Music. The jungle tree, statue, rock, cliff and arch meshes were generated with Tripo and then cleaned, lit and assembled in Blender. [Owner: state how AI coding assistants were used for the code and shaders.] No third-party film, game or song material is included.

**Built with.** Unity, C#, Meta XR SDK, OpenXR, Blender, ElevenLabs, Tripo, librosa, ffmpeg.

**Demo video.** [link]. The footage is the app running in the Unity editor on a PC, driven by a scripted autopilot, and is labelled "Unity editor capture"; it is not headset capture.

**Try it.** Release channel "Competition" on the Meta Developer Dashboard.
