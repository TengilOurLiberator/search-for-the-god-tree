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

## Second batch (v1), 2026-10-10

Same generator, workflow and fragments; subjects in [../../jobs/concepts-v1.subjects.json](../../jobs/concepts-v1.subjects.json), prompts and seeds in [../../jobs/concepts-v1.json](../../jobs/concepts-v1.json), log in [../../jobs/concepts-v1.log.jsonl](../../jobs/concepts-v1.log.jsonl). 120 images generated (30 subjects, 4 seeds each); one per subject chosen.

| File | Seed |
|---|---|
| `con_barkbound.png` | 3303 |
| `con_burrling_alpha.png` | 3303 |
| `con_dagger_thorn.png` | 1101 |
| `con_flora_husk_fragment.png` | 2202 |
| `con_flora_mote_flower.png` | 1101 |
| `con_flora_wind_tree.png` | 2202 |
| `con_huskhaven_inn_interior.png` | 3303 |
| `con_mote_wisp.png` | 2202 |
| `con_prop_barrel.png` | 2202 |
| `con_prop_bench.png` | 2202 |
| `con_prop_cart.png` | 2202 |
| `con_prop_chest.png` | 2202 |
| `con_prop_crate.png` | 2202 |
| `con_prop_fence.png` | 2202 |
| `con_prop_market_stall.png` | 2202 |
| `con_prop_mote_vial.png` | 2202 |
| `con_prop_signpost.png` | 3303 |
| `con_prop_well.png` | 2202 |
| `con_root_crawler.png` | 3303 |
| `con_rooted_child.png` | 2202 |
| `con_rooted_elder.png` | 2202 |
| `con_rooted_merchant.png` | 2202 |
| `con_rooted_smith.png` | 2202 |
| `con_rootwarden_arena.png` | 2202 |
| `con_thornback_elder.png` | 2202 |
| `con_underroot_gate.png` | 2202 |
| `con_underroot_hall.png` | 3303 |
| `con_windlow_cradle_road.png` | 2202 |
| `con_woken_shieldbearer.png` | 2202 |
| `con_woken_weaver.png` | 2202 |

Notes: `con_dagger_thorn` now has the wooden thorn blade. `con_barkbound` is closer (rust, moss, roots at the feet) but is still mostly a knight; it needs a different approach. `con_mote_wisp` came out as a small dandelion-headed creature instead of a floating light: not what was asked, kept because it is charming and may suit the world better.

## Third batch (v2, weapons), 2026-10-10

Subjects in [../../jobs/concepts-v2-weapons.subjects.json](../../jobs/concepts-v2-weapons.subjects.json), prompts and seeds in [../../jobs/concepts-v2-weapons.json](../../jobs/concepts-v2-weapons.json). 64 images (16 weapons, 4 seeds each); one per weapon chosen.

| File | Seed |
|---|---|
| `con_wpn_buckler.png` | 1101 |
| `con_wpn_club_root.png` | 3303 |
| `con_wpn_greatsword.png` | 3303 |
| `con_wpn_halberd.png` | 4404 |
| `con_wpn_handaxe.png` | 1101 |
| `con_wpn_kite_shield.png` | 2202 |
| `con_wpn_mace.png` | 3303 |
| `con_wpn_pickaxe.png` | 1101 |
| `con_wpn_quarterstaff.png` | 3303 |
| `con_wpn_rapier.png` | 3303 |
| `con_wpn_scimitar.png` | 1101 |
| `con_wpn_shortsword.png` | 3303 |
| `con_wpn_staff_weaver.png` | 1101 |
| `con_wpn_torch.png` | 3303 |
| `con_wpn_wand_thorn.png` | 1101 |
| `con_wpn_warhammer.png` | 3303 |

Note: the mace came out as a blunt maul, not a flanged mace; kept as a maul.
