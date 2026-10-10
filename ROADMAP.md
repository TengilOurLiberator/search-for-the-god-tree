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
- [ ] Still failing in 3D: the Rootwarden, glowing orbs, flames, the arch and the boulder
- [ ] Hand tracking (wanted; movement without thumbsticks is the open question)
- [ ] Equipped items are not shown on the character yet; the character is the rig's grey mannequin

## Milestones

### 1. Foundations: in progress

- [x] Repository skeleton, README, AGENTS, CONTRIBUTING, this roadmap
- [x] Canon drafted and approved by the owner (decision 0008)
- [x] Automated checks with tests (21 tests pass locally on Windows PowerShell 5.1)
- [x] Pull request and issue templates
- [x] Licences verified from primary sources ([THIRD_PARTY_LICENCES.md](THIRD_PARTY_LICENCES.md)); approved generator list as data
- [x] Licence choice: MIT for code, CC0 1.0 for assets (owner, 2026-10-09)
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
### 4. Open to outside agents: not started

Briefs published, trust ladder in effect. Requires something runnable (2) and a style kit (3).

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
