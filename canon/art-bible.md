# Art bible

Status: Approved by the owner, 2026-10-09 (decisions/0008). Numbers are starting points and will be tuned in the VR sandbox and pipeline milestones.

The look is a bright, cel-shaded anime. If a choice is between readable and realistic, choose readable.

## Rules of the look

1. **Silhouette first.** Every character, enemy and weapon must be recognisable as a flat black shape at ten metres. Enemy attacks must change the silhouette before they land.
2. **Flat colour, clean shapes.** Two or three tones per material: base, shadow, and sometimes a highlight. No photographic texture noise, no grime passes, no realistic skin pores.
3. **Coloured shadows.** Shadows lean blue-violet outdoors and warm violet indoors. Never grey or black.
4. **One strong light.** Outdoors the sun sets the mood; the Godtree's glow is the second light near roots and at night. Dungeons are lit by motes, lanterns and the player.
5. **Colour carries meaning.** Danger is red-pink, healing is mint, motes and magic are gold, the Godtree is pale bark and soft green. Nothing else uses those colours at full strength.
6. **Outlines on characters.** Characters, enemies and held weapons have an ink outline. Environments do not, so living things stand out.
7. **Proportions.** Anime proportions at roughly six and a half to seven heads tall. Hands and weapons slightly large, because players look at them all day.

## Palette

The anchor colours are in [data/budgets.json](../data/budgets.json) under `palette`. Key art, the interface and the golden reference assets are built from them, and recipe prompts quote them.

## Rendering

- Unity Universal Render Pipeline.
- Avatars are VRM 1.0 with the MToon shader (only VRM 1.0 has an MToon shader for this pipeline).
- The toon shader for environments, weapons and enemies is not chosen yet; it is settled in the pipeline milestone and must match MToon characters side by side.
- Realistic texture sets from libraries are raw material for stylising, never used as-is.

## Budgets

Target: 90 frames per second in VR on a mid-high gaming PC. The enforced numbers live in [data/budgets.json](../data/budgets.json); this table is a summary and the data file wins if they differ.

| Budget class | Triangles (max) | Texture side (max) |
|---|---|---|
| Player character | 40,000 | 2048 |
| Non-player character | 25,000 | 2048 |
| Weapon or shield | 5,000 | 1024 |
| Common enemy | 20,000 | 2048 |
| Boss | 60,000 | 2048 |
| Modular environment piece | 8,000 | 2048 |
| Small prop | 2,000 | 1024 |
| Hero prop | 10,000 | 2048 |

- Texture sides are powers of two (interface art and concept art are exempt).
- No file over 50 MB.

## Naming

`<category>_<name>[_<part>].<ext>`, lowercase letters, digits and underscores.

| Prefix | For | Example |
|---|---|---|
| `chr` | characters | `chr_rooted_innkeeper.vrm` |
| `wpn` | weapons and shields | `wpn_sword_training.glb` |
| `enm` | enemies and bosses | `enm_thornback.glb` |
| `env` | environment pieces | `env_husk_wall_a.glb` |
| `prp` | props | `prp_lantern_albedo.png` |
| `ui` | interface art | `ui_icon_heal.png` |
| `vfx` | effects | `vfx_mote_soft.png` |
| `anm` | animation clips | `anm_thornback_charge.anim` |
| `sfx`, `mus`, `vox` | audio | `sfx_sword_hit_wood_01.ogg` |
| `con` | concept art | `con_huskhaven_gate.png` |

## The style kit

The style kit ([stylekit/](../stylekit/)) holds the approved generators, shared prompt fragments and golden reference assets that make work from many hands look like one game. It does not exist yet; it is built in the pipeline milestone, and asset contributions open only after it does.
