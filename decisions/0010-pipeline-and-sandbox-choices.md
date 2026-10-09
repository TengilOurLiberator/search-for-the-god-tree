# 0010: Pipeline and sandbox choices made in session 1

- **Date:** 2026-10-10
- **Decided by:** Lead (technical; no cost, no licence change beyond what decision 0009 approved)

| Choice | Reason |
|---|---|
| **Generation tools live outside the repository** in `D:\godtree-tools` (ComfyUI portable 0.39.0, model weights, a Python 3.11 environment for TripoSR and audio finishing, uv, Git LFS). Raw outputs go to `D:\godtree-work`. | Tools and weights are large and are not ours to redistribute. Only recipes, workflows and finished assets are committed. |
| **Generation is driven by queue scripts** in `tools/pipeline/` that read job files in `stylekit/jobs/` and can run unattended and resume. | The owner asked that generation continue on the PC without using session budget. Job files double as provenance. |
| **ComfyUI is the host for image and audio models**; workflows are stored in API format in `stylekit/workflows/`. | One server, one interface, and the workflow file is the recipe. |
| **TripoSR runs with a local stand-in for `torchmcubes`** (`torchmcubes.py`, marching cubes via scikit-image) and `transformers` pinned to 4.35.0. | The real package needs a C++ build; newer `transformers` cannot load the weights. |
| **Models are finished automatically in Blender** (`blender_finish_model.py`): stand upright, weld, decimate to budget, unwrap, bake vertex colour to texture, scale, export `.glb`. | Repeatable, no hand editing, and the triangle budget is met by construction. |
| **Audio is finished by `finish_audio.py`**: 48 kHz Ogg Vorbis, sound effects mono and trimmed, peak-levelled. A gain cut sits in the sound-effect workflow. | Stable Audio Open's impacts clipped at full scale before the gain cut was added. |
| **Unity imports `.glb` through `com.unity.cloud.gltfast` 6.20.0** (Apache-2.0, read on its repository 2026-10-10; referenced in the manifest, not vendored). `.glb` stays the model interchange format. | The checks measure `.glb` directly, and Unity needs an importer for it. |
| **The sandbox is built by an editor script** (`Godtree > Build Sandbox`) from primitives plus whatever generated assets exist. | The scene can always be rebuilt from nothing and never depends on a hand-edited file. |
| **GPU power cannot be capped without administrator rights**, so queues rest between jobs and pause above 83 C. | The RTX 5090 drew its full 600 W during image batches. Peak temperature seen: 65 C. |

**Quality verdicts (lead's judgement, session 1).**
- Z-Image Turbo concepts: good, consistent, usable as the visual target.
- TripoSR models: placeholder grade. Fine for simple props and weapons; poor on creatures, trees, thin parts; colour is soft.
- Audio: unjudged. Nobody has listened.
- Sandbox: unjudged in a headset.
