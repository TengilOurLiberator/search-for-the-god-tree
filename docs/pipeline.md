# The asset pipeline

How assets are generated on the owner's PC. Everything here runs locally and unattended; none of it needs a Claude session, so it can keep working when session budget is low.

## Where things are

| Place | What |
|---|---|
| `D:\godtree-tools\` | Tools and weights, outside the repository: ComfyUI portable, `models\`, a Python 3.11 environment (`triposr-venv`), TripoSR, uv, Git LFS |
| `D:\godtree-work\` | Raw outputs, logs and contact sheets, outside the repository |
| `stylekit/workflows/` | ComfyUI workflows in API format |
| `stylekit/jobs/` | Job files (what to generate) and run logs (what was generated) |
| `tools/pipeline/` | The scripts below |

## The scripts

| Script | Does |
|---|---|
| `New-ConceptJobs.ps1` | Expands a `*.subjects.json` file into image jobs using `stylekit/prompt-fragments.json` |
| `Invoke-GenerationQueue.ps1` | Runs any ComfyUI job file (images, audio, cut-outs, 3D). Starts ComfyUI if needed, skips finished jobs, pauses above 83 C, rests between jobs |
| `New-ContactSheet.ps1` | Tiles a folder of images into review sheets |
| `Invoke-ModelQueue.ps1` | Image to 3D with TripoSR, then Blender finishing. Needs cut-out images |
| `triposr_run.py` | The only permitted way to run TripoSR (blocks its non-commercial background remover) |
| `blender_finish_model.py` | Decimate to budget, unwrap, bake, scale, export `.glb`, render a preview |
| `finish_audio.py` | Raw audio to 48 kHz Ogg Vorbis, with measurements |
| `Finish-TrellisModels.ps1` | Blender finishing for TRELLIS.2 output; refuses anything over its triangle budget |
| `Publish-Models.ps1`, `Publish-Audio.ps1` | Copy chosen results into `unity/Assets/Godtree/` and write their recipes |

## Typical runs

Concept images (about 3 seconds each):

```bash
powershell -ExecutionPolicy Bypass -File tools/pipeline/New-ConceptJobs.ps1 -SubjectsPath stylekit/jobs/concepts-v1.subjects.json
```

```bash
powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-GenerationQueue.ps1 -JobsPath stylekit/jobs/concepts-v1.json -OutDir D:\godtree-work\concepts-v1
```

Cut-outs, then models:

```bash
powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-GenerationQueue.ps1 -JobsPath stylekit/jobs/cutouts-v0.json -OutDir D:\godtree-work\cutouts
```

```bash
powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-ModelQueue.ps1 -JobsPath stylekit/jobs/models-v0.json -OutDir D:\godtree-work\models-v0-clean -CutoutDir D:\godtree-work\cutouts
```

Music and sound effects use `Invoke-GenerationQueue.ps1` with `music-v0.json` or `sfx-v0.json`, then `finish_audio.py`.

## Rules

- **Only approved generators** (`data/approved-generators.json`). Before using a new tool, list what it downloads on first run and read each licence ([decisions/0011](../decisions/0011-background-removal-incident.md)).
- **Nothing is an asset until it is published with a recipe** and passes `tools/checks/Run-Checks.ps1 -All`.
- **Do not run two queues against the same output folder at once**; they will race for files.
- **Start ComfyUI before a long chain of commands** if a calling script seems to hang at the end: a server started by a queue keeps the caller's output open.
- **Watch the GPU.** An RTX 5090 draws its full 600 W in image batches and its power limit cannot be lowered without administrator rights. Peak seen: 70 C.

## What the lead found (2026-10-10)

| Stage | Tool | Verdict |
|---|---|---|
| Concepts | Z-Image Turbo | Good and consistent. 3 s per image, 23 GB |
| Cut-outs | BiRefNet | Clean, also removes drawn shadows. Under 1 s |
| 3D | TripoSR | Placeholder grade: soft colour, fails on creatures and thin parts. About 10 s per model |
| 3D | TRELLIS.2 (ComfyUI built-in nodes) | **Approved (decision 0012).** Sharp colour and clean shape on hard-surface objects and characters at game budgets; 30 s to 6 minutes per model, up to 30 GB. Two workflows: `trellis2_image_to_glb` (remesh 384) for weapons and simple props, and `trellis2_image_to_glb_coarse` (remesh 192) for creatures, buildings and anything spiky, furry or thatched, which shatter at the finer setting |
| Music | ACE-Step 1.5 | Runs (90 s of music in 8 s). Unheard |
| Sound effects | Stable Audio Open 1.0 | Runs (3 s each). Unheard. Needs the gain cut that is in the workflow |
