# Survey: free local tools and models that could help build the game

Date: 2026-10-10. Four research agents searched widely (GitHub, Hugging Face, ComfyUI registries, papers, forums). **Nothing here is installed or approved by being listed.** Licences are as the agents reported them; "unverified" means only secondary sources were read. Before any of these is used, the lead reads the licence at its source and records it in `THIRD_PARTY_LICENCES.md` (decision 0011). None was tested on this PC.

## Worth adopting, in order of use

| Need | Candidate | Licence as reported | Notes |
|---|---|---|---|
| People and creatures that move | **Quaternius Universal Animation Library 1 and 2**; **Mesh2Motion assets** | CC0 | Humanoid locomotion, melee, gestures; creature rigs and clips (fox, bird, dragon, spider, snake). Fits decision 0007 as it stands |
| Rigging, humanoid and not | **SkinTokens** (successor to UniRig) | MIT code and weights | Built for the graphics card generation this PC has; command line with batch mode. Windows untested |
| Motion from a text description | **Kimodo** (NVIDIA) | Apache-2.0 code; NVIDIA Open Model License weights, "ready for commercial use" | Its text encoder is a gated model under Meta's licence: read before use. Avoid its SMPLX checkpoint |
| Monsters in separate parts | **PartCrafter** | MIT code and weights | Image to several part meshes, geometry only. One wrapper is said to embed restricted code: audit first. Fallback with no licence risk: split a rigged model along its bone boundaries in Blender |
| Talking NPCs: running the language model | **LLMUnity** | Apache-2.0 | Built on llama.cpp; streaming, forced structured output, per-NPC memory. Fallback: the same model in a separate server process, so a fault cannot take the game down |
| Talking NPCs: the model | **Qwen3.5-4B** or **Gemma 4 E4B** | Apache-2.0 | About 3 to 5 GB. Test both on our character sheets |
| Talking NPCs: hearing | **Parakeet TDT 0.6b** through **sherpa-onnx** / Unity-Sherpa-ONNX; or **whisper.unity** | CC-BY-4.0 (credit needed); Apache-2.0; MIT | Runs on the processor, leaving the graphics card to VR |
| Talking NPCs: voice | **Kokoro** (already approved) through sherpa-onnx | Apache-2.0 weights | 54 voices, sentence by sentence. Check whether the build bundles GPL phoneme data before shipping |
| Talking NPCs: knowing when you stopped speaking | **Silero VAD**; **Smart Turn v3** | MIT; BSD-2 | |
| Talking NPCs: mouths | **uLipSync** | MIT | |
| Distinct voices designed from a description | **Qwen3-TTS VoiceDesign** | Apache-2.0 | For offline voice design, not for live speech |
| A second pair of eyes on screenshots | **Qwen3-VL** 8B or 32B | Apache-2.0 | A local model that can look at the robot player's pictures and flag faults |
| Consistent character turnarounds | **Qwen-Image-Edit-2511** | Apache-2.0 (unverified) | Front, side and back of one design, to feed 3D |
| Enemy brains | **Fluid Behavior Tree** with Unity's navigation package | MIT | Unity's own behaviour package is under Unity's terms of service, not an open licence: do not use |
| Cutting things | **UnitySkinnedMeshSlicer_3A**, **EzySlice**, **OpenFracture** | MIT | As reference code. See below |
| Multiplayer, later | **PurrNet** with Steamworks.NET | MIT | Fully open, active, has owner-controlled physics bodies. FishNet's licence is not open; decision 0005 should be revisited at the multiplayer milestone |
| Effects as animated images | **Wan 2.2 TI2V-5B** | Apache-2.0, no claim on outputs | For flames, slashes and magic as flipbooks |
| Textures | **PBRify Remix** (already approved), Real-ESRGAN | CC0; BSD-3 | |

## Dismemberment: the approach the research points to

Do not cut a live, animated character along an arbitrary plane; the one open project that tried was withdrawn by its author as unsolvable. Instead, prepare each enemy in advance as separate limbs with closed stumps at fixed joints (neck, shoulders, elbows, hips, knees). On a hard enough blade hit near a joint: detach that limb as its own physical object, show both stumps, and play the effect. This is cheap enough for VR and simple to send over a network later ("joint N severed").

## Ruled out by licence

Non-commercial or research-only: Cube 3D and CubePart, PartField (and what depends on it: PartUV, OmniPart, PartSAM), RigAnything, MMAudio, TangoFlux, AudioX, Sony Woosh, F5-TTS, XTTS, FLUX.2 klein 9B and dev, Ubisoft CHORD, 4x-UltraSharp and similar upscalers, GVHMR. Territory or revenue limits: the Hunyuan family, SPAR3D, IndexTTS-2. Copyleft that would bind the game: Piper (GPL-3), KoboldCpp (AGPL), NobodyWho (EUPL). Vendor-tied: OVR LipSync.

## Already settled

One source repeated that TRELLIS.2 depends on a non-commercial NVIDIA library. Decision 0012 covers this: we use only ComfyUI's built-in nodes, which do not include it.
