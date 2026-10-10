# Lead hand-off: where the work stands

Updated 2026-10-10, mid-afternoon. Read this after AGENTS.md and ROADMAP.md. It is the working state that is not obvious from the files.

## What is running or waiting right now

1. **TRELLIS.2 run for the whole sandbox kit in the new look** (decision 0014).
   - Job file: `stylekit/jobs/trellis2-f1.json` (60 models). Raw output: `D:\godtree-work\trellis2-f1\*.glb`. Log: `queue.log` there.
   - It was started detached. If it is not running and fewer than 60 `.glb` files exist, start it again; it skips finished jobs:
     `powershell -ExecutionPolicy Bypass -File tools/pipeline/Invoke-GenerationQueue.ps1 -JobsPath stylekit/jobs/trellis2-f1.json -OutDir D:\godtree-work\trellis2-f1 -JobTimeoutSeconds 2400`
2. **Then, in order:**
   1. Finish: `tools/pipeline/Finish-TrellisModels.ps1 -JobsPath stylekit/jobs/trellis2-f1.json -RawDir D:\godtree-work\trellis2-f1 -OutDir D:\godtree-work\trellis2-f1\finished`
   2. Contact sheet of `*_preview.png` with `New-ContactSheet.ps1`, and **look at every model**. Keep only the good ones (decision 0013: generate many, keep few).
   3. Publish keepers with `Publish-Models.ps1 -Trellis -ModelJobs stylekit/jobs/trellis2-f1.json -ConceptSeeds stylekit/golden/f1/README.md -WorkDir D:\godtree-work\trellis2-f1\finished -Ids "..."`, using `-ConceptJobs stylekit/jobs/concepts-f1-models.json` for most and `stylekit/jobs/concepts-f1-redo.json` for `wpn_sword_training`, `env_boulder`, `env_rock_cluster`.
   4. Remove old-style models that have no new-style replacement and look out of place.
   5. Close Unity, run `Godtree.EditorTools.SandboxBuilder.Build`, run the PlayMode tests, run `SandboxBuilder.Screenshots`, look at `docs/images/sandbox_player_view.png`.
   6. Run `tools/checks/Run-Checks.ps1 -All`, commit, push.

## Rules learned the hard way

- **Never run generation while the owner is in the headset.** It takes the whole graphics card. Never run two queues into one output folder.
- **Unity must be closed** before any batch-mode build or test; the editor locks the project.
- **Open the sandbox for the owner** with `Unity.exe -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.OpenAndPlay`. The headset must be on and Link running or the VR session stays idle and nothing is recorded.
- **Recordings** are in `D:\godtree-work\recordings\<date-time>\`. `D:\godtree-work\analyse.py` summarises one (edit the path at the top). No events and a constant 24.5 cm hand gap means the headset was not tracking.
- **Git** on this machine needs `D:\godtree-tools\git-lfs` on PATH and, for pushes, `GCM_INTERACTIVE=always` and `GIT_TERMINAL_PROMPT=1` in the shell.
- **Concepts that feed 3D must be solid shapes**: no glow, glass, flames, thin loops. Objects use `style_object` from the fragments file.
- **Before using any new tool**, list what it downloads and read each licence (decision 0011).

## Open with the owner

- Has not yet felt, in the headset: point-and-pull, snapped weapon holds, the character menu, damage numbers, lift and ward knots, the eleven newer weapons. Weapon grip points and angles are computed guesses.
- Hand tracking: wanted; not started; movement without thumbsticks is undecided.
- Rename "the Underroot" (docs/name-check.md).
- Listen to the audio; none of it has been heard.
- Purge of superseded files from git history (decision 0011): lead recommends leaving it.
- GPU power cap for unattended runs.

## Next ideas, not started

- A toon shader in Unity so models show the painted look instead of flat standard lighting.
- Rigging the character models (UniRig is approved) so the mirror shows a real character and equipment can appear on it.
- Landscape and key art in the new look for a sky backdrop with the Godtree.
- Enemies that move and attack, so vigor and armour mean something.
