# 0006: First generator rulings

- **Date:** 2026-10-09
- **Decided by:** Lead recommends; items marked *owner* wait for the owner. The enforced list is `data/approved-generators.json`; evidence is in THIRD_PARTY_LICENCES.md.

**The test.** A generator is approved only if (a) it runs locally and free, (b) its licence allows commercial use of outputs, (c) nothing in it stops output files being published under CC0, and (d) any contributor anywhere can use it.

**Approved.** Z-Image Turbo, FLUX.2 klein 4B, Animagine XL 4.0, ComfyUI, Blender, TripoSR, UniRig, Mesh2Motion, PBRify Remix, Marigold IID, Lotus Normal, ACE-Step 1.5, Kokoro.

**Conditional.** TRELLIS.2 (needs a proven workflow free of its non-commercial parts); StableGen and Stable Projectorz (approved models only; sub-model licences unread); DeepBump; Stable Fast 3D and Stable Audio Open (*owner*: registration and revenue ceiling); Chatterbox (*owner*: watermark).

**Rejected.** Hunyuan3D 2.1 and HY-Motion 1.0 fail test (d): their licences exclude the EU, UK and South Korea. NoobAI-XL, Qwen-Image 2.1, FLUX.2 dev, FLUX.2 klein 9B, CHORD, GVHMR, XTTS v2 fail (b). Mixamo fails (c).

**Consequence.** There is no approved animation generator and, until the owner rules, no approved sound-effect generator. Animation comes from CC0 libraries, hand work in Blender, and physics.

**Status changes** are licence rulings: the lead recommends, the owner decides, and a new decision entry records it. Checks accept only `approved`.
