# Survey: animation, modelling and agent tooling (2026-10-16)

Four research agents searched official repositories, model cards, docs and trade press, with news from July to October 2026. **Nothing here is installed or approved by being listed.** "Verified" means the licence was read at its source by the agent; the lead has not re-read any of it. Before use, the lead reads the licence and records it in `THIRD_PARTY_LICENCES.md` (decision 0011). Earlier survey: [tools-survey-2026-10-10.md](tools-survey-2026-10-10.md).

## The short version

| Need | Best route found | Licence (as reported) | State |
|---|---|---|---|
| Four-legged creatures that move | **Mesh2Motion** fox, horse and spider rigs with clips (horse added 2026-08-02), plus Unity Animation Rigging IK for feet on the ground | MIT code, CC0 rigs and clips (verified) | Ready; fox and spider already downloaded |
| More human animation | **Mesh2Motion**: over 160 CC0 human clips by update 13 (2026-09-25); we use 44 | CC0 | Ready |
| Human motion from text | **Kimodo SOMA-RP v1.2** (2026-10-15), or **ARDY** (SIGGRAPH 2026) for runtime | Apache-2.0 code; NVIDIA Open Model License weights, commercial use stated (verified) | Linux or WSL2; may need gated Llama-3 text encoder (unconfirmed for Kimodo). Coarse fingers. Never the SMPL-X checkpoint (research only) |
| Motion capture from a phone video | **GEM-X** (NVIDIA), through the early **video2mocap** wrapper | Apache-2.0 code; Open Model License weights (verified) | 77 joints with hands; Linux or WSL2; untested |
| Multi-leg walking baked in Blender | **Spider Walker** add-on (2026-08-21) | MIT (verified); partly AI-written | Blender 5.2; review the code first |
| Creatures that path-find | Unity **AI Navigation** package | Unity Companion License | In Unity 6; fallback DotRecast (Zlib) |
| Moving mouths | **uLipSync** (live) and **Rhubarb** (baked lines) | MIT; MIT with BSD parts | Ready; faces need hand-made shape keys |
| 3D from several views | **Pixal3D multi-view** in ComfyUI's built-in nodes (weights 2026-08-31) | MIT since 2026-05-20; before that academic-only and no EU. Pin a commit after that date | Try on the models that failed |
| Clean low-poly props | **AutoRemesher 1.0** (2026-07) or **Instant Meshes**, then a bake in Blender | MIT; BSD-3 | Scriptable from the command line |
| Concept turnarounds for multi-view | **Qwen-Image-Edit-2511** with the Multiple-Angles and turnaround LoRAs | Apache-2.0 | |
| Painted-look control on a mesh | **StableGen** (Qwen or FLUX.2 klein 4B modes only), **Material Maker** | GPL-3.0 tool; MIT | Avoid StableGen's TRELLIS.2 native mode (nvdiffrast) |

## Tools an agent can drive (MCP and command line)

| Tool | What it adds | Licence and cost | Notes |
|---|---|---|---|
| **Unity CLI** with the Pipeline package (official; replaces the deprecated in-Editor MCP) | Play mode, tests, screenshots, builds, C# evaluation, `unity mcp` | Free per Unity's docs | Beta (1.0.0-beta.13, 2026-10-07). Needs `unity auth login`; whether Unity Personal qualifies is **not stated anywhere found** |
| **CoplayDev/unity-mcp** | The same, community | MIT | v10.3.0 (2026-10-04); fallback if the Unity CLI needs a paid plan |
| **Blender Lab MCP** (official) | Screenshots, renders, bpy API search, Python | GPL-3.0, free; Blender 5.1+ | v1.0.3 (2026-09-11). Open Windows bugs: headless mode hangs |
| **mcp-for-blender** (was blender-mcp) | Multi-angle and animation-strip views | MIT; paid generation optional | Telemetry on by default |
| **dcc-mcp-blender** | Typed rigging and animation tools | MIT | Pre-1.0, 47 stars: trial only |
| **Comfy-Org/comfy-mcp** | Run ComfyUI workflows | AGPL-3.0 or commercial | Use as an outside tool, never copy into the repository |
| **kimodo-mcp** (wifimetas) | Kimodo with presets, checks and FBX/GLB export | Apache-2.0 | One-person project; audit before use |

## Ruled out

| Item | Why |
|---|---|
| Cascadeur Free | Non-commercial only; exports only its own format. Quadruped posing is Pro |
| UniMate | Non-commercial weights; trained on Mixamo |
| Truebones Zoo | Redistribution forbidden |
| Kimodo SMPL-X; GVHMR, WHAM, TRAM, PromptHMR | Research or non-commercial |
| BONES-SEED dataset | Restricted; never in the repository. Prefer Kimodo RP checkpoints over SEED ones |
| Qwen-Image 2.1, Meta MeshFlow, TriFlow, PartPacker, PartUV (via PartField) | Research or non-commercial |
| RMBG (BRIA) in any tool's default path: Pixal3D-ComfyUI `auto_remove`, PartCrafter | Non-commercial. Use BiRefNet |
| PhilS94 spider, GPL spider projects | No licence or copyleft |
| Unity's new animation system, motion matching for creatures | Paused or not useful yet |

## Not found

Nothing free generates four-legged motion; no permissive neural retopology worth trusting; nothing that handles glass or glow in 3D generation (model the solid part and make the glow in Unity); no free automatic facial rig; no fix for the physics hand trailing the controller in fast swings (BIMOS 2.x is unreleased and its recent commits do not address it).

## Sources

Per-item links are in the agents' reports as kept by the lead; the main ones: [Mesh2Motion news](https://mesh2motion.org/news), [Kimodo](https://github.com/nv-tlabs/kimodo), [ARDY](https://github.com/nv-tlabs/ardy), [GEM-X](https://github.com/NVlabs/GEM-X), [video2mocap](https://github.com/freddysae0/video2mocap), [Spider Walker](https://github.com/iltaen/spider_walker), [uLipSync](https://github.com/hecomi/uLipSync), [Rhubarb](https://github.com/DanielSWolf/rhubarb-lip-sync), [Pixal3D licence commit](https://huggingface.co/TencentARC/Pixal3D/raw/b648b3c5a83fcee3628c8c608a64e26d73cdfce4/LICENSE), [AutoRemesher](https://www.cgchannel.com/2026/07/get-free-retopology-tool-autoremesher-and-its-blender-bridge/), [StableGen](https://github.com/sakalond/StableGen), [Unity CLI](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli), [Blender Lab MCP](https://projects.blender.org/lab/blender_mcp), [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp), [Cascadeur plans](https://cascadeur.com/plans), [AI Navigation licence](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/license/LICENSE.html).
