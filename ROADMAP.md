# Roadmap

Last made true: 2026-10-09 (session 1). If this file and reality disagree, fix this file first.

Order of work: melee physics, then multiplayer, then role-playing systems, then magic and advanced traversal. Later systems must never block earlier ones.

## Waiting on the owner

| # | Decision | Lead's recommendation |
|---|---|---|
| 1 | VRoid avatars cannot be CC0 (pixiv keeps rights in the base body and presets). How are avatars licensed or made? | Use VRoid for placeholder avatars in a separate folder under pixiv's terms, excluded from CC0; test a fully CC0 route (generated mesh, UniRig, VRM export) in milestone 3 and decide then |
| 2 | Stability AI Community License (Stable Audio Open, Stable Fast 3D): register with Stability AI and accept the US $1M revenue ceiling? | Accept for Stable Audio Open only: it is the sole sound-effect generator candidate. Skip Stable Fast 3D |
| 3 | Chatterbox voices carry a permanent inaudible watermark. Accept? | Decline; Kokoro covers our needs |
| 4 | Is `Corbino50211/BIMOS-Active-Ragdolls` your repository? It has no licence, so nobody may use it | If yours, add an MIT licence. If not, write active ragdolls in-house |
| 5 | Add the full CC0 1.0 legal text | Covered by the install authorisation in decision 0008; lead to add |

## Milestones

### 1. Foundations: in progress

- [x] Repository skeleton, README, AGENTS, CONTRIBUTING, this roadmap
- [x] Canon drafted and approved by the owner (decision 0008)
- [x] Automated checks with tests (21 tests pass locally on Windows PowerShell 5.1)
- [x] Pull request and issue templates
- [x] Licences verified from primary sources ([THIRD_PARTY_LICENCES.md](THIRD_PARTY_LICENCES.md)); approved generator list as data
- [x] Licence choice: MIT for code, CC0 1.0 for assets (owner, 2026-10-09)
- [x] First commit pushed to GitHub
- [ ] The continuous-integration workflow: confirm its first run on GitHub passes
- [ ] Toolchain: Git LFS not installed; Unity 6000.3 LTS not installed
- [ ] Open licence decisions above

### 2. VR sandbox: not started

Physics player rig (BIMOS), one sword, one training dummy, in `unity/`. First task: confirm BIMOS 1.0.0 works on Unity 6000.3. Blocked by toolchain (item 6).

### 3. Asset pipeline proof and style kit: not started

One character, weapon, enemy and environment kit generated locally end to end, with recipes and golden reference assets. Also settles: a clean TRELLIS.2 workflow, the environment toon shader, the model interchange format, rendered previews on pull requests, and the avatar route. Blocked by toolchain and canon approval.

Known gaps going in: no approved animation generator, no approved sound-effect generator, one unconditionally approved 3D generator (TripoSR).

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
