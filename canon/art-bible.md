# Art bible

Status: Approved by the owner, 2026-10-09 (decisions/0008). Numbers are starting points and will be tuned in the VR sandbox and pipeline milestones.

The look is a **painterly anime action-RPG**: a stylised 3D world with the polish of a modern anime role-playing game. Chosen by the owner on 2026-10-10 ([decision 0014](../decisions/0014-art-direction-anime-rpg.md)), replacing the flatter storybook look of the first draft. If a choice is between readable and realistic, still choose readable.

## Rules of the look

1. **Silhouette first.** Every character, enemy and weapon must be recognisable as a flat black shape at ten metres. Enemy attacks must change the silhouette before they land.
2. **Painterly, not flat and not photographic.** Surfaces carry brushed, hand-painted texture and believable material: engraved metal, worn leather, woven cloth, wood grain. No photo textures, no grime for its own sake.
3. **Soft cel shading.** A clear light side and shadow side with a soft edge between them, warm bounce light in the shadows and a cool rim light on the edge away from the sun. Shadows are coloured, never grey or black.
4. **Realistic proportions, anime faces.** Bodies are built like real people, about seven to seven and a half heads tall; faces are expressive and clearly anime. Hands and weapons are true to scale.
5. **Detailed, ornate equipment.** Gear is designed, not generic: layered cloth and leather, metal trims, straps, small accessories, engraved or leaf-shaped details that tie it to the Godtree.
6. **Lush colour, luminous highlights.** Bright, saturated, natural colour. Light sources and magic glow.
7. **Colour carries meaning.** Danger is red-pink, healing is mint, motes and magic are gold, the Godtree is pale bark and soft green. Nothing else uses those colours at full strength.
8. **One strong light, with depth.** Outdoors the sun sets the mood and distance fades into haze; the Godtree's glow is the second light near roots and at night. Dungeons are lit by motes, lanterns and the player.

## What the 3D step can and cannot do (2026-10-10)

Concepts that feed image-to-3D are drawn as **solid shapes**: no glow, no glass, no thin loops or loose strands, no flames. Those are added in the engine as effects. A model that loses its shape or colour in conversion is discarded, however good its concept.
## Palette

The anchor colours are in [data/budgets.json](../data/budgets.json) under `palette`. Key art, the interface and the golden reference assets are built from them, and recipe prompts quote them.

## Rendering

- Unity Universal Render Pipeline.
- Models carry their painted look in their base-colour texture and are lit with soft cel shading. The shader that does this in the engine is not chosen yet; until it is, the sandbox uses Unity's standard lit shader, which is flatter and duller than the target.
- Avatars are planned as VRM 1.0 (only VRM 1.0 has a toon shader for this pipeline); whether its shader can deliver this look is untested.
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
