# Roadmap

Last made true: 2026-10-09 (session 1). If this file and reality disagree, fix this file first.

Order of work: melee physics, then multiplayer, then role-playing systems, then magic and advanced traversal. Later systems must never block earlier ones.

## Waiting on the owner

| # | Item | Note |
|---|---|---|
| 1 | Register with Stability AI before any commercial release | Condition of the Stable Audio Open licence (decision 0009). Not urgent |

Nothing else is waiting.

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
- [x] 21 placeholder-grade models with recipes in `unity/Assets/Godtree/Art/` (6 weapons, 13 props, 2 environment pieces)
- [ ] **Golden-quality 3D.** TripoSR gives usable shapes for simple props and weapons but soft, washed-out colour, and fails on creatures, trees and anything thin. Next: trace TRELLIS.2's code for its non-commercial parts and prove a clean workflow (decision 0006), or sharpen textures by projecting the concept image onto the mesh
- [ ] Unity cannot import `.glb` yet: choose and verify a glTF importer (UniVRM's UniGLTF arrives with the avatar work; Unity's glTFast needs a licence read)
- [ ] Environment toon shader that matches MToon characters
- [ ] Avatar route (VRoid placeholders approved; CC0 route untested)
- [ ] Animation: no approved generator; CC0 libraries and physics only
- [ ] Audio: ACE-Step, Stable Audio Open and Kokoro approved but not installed
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
