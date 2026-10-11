# Lead hand-off: where the work stands

Updated 2026-10-11, in the small hours. Read this after AGENTS.md and ROADMAP.md. It is the working state that is not obvious from the files.

## The owner's standing direction (decision 0016)

Sandbox first. Tonight's list, in the owner's order: a way for the lead to playtest alone; arrows fixed; staffs and wands that cast, by element and tier; animated people and creatures; free AI conversation with every NPC; monsters dismembered when killed (stylised blood); then make the sandbox a small town (Huskhaven) with an outside area for monsters; then keep making weapons, equipment and spells. The owner's Claude plan is shared with their job: watch usage, push bulk work onto the PC, watch the graphics card.

## What exists now, and how each part was checked

Everything below was checked by the robot player and by looking at its pictures. **None of it has been seen or felt in the headset.**

| Part | Where | Checked by |
|---|---|---|
| Robot player: drives head, hands and buttons through the real input actions; walks with path-finding; saves pictures | `Scripts/Diagnostics/RobotPlayer.cs`, `Tests/PlayMode/RobotPlaytests.cs` (category Robot) | Its own routines |
| Arrows no longer stick in the bow | `SandboxAbilities.LooseArrow` | Robot puts 5 of 5 arrows in the dummy |
| Staffs and wands: Ember, Frost, Storm; tiers 1 to 3; hold TRIGGER longer for the bigger spells | `SpellFocus.cs`, `FocusCasting.cs`; spell table built in `SandboxBuilder.BuildSpellTable` | Robot casts 8 spells and checks each |
| The player's body | `PlayerBody.cs` (decision 0015) | Mirror picture |
| Animated townsfolk (8), on the CC0 Mesh2Motion human skeleton with 44 clips | `NpcActor.cs`; `recipes/anm_human_basic`; `SandboxBuilder.PlaceActor`, `BuildHumanAnimator` | Robot walks up; they turn and greet |
| Fighting monsters: Barkbound, Rootwarden | `EnemyActor.cs`; `SandboxBuilder.MakeEnemy` | Robot fights the Barkbound to the death |
| Dismemberment: models pre-cut into ten capped pieces; a joint is severed, nothing is cut at run time | `Combat/Dismemberable.cs`; `blender_rig_body.py --parts` | Robot's killing blow takes the head off |
| Talking NPCs by voice | `NpcMind.cs`, `VoiceInput.cs`; `tools/voice/` | Robot asks the innkeeper four things, one of them by generated speech |
| Huskhaven as a walled town; the Windlow meadow and the Rootwarden's arch beyond the gate | layout table in `SandboxBuilder.BuildGallery`; twelve building models | Pictures from above and from the square; the robot walks out through the gate to fight |
| 38 weapons and foci; three tiers of staff per element | `BuildGallery` (tables), `BuildSpellTable` | Robot pulls, holds and swings every one and writes `D:\godtree-work\robot\weapons_report.txt`; grip check picture |
| Equipment shown on the body: forearm guards, wrist rings, belt charms (head and chest pieces only if their models pass) | `PlayerEquipment.cs`; `SandboxBuilder.BuildPlayerEquipment` | Robot wears each piece and photographs it |

Twenty-two PlayMode tests (`-testCategory Robot` runs the ten robot routines alone; the whole suite takes about thirteen minutes). The talking test skips itself when the helper programs are not running. The robot's pictures are in `D:\godtree-work\robot\<routine>\`.

## How to run things

- **Open the sandbox for the owner:** `Unity.exe -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.OpenAndPlay`. Headset on and Link running first, or the VR session stays idle.
- **Talking NPCs need two helper programs:** `powershell -ExecutionPolicy Bypass -File tools/voice/Start-NpcServices.ps1` before Play (`-Stop` to end them). They take about 5 GB of graphics memory. Do not pipe that script's output into anything: the programs it starts keep the pipe open and the caller hangs.
- **Stop the helper programs and ComfyUI before the other one runs.** Generation takes up to 24 GB; the two together do not fit comfortably beside VR.
- **Robot playtests:** `Unity.exe -batchmode -projectPath unity -runTests -testPlatform PlayMode -testCategory Robot -testResults out.xml`. Set `RobotPlayer.DumpPlans = true` in a routine to get its route plans as text maps (`D:\godtree-work\robot\plan_NN.txt`); that is how the "gate too narrow" and "search too narrow" faults were found.
- **A new rigged person or man-shaped monster:** concept with the `character_tpose` framing, then `tools/pipeline/Invoke-RiggedCharacters.ps1` (image to 3D, finishing, rigging, publishing, recipe). Monsters (`enm_`) are cut into parts automatically. Look at the `_check_pose` and `_check_parts` pictures in the work folder before trusting a model.
- **A new player body:** decision 0015.

