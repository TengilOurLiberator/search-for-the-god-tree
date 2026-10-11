# Roadmap

Last made true: 2026-10-10 (session 1). If this file and reality disagree, fix this file first.

Order of work: melee physics, then multiplayer, then role-playing systems, then magic and advanced traversal. Later systems must never block earlier ones.

## Waiting on the owner

| # | Item | Lead's recommendation |
|---|---|---|
| 1 | **Test the sandbox in your headset** (steps under milestone 2) | Everything about feel is unverified until you do |
| 2 | Done: DINOv3 licence accepted, TRELLIS.2 approved ([decision 0012](decisions/0012-trellis2-approved.md)) | Zone 1 kit is being regenerated |
| 3 | **A non-commercial background remover was used by mistake** and fixed ([decision 0011](decisions/0011-background-removal-incident.md)). The superseded files remain in git history. Purge history? | Leave it; they were placeholders, public for a few hours |
| 4 | **Listen to the audio** in `unity/Assets/Godtree/Audio/` (10 music sketches, 78 sound effects). Nobody has heard any of it | Mark keepers; the rest get regenerated |
| 5 | **Style distance.** Look at `docs/review/session-1/concepts-*.jpg`. The characters sit close to a famous hand-drawn film look. How far should version 1 of the style move away? | Move: bolder ink lines, harder shadow edges, our own face shapes |
| 6 | **Later zones.** `docs/review/session-1/concepts-zones-*.jpg` are mood sketches for zones 2 to 5, to react to. Nothing in them is canon | Say which directions you like |
| 7 | **Rename "the Underroot"?** The name-clash check ([docs/name-check.md](docs/name-check.md)) found it in two major games and as an upcoming Steam title. All other names are clear or generic | Rename; pick from the candidates in that file or give your own |
| 9 | Register with Stability AI before any commercial release | Condition of the Stable Audio Open licence. Not urgent |
| 10 | **Player body and licences** ([decision 0015](decisions/0015-player-body.md)): the rig's MIT skeleton was used to measure the body's weights; none of it is in the CC0 file | Accept as CC0; say if you want the stricter route |
| 11 | **Three sandbox choices that touch canon** ([decision 0016](decisions/0016-owner-direction-session-4.md)): elemental staffs, stylised blood, and eight named townsfolk with natures the lead invented | Try them first; then say which become canon |
| 12 | **Talking NPCs cannot ship as built**: they need helper programs, a strong PC, and one speech part is GPL | Decide after trying it whether it is worth designing the game around |
| 13 | Done: opened for assets, documentation and proposals ([decision 0018](decisions/0018-open-for-art-and-hand-made-work.md)). **Check your large-file allowance** at GitHub's billing page: the repository holds about 0.85 GB of large files and the free allowance is either 1 GB or 10 GB | If it is 1 GB, decide on paid storage or an overflow before artists send much |
| 8 | GPU power limit: image batches draw the full 600 W. Lowering it needs administrator rights | Set a limit of about 450 W in your GPU tool for unattended runs |

## Current priority (decision 0013, 2026-10-10)

The sandbox comes first: make it good enough for the owner to judge how the game plays, and make it look good. Prototypes of later systems (bow, spells, character menu, equipment and stats) are being built in the sandbox ahead of the milestone order below. Findings from recorded play sessions are in [docs/playtests.md](docs/playtests.md).

