# Lead hand-off: where the work stands

Updated 2026-10-10, mid-afternoon. Read this after AGENTS.md and ROADMAP.md. It is the working state that is not obvious from the files.

## What is running or waiting right now

Nothing is running. State at the end of 2026-10-10:

- **Sixty models were generated in the new look** (`stylekit/jobs/trellis2-f1.json`); the lead kept 40 and discarded 20. Review sheets: `docs/review/session-1/models-f1-weapons.jpg` and `models-f1-rest.jpg`.
- **Kept (new look):** 19 weapons (training sword, short sword, greatsword, scimitar, rapier, dagger, woodcutter's axe, hand axe, pickaxe, maul, war hammer, root club, spear, halberd, quarterstaff, bow, kite shield, buckler, torch); 6 characters (starter, weaver, innkeeper, smith, elder, child); the Barkbound; scenery (banner, bush, rock cluster, stone wall, tent, tree); props (barrel, bench, chest, crate, lantern, market stall, mote vial, signpost).
- **Discarded, and why:** anything spiky, furry, thatched or with thin spokes still shatters in mesh reduction (Burrling, both Thornbacks, Rootwarden, root crawler, husk house, well, cart, stump, cradle shrine, fence); amber orbs turn black (staff, wand); flames become a flat card (campfire); plus the arch, boulder, merchant, shield-bearer, husk shield and dummy.
- **Still old-look placeholders in the sandbox:** husk shield, cart, cradle shrine, dummy model, fence, well, husk house, Thornback, Rootwarden. Replace or remove them when a method for those shapes exists.
- **The sandbox has been rebuilt** with all of the above; nine PlayMode tests pass. The owner has not seen it in the headset.

## The real open problem in 3D

Shapes with many thin or spiky parts. Ideas not yet tried: keep the dense mesh and bake a normal map onto a simple low-poly shell (the stock ComfyUI template does this); generate such subjects at a coarser remesh resolution; or redraw them as chunky solid forms the way the retries were.
## Rules learned the hard way

- **Never run generation while the owner is in the headset.** It takes the whole graphics card. Never run two queues into one output folder.
- **Unity must be closed** before any batch-mode build or test; the editor locks the project.
- **Open the sandbox for the owner** with `Unity.exe -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.OpenAndPlay`. The headset must be on and Link running or the VR session stays idle and nothing is recorded.
- **Recordings** are in `D:\godtree-work\recordings\<date-time>\`. `D:\godtree-work\analyse.py` summarises one (edit the path at the top). No events and a constant 24.5 cm hand gap means the headset was not tracking.
- **Git** on this machine needs `D:\godtree-tools\git-lfs` on PATH and, for pushes, `GCM_INTERACTIVE=always` and `GIT_TERMINAL_PROMPT=1` in the shell.
- **Concepts that feed 3D must be solid shapes**: no glow, glass, flames, thin loops. Objects use `style_object` from the fragments file.
- **Before using any new tool**, list what it downloads and read each licence (decision 0011).
- **Run `tools/checks/Run-Checks.ps1 -All` and `tools/checks/tests/Run-Tests.ps1` before every commit and do not push if either fails.** Twice a failing state was pushed because the result was printed but not acted on.
- **Look before you trust.** `Godtree > Render Grip Check` (docs/images/grip_check.png) shows every weapon's hold; the PlayMode tests save a picture of the character menu (docs/images/menu_preview.png); `Godtree > Render Sandbox Screenshots` shows the scene. Each of these caught real faults that the tests alone did not.
- **A material created while its shader is broken keeps zeroed properties.** Delete such materials and rebuild.

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