## Rules learned the hard way

- **Never run generation while the owner is in the headset.** Never run two queues into one output folder.
- **Unity must be closed** before any batch-mode build or test.
- **Run `tools/checks/Run-Checks.ps1 -All`, `tools/checks/tests/Run-Tests.ps1` and the PlayMode tests before every commit; do not push if any fails.**
- **Look before you trust.** Every fault found tonight was found by a picture or a robot run, not by reasoning: arrows in the bow, the mirror blocked by the dummy, the headless mirror, the gate too narrow to walk through, a creature striking through a wall.
- **Before using any new tool**, list what it downloads and read each licence (decision 0011). Record it in `THIRD_PARTY_LICENCES.md`.
- **Thin, loose shapes shatter in 3D generation** (coats, tunic hems, hat brims, thatch). Use the coarse workflow, or describe the thing as solid.
- **A script that edits a file by replacing text must keep the line ending.** Three times tonight a replacement joined two lines of Python; a syntax check (`python -c "import ast; ast.parse(...)"`) after every edit catches it.
- **Recordings** of the owner's sessions are in `D:\godtree-work\recordings\<date-time>\`.
- **Git** needs `D:\godtree-tools\git-lfs` on PATH and, for pushes, `GCM_INTERACTIVE=always` and `GIT_TERMINAL_PROMPT=1`.

## Things the owner should know or decide

- **Nothing since session 4 has been in the headset.** First things to try: talk to Tobin the innkeeper; cast with each staff; fight the Barkbound beyond the gate; look at your own body.
- **Talking NPCs are a prototype that cannot ship as built.** They need helper programs beside the game, a strong PC, and one speech component is GPL (fine as a local tool, not inside a released game). See `THIRD_PARTY_LICENCES.md` section 8.
- **Sandbox inventions that are not canon** until the owner says so: the eight people's names and natures (Tobin, Mira, Hale, Senna, Nilo, Aren, Lysa, Doran; `SandboxBuilder.GiveMind`); elemental staffs (the world bible's magic is weaving motes into knots, with no elements); stylised blood (the art bible is bright and gentle).
- **Licence point on the player's body** (decision 0015).
- **Multiplayer:** the survey found PurrNet (MIT); the networking library in decision 0005 is not open source. Revisit at that milestone.
- Still open from before: listen to the audio; rename "the Underroot"; history purge; graphics card power cap.

## Known weak spots

- **The physics hand trails the controller by about 34 cm in a 5.5 m/s swing**, empty or holding anything (robot weapons report). That is the rig's own lag and is probably what "weapons do not follow my hand" felt like in session 1. Not yet tuned: it needs the owner's feel.
- The weapons routine sometimes has one or two odd readings (a pull that brings nothing, a hand left behind). They differ from run to run; the routine allows two.
- Small generated things at the 2,000 triangle budget are rough (the bracer, the charms). Hollow things (helms, hoods, breast pieces) came out with black patches at the coarse setting and were thrown away.
- The Barkbound is hunched and small in play; its model reads more as armour than as bark.
- The old husk house is badly shattered up close; town buildings are being regenerated (`stylekit/jobs/concepts-f1-town.subjects.json`).
- Rigged people have ragged hems (elder, weaver) and fingers that do not bend. Mouths do not move when they speak.
- The staff is held near its head, not mid-shaft.
- Creatures walk straight at the player with only a sidestep for obstacles; they have no real path-finding.
- A model this small can be talked out of character. Whisper mishears place names ("Huskhaven").
- Burrlings, Thornbacks and the root crawler are still statues.

## Next, in the owner's order

1. More weapons, equipment and spells, generated in batches on the PC and judged from pictures (`Invoke-ModelRun.ps1` does image to finished model unattended).
2. Paving for the town square; lit lanterns; doors that open.
3. Four-legged creatures on the CC0 fox and spider rigs already fetched (`D:\godtree-tools\downloads\mesh2motion`).