- [x] Point-and-pull grabbing with snapped holds; held weapons no longer fight the body or a second hand
- [x] Bow that draws and shoots; four spell prototypes (mote bolt, push, lift, ward)
- [x] Character menu (B button): mirror view, stats, four equipment slots that change stats; damage numbers and dummy health
- [ ] Owner to try all of the above in the headset; none of it has been felt yet
- [x] Art direction changed to the painterly anime action-RPG look (decision 0014); sixty models regenerated, forty kept: 19 weapons, 6 characters, the Barkbound, scenery and props
- [x] Spiky, furry and thatched shapes solved with a coarser 3D setting; twelve more models in the new look (Burrling, both Thornbacks, root crawler, husk house, well, cart and others)
- [x] The sandbox looks like the game: stylised shader, sun, sky, haze, colour grading, painted meadow ground, music, and the Godtree on the horizon
- [x] Every weapon's hold checked by rendering it (`docs/images/grip_check.png`); three that would have been held by the blade were fixed
- [x] The sandbox is laid out as a place: a road through a stone arch toward the Godtree, a camp with a lit fire, a corner of town, creatures beyond the wall, hills all round; the training dummy uses its generated model (2026-10-10, evening; not yet seen in the headset)
- [ ] Flames and glow are made in Unity (particles), not in 3D generation, which cannot do them
- [ ] Hand tracking (wanted; researched in docs/research/hand-tracking-and-player-body.md: over Link it is a developer-only feature, so the first step is the owner switching it on in the Link app, then an editor prototype)
- [x] The player has a generated character's body in place of the rig's grey mannequin, and the menu's mirror shows it with its head ([decision 0015](decisions/0015-player-body.md)); fingers do not bend; not yet seen in the headset
- [x] The lead can playtest alone: a robot player drives the rig without a headset, finds its way round obstacles and saves pictures (decision 0016)
- [x] Arrows no longer stick in the bow (seen by the owner in session 4, reproduced and fixed by the robot)
- [x] Staffs and wands cast: three elements, three tiers, nine spells
- [x] Eight townsfolk and adventurers are animated on a CC0 skeleton with 44 CC0 clips; they turn to the player and greet
- [x] The Barkbound and the Rootwarden walk up and fight; the player has health
- [x] Dismemberment with stylised blood: a hard blade blow on a weakened creature takes a limb; the killing blow always does
- [x] Free conversation with every townsperson by voice, through a language model and speech running on the PC (prototype; cannot ship as built, see THIRD_PARTY_LICENCES.md section 8)
- [ ] Owner to try all of the above in the headset
- [x] Huskhaven is a small walled town; the Windlow meadow, its creatures and the Rootwarden's arch lie beyond the gate
- [x] Seven more arms after that (50 weapons and foci in all, with a second bow)
- [x] Sixteen more arms (38 in all), with a wand and a great staff for each element; the robot pulls, holds and swings every one
- [x] Equipment is shown on the body: ten of the twelve menu items (forearms, wrists, belt, a padded jack, three for the head)
- [x] A fourth element, root (thorn dart, snare, mend); frost slows creatures and roots hold them
- [x] Five kinds of man-shaped creature in the meadow, each with its own size, toughness and weapon
- [x] Night report for the owner: [docs/reports/2026-10-11-night.md](docs/reports/2026-10-11-night.md)
- [ ] Burrlings, Thornbacks and the root crawler are still statues
- [ ] Chain Shirt and Mote-silk Robe show nothing on the body: their models came out badly twice

## Milestones

### 1. Foundations: in progress

- [x] Repository skeleton, README, AGENTS, CONTRIBUTING, this roadmap
- [x] Canon drafted and approved by the owner (decision 0008)
- [x] Automated checks with tests (21 tests pass locally on Windows PowerShell 5.1)
- [x] Pull request and issue templates
- [x] Licences verified from primary sources ([THIRD_PARTY_LICENCES.md](THIRD_PARTY_LICENCES.md)); approved generator list as data
- [x] Licence choice: public domain throughout. Code under the Unlicense (owner, 2026-10-11, [decision 0019](decisions/0019-code-to-public-domain.md); it was MIT before), assets under CC0 1.0 (owner, 2026-10-09)
- [x] First commit pushed to GitHub
- [x] Continuous integration: check-script tests pass on GitHub (Linux, PowerShell 7). The pull-request job has not run yet (no pull request so far)
- [x] Toolchain: Git LFS 3.8.0, Unity 6000.3.26f1, ComfyUI 0.39.0 with Z-Image Turbo (in D:\godtree-tools, outside the repository)

### 2. VR sandbox: built, awaiting the owner's headset test

- [x] Unity 6000.3.26f1 project in `unity/`, URP, OpenXR loader with eight controller profiles, 144 Hz physics
- [x] BIMOS 1.0.0 referenced by commit; it compiles on 6000.3
- [x] Scene `Assets/Godtree/Scenes/Sandbox.unity`: BIMOS rig, a placeholder sword on a plinth, a spring-jointed training dummy that flashes when struck. Rebuilt any time by `Godtree > Build Sandbox`
- [x] Four headless PlayMode tests pass (scene loads, rig spawns, sword rests, dummy rocks when hit and recovers)
- [ ] **Owner: test in the headset.** Open `unity/` with Unity 6000.3.26f1, open the Sandbox scene, start your headset's OpenXR runtime, press Play. Report: does the rig track, can you pick up the sword by the grip, is the hand orientation on the grip right, does the dummy react believably
- [ ] Tune from that report (grab alignment and joint springs are untested guesses)
- [ ] Replace placeholders with generated models

