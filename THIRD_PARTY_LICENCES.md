# Third-party licences and compatibility

What we read, where, and when. This file is the evidence behind [data/approved-generators.json](data/approved-generators.json) and the stack table in [AGENTS.md](AGENTS.md).

- **Verified on:** 2026-10-09, from official repositories, model pages and terms pages (links per entry).
- **How:** most pages were read through a tool that summarises, so wording here is paraphrased. A few gated licence files could not be opened and were read from the publisher's official repository instead. Anything not confirmed from a primary source is marked **not verified**.
- **This is a reading of licence text, not legal advice.** Entries marked *owner decision* have legal weight and wait for the owner.
- **Found something wrong or out of date?** Say so in your pull request or open a licence report issue. That is useful work.

What this project needs from a licence: commercial use of outputs, and redistribution of the output files in a public repository under CC0. Almost no licence addresses public-repository redistribution directly; "allowed" below means the licence either disclaims rights in outputs or places no restriction on them.

## 1. Engine and packages

Third-party packages are **referenced by version in the Unity package manifest and fetched by each contributor. They are not copied into this repository** and are not covered by this project's MIT licence.

| Item | Licence | Supports Unity | State | Verdict | Source |
|---|---|---|---|---|---|
| Unity 6 | Unity Editor Software Additional Terms (updated 2026-06-30). Unity Personal is free under US $200,000 revenue plus funding in 12 months. | n/a | 6000.0 LTS support ends October 2026. **6000.3 LTS** is supported until December 2027. | **Use 6000.3 LTS.** Terms neither forbid nor expressly permit committing project files; keep `Library/` and all engine binaries out. | [support](https://unity.com/releases/unity-6/support), [terms](https://unity.com/legal/editor-terms-of-service/software) |
| BIMOS (physics rig) | MIT | 6000.0; depends on URP 17 and OpenXR 1.12; no vendor SDK | Single author. Only release v1.0.0 (2024-12-06); active development on branches (2.0.0 unreleased adds a Meta OpenXR package). Known limits: no custom avatars, physics tied to 144 Hz, no haptics. | OK. Not yet tested on 6000.3. | [repo](https://github.com/KadenZombie8/BIMOS) |
| BIMOS-Active-Ragdolls | **No licence file at all** | "6000.0+" | Third-party add-on created 2026-09-29, no releases | **Not usable as-is.** With no licence, nobody has permission to use it. Usable if its author adds one. *Owner question: is this your repository?* | [repo](https://github.com/Corbino50211/BIMOS-Active-Ragdolls) |
| UltimateXR (fallback rig) | MIT | 2021.2+; Unity 6 not mentioned | Last release 2024-01 | **Dropped.** No OpenXR integration; needs vendor SDKs, which the stack forbids. | [repo](https://github.com/VRMADA/ultimatexr-unity) |
| FishNet | Custom FirstGearGames licence (not MIT, not an open-source licence). Grants reproduce, modify and use for a developed game; says nothing about distributing the source. | Unity 6 LTS "fully supported" per docs | Very active (v4.7.3, 2026-09-02) | OK **only as a manifest reference**; never vendored. | [licence](https://github.com/FirstGearGames/FishNet/blob/main/LICENSE.md), [compatibility](https://fish-networking.gitbook.io/docs/overview/readme/features/unity-compatibility) |
| FishySteamworks | BSD-2-Clause | Not stated | **Stale:** last release 4.1.1 (2024-08-26) while FishNet is at 4.7.3 | Licence OK. Compatibility with current FishNet **not verified**; test first thing in the multiplayer milestone. | [repo](https://github.com/FirstGearGames/FishySteamworks) |
| Steamworks.NET | MIT | 2019.4+ | Active (2026-08) | OK as a manifest reference. It ships Valve's binaries inside the package. | [repo](https://github.com/rlabrecque/Steamworks.NET) |
| Steamworks SDK binaries | Valve Steamworks SDK Access Agreement (proprietary). Redistributable binaries may be distributed with your software in object-code form. | n/a | n/a | **Never commit the DLLs.** They arrive through the Steamworks.NET package and ship only inside game builds. MIT code is compatible with Steam distribution per Valve. Read section 1.1 yourself before relying on this. | [agreement](https://partner.steamgames.com/documentation/sdk_access_agreement), [open source](https://partner.steamgames.com/doc/sdk/uploading/distributing_opensource) |
| UniVRM and MToon | MIT | 2022.3 LTS "or later"; Unity 6 not named | Active (v0.131.3, 2026-10-02) | OK. **Use VRM 1.0** (`com.vrmc.vrm`): the only MToon shader for URP is MToon10; VRM 0.x falls back to unlit. | [repo](https://github.com/vrm-c/UniVRM), [materials](https://vrm.dev/en/univrm1/material/) |
| VRoid Studio | Proprietary pixiv terms (revised 2026-06-24). Commercial use and distribution of exported models allowed. | n/a | Active | ***Owner decision.* Output cannot be CC0.** pixiv's base body, preset items and sample models stay pixiv's; the guidelines say they are "not CC0", and the licence to them cannot be sublicensed. Every VRoid export contains the base body. Also: building a character-creation application from VRoid data needs a separate pixiv licence. | [guidelines](https://vroid.com/en/studio/guidelines), [terms](https://policies.pixiv.net/en.html#vroidstudio) |
| Yarn Spinner for Unity | MIT | 2022.3+; Unity 6 not named | Active (v3.2.8, 2026-08) | OK. Free version available by git or OpenUPM. | [repo](https://github.com/YarnSpinnerTool/YarnSpinner-Unity) |
| UnInventory | MIT | 2019.2 | **Abandoned:** one release, last commit 2020-05-22; flat-screen drag-and-drop interface | **Dropped** on technical grounds; inventory will be written in-house. | [repo](https://github.com/GoodCatGames/UnInventory) |
| Unity Behavior | Unity Terms of Service (not the Companion License) | 6000.0; latest 1.0.16 (2026-05) | **Maintenance mode** per official Unity posts (2026-05-01): fixes only, no new features | OK as a manifest reference, with risk. Keep enemy AI behind our own interface so it can be replaced. | [licence](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/license/LICENSE.html), [announcement](https://discussions.unity.com/t/update-on-behavior-package-support-and-team-presence/1718517) |

### Steam

| Claim | Finding | Source |
|---|---|---|
| Testing with the shared app ID 480 is free | Valve's docs name 480 as the example game's ID and describe testing with `steam_appid.txt`. **No Valve statement was found that expressly authorises testing your own game on 480**; it is a widespread convention. Limits: never ship `steam_appid.txt`; Steam must be running; two builds on one PC cannot connect, so tests need two accounts on two machines. | [docs](https://partner.steamgames.com/doc/sdk/api/example) |
| A Steam release costs a one-time $100 | Confirmed: US $100 per new app, non-refundable, recoupable after US $1,000 adjusted gross revenue. | [docs](https://partner.steamgames.com/doc/gettingstarted/appfee) |

## 2. Generators and tools

### Approved

| Item | Code licence | Weights licence | Video memory | Notes | Source |
|---|---|---|---|---|---|
| Z-Image Turbo | Apache-2.0 | Apache-2.0 | 16 GB | Bundled text encoder (Qwen3) is Apache-2.0. Origin of the bundled VAE not named. | [model](https://huggingface.co/Tongyi-MAI/Z-Image-Turbo) |
| FLUX.2 klein 4B | Apache-2.0 | Apache-2.0 | 13 GB (model page) or 8 GB (repository) | The publisher's separate usage policy was not read. | [model](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B) |
| Animagine XL 4.0 | n/a | CreativeML Open RAIL++-M, no added terms | not stated | Licensor claims no rights in outputs. Use restrictions (Attachment A) bind whoever runs the model. | [model](https://huggingface.co/cagliostrolab/animagine-xl-4.0) |
| ComfyUI | GPL-3.0 | n/a | n/a | GPL does not reach generated images. Never copy its code here. Each custom node has its own licence. | [licence](https://github.com/comfyanonymous/ComfyUI/blob/master/LICENSE) |
| Blender | GPL-2.0-or-later | n/a | n/a | Blender states that files you create are yours. Scripts using its API must be GPL-compatible if published. | [licence](https://www.blender.org/about/license/) |
| TripoSR | MIT | MIT | about 6 GB | Licences of its image tokenizer and background remover not read. | [repo](https://github.com/VAST-AI-Research/TripoSR) |
| UniRig | MIT | MIT by model-page tag only | 8 GB | Upstream component licences not read. | [repo](https://github.com/VAST-AI-Research/UniRig) |
| Mesh2Motion | MIT | n/a | n/a | Bundled rigs and animations are CC0. | [repo](https://github.com/scottpetrovic/mesh2motion-app) |
| PBRify Remix | n/a | CC0, trained on CC0 data | not stated | Best licence fit for material maps. Runs in chaiNNer. | [repo](https://github.com/Kim2091/PBRify_Remix) |
| Marigold IID Appearance v1-1 | n/a | CreativeML Open RAIL++-M | not stated | | [model](https://huggingface.co/prs-eth/marigold-iid-appearance-v1-1) |
| Lotus Normal g v1-1 | n/a | Apache-2.0 | not stated | | [model](https://huggingface.co/jingheya/lotus-normal-g-v1-1) |
| ACE-Step 1.5 | MIT | MIT | under 4 GB claimed; XL models 12 to 20 GB | Model page says generated music may be used commercially. Training-data claim is self-reported. | [repo](https://github.com/ace-step/ACE-Step-1.5) |
| Kokoro-82M | Apache-2.0 | Apache-2.0 | not stated | espeak-ng (GPL-3.0) is a separate tool; never bundle it. Per-voice terms not read. | [model](https://huggingface.co/hexgrad/Kokoro-82M) |

### Conditional (not accepted by the checks until promoted)

| Item | Licence | The condition | Source |
|---|---|---|---|
| TRELLIS.2 | Code and weights MIT, **but the default pipeline loads non-commercial parts**: the RMBG-2.0 background remover (CC BY-NC 4.0) and Nvidia's nvdiffrast and nvdiffrec (research and evaluation only). Its image encoder uses the custom DINOv3 licence (no commercial limit found). Needs 24 GB. | A workflow that supplies pre-masked images and avoids the Nvidia code paths must be proven in the pipeline milestone. Which stages call those libraries has **not been traced in the code**. | [repo](https://github.com/microsoft/TRELLIS.2) |
| Stable Fast 3D | Stability AI Community License (2024-07-05) | *Owner decision.* Commercial use requires registering with Stability AI and is free only under US $1,000,000 annual revenue. You own outputs; they may not train other foundation models. | [licence](https://github.com/Stability-AI/stable-fast-3d/blob/main/LICENSE.md) |
| Stable Audio Open 1.0 and Small | Stability AI Community License | *Owner decision.* Same terms as above. Trained partly on CC BY audio; whether that creates any duty on outputs is not addressed. **This is the only sound-effect generator on the candidate list.** | [model](https://huggingface.co/stabilityai/stable-audio-open-1.0) |
| Chatterbox | MIT | *Owner decision.* Every output carries an inaudible watermark with no documented way to turn it off. | [repo](https://github.com/resemble-ai/chatterbox) |
| StableGen | GPL-3.0 | Approved checkpoints only; never its native TRELLIS.2 texture mode. IPAdapter and ControlNet model licences not read. | [repo](https://github.com/sakalond/StableGen) |
| Stable Projectorz | AGPL-3.0 (now open source; the brief believed it closed) | Approved back-end models only. Bundled ControlNet licences not read. | [repo](https://github.com/IgorAherne/StableProjectorz) |
| DeepBump | GPL-3.0 code; bundled models carry no separate licence | Training data undisclosed. | [repo](https://github.com/HugoTini/DeepBump) |

### Rejected

| Item | Licence | Why | Source |
|---|---|---|---|
| Hunyuan3D 2.1 | Tencent Hunyuan 3D 2.1 Community License | **Licence does not apply in the EU, UK or South Korea.** Not every contributor could reproduce an asset. Was "on hold" in the brief. | [licence](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1/blob/main/LICENSE) |
| HY-Motion 1.0 | Tencent HY-Motion 1.0 Community License | **Same territorial exclusion.** Also forbids using outputs to improve other AI models, which a CC0 dedication cannot pass on, and its FBX export depends on an Autodesk SDK wheel whose licence could not be opened. Was a candidate in the brief. | [licence](https://huggingface.co/tencent/HY-Motion-1.0) |
| NoobAI-XL and merges | Fair AI Public License 1.0-SD plus authors' terms | Added terms prohibit commercial use of generated products. Confirmed. | [model](https://huggingface.co/Laxhar/noobai-XL-1.1) |
| Qwen-Image 2.1 | Qwen Research License (2026-09-20) | Research and evaluation only. Confirmed. | [licence](https://huggingface.co/Qwen/Qwen-Image-2.1) |
| FLUX.2 dev | FLUX [dev] Non-Commercial License v2.0 | Outputs may be used commercially, but the model may only be run for non-commercial purposes. Confirmed. | [licence](https://github.com/black-forest-labs/flux2/tree/main/model_licenses) |
| FLUX.2 klein 9B | FLUX Non-Commercial License v2.1 | Same. Confirmed. | [model](https://huggingface.co/black-forest-labs/FLUX.2-klein-9B) |
| CHORD (and ComfyUI-Chord) | Ubisoft Machine Learning License (Research-Only - Copyleft) | Research only. Confirmed. | [repo](https://github.com/ubisoft/ubisoft-laforge-chord) |
| GVHMR | Custom non-commercial; needs SMPL and SMPL-X (non-commercial, no redistribution) | Confirmed. | [repo](https://github.com/zju3dv/GVHMR) |
| XTTS v2 | Coqui Public Model License 1.0.0 | Non-commercial limit attaches to outputs. Confirmed. | [licence](https://huggingface.co/coqui/XTTS-v2) |

### Not verified

StableMaterials (licence tag only, no text), PBRFusion (model page gated), ComfyUI-TextureAlchemy (no licence shown). Do not use.

## 3. Ready-made asset sources

Only CC0 material may be folded into assets ([decisions/0007](decisions/0007-cc0-only-asset-inputs.md)).

| Source | Licence | Redistribution in a public repository | Notes | Source |
|---|---|---|---|---|
| Poly Haven | CC0 | Yes, stated explicitly | Site logos and renders are not CC0. No scraping. | [licence](https://polyhaven.com/license) |
| ambientCG | CC0 1.0 | Yes, stated explicitly | | [licence](https://docs.ambientcg.com/license/) |
| Quaternius | CC0 | Yes (by CC0) | Paid-tier files: check the licence file inside the download. | [faq](https://quaternius.com/faq.html) |
| Kenney | CC0 | Yes (by CC0) | The Kenney logo is reserved. | [support](https://kenney.nl/support) |
| KayKit | CC0 1.0 | Yes (by CC0) | Verified on one pack. | [itch](https://kaylousberg.itch.io/kaykit-adventurers) |
| **Mixamo** | Adobe General Terms of Use (2025-10-03) | **No** | The Mixamo FAQ allows use in games but is silent on redistribution. Adobe's General Terms, section 3.6, allow its content files to be embedded in your product but never distributed stand-alone, and the licence cannot be sublicensed. Raw or retargeted clips in a public CC0 repository are therefore out. That section 3.6 covers Mixamo is the natural reading but Adobe does not say so in so many words. | [faq](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html), [terms](https://www.adobe.com/legal/terms.html) |

## 4. What changed from the original brief

1. **Animation has no approved generator.** HY-Motion is rejected and Mixamo cannot be used. What remains: CC0 animation libraries (Quaternius, KayKit, Mesh2Motion), hand animation in Blender, and physics-driven motion, which is the plan for enemies anyway.
2. **Sound effects have no approved generator** until the owner rules on the Stability AI licence.
3. **VRoid avatars cannot be CC0.** Owner decision needed on how avatars are licensed or made.
4. **TRELLIS.2 is conditional**, and Hunyuan3D 2.1 is rejected, so the only unconditionally approved 3D generator today is TripoSR.
5. **Unity 6000.3 LTS**, not 6000.0.
6. **UltimateXR dropped** (no OpenXR). There is currently no fallback physics rig.
7. **BIMOS-Active-Ragdolls has no licence.**
8. **UnInventory dropped** (abandoned, flat-screen).
9. **FishNet, Steamworks.NET and Unity Behavior are referenced, never vendored.**
10. **FishySteamworks is two years stale**; untested against current FishNet.
11. **Stable Projectorz is open source** (AGPL-3.0), not closed.
12. **Unity Behavior is in maintenance mode.**

## 5. Not confirmed from a primary source

- Autodesk FBX SDK licence and the wheel HY-Motion uses for export.
- Licences of sub-models: CLIP ViT-L/14, the TripoSR tokenizer and background remover, UniRig's upstream components, ACE-Step's text encoder and VAE, Kokoro's per-voice terms.
- Whether Chatterbox's watermark can be disabled.
- Paid-tier licence files for Quaternius and KayKit.
- Explicit Unity 6 support for UniVRM and Yarn Spinner (both say "2022.3 or later").
- BIMOS on Unity 6000.3.
- Video memory for Animagine XL 4.0, Kokoro, Chatterbox, Stable Audio Open and the material models.

## 6. Owner rulings after verification (2026-10-09, decision 0009)

These override the "owner decision" and "conditional" marks above for the items named.

- **Stable Audio Open: approved.** The owner must register with Stability AI before any commercial release, and the US $1M revenue ceiling applies.
- **Chatterbox: approved**, accepting the watermark. Kokoro remains the default.
- **VRoid Studio: approved for placeholder avatars only**, kept in a separate folder under pixiv's terms and excluded from CC0.
- **Stable Fast 3D: not ruled on**; stays conditional.
- **BIMOS-Active-Ragdolls: not used.** Active ragdolls are written in-house.
