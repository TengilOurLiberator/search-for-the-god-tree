# 0020: As lifelike as VR can be, in the anime style; the lead directs agents

- **Date:** 2026-10-16
- **Decided by:** Owner

**Decisions.**
1. **The goal is VR as lifelike as possible while keeping the anime look.** Lifelike means how things behave and feel: weight, contact, hands that keep up, creatures that move like animals and find their way, sound placed in space, faces that move when people speak. The art direction of decision 0014 stands: lifelike behaviour, anime appearance.
2. **The lead works as a director.** It assigns tasks to agents suited to them, roughly: the largest model for design and hard problems (game-state recording, physics, locomotion), the middle one for well-specified building work (rigging scripts, integrations, research), the smallest for mechanical work (running suites, collecting pictures, summaries). The lead writes the briefs, reviews the results, integrates, and commits.
3. **The lead must be able to read the state of the game**, not only pictures: where items, projectiles and creatures are, how they move, and what each frame costs, in both robot runs and the owner's headset sessions.
4. **Sandbox first continues** (decision 0013): keep refining the sandbox area and its mechanics.

**Constraints the lead keeps.** Only one Unity process can hold the project, so at most one agent runs Unity at a time and agents work on separate files. Agents do not commit; the lead reviews and commits. The owner's usage limit is shared with their job (decision 0009): agents are used where they save real time, not by default.

**Affects.** How work is organised; `Scripts/Diagnostics/`; the creature pipeline; docs/lead-handoff.md.