Nothing here has been seen in a headset. Passing tests prove the physics behaves, not that it feels right.
### 3. Asset pipeline proof and style kit: in progress

- [x] Image pipeline proven: Z-Image Turbo in ComfyUI, driven unattended by `tools/pipeline/Invoke-GenerationQueue.ps1` (about 3 s per image, 23 GB video memory)
- [x] Style kit v0: prompt fragments, one proven workflow, 50 golden concept images covering Zone 1 (town, wilderness, dungeon, enemies, characters, props, weapons)
- [x] 3D pipeline proven end to end: concept image, TripoSR, `tools/pipeline/blender_finish_model.py` (decimate, unwrap, bake, scale), `.glb`, recipe, budget check. Driven by `tools/pipeline/Invoke-ModelQueue.ps1` and `Publish-Models.ps1`
- [x] 23 models with recipes in `unity/Assets/Godtree/Art/`. **20 are TRELLIS.2 quality**: sword, axe, spear, bow, dagger, shield, training dummy, bench, chest, crate, fence, signpost, barrel, cart, market stall, well, husk house, stone wall, and two enemies (Thornback, Rootwarden boss). 3 are still TripoSR placeholders: lantern, cradle shrine, mote vial
- [ ] TRELLIS.2 still fails on: glowing or glass objects (lantern, mote vial), the cradle shrine, the Burrling (spikes collapse to a blob), the husk fragment and the wind tree. See `docs/review/session-1/models-trellis2-zone1-second-pass.jpg`. These need different concept images or a hand-made approach
- [x] Standard TRELLIS.2 workflow settled: remesh at 384, QEM reduction straight to the triangle budget inside ComfyUI, then Blender for scale and export
- [x] Unity imports `.glb` through glTFast 6.20.0 (Apache-2.0)
- [ ] Environment toon shader that matches MToon characters
- [ ] Avatar route (VRoid placeholders approved; CC0 route untested)
- [ ] Animation: no approved generator; CC0 libraries and physics only
- [x] Audio pipeline proven: ACE-Step 1.5 (music) and Stable Audio Open 1.0 (sound effects) in ComfyUI, finished by `tools/pipeline/finish_audio.py`. 10 music sketches and 78 sound effects (26 sounds, 3 variations each) with recipes in `unity/Assets/Godtree/Audio/`
- [ ] **Nobody has listened to the audio.** It was checked by measurement only. Owner or a contributor to listen and mark keepers; Kokoro (voice) not installed yet
- [ ] Rendered previews posted automatically on pull requests
- [ ] Style distance: character concepts sit close to a well-known hand-drawn film look; version 1 of the fragments should push toward our own. Owner to look at `stylekit/golden/concepts/` and say how far
### 4. Open to outside agents: open for art, documentation and proposals

Briefs published, trust ladder in effect. Requires something runnable (2) and a style kit (3).

- [x] Scheduled review built (2026-10-11, [decision 0017](decisions/0017-scheduled-review-and-voted-ideas.md)): pull requests every 15 minutes and voted ideas once a day, through `tools/review/safe.mjs`. Its 58 offline tests pass and it reads this repository; it has not yet reviewed a real pull request or idea, because there are none
- [x] Ideas board: Discussions switched on, category Ideas ([docs/voting.md](docs/voting.md))
- [x] Opened by the owner for assets, documentation and proposals (2026-10-11, [decision 0018](decisions/0018-open-for-art-and-hand-made-work.md)). Code stays at tier 1
- [x] A route for artists and animators working by hand: [docs/artists.md](docs/artists.md), an offer form, [CREDITS.md](CREDITS.md). No outside contribution has arrived yet, so none of it has been used
- [ ] First task briefs published as issues, starting with the "wanted most" list in docs/artists.md
- [ ] The owner's first merge of an outside contribution

### 5. Multiplayer slice: not started

2 to 4 players in one room. First task: confirm FishySteamworks (last released 2024) works with current FishNet; find another Steam transport if not.

### 6. Zone 1: not started

Town, wilderness, dungeon, boss, with stats, loot, inventory and saving. Inventory is in-house.

### 7. Magic and traversal: not started

### 8. Hub town, parties, matchmaking, then cloud saves: not started

### 9. Further zones and a public playtest: not started

A Steam release costs a one-time US $100 and is the owner's decision.

## Risks being watched

- BIMOS is a single-author project with one release; there is no fallback rig now that UltimateXR is ruled out.
- Unity Behavior is in maintenance mode.
- Large files: Git LFS hosting quotas will bite once assets arrive. Raise overflow storage with the owner before milestone 3 ends.
