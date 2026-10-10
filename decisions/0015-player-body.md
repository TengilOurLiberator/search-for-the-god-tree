# 0015: How a generated character becomes the player's body

- **Date:** 2026-10-10
- **Decided by:** Lead. One point is the owner's to confirm (see "For the owner").

**Decision.** The player's body is a generated character bound to the player rig's own skeleton, by this route:

1. A concept drawn for the purpose: realistic proportions, arms held clear of the body, close-fitting clothes, nothing hanging from the arms (framing `character_tpose` in the prompt fragments).
2. TRELLIS.2 and the usual Blender finishing, as for any model.
3. `tools/pipeline/blender_rig_body.py`: straightens the model into a T-pose, fits its arm length to the skeleton, and binds it with Blender's automatic weights. Fingers are left out.
4. The published file is the mesh alone. Its binding is written into four extra texture-coordinate sets; it contains no skeleton.
5. In the game, `Scripts/Sandbox/PlayerBody.cs` rebuilds the skinned mesh against the rig's skeleton at start and swaps it onto the rig's renderer. The rig's animation is untouched. If anything fails to line up, the rig's mannequin stays.

**Why this route.** The research (`docs/research/hand-tracking-and-player-body.md`) recommended against upgrading the rig to its unreleased version, and found that a re-exported skeleton risks breaking the rig's hand and arm offsets. Keeping the rig's skeleton exactly as it is and changing only the mesh avoids both. The earlier generated characters cannot be used: they are stocky, stand with arms against the body and wear cloaks, and a VR body must have arms the length of the player's and free of the torso.

**The head.** The rig hides the head by shrinking the neck bone, so a first-person player never sees inside their own face. A mirror then shows a headless body. A second copy of the head is drawn on its own layer (29), which only the menu's mirror camera looks at.

**For the owner: a licence point.** Assets are CC0 and decision 0007 allows only CC0 material inside them. The rig's skeleton comes from BIMOS, which is MIT. It was used to measure the weights, as a tool, and no part of it is in the published file: the file has our mesh, our weights, and bone numbers that index a list of ordinary humanoid bone names. The lead's view is that this keeps the asset CC0. If the owner would rather be stricter, the alternative is to compute the weights inside the game from the rig's skeleton at start, at some cost in quality.

**Limits, stated plainly.** Fingers do not bend. Shoulders and hips deform roughly. Equipment chosen in the character menu is still not shown on the body. Nothing here has been seen in the headset; it was judged from rendered test poses and the menu's mirror picture.

**Affects.** `stylekit/prompt-fragments.json` (new framing), `tools/pipeline/blender_rig_body.py`, `recipes/chr_body_woken_starter/`, `Scripts/Sandbox/PlayerBody.cs`, `Editor/SandboxBuilder.cs`, the PlayMode tests.