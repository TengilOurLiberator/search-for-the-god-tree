# Lead hand-off: where the work stands

Updated 2026-10-10, evening. Read this after AGENTS.md and ROADMAP.md. It is the working state that is not obvious from the files.

## What is running or waiting right now

Nothing is running. State at the end of 2026-10-10:

- **Every model in the sandbox is in the new look** (decision 0014): 21 weapons, 8 characters, 6 creatures, scenery and props. Review sheets are in `docs/review/session-1/`.
- **The sandbox is a small place now, not a row of objects.** `SandboxBuilder.BuildGallery` holds a layout table (name, x, z, turn): a road from the spawn through a stone arch toward the Godtree, a camp with a lit fire on the right, a house, well and cart on the left, creatures beyond the wall. People stand at named spots. A model not in the table still appears, in a row at z = 38, so nothing is silently hidden.
- **Ground:** `Godtree/Ground` paints by world position (grass at two sizes, dry patches, the road). The ring of hills uses the same material. To move the road, change the shader's road properties; it always runs along world Z.
- **Training dummy:** one generated model drawn three times, each material showing one slice (`_ClipAxis/_ClipMin/_ClipMax` on `Godtree/Toon`). Cut heights come from the model's outline. Replace the model and the cuts follow.
- **Screenshots** (`Godtree > Render Sandbox Screenshots`) now include road, camp and gate views.
- **Player body:** a generated character on the rig's skeleton (decision 0015). To make another: a concept with the `character_tpose` framing, TRELLIS.2, `Finish-TrellisModels.ps1`, then `blender_rig_body.py` (look at its three check pictures), publish, and point `SandboxBuilder.BuildPlayerBody` at it. The rig hides the head by shrinking the neck bone to zero; anything attached to the head must be placed from the neck and chest, as `PlayerBody.LateUpdate` does.
- **Sky, wind, fire, sparks:** `Godtree/Sky` (clouds from noise), `_Sway` on `Godtree/Toon` (set per model in `ApplyToonToGeneratedModels`), `Godtree/Glow` for flames, sparks and trails.
- Twelve PlayMode tests pass. **The owner has seen none of the evening's work in the headset.**
## 3D: what works now

Spiky, furry and thatched shapes shattered at the standard remesh setting (384). The **coarse workflow** (`stylekit/workflows/trellis2_image_to_glb_coarse.api.json`, remesh 192) fixed twelve of thirteen: Burrling, both Thornbacks, root crawler, husk house, well, cart, stump, cradle shrine, fence, merchant, shield-bearer. Use it for creatures and buildings; keep the standard one for weapons and simple props. Still failing: the Rootwarden (noisy surface), amber orbs (staff, wand), flames (campfire), arch, boulder. The Rootwarden and husk shield in the sandbox are the last old-look models.

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
- Hand tracking: wanted; researched, not started (docs/research/hand-tracking-and-player-body.md). Over Link it is a developer-only feature that may not work in a built game. First step is the owner switching on Developer Runtime Features in the Link app; then a one-to-two-day editor prototype.
- Rename "the Underroot" (docs/name-check.md).
- Listen to the audio; none of it has been heard.
- Purge of superseded files from git history (decision 0011): lead recommends leaving it.
- GPU power cap for unattended runs.

## Next ideas, not started

- Show equipped items on the player's body; bend the fingers (the generated hand is one solid shape).
- Enemies that move and attack, so vigor and armour mean something.
- Clouds, and a painted far landscape behind the hills.
- The elder model has small holes in its face; regenerate it.