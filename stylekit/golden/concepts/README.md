# Golden concepts, v0

Reference images that define the look of Zone 1. They are concept art, not game assets: use them as the visual target and as input images for 3D generation.

- **Generator:** Z-Image Turbo (`z_image_turbo_bf16`, Apache-2.0) in ComfyUI 0.39.0, 9 steps, cfg 1, `res_multistep` / `simple`.
- **Workflow:** [../../workflows/z_image_turbo_t2i.api.json](../../workflows/z_image_turbo_t2i.api.json)
- **Prompts:** built by `tools/pipeline/New-ConceptJobs.ps1` from [../../jobs/concepts-v0.subjects.json](../../jobs/concepts-v0.subjects.json) and [../../prompt-fragments.json](../../prompt-fragments.json) (fragments version 0). The exact prompt and seed of every image is in [../../jobs/concepts-v0.json](../../jobs/concepts-v0.json); the run log is [../../jobs/concepts-v0.log.jsonl](../../jobs/concepts-v0.log.jsonl).
- **Hardware:** RTX 5090, about 3 seconds per image, peak video memory about 23 GB (the full-precision model; the model card says 16 GB cards work).
- **Made:** 2026-10-09. 88 images generated (22 subjects, 4 seeds each); one per subject chosen by the lead. No manual clean-up.
- **Licence:** CC0 1.0.

| File | Seed |
|---|---|
| `con_axe_woodcutter.png` | 2202 |
| `con_bow_willow.png` | 2202 |
| `con_burrling.png` | 2202 |
| `con_cradle_shrine.png` | 2202 |
| `con_dummy_training.png` | 2202 |
| `con_envkit_husk_house.png` | 2202 |
| `con_envkit_stone_wall.png` | 1101 |
| `con_godtree_horizon.png` | 2202 |
| `con_huskhaven_gate.png` | 2202 |
| `con_huskhaven_square.png` | 1101 |
| `con_lantern_husk.png` | 3303 |
| `con_rooted_innkeeper.png` | 2202 |
| `con_rootwarden.png` | 1101 |
| `con_shield_husk.png` | 2202 |
| `con_spear_meadow.png` | 2202 |
| `con_sword_training.png` | 2202 |
| `con_thornback.png` | 2202 |
| `con_underroot_cave.png` | 1101 |
| `con_windlow_meadow.png` | 2202 |
| `con_woken_starter.png` | 3303 |

## Lead's notes on v0

- **Good:** flat two-tone shading, coloured shadows, readable silhouettes and a consistent palette across objects, creatures, characters and landscapes.
- **Not selected, to redo:** `con_barkbound` (came out as a plain knight; needs visible bark and roots) and `con_dagger_thorn` (came out with a steel blade; the blade should be a wooden thorn).
- **Godtree scale:** better after the fragment was rewritten to put the tree beyond the mountains, but it still reads as a large oak. Key art needs a dedicated pass.
- **Style distance:** the character rendering sits close to a well-known hand-drawn film look. Version 1 of the fragments should push toward our own: bolder ink outlines and harder cel shadow edges.
