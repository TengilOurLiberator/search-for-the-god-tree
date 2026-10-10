# 0013: Sandbox first; prototypes ahead of the roadmap; quality bar for models

- **Date:** 2026-10-10
- **Decided by:** Owner, after the first headset sessions

**Decisions.**
1. **The sandbox comes first.** Its job is to let the owner judge how the game plays. Work on it takes priority over the milestone order in the original brief.
2. **Prototypes of later systems are wanted now, in the sandbox:** a bow that shoots, spells, more weapons, a character menu with equipment that changes stats, and a view of the player's character. These are try-it-out prototypes. They do not settle the final design of the role-playing or magic systems, and they live under `Scripts/Sandbox` and `Scripts/Combat` marked as such.
3. **Grabbing:** objects are picked up by pointing and squeezing grip; they fly to the hand and snap into a proper hold. "Having to pick it up the right way makes the items difficult to handle." Touch-grabs snap the same way.
4. **Hand tracking is wanted** (grab by closing the hand, no controller). Not started; movement without thumbsticks is the open design question.
5. **3D models: generate many, keep few.** The lead judges every model from a rendered preview before it enters the game, and discards freely. The aim is that the game "looks really cool"; a model that is merely within budget is not good enough.

**How play is evaluated.** The sandbox records every session (`Godtree.Diagnostics.SessionRecorder`): controller-versus-hand gaps, held-object motion, grabs, hits, shots and spells, plus a snapshot each second. The lead reads the numbers and frames; the owner says how it felt. Findings so far are in `docs/playtests.md`.

**Note on pillar 1** ("Your hands are the game"). Point-and-pull and snapped holds are aids for picking things up. They do not decide any combat outcome: once held, a weapon is a physical object and a hit is a real collision.
